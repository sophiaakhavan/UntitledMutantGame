using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public bool IsCasting { get; set; } = false;
    /// <summary>
    /// Equip the weapon to the enemy's hand or grab point.
    /// </summary>
    /// <param name="grabPoint">The point where the weapon is attached.</param>
    public virtual void Equip(Transform grabPoint)
    {
        transform.SetParent(grabPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
    }

    /// <summary>
    /// Given a distance between enemy and player, if the player is further than the casting range of the weapon from the enemy,
    /// return 1. If the player is closer than the casting range, return -1. If within the range, return 0.
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public abstract int DistanceInRange(float distance);

    /// <summary>
    /// Use the weapon against a target.
    /// </summary>
    /// <param name="target">The target to attack.</param>
    public abstract void Use(Transform target);
}
