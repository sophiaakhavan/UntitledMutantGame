using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public enum EnemyState
{
    Idle, // Roam
    Confused, // Paused in place, contemplating (no target)
    Suspicious, // Haven't found a target, looking around
    Detected, // Face the target
    Chase // Chase target (only if target is player)
}

[RequireComponent(typeof(DetectionLevelSystem))]
/// <summary>
/// Handles all enemy movement and attack behavior
/// </summary>
public abstract class EnemyAI : MonoBehaviour
{
    public Vector3 EyeLocation => eyePoint.position;
    public Vector3 EyeDirection => eyePoint.forward;
    public float VisionConeAngle => visionConeAngle;
    public float VisionConeRange => visionConeRange;
    public Color VisionConeColor => visionConeColor;
    public float HearingRange => hearingRange;
    public Color HearingRangeColor => hearingRangeColor;
    public float CosVisionConeAngle { get; private set; } = 0f;

    protected Transform player;
    protected bool hasWeapon = false;

    [SerializeField] float roamSpeed = 4f;
    [SerializeField] float chaseSpeed = 8f;
    [SerializeField] EnemyState currentState = EnemyState.Idle;
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] Transform eyePoint; // Where enemy's eyes are for player detection calculation
    [SerializeField] TextMeshProUGUI feedbackDisplay;
    [SerializeField] float visionConeAngle = 60f;
    [SerializeField] float visionConeRange = 14f;
    [SerializeField] Color visionConeColor = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] float hearingRange = 20f;
    [SerializeField] Color hearingRangeColor = new Color(1f, 1f, 0f, 0.25f);

    [Header("Weapon Settings")]
    [SerializeField] GameObject targetWeaponObject;
    [SerializeField] Transform enemyGrabPoint;


    Weapon targetWeapon;
    NavMeshAgent agent;
    int currentPatrolIndex = 0;
    Vector3 playerLastSeenPos;

    GameObject currTarget; // Detectable Target
    DetectionLevelSystem Detection;

    bool isGrabbingWeapon = false;
    bool isPlayerSpotted = false; // True only if currently chasing or had been chasing before currently detected

    void Awake()
    {
        CosVisionConeAngle = Mathf.Cos(VisionConeAngle * Mathf.Deg2Rad);
        Detection = GetComponent<DetectionLevelSystem>();
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetWeapon = targetWeaponObject.GetComponent<Weapon>();
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Update()
    {
        switch(currentState)
        {
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Detected:
                HandleDetected();
                break;
            case EnemyState.Suspicious:
                HandleSuspicious();
                break;
            case EnemyState.Confused:
                HandleConfused();
                break;
            case EnemyState.Idle:
                Roam();
                break;
        }
    }

    public void ReportCanSee(DetectableTarget seen)
    {
        Detection.ReportCanSee(seen);
    }

    public void ReportCanHear(GameObject source, Vector3 location, EHeardSoundCategory category, float intensity)
    {
        Detection.ReportCanHear(source, location, category, intensity);
    }

    public void OnFullyDetected(GameObject target)
    {
        if (isGrabbingWeapon)
            return;
        isPlayerSpotted = true;
        currTarget = target;
        currentState = EnemyState.Chase;
    }

    public void OnDetected(GameObject target)
    {
        if (isGrabbingWeapon)
            return;
        currTarget = target;
        currentState = EnemyState.Detected;
    }

    public void OnSuspicious()
    {
        if (isGrabbingWeapon)
            return;
        isPlayerSpotted = false;
        currTarget = null;
        currentState = EnemyState.Suspicious;
    }

    public void OnLostFullDetect(GameObject target)
    {
        if (isGrabbingWeapon)
            return;
        currTarget = target;
        currentState = EnemyState.Detected;
    }

    public void OnLostSuspicion()
    {
        if (isGrabbingWeapon)
            return;
        currTarget = null;
        currentState = EnemyState.Confused;
        
    }

    public void OnFullyLost()
    {
        if (isGrabbingWeapon)
            return;
        currTarget = null;
        currentState = EnemyState.Idle;
    }

    public bool IsDetectingPlayer()
    {
        return currTarget != null && currTarget.CompareTag("Player");
    }

    /// <summary>
    /// If detected target is player, chase player
    /// </summary>
    protected virtual void HandleChase()
    {
        feedbackDisplay.text = "Chasing";

        playerLastSeenPos = player.position;

        if (!hasWeapon)
        {
            if (!isGrabbingWeapon)
            {
                isGrabbingWeapon = true;
            }
            GrabWeapon();
        }
        else // Weapon is wielded, try to catch player
        {
            ChasePlayer();
        }
    }

    protected virtual void HandleDetected()
    {
        feedbackDisplay.text = "Detected";

        if (currTarget.CompareTag("Player") && isPlayerSpotted)
        {
            // Had been chasing beforehand, so move to where player last was seen
            MoveTowards(playerLastSeenPos, roamSpeed);
        }
        else // Non-player detectable target or hadn't been chasing player beforehand
        {
            MoveTowards(currTarget.transform.position, roamSpeed, currTarget.GetComponentInChildren<DetectableTarget>().Radius);
        }
    }

    protected virtual void HandleSuspicious()
    {
        feedbackDisplay.text = "Suspicious";
        agent.isStopped = true;
        agent.ResetPath();
    }

    protected virtual void HandleConfused()
    {
        feedbackDisplay.text = "Confused";
        agent.isStopped = true;
        agent.ResetPath();
    }

    /// <summary>
    /// Once the enemy has grabbed a weapon, this function handles behavior for chasing the player,
    /// using the weapon when the player is within range, or stepping backwards if the player is too close.
    /// Also ensures that the enemy and its equipped weapon are properly facing the player.
    /// </summary>
    protected virtual void ChasePlayer()
    {
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        if(targetWeapon == null)
        {
            Debug.LogError("Target weapon not specified!");
            return;
        }

        // Rotate enemy about the y axis to ensure that it is facing the player
        FaceTarget(player.position);

        // Point the weapon at the player
        Vector3 weaponToPlayer = (player.position - targetWeapon.transform.position).normalized;
        enemyGrabPoint.forward = weaponToPlayer;

        float distance = Vector3.Distance(transform.position, player.position);
        int rangeVal = targetWeapon.DistanceInRange(distance);

        switch(rangeVal)
        {
            case 0: // Player within range, stop in track and use weapon
                agent.isStopped = true;
                agent.ResetPath();
                targetWeapon.Use();
                break;
            case 1: // Player too far, move towards player
                MoveTowards(player.position, chaseSpeed, currTarget.GetComponentInChildren<DetectableTarget>().Radius);
                break;
            case -1: // Player too close, Step backward from player
                MoveAwayFrom(player.position, chaseSpeed);
                break;
        }
    }

    /// <summary>
    /// Patrol around specified patrol points. 
    /// </summary>
    protected virtual void Roam()
    {
        feedbackDisplay.text = "Idle";

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("No patrol points assigned!");
            return;
        }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector3 targetPosition = targetPoint.position;
        MoveTowards(targetPosition, roamSpeed);

        // Only care about the xz distance between enemy and patrol point
        Vector3 horizontalEnemyPosition = transform.position;
        horizontalEnemyPosition.y = 0f;
        targetPosition.y = 0f;

        // Check if the enemy has reached the current patrol point
        if (Vector3.Distance(horizontalEnemyPosition, targetPosition) < 1f)
        {
            // Move to the next patrol point in the array
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    /// <summary>
    /// If close enough to weapon, equips weapon. Otherwise, moves enemy towards weapon.
    /// </summary>
    protected virtual void GrabWeapon()
    {
        if (Vector3.Distance(transform.position, targetWeapon.transform.position) < 5f)
        {
            if (targetWeapon != null)
            {
                hasWeapon = true;
                isGrabbingWeapon = false;
                targetWeapon.Equip(enemyGrabPoint);
                // Once weapon is equipped, head to where player was last seen
                MoveTowards(playerLastSeenPos, chaseSpeed);
            }
        }
        else
        {
            MoveTowards(targetWeapon.transform.position, chaseSpeed);
        }
    }

    /// <summary>
    /// Moves enemy agent towards a target at a specified speed.
    /// Make sure to specify a radius for detectable targets (which is specified in the DetectableTarget Radius field)
    /// </summary>
    /// <param name="target"></param>
    /// <param name="speed"></param>
    /// <param name="targetRadius"></param>
    protected virtual void MoveTowards(Vector3 target, float speed, float targetRadius = 0.0f)
    {
        // For detectable targets, rather than moving toward the center of the target, stop at its radius
        Vector3 directionToTarget = (target - agent.transform.position).normalized;
        Vector3 stopPoint = target - (directionToTarget * targetRadius);

        agent.speed = speed;
        if (targetRadius > 0.0f)
        {
            agent.destination = stopPoint;
        }
        else
        {
            agent.destination = target;
        }
    }

    /// <summary>
    /// TODO: make this work properly
    /// </summary>
    /// <param name="target"></param>
    /// <param name="speed"></param>
    protected virtual void MoveAwayFrom(Vector3 target, float speed)
    {
        Vector3 directionAwayFromTarget = (transform.position - target).normalized;

        float distance = Vector3.Distance(transform.position, target);
        Vector3 newTargetPosition = transform.position + directionAwayFromTarget * distance * 2f;

        agent.destination = newTargetPosition;

        FaceTarget(target);
    }

    private void FaceTarget(Vector3 target)
    {
        // Turn to face the target
        Vector3 enemyToTarget = (target - transform.position).normalized;
        transform.forward = new Vector3(enemyToTarget.x, transform.forward.y, enemyToTarget.z);
    }
}

