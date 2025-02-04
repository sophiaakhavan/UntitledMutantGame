using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectableTarget : MonoBehaviour
{
    public bool IsInMotion;
    public float Radius;
    private Vector3 prevPos;

    void Start()
    {
        DetectableTargetManager.Instance.Register(this);
    }

    void Update()
    {
        IsInMotion = Vector3.Distance(prevPos, transform.position) > 0.01f;

        prevPos = transform.position; // Update the previous position
    }

    private void OnDestroy()
    {
        if(DetectableTargetManager.Instance != null)
        {
            DetectableTargetManager.Instance.Deregister(this);
        }
    }

}
