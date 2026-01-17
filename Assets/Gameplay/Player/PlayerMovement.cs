using UnityEngine;

/// <summary>
/// Smooth velocity-based player movement with acceleration/deceleration
/// Uses CharacterController for physics-free movement
/// Movement is relative to camera direction
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 8f;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 18f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private PlayerInputHandler input;
    private PlayerGroundCheck groundCheck;
    private PlayerSwing playerSwing;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float currentSpeed;
    private Vector3 knockbackVelocity;
    private float knockbackDeceleration = 15f;
    
    // Run sound tracking
    private bool wasMoving = false;
    private float runSoundCooldown = 0f;
    private const float runSoundInterval = 0.3f; // Play run sound every 0.3 seconds while running
    
    // Public properties for jump system
    public float VerticalVelocity => verticalVelocity;
    public float Gravity => gravity;
    
    // Public property to expose camera transform
    public Transform CameraTransform => cameraTransform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();
        groundCheck = GetComponent<PlayerGroundCheck>();
        playerSwing = GetComponent<PlayerSwing>();

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("PlayerMovement: No camera assigned and Camera.main is null. Movement will use world space.");
            }
        }

        // Warn if ground check is missing
        if (groundCheck == null)
        {
            Debug.LogWarning("PlayerMovement: PlayerGroundCheck component not found. Ground detection will use CharacterController.isGrounded (less reliable).");
        }
    }

    private void Update()
    {
        // Don't apply normal movement/gravity if swinging
        if (playerSwing != null && playerSwing.IsSwinging)
        {
            // Swinging handles its own movement
            HandleRunSound(false); // Stop run sound when swinging
            return;
        }
        
        HandleMovement();
        ApplyGravity();
        MoveCharacter();
        HandleRunSound(IsMoving() && groundCheck != null && groundCheck.IsGrounded);
    }

    private void HandleMovement()
    {
        Vector2 inputDir = input.MoveInput;
        Vector3 inputVector = new Vector3(inputDir.x, 0, inputDir.y);

        // Camera-relative movement
        Vector3 moveDirection = Vector3.zero;
        
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * inputVector.z + camRight * inputVector.x).normalized;
        }
        else
        {
            // Fallback to world space if no camera
            moveDirection = inputVector.normalized;
        }

        // Calculate target speed based on input magnitude
        float targetSpeed = inputVector.magnitude > 0.1f ? runSpeed : 0f;
        float accelRate = targetSpeed > currentSpeed ? acceleration : deceleration;

        // Smoothly interpolate current speed toward target
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            accelRate * Time.deltaTime
        );

        horizontalVelocity = moveDirection * currentSpeed;

        // Rotate character toward movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void ApplyGravity()
    {
        // Use reliable ground check if available, fallback to CharacterController
        bool isGrounded = groundCheck != null ? groundCheck.IsGrounded : controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void MoveCharacter()
    {
        // Apply knockback deceleration
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDeceleration * Time.deltaTime);
        
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity + knockbackVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Apply knockback force to the player
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0; // Keep knockback horizontal
        direction.Normalize();
        knockbackVelocity = direction * force;
    }

    /// <summary>
    /// Returns normalized speed (0-1) for animator blending
    /// </summary>
    public float GetNormalizedSpeed()
    {
        return currentSpeed / runSpeed;
    }

    /// <summary>
    /// Returns whether the player is currently moving
    /// </summary>
    public bool IsMoving()
    {
        return currentSpeed > 0.1f;
    }
    
    /// <summary>
    /// Set vertical velocity (used by PlayerJump to apply jump force)
    /// </summary>
    public void SetVerticalVelocity(float velocity)
    {
        verticalVelocity = velocity;
    }

    /// <summary>
    /// Handle run sound - plays periodically while running
    /// </summary>
    private void HandleRunSound(bool isMoving)
    {
        if (isMoving && !wasMoving)
        {
            // Just started moving - play run sound immediately
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Run", volume: 0.6f);
            }
            runSoundCooldown = runSoundInterval;
        }
        else if (isMoving && wasMoving)
        {
            // Still moving - play run sound periodically
            runSoundCooldown -= Time.deltaTime;
            if (runSoundCooldown <= 0f)
            {
                if (AudioManager.Instance != null)
                {
                    // Add slight pitch variation to avoid repetition
                    float pitch = Random.Range(0.95f, 1.05f);
                    AudioManager.Instance.PlaySFX("Run", volume: 0.6f, pitch: pitch);
                }
                runSoundCooldown = runSoundInterval;
            }
        }
        
        wasMoving = isMoving;
    }
}
