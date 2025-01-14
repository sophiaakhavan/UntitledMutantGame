using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGrabController : MonoBehaviour
{
    [SerializeField] private Transform playerCameraTransform;

    [Header("Grab Settings")]
    [SerializeField] private float grabRange = 5f;
    [SerializeField] private LayerMask grabbableLayer;
    [SerializeField] private Transform grabPoint;

    private PlayerInputHandler inputHandler;
    private GrabbableObject grabbedObject = null;

    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler not found for Grab Controller!");
        }
    }

    private void Update()
    {
        if (inputHandler.GrabInput)
        {
            if (grabbedObject == null) // If not currently holding an object
            {
                TryGrabObject();
            }
        }
        else if (grabbedObject != null) // Releasing Grab input with an object grabbed
        {
            grabbedObject.Drop();
            grabbedObject = null;
        }
    }

    /// <summary>
    /// Upon applying Grab Input, check if there exists a GrabbableObject within reasonable distance and
    /// line of sight of player. If so, grab the object.
    /// </summary>
    private void TryGrabObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayer))
        {
            if (hit.transform.TryGetComponent(out grabbedObject))
            {
                grabbedObject.Grab(grabPoint);
            }
        }
    }



}
