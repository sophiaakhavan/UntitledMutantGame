using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButterflyNet : Weapon
{
    private void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            Debug.Log("Butterfly Net successfully caught the bird!");
        }
    }
}
