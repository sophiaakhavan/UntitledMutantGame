using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class Weapon : MonoBehaviour
{
    public bool IsCasting { get; set; } = false;
    [SerializeField] protected Transform minDist;
    [SerializeField] protected Transform maxDist;
    [SerializeField] private Transform GrabPoint; // Weapon's grab point
    private Animator animator;

    protected void Start()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Equip the weapon to the enemy's hand or grab point.
    /// </summary>
    /// <param name="grabPoint">The point where the weapon is attached.</param>
    public virtual void Equip(Transform enemyGrabPoint)
    {
        if (GrabPoint == null)
        {
            Debug.LogError("Weapon GrabPoint is not assigned!");
            return;
        }
        Vector3 grabPointOffset = GrabPoint.position - transform.position;
        transform.position = enemyGrabPoint.position - grabPointOffset;
        transform.rotation = Quaternion.LookRotation(enemyGrabPoint.forward, enemyGrabPoint.up);
        transform.SetParent(enemyGrabPoint);

        animator.SetTrigger("Base_Idle");
    }

    /// <summary>
    /// Given a distance between enemy and player, if the player is further than the casting range of the weapon from the enemy,
    /// return 1. If the player is closer than the casting range, return -1. If within the range, return 0.
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public virtual int DistanceInRange(float distance)
    {
        if(minDist == null || maxDist == null)
        {
            Debug.LogError("Min/Max distances not assigned!");
        }
        float minDistance = Vector3.Distance(transform.position, minDist.position);
        float maxDistance = Vector3.Distance(transform.position, maxDist.position);

        if (distance <= maxDistance)
        {
            if (distance < minDistance) // Too close
            {
                return -1;
            }
            return 0; // Within range
        }

        return 1; // Too far
    }

    /// <summary>
    /// Use the weapon against a target.
    /// </summary>
    /// <param name="target">The target to attack.</param>
    public virtual void Use()
    {
        if (IsCasting) // Prevent starting a new cast if already casting
        {
            return;
        }
        CastWeapon();
    }

    /// <summary>
    /// Called when player is within the range to cast.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    protected virtual void CastWeapon()
    {
        animator.SetTrigger("Base_Attack");
    }

}
