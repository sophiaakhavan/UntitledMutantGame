using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    [Header("General Settings")]
    public float roamSpeed = 2f;
    public float chaseSpeed = 4f;
    public float detectionRadius = 10f;
    public Transform[] patrolPoints;

    [Header("Weapon Settings")]
    public GameObject targetWeaponObject;
    public Transform weaponGrabPoint;

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
        float distance = Vector3.Distance(transform.position, player.position);
        int rangeVal = targetWeapon.DistanceInRange(distance);
        if (targetWeapon != null && rangeVal == 0)
        {
            targetWeapon.Use(player);
        }
        else
        {
            if (rangeVal == 1)
            {
                //Prevent movement if currently casting weapon
                if(targetWeapon != null && targetWeapon.IsCasting)
                {
                    return;
                }
                MoveTowards(player.position, chaseSpeed);
            }
            else
            {
                //TODO: move away from
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
            targetWeapon.Equip(weaponGrabPoint);
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

        // Optional: Face the direction of movement
        transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
    }
}
