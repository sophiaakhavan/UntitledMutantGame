using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButterflyNet : Weapon
{
    [Header("Casting Settings")]
    public float castTime = 2f; // Time to cast the net
    public Transform handle;
    public Transform net;
    public float rotationAngle = 0f; // Tracks the amount of rotation
    public float rotationSpeed = 60f; // Degrees per second
    public float maxRotationAngle = 170f; // Maximum allowed rotation

    public override void Use(Transform target)
    {
        if (!IsCasting) // Prevent starting a new cast if already casting
        {
            StartCoroutine(CastNet(target));
        }
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

        if (netCollider != null && handle != null)
        {
            // Calculate actual handle length
            float handleLength = handle.transform.lossyScale.y;

            // Calculate actual net diameter
            float netDiameter = (netCollider.radius * Mathf.Max(netCollider.transform.lossyScale.x, netCollider.transform.lossyScale.y, netCollider.transform.lossyScale.z))*2;

            // Return both ranges
            return (handleLength, handleLength + netDiameter);
        }

        Debug.LogWarning("One or more colliders are missing!");
        return (0f, 0f); // Default to 0 if missing components
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

        Quaternion initialRotation = transform.localRotation;

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
            transform.Rotate(Vector3.right, step, Space.Self);
            rotationAngle += step;

            yield return null; // Wait until the next frame

        }

        // If no valid hit or rotation exceeded the limit, reset rotation
        Debug.Log("Butterfly Net missed. Resetting...");
        transform.localRotation = initialRotation;

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
