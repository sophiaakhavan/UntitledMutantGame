using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class TrackedTarget
{
    public DetectableTarget Detectable;
    public Vector3 RawPosition;

    public float LastSensedTime = -1f;
    public float Detection; // 0     = not aware (will be culled); 
                            // 0-1   = rough idea (no set location); 
                            // 1-2   = likely target (location)
                            // 2     = fully detected
    public bool IsPlayer = false;

    public bool UpdateDetection(DetectableTarget target, Vector3 position, float detection, float minDetection)
    {
        var oldDetection = Detection;

        if (target != null)
            Detectable = target;
        RawPosition = position;
        LastSensedTime = Time.time;
        Detection = Mathf.Clamp(Mathf.Max(Detection, minDetection) + detection, 0f, 2f);

        // Detect major threshold change
        if (oldDetection < 2f && Detection >= 2f)
            return true;
        if (oldDetection < 1f && Detection >= 1f)
            return true;
        if (oldDetection <= 0f && Detection >= 0f)
            return true;

        return false;
    }

    public bool DecayDetection(float decayTime, float amount)
    {
        // detected too recently - no change
        if ((Time.time - LastSensedTime) < decayTime)
            return false;

        var oldDetection = Detection;

        Detection -= amount;

        if (oldDetection >= 2f && Detection < 2f)
            return true;
        if (oldDetection >= 1f && Detection < 1f)
            return true;
        return Detection <= 0f;
    }
}

[RequireComponent(typeof(EnemyAI))]
public class DetectionLevelSystem : MonoBehaviour
{
    [SerializeField] AnimationCurve visionSensitivity;
    [SerializeField] float visionMinimumDetection = 1f;
    [SerializeField] float visionDetectionBuildRate = 10f;

    [SerializeField] float hearingMinimumDetection = 0f;
    [SerializeField] float hearingDetectionBuildRate = 5f;

    [SerializeField] float proximityMinimumDetection = 0f;
    [SerializeField] float proximityDetectionBuildRate = 1f;

    // How rapidly the AI forgets about a target
    [SerializeField] float detectionDecayDelay = 0.1f;
    [SerializeField] float detectionDecayRate = 0.1f;

    Dictionary<GameObject, TrackedTarget> targets = new Dictionary<GameObject, TrackedTarget>();
    EnemyAI enemyAI;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    void Update()
    {
        List<GameObject> toCleanup = new List<GameObject>();
        foreach (var targetGO in targets.Keys)
        {
            if (targets[targetGO].DecayDetection(detectionDecayDelay, detectionDecayRate * Time.deltaTime))
            {
                if (targets[targetGO].Detection <= 0f)
                {
                    enemyAI.OnFullyLost();
                    toCleanup.Add(targetGO);
                }
                else
                {
                    if (targets[targetGO].Detection >= 1f)
                        enemyAI.OnLostFullDetect(targetGO);
                    else
                        enemyAI.OnLostSuspicion();
                }
            }
        }

        // cleanup targets that are no longer detected
        foreach (var target in toCleanup)
            targets.Remove(target);
    }

    void UpdateDetectionLevel(GameObject targetGO, DetectableTarget target, Vector3 position, float detection, float minDetection)
    {
        // not in targets
        if (!targets.ContainsKey(targetGO))
        {
            targets[targetGO] = new TrackedTarget();
            targets[targetGO].IsPlayer = targetGO.CompareTag("Player");
        }

        // update target detection
        if (targets[targetGO].UpdateDetection(target, position, detection, minDetection))
        {
            if (targets[targetGO].Detection >= 2f)
            {
                if (targets[targetGO].IsPlayer)
                    enemyAI.OnFullyDetected(targetGO);
                else
                    enemyAI.OnDetected(targetGO);
            }
            else if (targets[targetGO].Detection >= 1f)
                enemyAI.OnDetected(targetGO);
            else if (targets[targetGO].Detection >= 0f)
                enemyAI.OnSuspicious();
        }
    }

    /// <summary>
    /// Notify the detection system of seen target and initate update to target's detection level.
    /// </summary>
    /// <param name="seen"></param>
    public void ReportCanSee(DetectableTarget seen)
    {
        // determine where the target is in the field of view
        var vectorToTarget = (seen.transform.position - enemyAI.EyeLocation).normalized;
        var dotProduct = Vector3.Dot(vectorToTarget, enemyAI.EyeDirection);

        // determine the detection contribution
        var detection = visionSensitivity.Evaluate(dotProduct) * visionDetectionBuildRate * Time.deltaTime;

        UpdateDetectionLevel(seen.gameObject, seen, seen.transform.position, detection, visionMinimumDetection);
    }

    /// <summary>
    /// Notify the detection system of heard sound and initiate update to target's detection level.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="location"></param>
    /// <param name="category"></param>
    /// <param name="intensity"></param>
    public void ReportCanHear(GameObject source, Vector3 location, EHeardSoundCategory category, float intensity)
    {
        var detection = intensity * hearingDetectionBuildRate * Time.deltaTime;

        UpdateDetectionLevel(source, null, location, detection, hearingMinimumDetection);
    }

    /// <summary>
    /// Notify the detection system of target in close proximity and initiate update to target's detection level.
    /// </summary>
    /// <param name="target"></param>
    public void ReportInProximity(DetectableTarget target)
    {
        var detection = proximityDetectionBuildRate * Time.deltaTime;

        UpdateDetectionLevel(target.gameObject, target, target.transform.position, detection, proximityMinimumDetection);
    }
}