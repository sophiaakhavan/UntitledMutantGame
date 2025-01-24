using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class ProximitySensor : MonoBehaviour
{
    EnemyAI enemyAI;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    void Update()
    {
        for (int index = 0; index < DetectableTargetManager.Instance.AllTargets.Count; ++index)
        {
            var candidateTarget = DetectableTargetManager.Instance.AllTargets[index];

            // Skip if ourselves
            if (candidateTarget.gameObject == gameObject)
                continue;

            if (Vector3.Distance(enemyAI.EyeLocation, candidateTarget.transform.position) <= enemyAI.ProximityDetectionRange)
                enemyAI.ReportInProximity(candidateTarget);
        }
    }
}