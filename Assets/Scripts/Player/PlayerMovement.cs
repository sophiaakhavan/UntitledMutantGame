using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float walkSpeed = 5f; // Walking speed
    public float flySpeed = 5f; // Base flying speed
    public float maxFlySpeed = 10f;
    public float gravityScale = 0.2f; // Gravity reduction during gliding
    public float flapForce = 5f; // Force applied when flapping
    public float diveMultiplier = 50f;

    [Header("Ground Detection")]
    public float groundCheckRadius = 0.3f; // Radius for checking ground
    public LayerMask groundLayer; // Layer that represents the ground

    [Header("Flapping Speed Boost Settings")]
    public float flapSpeedBoost = 5f; // Additional speed gained from flapping
    public float speedDecayRate = 2f; // Rate at which the boost decays (units/second)

    private float currentSpeedBoost = 0f; // Tracks the current speed boost

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isFlying = false;
    private bool canFlap = true;
    private bool isGliding = false;
    private Vector3 glideDirection = new Vector3(0f, 0f, 0f);

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
            return;
        }

        // Check if player is on the ground or not, determine appropriate 
        CheckGroundStatus();

    }

    private void FixedUpdate()
    {
        if (isFlying)
        {
            ApplyFlyingPhysics();
        }
    }
    private IEnumerator ResetFlapCooldown()
    {
        canFlap = false;
        yield return new WaitForSeconds(0.1f); // Slight delay to stabilize physics
        canFlap = true;
    }

    private void CheckGroundStatus()
    {
        // Get the bottom center of the collider
        Collider collider = GetComponent<Collider>();
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
            if (isFlying)
            {
                StartCoroutine(ResetFlapCooldown());
            }
            else
            {
                HandleWalking();
                if (inputHandler.JumpInput)
                {
                    TakeOff();
                }
            }
            isFlying = false;
            isGliding = false;
        }
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

    private void TakeOff()
    {
        // Launch into the air
        isFlying = true;
        Flap();
    }

    private void Flap()
    {
        // Reset vertical velocity to ensure consistent upward force
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(Vector3.up * flapForce, ForceMode.Impulse);

        // If player is already moving while in the air
        if (Mathf.Abs(rb.velocity.x) > 0f && Mathf.Abs(rb.velocity.z) > 0f)
        {
            // Apply forward speed boost
            currentSpeedBoost = flapSpeedBoost;
        }

        inputHandler.ResetJumpInput();
    }

    /// <summary>
    /// While the bird is in the air, determine its velocity depending on whether it is gliding or not.
    /// Player can change the direction of flight with WASD, and the bird will glide as long as it is in the air.
    /// Direction must be given to enable gliding.
    /// Player can press W while facing downward to dive.
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

        // Flap upwards to gain height
        if (inputHandler.JumpInput)
        {
            Flap();
        }

        // Base velocity
        Vector3 newVelocity = rb.velocity;

        if (moveInput.magnitude > 0.1f && !isGliding) // If player applies input, begin gliding
        {
               isGliding = true;
        }

        if (isGliding)
        {
            // Default gliding direction (current velocity direction)
            //Vector3 glideDirection = rb.velocity.normalized;

            // Adjust direction with WASD input + look direction
            if (moveInput.magnitude > 0.1f)
            {
                glideDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            }

            // Maintain forward motion even without input
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

            // Default glide velocity
            newVelocity = glideDirection * effectiveSpeed;

            // Gradual descent due to gravity
            newVelocity.y = rb.velocity.y + (Physics.gravity.y * Time.deltaTime)*gravityScale;

            // Dive mechanics (W (moveInput.y) + Look down)
            if (Mathf.Abs(moveInput.y) > 0 && Vector3.Dot(forward, Vector3.down) > 0.5f)
            {
                newVelocity *= diveMultiplier;
            }
            // TODO: Climb mechanics (W + Look up) -- Tilting up after diving
            //else if(moveInput.y > 0 && Vector3.Dot(forward, Vector3.up) > 0.5f)
            //{
            //    float climbReduction = Mathf.Max(0f, rb.velocity.magnitude - flySpeed * 0.5f); // Reduce speed for climb
            //    newVelocity += Vector3.up * climbReduction * Time.deltaTime;
            //    newVelocity = Vector3.ClampMagnitude(rb.velocity, rb.velocity.magnitude); // Avoid excessive climb

            //}
        }

        // Apply the calculated velocity to the Rigidbody
        rb.velocity = newVelocity;

    }
}
