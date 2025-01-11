using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform player; // The parrot or player object
    public Vector3 offset = new Vector3(0, 5, -10); // Offset from the player

    [Header("Sensitivity Settings")]
    public float lookSensitivity = .15f;

    private float currentYaw = 0f; // Horizontal rotation (yaw)
    private float currentPitch = 0f; // Vertical rotation (pitch)
    private float maxPitchAngle = 45f; // Max up/down camera angle
    private float maxFlightPitchAngle = 30f; // Max amount to look up or down during flight

    private PlayerInputHandler inputHandler;
    private PlayerMovementController movementController;

    private void Start()
    {
        inputHandler = player.GetComponent<PlayerInputHandler>();
        if(inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler not found on the player GameObject!");
        }
        movementController = player.GetComponent<PlayerMovementController>();
        if (movementController == null)
        {
            Debug.LogError("Player Movement Controller not found on the player GameObject!");
        }
    }

    private void LateUpdate()
    {
        if (inputHandler != null)
        {
            HandleMouseLook();
        }

        FollowPlayer();
    }

    private void HandleMouseLook()
    {
        // Get mouse input
        Vector2 lookInput = inputHandler.LookInput;

        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

        // Adjust yaw (horizontal rotation) and pitch (vertical rotation)
        currentYaw += mouseX;
        currentPitch -= mouseY;

        if(movementController != null && movementController.IsFlying)
        {
            currentPitch = Mathf.Clamp(currentPitch, -maxFlightPitchAngle, maxFlightPitchAngle);
        }
        else
        {
            currentPitch = Mathf.Clamp(currentPitch, -maxPitchAngle, maxPitchAngle);
        }

        // Rotate the camera
        Quaternion cameraRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        transform.rotation = cameraRotation;
    }

    private void FollowPlayer()
    {
        if (player == null) return;

        // Keep the camera at a fixed offset behind the player
        transform.position = player.position + transform.rotation * offset;
    }
}
