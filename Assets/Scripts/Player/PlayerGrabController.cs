using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGrabController : MonoBehaviour
{
    [Header("Grab Settings")]
    [SerializeField] private float grabRange = 3f; // Max distance to grab object
    [SerializeField] private LayerMask grabbableLayer; // Layer for grabbable objects
    [SerializeField] private Transform holdPoint; // Where the grabbed object will be held
    [SerializeField] private float moveSpeed = 10f; // Speed at which objects move to the hold position
    [SerializeField] private float weightDelayFactor = 0.2f; // Increase delay with object mass

    private PlayerInputHandler inputHandler;
    private Rigidbody grabbedObject = null;

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
            if (grabbedObject == null) // Attempt to grab an object
            {
                TryGrabObject();
            }
            else // Already grabbed object
            {
                MoveObject();
            }
        }
        else if (grabbedObject != null) // Releasing Grab input with an object grabbed
        {
            ReleaseObject();
        }
    }

    /// <summary>
    /// Upon applying Grab Input, check if there exists a GrabbableObject within reasonable distance and
    /// line of sight of player. If so, grab the object.
    /// </summary>
    private void TryGrabObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayer))
        {
            if (hit.collider.CompareTag("GrabbableObject") && hit.collider.TryGetComponent(out Rigidbody rb))
            {
                grabbedObject = rb;
                grabbedObject.useGravity = false;
                grabbedObject.drag = 10f; // Increase drag for stable holding
            }
        }
    }

    private void MoveObject()
    {
        if (grabbedObject == null)
        {
            return;
        }
        // Calculate the target position and delay effect based on object weight
        Vector3 targetPosition = holdPoint.position;
        float delay = Vector3.Distance(grabbedObject.position, targetPosition) * weightDelayFactor * grabbedObject.mass;
        Vector3 smoothedPosition = Vector3.Lerp(grabbedObject.position, targetPosition, Time.deltaTime * moveSpeed / (1 + delay));

        grabbedObject.MovePosition(smoothedPosition);

        // Allow object rotation to follow the camera view
        Quaternion targetRotation = Camera.main.transform.rotation;
        grabbedObject.MoveRotation(Quaternion.Slerp(grabbedObject.rotation, targetRotation, Time.deltaTime * moveSpeed));
    }

    private void ReleaseObject()
    {
        if (grabbedObject == null)
        {
            return;
        }
        grabbedObject.useGravity = true;
        grabbedObject.drag = 0f; // Reset drag
        grabbedObject = null;
    }

}
