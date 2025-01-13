using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 5, -10);

    [Header("Sensitivity Settings")]
    public float lookSensitivity = .15f;

    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float maxPitchAngle = 45f;
    private float maxFlightPitchAngle = 30f;

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
        Vector2 lookInput = inputHandler.LookInput;

        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

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

        Quaternion cameraRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        transform.rotation = cameraRotation;
    }

    private void FollowPlayer()
    {
        if (player == null) return;

        transform.position = player.position + transform.rotation * offset;
    }
}
