using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    public Transform cameraTransform;
    public bool IsFlying { get { return isFlying; } set { isFlying = value; } }

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float flySpeed = 10f; // Base flying speed
    [SerializeField] private float maxFlySpeed = 20f;
    [SerializeField] private float maxGlideSpeed = 40f;
    [SerializeField] private float flapForce = 12f;
    [SerializeField] private float jumpForce = 16f; // Force applied when jumping off the ground
    [SerializeField] private float flapThreshold = 0.3f; // Seconds to hold Lift for a flap
    [SerializeField] private float flapCooldown = 0.5f; // Seconds until player can flap again
    [SerializeField] private float defaultGravityScale = 2f;
    [SerializeField] private float glideGravityScale = 0.5f; // Scale to counteract gravity when gliding
    [SerializeField] private float minFallSpeedMultiplier = 0.2f; // Fraction of terminal velocity to define min fall speed when gliding

    [Header("Flapping Speed Boost Settings")]
    [SerializeField] private float flapSpeedBoost = 1f;
    [SerializeField] private float speedDecayRate = 5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.3f; // Radius for checking if player touching ground layer

    [Header("Visual Effects")]
    [SerializeField] private float moveDirectionDelay = 0.2f; // Amount to delay player transform after directional change
    [SerializeField] private float rollIntensity = 90f; // Degrees to roll when moving left/right during flight
    [SerializeField] private float rotationSmoothing = 2f; // Smoothing speed for rotation

    float currentRoll;

    private float currentSpeedBoost = 0f; // Tracks the current speed boost

    private float spacePressTime = 0f; // Track time Lift input has been held

    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isFlying = false;
    private bool canFlap = true;
    private bool isGliding = false;
    private Vector3 flyDirection = new Vector3(0f, 0f, 0f);

    private PlayerInputHandler inputHandler;
    private Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

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

        CheckGroundStatus();

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
            ResetRollAndPitch();
            flyDirection = new Vector3(0f, 0f, 0f);
            HandleWalking();
            isFlying = false;
            isGliding = false;
            animator.SetBool("IsGliding", false);
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
                animator.SetBool("IsGliding", true);
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
                Flap(true);
            }
            else
            {
                if (pressDuration <= flapThreshold)
                {
                    Flap(false); // Air flap
                }
                else
                {
                    animator.SetBool("IsGliding", false);
                    isGliding = false; // If player lets go, no longer gliding
                }
            }
        }

        // Disable gliding when JumpInput is no longer held
        if (!inputHandler.LiftInput && isGliding)
        {
            animator.SetBool("IsGliding", false);
            isGliding = false;
        }
    }

    private IEnumerator ResetFlapCooldown()
    {
        canFlap = false;
        yield return new WaitForSeconds(flapCooldown); // Slight delay to stabilize physics
        canFlap = true;
    }

    private void HandleWalking()
    {
        // Get input
        Vector2 moveInput = inputHandler.MoveInput;

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

        if (moveDirection.magnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
        }
    }

    /// <summary>
    /// Flap wings once, exerting an upwards impulse onto the player and temporarily increasing flight speed.
    /// If jumping off the ground, provide input true and apply jumpForce instead of flapForce.
    /// </summary>
    private void Flap(bool isJump)
    {
        if (!canFlap) return;

        // Reset vertical velocity to ensure consistent upward force
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (isJump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else
        {

            animator.SetTrigger("Flap");
            rb.AddForce(Vector3.up * flapForce, ForceMode.Impulse);

            // If player is already moving while in the air, apply forward speed boost
            if (Mathf.Abs(rb.velocity.x) > 0f && Mathf.Abs(rb.velocity.z) > 0f)
            {
                currentSpeedBoost = flapSpeedBoost;
            }
            StartCoroutine(ResetFlapCooldown());
        }
    }

    /// <summary>
    /// While the player is in the air, determine its velocity depending on whether it is gliding or not.
    /// Player can initiate forward motion with forward move input and can steer via look direction
    /// Lift Input must be constantly pressed to glide.
    /// </summary>
    private void ApplyFlyingPhysics()
    {
        Vector2 moveInput = inputHandler.MoveInput;
        Vector3 lookDirection = cameraTransform.forward;
        lookDirection.Normalize();
        // Have separate logic between xz (horizontal) movement and y movement
        Vector3 horizontalLookDirection = new Vector3(lookDirection.x, 0f, lookDirection.z).normalized;

        Vector3 newVelocity = rb.velocity;

        // Check for existing horizontal momentum or, if none, a new forward input
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        // Check if player's existing horizontal momentum is in the same direction as look direction
        float forwardDot = Vector3.Dot(horizontalVelocity.normalized, horizontalLookDirection);

        bool hasForwardMomentum = forwardDot > 0.1f && horizontalVelocity.magnitude > 0.1f;
        bool isPressingForward = moveInput.y > 0.1f; // W key is pressed

        if (hasForwardMomentum || isPressingForward)
        {
            // Smooth the changes in flight direction (creating a slight "delay" after look direction changes)
            flyDirection = Vector3.Slerp(flyDirection, horizontalLookDirection, Time.deltaTime / moveDirectionDelay);
            // While visually the transform's forward should be in look direction, the flyDirection should distinguish between xz and y
            transform.forward = Vector3.Slerp(transform.forward, lookDirection, Time.deltaTime / moveDirectionDelay);

            // Either maintain existing horizontal motion or set to flySpeed, whichever is higher
            float effectiveSpeed = Mathf.Max(horizontalVelocity.magnitude, flySpeed);

            // Decay the speed boost over time
            if (currentSpeedBoost > 0f)
            {
                currentSpeedBoost -= speedDecayRate * Time.deltaTime;
                currentSpeedBoost = Mathf.Max(0f, currentSpeedBoost); // Ensure it doesn't go below 0
                effectiveSpeed += currentSpeedBoost;
            }

            effectiveSpeed = Mathf.Clamp(effectiveSpeed, flySpeed, maxFlySpeed);

            newVelocity = flyDirection * effectiveSpeed;
        }

        /*
         * When gliding, rate of falling is slower and horizontal speed gradually grows faster.
         * If looking up, horizontal motion should significantly decrease.
         * If looking down, horizontal and downward vertical motion should increase.
         */
        if (isGliding)
        {
            float pitch = Vector3.Dot(lookDirection, Vector3.up);
            bool isClimbing = pitch > 0.0f;
            bool isDiving = pitch < 0.0f;
            float diveBoostMultiplier = Mathf.Clamp01(-pitch);
            float climbReductionMultiplier = Mathf.Clamp01(pitch);

            float targetHorizontalSpeed = maxGlideSpeed;
            float currentHorizontalSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

            //Adjust target horizontal speed based on pitch
            if (isDiving)
            {
                targetHorizontalSpeed += diveBoostMultiplier * 40.0f;
            }
            else if (isClimbing)
            {
                targetHorizontalSpeed -= climbReductionMultiplier * 40.0f;
            }

            // Smoothly transition to the target horizontal speed
            currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, targetHorizontalSpeed, Time.deltaTime * 0.5f); // Adjust 0.5f for speed increase rate
            newVelocity = flyDirection.normalized * currentHorizontalSpeed;

            // Simulate lift by applying upward force
            float liftForce = Mathf.Abs(Physics.gravity.y) / glideGravityScale;

            // Lift decreases proportionally to downward pitch (dive)
            if(isDiving)
            {
                liftForce *= 1.0f - diveBoostMultiplier;
            }
            // Lift increases proportionally to upward pitch (climb)
            else if(isClimbing)
            {
                liftForce *= 1.0f + climbReductionMultiplier * 0.5f;
            }

            newVelocity.y = rb.velocity.y + (Physics.gravity.y + liftForce) * Time.deltaTime * defaultGravityScale;

            // Ensure the falling speed does not exceed expected terminal velocity + is at least minDescentSpeed
            float terminalVelocity = -(rb.mass * Mathf.Abs(Physics.gravity.y)) / rb.drag;
            float minDescentSpeed = Mathf.Abs(terminalVelocity) * minFallSpeedMultiplier;
            newVelocity.y = Mathf.Clamp(newVelocity.y, terminalVelocity, -minDescentSpeed);
        }
        else
        {
            // Natural descent due to gravity
            newVelocity.y = rb.velocity.y + (Physics.gravity.y * Time.deltaTime) * defaultGravityScale;
        }

        rb.velocity = newVelocity;

        PerformRollRotation();

    }

    /// <summary>
    /// Rotate the player about its roll during gliding
    /// </summary>
    private void PerformRollRotation()
    {
        Vector3 horizontalForwardDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 horizontalLookDirection = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        float horizontalMovement = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

        // Angle between current fly direction and current look direction
        float angle = Vector3.SignedAngle(horizontalForwardDirection, horizontalLookDirection, Vector3.up);

        // Map angle to a roll value using rollIntensity
        float targetRoll = Mathf.Clamp(-angle * rollIntensity, -rollIntensity, rollIntensity); // Roll intensity scales with the angle

        // Smoothly interpolate towards the target roll if looking left or right, and if moving horizontally
        if (Mathf.Abs(angle) > 1f && horizontalMovement > 0.1f)
        {
            currentRoll = Mathf.LerpAngle(currentRoll, targetRoll, Time.deltaTime * rotationSmoothing);
        }
        else // Gradually decay the roll back to neutral
        {
            currentRoll = Mathf.LerpAngle(currentRoll, 0f, Time.deltaTime * rotationSmoothing);
        }

        // Add roll rotation around forward axis
        Quaternion rollRotation = Quaternion.AngleAxis(currentRoll, transform.forward); // Roll around the forward axis
        transform.rotation = Quaternion.Slerp(transform.rotation, rollRotation * transform.rotation, Time.deltaTime * rotationSmoothing);
    }

    private void ResetRollAndPitch()
    {
        currentRoll = 0.0f;
        // Keep current yaw (horizontal rotation)
        float currentYaw = transform.rotation.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
    }
}
