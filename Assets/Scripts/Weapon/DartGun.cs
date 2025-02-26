using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DartGun : ProjectileWeapon
{
    [SerializeField] private float dartSpeed = 20f;
    [SerializeField] private GameObject dartPrefab;

    private void Start()
    {
        base.Start();
        if (dartPrefab == null)
        {
            Debug.LogError("Dart prefab is not assigned in the Inspector!");
            return;
        }
    }

    /// <summary>
    /// Spawn dart from projectile spawn point. Called by animation event when gun fires.
    /// </summary>
    public void ShootDart()
    {
        Quaternion gunRotation = Quaternion.LookRotation(projectileSpawnPoint.forward);
        // Create and shoot dart from the projectile spawn point in the direction that the gun is facing
        GameObject dartInstance = (GameObject)Instantiate(dartPrefab, projectileSpawnPoint.position, gunRotation);

        Rigidbody rb = dartInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = projectileSpawnPoint.forward * dartSpeed;
        }

        Debug.Log("Dart fired!");
    }
}
