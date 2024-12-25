using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private float roamSpeed = 4f;
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Weapon Settings")]
    [SerializeField] private GameObject targetWeaponObject;
    [SerializeField] private Transform grabPoint;

    protected Transform player;
    protected bool hasWeapon = false;
    protected bool isChasing = false;

    private Weapon targetWeapon;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetWeapon = targetWeaponObject.GetComponent<Weapon>();

        // Start roaming or idle behavior (override as needed)
        StartRoaming();
    }

    protected virtual void Update()
    {
        if(PlayerDetected() && !isChasing)
        {
            isChasing = true;
        }
        if (isChasing)
        {
            if (!hasWeapon)
            {
                MoveTowards(targetWeapon.transform.position, chaseSpeed);

                if (Vector3.Distance(transform.position, targetWeapon.transform.position) < 5f)
                {
                    GrabWeapon();
                }
            }
            else // Weapon is wielded, try to catch player
            {
                HandleChaseBehavior();
            }
        }
        else
        {
/*            isChasing = false;

            if (!hasWeapon)
            {
                Roam();
            }
            else
            {
                HandleIdleBehavior();
            }*/
        }
    }

    protected virtual bool PlayerDetected()
    {
        return Vector3.Distance(transform.position, player.position) <= detectionRadius;
    }

    protected virtual void HandleChaseBehavior()
    {
        if (player == null)
        {
            Debug.Log("Player not found!");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        int rangeVal = targetWeapon.DistanceInRange(distance);

        // Rotate grab point's forward about the y axis
        if(grabPoint != null)
        {
            Vector3 currGrabDirection = grabPoint.forward;
            Vector3 directionToPlayer = (player.position - grabPoint.position).normalized;
            //grabPoint.forward = new Vector3(directionToPlayer.x, currGrabDirection.y, directionToPlayer.z);
            grabPoint.forward = directionToPlayer;
        }
        

        if (targetWeapon != null && rangeVal == 0)
        {
            targetWeapon.Use();
        }
        else
        {
            //// Prevent movement if currently casting weapon
            //if (targetWeapon != null && targetWeapon.IsCasting)
            //{
            //    return;
            //}

            if (rangeVal == 1)
            {
                MoveTowards(player.position, chaseSpeed);
            }
            else if(rangeVal == -1) // Too close
            {
                // Step backward from player
                MoveAwayFrom(player.position, chaseSpeed);
            }
        }
    }

    protected virtual void HandleIdleBehavior()
    {
        // Default idle behavior (e.g., roaming between patrol points)
    }

    protected virtual void GrabWeapon()
    {
        if (targetWeapon != null)
        {
            hasWeapon = true;
            targetWeapon.Equip(grabPoint);
        }
    }

    protected virtual void StartRoaming()
    {
        // Override this to implement roaming or patrol behavior
    }

    protected virtual void Roam()
    {
        // Override this for patrol logic
    }

    protected virtual void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Face the direction of movement
        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
    }

    protected virtual void MoveAwayFrom(Vector3 target, float speed)
    {
        // Calculate the direction away from the target
        Vector3 directionAwayFromTarget = (transform.position - target).normalized;

        // Calculate the new position, moving twice the current distance away
        float distance = Vector3.Distance(transform.position, target);
        Vector3 newTargetPosition = transform.position + directionAwayFromTarget * distance * 2f;

        // Move the enemy to the new position
        Vector3 moveDirection = (newTargetPosition - transform.position).normalized;
        transform.position += moveDirection * speed * Time.deltaTime;

        // Ensure the enemy is still facing the target
        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
    }
}
