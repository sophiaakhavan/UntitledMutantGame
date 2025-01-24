using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class HearingSensor : MonoBehaviour
{
    EnemyAI enemyAI;
    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        HearingManager.Instance.Register(this);
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if(HearingManager.Instance != null)
        {
            HearingManager.Instance.Deregister(this);
        }
    }

    public void OnHeardSound(GameObject source, Vector3 location, EHeardSoundCategory category, float intensity)
    {
        // Outside of hearing range
        if(Vector3.Distance(location, enemyAI.EyeLocation) > enemyAI.HearingRange)
            return;

        enemyAI.ReportCanHear(source, location, category, intensity);
    }
}
