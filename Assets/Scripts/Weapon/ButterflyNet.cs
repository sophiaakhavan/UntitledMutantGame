using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButterflyNet : Weapon
{
    [Header("Casting Settings")]
    [SerializeField] private float castTime = 2f; // Time to cast the net
    [SerializeField] private Transform handle;
    [SerializeField] private Transform net;
    [SerializeField] private float rotationAngle = 0f; // Tracks the amount of rotation
    [SerializeField] private float rotationSpeed = 60f; // Degrees per second
    [SerializeField] private float maxRotationAngle = 170f; // Maximum allowed rotation
    [SerializeField] private Transform handlePivot; // Pivot to rotate handle around

    public override void Use(Transform target)
    {
        if (IsCasting) // Prevent starting a new cast if already casting
        {
            return;
        }
        StartCoroutine(CastNet(target));
    }

    public override void Equip(Transform grabPoint)
    {
        base.Equip(grabPoint);

        // Adjust position of handle
        if (handle == null)
        {
            Debug.LogError("Handle reference is missing on ButterflyNet!");
            return;
        }

        // Calculate the offset needed to align the bottom of the handle to the grab point
        float handleLength = handle.transform.localScale.y;
        Vector3 offset = new Vector3(0, handleLength / 2, 0);

        // Adjust the local position of the net to align correctly
        transform.localPosition += offset;

    }

    /// <summary>
    /// If target is too far (distance provided as input) from the enemy to be caught with net, return 1.
    /// If target is within the range (as in, can be caught with the actual net portion), return 0.
    /// If target is too close, return -1.
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public override int DistanceInRange(float distance)
    {
        var (minRange, maxRange) = CalculateRanges();
        if (distance <= maxRange)
        {
            if(distance < minRange) // Too close
            {
                return -1;
            }
            return 0; // Within range
        }

        return 1; // Too far
    }

    /// <summary>
    /// Based on lengths of handle and actual net portion of the weapon, provide a minimum and maximum to define
    /// the range for the section of the weapon that can catch the target.
    /// </summary>
    /// <returns></returns>
    private (float minRange, float maxRange) CalculateRanges()
    {
        SphereCollider netCollider = net.GetComponent<SphereCollider>();
        CapsuleCollider handleCollider = handle.GetComponent<CapsuleCollider>();

        if (netCollider == null || handleCollider == null)
        {
            Debug.LogWarning("One or more colliders are missing!");
            return (0f, 0f); // Default to 0 if missing components
        }
        // TODO: Calculate actual handle length
        //float handleLength = Mathf.Max(handleCollider.bounds.size.x, handleCollider.bounds.size.y, handleCollider.bounds.size.z);
        float handleLength = Mathf.Max(handleCollider.transform.lossyScale.x, handleCollider.transform.lossyScale.y, handleCollider.transform.lossyScale.z);

        // Calculate actual net diameter
        float netDiameter = (netCollider.radius * Mathf.Max(netCollider.transform.lossyScale.x, netCollider.transform.lossyScale.y, netCollider.transform.lossyScale.z))*2;

        // Return both ranges
        return (handleLength, handleLength + netDiameter);
    }

    /// <summary>
    /// Called when player is within the range to cast.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private IEnumerator CastNet(Transform target)
    {
        IsCasting = true;
        Debug.Log("Butterfly Net is being used!");

        // Wait for the casting time
        yield return new WaitForSeconds(castTime);

        rotationAngle = 0f; // Reset rotation angle
        Quaternion initialRotation = transform.localRotation;
        Vector3 originalPosition = transform.position;

        // Align the net to face the player before casting
        Vector3 directionToPlayer = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
        transform.rotation = lookRotation; // Snap rotation to face the player

        // Enable collision detection during casting
        Collider netCollider = net.GetComponent<Collider>();
        if (netCollider == null)
        {
            Debug.LogError("Net Collider is missing!");
            yield break;
        }

        while (rotationAngle < maxRotationAngle && IsCasting)
        {
            // Rotate weapon forward
            float step = rotationSpeed * Time.deltaTime;
            transform.RotateAround(handlePivot.position, transform.right, step);
            rotationAngle += step;

            yield return null; // Wait until the next frame

        }

        // If no valid hit or rotation exceeded the limit, reset rotation and position
        Debug.Log("Butterfly Net missed. Resetting...");
        transform.localRotation = initialRotation;
        transform.position = originalPosition;

        IsCasting = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsCasting)
            return;
        if (collision.collider.CompareTag("Player"))
        {
            Debug.Log("Butterfly Net successfully caught the bird!");
        }

        IsCasting = false;
    }
}
