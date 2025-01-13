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
    [SerializeField] private Transform enemyGrabPoint;

    protected Transform player;
    protected bool hasWeapon = false;
    protected bool isChasing = false;

    private Weapon targetWeapon;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetWeapon = targetWeaponObject.GetComponent<Weapon>();

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

    /// <summary>
    /// Once the enemy has grabbed a weapon, this function handles behavior for chasing the player,
    /// using the weapon when the player is within range, or stepping backwards if the player is too close.
    /// Also ensures that the enemy and its equipped weapon are properly facing the player.
    /// </summary>
    protected virtual void HandleChaseBehavior()
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

    protected virtual void HandleIdleBehavior()
    {
        // Default idle behavior (e.g., roaming between patrol points)
    }

    protected virtual void GrabWeapon()
    {
        if (targetWeapon != null)
        {
            hasWeapon = true;
            targetWeapon.Equip(enemyGrabPoint);
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

        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
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
