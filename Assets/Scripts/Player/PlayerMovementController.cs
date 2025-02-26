using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementController : MonoBehaviour
{
    public Transform cameraTransform;
    public bool IsFlying { get { return isFlying; } set { isFlying = value; } }

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float minFlySpeed = 5f;
    [SerializeField] private float maxFlySpeed = 20f;
    [SerializeField] private float maxGlideSpeed = 35f;
    [SerializeField] private float maxDiveSpeed = 50f;
    [SerializeField] private float flapForce = 12f;
    [SerializeField] private float jumpForce = 16f; // Force applied when jumping off the ground
    [SerializeField] private float flapThreshold = 0.3f; // Seconds to hold Lift for a flap
    [SerializeField] private float flapCooldown = 0.5f;
    [SerializeField] private float defaultGravityScale = 2f;
    [SerializeField] private float glideGravityScale = 0.5f; // Scale to counteract gravity when gliding
    [SerializeField] private float minFallSpeedMultiplier = 0.2f; // Fraction of terminal velocity to define min fall speed when gliding
    [SerializeField] private float dragFactor = 0.99f; // Amount to reduce horizontal speed during gliding each frame

    [Header("Flapping Speed Boost Settings")]
    [SerializeField] private float flapSpeedBoost = 1f;
    [SerializeField] private float speedDecayRate = 5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private float groundCheckRadius = 0.3f;

    [Header("Visual Effects")]
    [SerializeField] private float moveDirectionDelay = 0.2f; // Amount to delay player transform after directional change
    [SerializeField] private float rollIntensity = 45f; // Degrees to roll when moving left/right during flight
    [SerializeField] private float rotationSmoothing = 10f; // Smoothing speed for rotation

    float currentRoll;

    private float currentSpeedBoost = 0f;

    private float spacePressTime = 0f; // Track time Lift input has been held

    private float targetHorizontalSpeed; // Target speed during gliding
    private float pitchSpeedFactor = 5f; // Amount to adjust speed each frame when diving/climbing

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
            Debug.LogError("Input handler not found on player!");
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
        if (collider == null) return;

        Vector3 origin = collider.bounds.center;
        origin.y = collider.bounds.min.y;

        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayer) ||
            Physics.CheckSphere(origin, groundCheckRadius, walkableLayer);

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
                spacePressTime = Time.time;
            }

            // Check if input exceeds the flap threshold for gliding
            if (!isGliding && !isGrounded && (Time.time - spacePressTime) > flapThreshold)
            {
                targetHorizontalSpeed = maxGlideSpeed;
                isGliding = true;
                animator.SetBool("IsGliding", true);
            }
        }

        // Handle LiftInput release
        if (!inputHandler.LiftInput && spacePressTime > 0f)
        {
            float pressDuration = Time.time - spacePressTime;
            spacePressTime = 0f;

            if (isGrounded)
            {
                isFlying = true;
                Flap(true, new Vector3(rb.velocity.x, 0f, rb.velocity.z));
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
    }

    private IEnumerator ResetFlapCooldown()
    {
        canFlap = false;
        yield return new WaitForSeconds(flapCooldown);
        canFlap = true;
    }

    private void HandleWalking()
    {
        Vector2 moveInput = inputHandler.MoveInput;

        if (Mathf.Abs(moveInput.x) < 0.1f && Mathf.Abs(moveInput.y) < 0.1f)
        {
            return;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        flyDirection = moveDirection;

        rb.velocity = new Vector3(moveDirection.x * walkSpeed, rb.velocity.y, moveDirection.z * walkSpeed);

        // Smoothy rotate the player in the input direction
        if (moveDirection.magnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
        }
    }

    /// <summary>
    /// Flap wings once, exerting an upwards impulse onto the player.
    /// If jumping off the ground, provide input true and apply jumpForce instead of flapForce. Also input the ground
    /// horizontal velocity upon jumping.
    /// If not jumping off the ground, applies a temporary speed boost.
    /// </summary>
    /// <param name="isJump"></param>
    /// <param name="groundVelocity"></param>
    private void Flap(bool isJump, Vector3? groundVelocity = null)
    {
        if (!canFlap) return;

        // Reset vertical velocity to ensure consistent upward force
        Vector3 horizontalVelocity = groundVelocity ?? new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.velocity = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);

        if (isJump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else
        {
            animator.SetTrigger("Flap");
            rb.AddForce(Vector3.up * flapForce, ForceMode.Impulse);

            Vector2 moveInput = inputHandler.MoveInput;
            // If player is already moving horizontally while in the air or flaps with forward move input, apply forward speed boost
            if (new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude > 0f || moveInput.y > 0.1f)
            {
                currentSpeedBoost = flapSpeedBoost;
            }
            StartCoroutine(ResetFlapCooldown());
        }
    }

    /// <summary>
    /// While the player is in the air, determine horizontal and vertical velocities depending on whether gliding or not.
    /// Player can initiate forward motion with forward move input and can steer via look direction
    /// </summary>
    private void ApplyFlyingPhysics()
    {
        Vector2 moveInput = inputHandler.MoveInput;
        Vector3 lookDirection = cameraTransform.forward.normalized;
        // Have separate logic between xz (horizontal) movement and y movement
        Vector3 horizontalLookDirection = new Vector3(lookDirection.x, 0f, lookDirection.z).normalized;
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        Vector3 newVelocity = rb.velocity;

        // Ensure player's existing horizontal momentum is in the same direction as look direction
        bool hasForwardMomentum = Vector3.Dot(horizontalVelocity.normalized, horizontalLookDirection) > 0.1f && horizontalVelocity.magnitude > 0.1f;
        bool isPressingForward = moveInput.y > 0.1f; // W key is pressed

        if (hasForwardMomentum || isPressingForward)
        {
            // Smooth the changes in flight direction
            flyDirection = Vector3.Slerp(flyDirection, horizontalLookDirection, Time.deltaTime / moveDirectionDelay);
            // While visually the transform's forward should be in look direction, the flyDirection should distinguish between xz and y
            transform.forward = Vector3.Slerp(transform.forward, lookDirection, Time.deltaTime / moveDirectionDelay);

            // Either maintain existing horizontal motion or set to minFlySpeed, whichever is higher
            float effectiveSpeed = Mathf.Max(horizontalVelocity.magnitude, minFlySpeed);

            if (currentSpeedBoost > 0f)
            {
                currentSpeedBoost -= speedDecayRate * Time.deltaTime;
                currentSpeedBoost = Mathf.Max(0f, currentSpeedBoost);
                effectiveSpeed += currentSpeedBoost;
            }

            effectiveSpeed = Mathf.Clamp(effectiveSpeed, minFlySpeed, maxFlySpeed);

            newVelocity = flyDirection * effectiveSpeed;
        }
        if (isGliding)
        {
            AdjustGlidingPhysics(ref newVelocity, lookDirection, horizontalVelocity.magnitude);
            
        }
        else
        {
            // Natural descent due to gravity
            newVelocity.y = rb.velocity.y + Physics.gravity.y * Time.deltaTime * defaultGravityScale;
        }

        rb.velocity = newVelocity;

        PerformRollRotation();

    }

    /// <summary>
    /// Adjusts velocity and lift forces during gliding.
    /// When gliding, rate of falling is slower and horizontal speed gradually grows faster.
    /// If looking up, horizontal motion significantly decreases (Climb).
    /// If looking down, horizontal and downward vertical motion increases (Dive).
    /// </summary>
    /// <param name="velocity"></param>
    /// <param name="lookDirection"></param>
    /// <param name="horizontalSpeed"></param>
    private void AdjustGlidingPhysics(ref Vector3 velocity, Vector3 lookDirection, float horizontalSpeed)
    {
        float pitch = Vector3.Dot(lookDirection, Vector3.up);
        bool isClimbing = pitch > 0.2f;
        bool isDiving = pitch < 0.2f;
        float pitchFactor = Mathf.Clamp01(Mathf.Abs(pitch));

        //Adjust target horizontal speed based on pitch
        targetHorizontalSpeed = Mathf.Clamp(targetHorizontalSpeed * dragFactor + (isDiving ? pitchFactor * pitchSpeedFactor : -pitchFactor * pitchSpeedFactor), minFlySpeed, maxDiveSpeed);

        // Smoothly transition to the target horizontal speed
        float smoothedHorizontalSpeed = Mathf.Lerp(horizontalSpeed, targetHorizontalSpeed, Time.deltaTime * 0.5f); // Adjust 0.5f for speed increase rate
        velocity = flyDirection.normalized * smoothedHorizontalSpeed;

        // Simulate lift by applying upward force
        // Lift decreases/increases proportionally to pitch
        float liftForce = Mathf.Abs(Physics.gravity.y) / glideGravityScale * (1f + (isClimbing ? pitchFactor * 0.5f : -pitchFactor));
        velocity.y = rb.velocity.y + (Physics.gravity.y + liftForce) * Time.deltaTime * defaultGravityScale;

        // Ensure the falling speed does not exceed expected terminal velocity + is at least minDescentSpeed
        float terminalVelocity = -(rb.mass * Mathf.Abs(Physics.gravity.y)) / rb.drag;
        float minDescentSpeed = Mathf.Abs(terminalVelocity) * minFallSpeedMultiplier;
        velocity.y = Mathf.Clamp(velocity.y, terminalVelocity, -minDescentSpeed);
    }

    /// <summary>
    /// Rotate the player about its roll during gliding
    /// </summary>
    private void PerformRollRotation()
    {
        Vector3 horizontalForwardDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 horizontalLookDirection = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        float horizontalSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

        // Angle (on xz plane) between current fly direction and current look direction
        float angle = Vector3.SignedAngle(horizontalForwardDirection, horizontalLookDirection, Vector3.up);

        float targetRoll; 

        // Smoothly interpolate towards the target roll if looking left or right, and if moving horizontally enough
        if (Mathf.Abs(angle) > 1f && horizontalSpeed > 1f)
        {
            // Map angle to a roll value using rollIntensity
            targetRoll = Mathf.Clamp(-angle * rollIntensity, -rollIntensity, rollIntensity);
        }
        else // Gradually decay the roll back to neutral
        {
            targetRoll = 0f;
        }
        currentRoll = Mathf.LerpAngle(currentRoll, targetRoll, Time.deltaTime * rotationSmoothing);

        // Smoothly add roll rotation
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
