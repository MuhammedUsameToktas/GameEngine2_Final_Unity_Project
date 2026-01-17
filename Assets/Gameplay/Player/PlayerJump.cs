using UnityEngine;

/// <summary>
/// Platformer jump system with coyote time, jump buffering, variable jump height, and slope-aware jumping
/// Modifies vertical velocity only - does not handle horizontal movement or input reading
/// </summary>
[RequireComponent(typeof(PlayerGroundCheck))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float maxJumpHeight = 3f;
    
    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.15f;
    
    [Header("Jump Buffering")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    
    [Header("Variable Jump Height")]
    [SerializeField] private float jumpReleaseMultiplier = 0.5f;
    [SerializeField] private float minJumpHeight = 0.5f;
    
    [Header("Slope Jumping")]
    [SerializeField] private float maxJumpableSlope = 45f;
    
    [Header("Animation Events")]
    [SerializeField] private bool enableAnimationEvents = true;
    [SerializeField] private Animator animator;
    
    // Animator parameter hashes (optional - for direct animator control)
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    
    private PlayerGroundCheck groundCheck;
    private PlayerInputHandler input;
    private PlayerMovement movement;
    
    // Coyote time tracking
    private float timeSinceGrounded;
    private bool wasGrounded;
    
    // Jump buffering tracking
    private float timeSinceJumpPressed;
    private bool jumpWasPressed;
    
    // Variable jump height tracking
    private bool isJumping;
    private bool jumpInputHeld;
    private bool hasAppliedVariableHeight;
    
    // Animation events
    public System.Action OnJumpStarted;
    public System.Action OnJumpEnded;
    public System.Action OnJumpCanceled;
    
    // Public properties for animation/state queries
    public bool IsJumping => isJumping;
    public bool CanJump => CanPerformJump();
    
    private void Awake()
    {
        groundCheck = GetComponent<PlayerGroundCheck>();
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        if (groundCheck == null)
        {
            Debug.LogError("PlayerJump: PlayerGroundCheck component not found. Jump will not work.");
        }
        
        if (input == null)
        {
            Debug.LogError("PlayerJump: PlayerInputHandler component not found. Jump will not work.");
        }
        
        if (movement == null)
        {
            Debug.LogError("PlayerJump: PlayerMovement component not found. Jump will not work.");
        }
    }
    
    private void Update()
    {
        UpdateCoyoteTime();
        UpdateJumpBuffer();
        HandleJumpInput();
        HandleVariableJumpHeight();
        HandleLanding();
        UpdateAnimator();
    }
    
    /// <summary>
    /// Update animator parameters if animator is available
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetBool(IsJumpingHash, isJumping);
            if (groundCheck != null)
            {
                animator.SetBool(IsGroundedHash, groundCheck.IsGrounded);
            }
        }
    }
    
    /// <summary>
    /// Detect landing to end jump state
    /// </summary>
    private void HandleLanding()
    {
        if (groundCheck != null && groundCheck.JustLanded && isJumping)
        {
            OnLanded();
        }
    }
    
    /// <summary>
    /// Track coyote time - allows jumping slightly after leaving ground
    /// </summary>
    private void UpdateCoyoteTime()
    {
        bool currentlyGrounded = groundCheck != null && groundCheck.IsGrounded;
        
        if (currentlyGrounded)
        {
            timeSinceGrounded = 0f;
            wasGrounded = true;
        }
        else if (wasGrounded)
        {
            timeSinceGrounded += Time.deltaTime;
        }
        
        // Reset coyote time if we've been in air too long
        if (timeSinceGrounded > coyoteTime)
        {
            wasGrounded = false;
        }
    }
    
    /// <summary>
    /// Track jump buffer - allows jumping slightly before landing
    /// </summary>
    private void UpdateJumpBuffer()
    {
        // Clear jump buffer when game is paused to prevent jump after resuming
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
        {
            jumpWasPressed = false;
            timeSinceJumpPressed = 0f;
            return;
        }

        bool jumpPressed = input != null && input.JumpPressed;
        
        if (jumpPressed)
        {
            timeSinceJumpPressed = 0f;
            jumpWasPressed = true;
        }
        else if (jumpWasPressed)
        {
            timeSinceJumpPressed += Time.deltaTime;
        }
        
        // Clear buffer if too much time has passed
        if (timeSinceJumpPressed > jumpBufferTime)
        {
            jumpWasPressed = false;
        }
    }
    
    /// <summary>
    /// Handle jump input and perform jump if conditions are met
    /// </summary>
    private void HandleJumpInput()
    {
        if (input == null || movement == null || groundCheck == null)
            return;
        
        jumpInputHeld = input.JumpHeld;
        
        // Check if we can jump (grounded + coyote time OR jump buffer active)
        bool canJumpNow = CanPerformJump();
        
        // Try to jump if conditions are met
        if (canJumpNow && (jumpWasPressed || input.JumpPressed))
        {
            PerformJump();
        }
    }
    
    /// <summary>
    /// Check if jump can be performed (coyote time OR jump buffer)
    /// </summary>
    private bool CanPerformJump()
    {
        if (groundCheck == null)
            return false;
        
        // Check if we're grounded (with coyote time)
        bool inCoyoteTime = wasGrounded && timeSinceGrounded <= coyoteTime;
        bool currentlyGrounded = groundCheck.IsGrounded;
        
        // Check slope angle if grounded
        if (currentlyGrounded || inCoyoteTime)
        {
            // Check if slope is jumpable
            float slopeAngle = Vector3.Angle(Vector3.up, groundCheck.GroundNormal);
            if (slopeAngle > maxJumpableSlope)
            {
                return false; // Too steep to jump
            }
        }
        
        // Can jump if grounded (with coyote time) OR if jump buffer is active
        return (currentlyGrounded || inCoyoteTime) && !isJumping;
    }
    
    /// <summary>
    /// Perform the jump by modifying vertical velocity
    /// </summary>
    private void PerformJump()
    {
        if (movement == null)
            return;
        
        // Calculate jump velocity based on desired jump height
        // Using physics: v = sqrt(2 * g * h) where g is positive gravity magnitude
        float gravityMagnitude = Mathf.Abs(movement.Gravity);
        float jumpVelocity = Mathf.Sqrt(2f * gravityMagnitude * maxJumpHeight);
        
        // Apply jump velocity
        movement.SetVerticalVelocity(jumpVelocity);
        
        // Set jumping state
        isJumping = true;
        hasAppliedVariableHeight = false;
        jumpWasPressed = false; // Consume the buffered jump
        
        // Fire animation event
        if (enableAnimationEvents)
        {
            OnJumpStarted?.Invoke();
        }
        
        // Play jump sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Jump");
        }
        
        Debug.Log($"Jump performed! Velocity: {jumpVelocity}");
    }
    
    /// <summary>
    /// Handle variable jump height - reduce velocity if jump button released early
    /// </summary>
    private void HandleVariableJumpHeight()
    {
        if (!isJumping || movement == null)
            return;
        
        // Check if we're still ascending
        bool isAscending = movement.VerticalVelocity > 0f;
        
        if (!isAscending)
        {
            // We've reached peak or started falling
            EndJump();
            return;
        }
        
        // If jump button is released while ascending, reduce velocity for variable height
        // Only apply once per jump to prevent continuous reduction
        if (!jumpInputHeld && isAscending && !hasAppliedVariableHeight)
        {
            float currentVelocity = movement.VerticalVelocity;
            float reducedVelocity = currentVelocity * jumpReleaseMultiplier;
            
            // Only reduce if it's above minimum jump height threshold
            // Calculate current height from velocity (approximate)
            float currentHeight = (currentVelocity * currentVelocity) / (2f * Mathf.Abs(movement.Gravity));
            float minHeight = minJumpHeight;
            
            if (currentHeight >= minHeight)
            {
                movement.SetVerticalVelocity(reducedVelocity);
                hasAppliedVariableHeight = true;
            }
        }
    }
    
    /// <summary>
    /// End the jump state (called when falling or landing)
    /// </summary>
    private void EndJump()
    {
        if (!isJumping)
            return;
        
        isJumping = false;
        
        // Fire animation event
        if (enableAnimationEvents)
        {
            OnJumpEnded?.Invoke();
        }
    }
    
    /// <summary>
    /// Called by PlayerGroundCheck or external systems when landing
    /// </summary>
    public void OnLanded()
    {
        EndJump();
        wasGrounded = true;
        timeSinceGrounded = 0f;
    }
    
    /// <summary>
    /// Cancel jump (for external systems like wall jumps, double jumps, etc.)
    /// </summary>
    public void CancelJump()
    {
        if (isJumping)
        {
            isJumping = false;
            
            if (enableAnimationEvents)
            {
                OnJumpCanceled?.Invoke();
            }
        }
    }
}
