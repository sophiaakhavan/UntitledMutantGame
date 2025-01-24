using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableTarget : MonoBehaviour
{

    void Start()
    {
        DetectableTargetManager.Instance.Register(this);
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if(DetectableTargetManager.Instance != null)
        {
            DetectableTargetManager.Instance.Deregister(this);
        }
    }
}
