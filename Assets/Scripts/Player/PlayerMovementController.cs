using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f; // Walking speed
    [SerializeField] private float flySpeed = 5f; // Base flying speed
    [SerializeField] private float maxFlySpeed = 10f;
    [SerializeField] private float defaultGravityScale = 2f; // Gravity scale normally
    [SerializeField] private float glideGravityScale = 0.2f; // Gravity scale during gliding
    [SerializeField] private float flapForce = 8f; // Force applied when flapping
    [SerializeField] private float diveMultiplier = 50f;
    [SerializeField] private float flapThreshold = 0.2f; // Seconds to hold Lift for a flap

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckRadius = 0.3f; // Radius for checking ground
    [SerializeField] private LayerMask groundLayer; // Layer that represents the ground

    [Header("Flapping Speed Boost Settings")]
    [SerializeField] private float flapSpeedBoost = 5f; // Additional speed gained from flapping
    [SerializeField] private float speedDecayRate = 2f; // Rate at which the boost decays (units/second)

    private float currentSpeedBoost = 0f; // Tracks the current speed boost

    private float spacePressTime = 0f; // Track time Lift input has been held

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isFlying = false;
    private bool canFlap = true;
    private bool isGliding = false;
    private Vector3 flyDirection = new Vector3(0f, 0f, 0f);

    private PlayerInputHandler inputHandler;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler not found on the same GameObject as PlayerMovement!");
        }
    }

    private void Update()
    {
        if (inputHandler == null)
        {
            Debug.Log("Input handler not found on player!");
            return;
        }

        // Check if player is on the ground or not
        CheckGroundStatus();

        // Process jumping/flapping/gliding
        TrackLiftInput();
    }

    private void FixedUpdate()
    {
        if (isFlying)
        {
            ApplyFlyingPhysics();
        }
    }


    private void CheckGroundStatus()
    {
        // Get the bottom center of the collider
        Collider collider = GetComponentInChildren<Collider>();
        if (collider == null) return; // Ensure the player has a collider

        Vector3 origin = collider.bounds.center;
        origin.y = collider.bounds.min.y; // Use the bottom of the collider

        // Perform a sphere check for ground contact
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayer);

        if (!isGrounded)
        {
            isFlying = true;
        }
        else
        {
            flyDirection = new Vector3(0f, 0f, 0f);
            HandleWalking();
            isFlying = false;
            isGliding = false;
        }
    }

    /// <summary>
    /// If player holds Lift Input within the established flap threshold, flap. Otherwise,
    /// if Lift Input is continuously held for longer than threshold, glide until it is released.
    /// </summary>
    private void TrackLiftInput()
    {
        if (inputHandler.LiftInput)
        {
            if (spacePressTime == 0f)
            {
                spacePressTime = Time.time; // Start timing when space is pressed
            }

            // Check if input exceeds the flap threshold for gliding
            if (!isGrounded && (Time.time - spacePressTime) > flapThreshold)
            {
                isGliding = true; // Enable gliding
            }
        }

        // Handle LiftInput release
        if (!inputHandler.LiftInput && spacePressTime > 0f)
        {
            float pressDuration = Time.time - spacePressTime;
            spacePressTime = 0f; // Reset the timer

            if (isGrounded)
            {
                isFlying = true;
                Flap();
            }
            else
            {
                if (pressDuration <= flapThreshold)
                {
                    Flap(); // Air flap
                }
                else
                {
                    isGliding = false; // If player lets go, no longer gliding
                }
            }
        }

        // Disable gliding when JumpInput is no longer held
        if (!inputHandler.LiftInput && isGliding)
        {
            isGliding = false;
        }
    }

    private IEnumerator ResetFlapCooldown()
    {
        canFlap = false;
        yield return new WaitForSeconds(0.1f); // Slight delay to stabilize physics
        canFlap = true;
    }

    private void HandleWalking()
    {
        // Get input
        Vector2 moveInput = inputHandler.MoveInput;

        // Exit early if there's no move input
        if (Mathf.Abs(moveInput.x) < 0.1f && Mathf.Abs(moveInput.y) < 0.1f)
        {
            return;
        }

        // Calculate movement direction relative to the camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f; // Ensure movement is horizontal
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;

        // Apply walking movement
        Vector3 newPosition = rb.position + moveDirection * walkSpeed * Time.deltaTime;
        rb.MovePosition(newPosition);
    }

    /// <summary>
    /// Flap wings once, exerting an upwards impulse onto the player and temporarily increasing flight speed
    /// </summary>
    private void Flap()
    {
        if (!canFlap) return;

        // Reset vertical velocity to ensure consistent upward force
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(Vector3.up * flapForce, ForceMode.Impulse);

        // If player is already moving while in the air, apply forward speed boost
        if (Mathf.Abs(rb.velocity.x) > 0f && Mathf.Abs(rb.velocity.z) > 0f)
        {
            currentSpeedBoost = flapSpeedBoost;
        }

        //inputHandler.ResetLiftInput();
        StartCoroutine(ResetFlapCooldown());
    }

    /// <summary>
    /// While the bird is in the air, determine its velocity depending on whether it is gliding or not.
    /// Player can change the direction of flight with WASD.
    /// Lift Input must be constantly pressed to glide.
    /// </summary>
    private void ApplyFlyingPhysics()
    {
        // Get WASD input
        Vector2 moveInput = inputHandler.MoveInput;

        // Get camera-relative directions
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // Base velocity
        Vector3 newVelocity = rb.velocity;

        // Adjust direction with WASD input + look direction
        if (moveInput.magnitude > 0.1f)
        {
            flyDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        }

        // Maintain forward motion
        Vector3 forwardVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float effectiveSpeed = Mathf.Max(forwardVelocity.magnitude, flySpeed);

        // Decay the speed boost over time
        if (currentSpeedBoost > 0f)
        {
            currentSpeedBoost -= speedDecayRate * Time.deltaTime;
            currentSpeedBoost = Mathf.Max(0f, currentSpeedBoost); // Ensure it doesn't go below 0
                                                                  // Adjust forward speed with speed boost
            effectiveSpeed += currentSpeedBoost;
        }

        effectiveSpeed = Mathf.Clamp(effectiveSpeed, flySpeed, maxFlySpeed);

        // Default flying velocity
        newVelocity = flyDirection * effectiveSpeed;

        // Slowed descent while gliding
        if (isGliding)
        {
            // Simulate lift by applying upward force
            float liftForce = Mathf.Abs(Physics.gravity.y) * (1 - glideGravityScale);
            newVelocity.y = rb.velocity.y + liftForce * Time.deltaTime;
            
            // Ensure the upward force doesn't completely negate gravity for realism
            newVelocity.y = Mathf.Max(newVelocity.y, Physics.gravity.y * glideGravityScale * Time.deltaTime);

            // Dive mechanics (W (moveInput.y) + Look down)
            //if (Mathf.Abs(moveInput.y) > 0 && Vector3.Dot(forward, Vector3.down) > 0.5f)
            //{
            //    newVelocity *= diveMultiplier;
            //}
        }
        else
        {
            // Natural descent due to gravity
            newVelocity.y = rb.velocity.y + (Physics.gravity.y * Time.deltaTime) * defaultGravityScale;
        }

        // Apply the calculated velocity to the Rigidbody
        rb.velocity = newVelocity;

    }
}
