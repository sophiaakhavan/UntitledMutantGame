using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class VisionSensor : MonoBehaviour
{
    [SerializeField] LayerMask detectionMask = ~0;

    EnemyAI enemyAI;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    void Update()
    {
        // Check all candidates
        for(int index = 0; index < DetectableTargetManager.Instance.AllTargets.Count; ++index)
        {
            var candidateTarget = DetectableTargetManager.Instance.AllTargets[index];

            // Skip if candidate is ourselves
            if(candidateTarget.gameObject == gameObject)
                continue;

            var enemyToTarget = candidateTarget.transform.position - enemyAI.EyeLocation;

            // If out of range, target is out of sight
            if (enemyToTarget.sqrMagnitude > (enemyAI.VisionConeRange * enemyAI.VisionConeRange))
                continue;

            enemyToTarget.Normalize();

            // If not within vision cone, target is out of sight
            if(Vector3.Dot(enemyToTarget, enemyAI.EyeDirection) < enemyAI.CosVisionConeAngle)
                continue;

            // Only see a non-player detectable target if it's moving
            if(!candidateTarget.gameObject.CompareTag("Player") && !candidateTarget.IsInMotion)
            {
                continue;
            }

            // Candidate is in range, within vision cone -- Raycast
            RaycastHit hitResult;
            if(Physics.Raycast(enemyAI.EyeLocation, enemyToTarget, out hitResult, 
                enemyAI.VisionConeRange, detectionMask, QueryTriggerInteraction.Collide))
            {
                if(hitResult.collider.GetComponentInParent<DetectableTarget>() == candidateTarget)
                    enemyAI.ReportCanSee(candidateTarget);
            }
        }
    }
}
