using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Confused,
    Chase
}

/// <summary>
/// Handles all enemy movement and attack behavior
/// </summary>
public abstract class EnemyAI : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private float roamSpeed = 4f;
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform eyePoint; // Where enemy's eyes are for player detection calculation

    [Header("Weapon Settings")]
    [SerializeField] private GameObject targetWeaponObject;
    [SerializeField] private Transform enemyGrabPoint;

    protected Transform player;
    protected bool hasWeapon = false;

    private Weapon targetWeapon;
    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private bool isHandlingConfused = false;
    private Vector3 playerLastSeenPos;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetWeapon = targetWeaponObject.GetComponent<Weapon>();
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Update()
    {
        if(PlayerDetected() && !currentState.Equals(EnemyState.Chase))
        {
            currentState = EnemyState.Chase;
        }

        switch(currentState)
        {
            case EnemyState.Chase:
                if (!hasWeapon)
                {
                    GrabWeapon();
                }
                else // Weapon is wielded, try to catch player
                {
                    ChasePlayer();
                }
                break;
            case EnemyState.Confused:
                if (!isHandlingConfused)
                {
                    isHandlingConfused = true;
                    StartCoroutine(HandleConfused());
                }
                break;
            case EnemyState.Idle:
                Roam();
                break;
        }
    }

    /// <summary>
    /// If player is within enemy line of sight (within 180 degrees in front of enemy), return true.
    /// </summary>
    /// <returns></returns>
    protected virtual bool PlayerDetected()
    {
        if (Vector3.Distance(transform.position, player.position) > detectionRadius)
            return false;

        Vector3 directionToPlayer = (player.position - eyePoint.position).normalized;

        // Horizontal angle check
        float horizontalAngle = Vector3.Angle(new Vector3(eyePoint.forward.x, 0, eyePoint.forward.z), new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        if (horizontalAngle > 90f)
            return false;

        // Vertical angle check (up to 45 degrees above forward, no limit below forward)
        float verticalAngle = Vector3.Angle(eyePoint.forward, directionToPlayer);
        if (verticalAngle > 45f && directionToPlayer.y > eyePoint.forward.y)
            return false;

        // Check if there is a clear line of sight to the player
        if (Physics.Raycast(eyePoint.position, directionToPlayer, out RaycastHit hit, detectionRadius))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if(!hasWeapon)
                {
                    playerLastSeenPos = player.position;
                }
                return true;
            }
        }

        return false;
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

        if (!PlayerDetected())
        {
            currentState = EnemyState.Confused;
            return;
        }

        // Rotate enemy about the y axis to ensure that it is facing the player
        Vector3 enemyToPlayer = (player.position - transform.position).normalized;
        transform.forward = new Vector3(enemyToPlayer.x, transform.forward.y, enemyToPlayer.z);

        // Point the weapon at the player
        Vector3 weaponToPlayer = (player.position - targetWeapon.transform.position).normalized;
        enemyGrabPoint.forward = weaponToPlayer;

        float distance = Vector3.Distance(transform.position, player.position);
        int rangeVal = targetWeapon.DistanceInRange(distance);

        switch(rangeVal)
        {
            case 0: // Player within range, use weapon
                targetWeapon.Use();
                break;
            case 1: // Player too far, move towards player
                MoveTowards(player.position, chaseSpeed);
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
    /// "Must have been the wind" behavior.
    /// When either the enemy has lost the player, or got distracted by something and decided it's nothing,
    /// stand in place (play looking around animation) for a few seconds (until looking around animation is over),
    /// then go back to roaming.
    /// </summary>
    protected virtual IEnumerator HandleConfused()
    {
        if (enemyGrabPoint != null)
        {
            enemyGrabPoint.localRotation = Quaternion.identity;
        }
        yield return new WaitForSeconds(3f);
        currentState = EnemyState.Idle;
        isHandlingConfused = false;
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
                targetWeapon.Equip(enemyGrabPoint);
                MoveTowards(playerLastSeenPos, chaseSpeed);
            }
        }
        else
        {
            MoveTowards(targetWeapon.transform.position, chaseSpeed);
        }
    }

    protected virtual void MoveTowards(Vector3 target, float speed)
    {
        agent.speed = speed;
        agent.destination = target;
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

        Vector3 moveDirection = (newTargetPosition - transform.position).normalized;
        transform.position += moveDirection * speed * Time.deltaTime;

        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
    }
}
