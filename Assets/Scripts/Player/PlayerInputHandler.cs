using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player Input Handler:
/// Separate input handling from game logic. Use Unity's input system for scalability and reusability.
/// Map player inputs to actions and provide input data to other systems.
/// </summary>

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions inputActions;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool LiftInput { get; private set; }
    public bool GrabInput { get; private set; }

    public void ResetLiftInput()
    {
        LiftInput = false;
    }

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Movement
        inputActions.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        // Looking
        inputActions.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => LookInput = Vector2.zero;

        // Jumping
        inputActions.Player.Lift.performed += ctx => LiftInput = true;
        inputActions.Player.Lift.canceled += ctx => LiftInput = false;

        // Grabbing
        inputActions.Player.Grab.performed += ctx => GrabInput = true;
        inputActions.Player.Grab.canceled += ctx => GrabInput = false;
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }

    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // Locks the cursor to the center
        Cursor.visible = false; // Hides the cursor
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None; // Frees the cursor
        Cursor.visible = true; // Shows the cursor
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

}
