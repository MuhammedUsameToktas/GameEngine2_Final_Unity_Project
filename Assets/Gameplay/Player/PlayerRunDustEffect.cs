using UnityEngine;

/// <summary>
/// Controls the run dust particle effect
/// Simple approach: Plays when player is grounded and moving
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerRunDustEffect : MonoBehaviour
{
    [Header("Particle System")]
    [SerializeField] private ParticleSystem dustParticleSystem;
    
    [Header("Settings")]
    [SerializeField] private bool enableDustEffect = true;
    [SerializeField] private float minSpeedToEmit = 0.1f; // Minimum speed before particles emit
    
    private PlayerGroundCheck groundCheck;
    private PlayerMovement playerMovement;
    private PlayerInputHandler inputHandler;
    private CharacterController controller;
    
    private bool wasGroundedLastFrame;
    
    private void Awake()
    {
        // Auto-find required components
        controller = GetComponent<CharacterController>();
        groundCheck = GetComponent<PlayerGroundCheck>();
        playerMovement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();
        
        // Auto-find particle system if not assigned
        if (dustParticleSystem == null)
        {
            dustParticleSystem = GetComponentInChildren<ParticleSystem>();
            
            if (dustParticleSystem == null)
            {
                Debug.LogWarning("PlayerRunDustEffect: No ParticleSystem assigned or found in children. Please assign one in the Inspector.");
            }
        }
    }
    
    private void Start()
    {
        // Ensure particle system starts stopped
        if (dustParticleSystem != null)
        {
            var main = dustParticleSystem.main;
            main.playOnAwake = false;
            
            if (dustParticleSystem.isPlaying)
            {
                dustParticleSystem.Stop();
            }
        }
        
        wasGroundedLastFrame = IsGrounded();
    }
    
    private void Update()
    {
        if (!enableDustEffect || dustParticleSystem == null)
            return;
        
        if (playerMovement == null)
            return;
        
        // Check if player is grounded (use multiple methods for reliability)
        bool isGrounded = IsGrounded();
        bool justLanded = groundCheck != null && groundCheck.JustLanded;
        
        // Check if player is moving
        bool isMoving = playerMovement.IsMoving();
        bool hasMovementInput = inputHandler != null && inputHandler.MoveInput.magnitude > 0.1f;
        float normalizedSpeed = playerMovement.GetNormalizedSpeed();
        bool hasEnoughSpeed = normalizedSpeed >= minSpeedToEmit;
        
        // Determine if player should be considered "moving" for particle purposes
        // After landing, check input directly to catch movement immediately
        bool shouldConsiderMoving = isMoving && hasEnoughSpeed;
        if (justLanded || (!wasGroundedLastFrame && isGrounded))
        {
            // Right after landing, also accept input as movement indicator
            // This ensures particles start immediately when landing while running
            shouldConsiderMoving = (isMoving && hasEnoughSpeed) || hasMovementInput;
        }
        
        // Should play particles: grounded AND moving
        bool shouldPlay = isGrounded && shouldConsiderMoving;
        
        // Force particle system to match desired state every frame
        // This ensures it always reflects current conditions, regardless of previous state
        if (shouldPlay)
        {
            if (!dustParticleSystem.isPlaying)
            {
                dustParticleSystem.Play();
            }
        }
        else
        {
            if (dustParticleSystem.isPlaying)
            {
                dustParticleSystem.Stop();
            }
        }
        
        // Update frame tracking
        wasGroundedLastFrame = isGrounded;
    }
    
    /// <summary>
    /// Check if player is grounded using multiple detection methods for reliability
    /// </summary>
    private bool IsGrounded()
    {
        // Primary check: Use PlayerGroundCheck if available (most reliable)
        if (groundCheck != null)
        {
            if (groundCheck.IsGrounded)
                return true;
        }
        
        // Fallback: Use CharacterController.isGrounded
        if (controller != null && controller.isGrounded)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Enable or disable the dust effect
    /// </summary>
    public void SetDustEffectEnabled(bool enabled)
    {
        enableDustEffect = enabled;
        
        if (!enabled && dustParticleSystem != null && dustParticleSystem.isPlaying)
        {
            dustParticleSystem.Stop();
        }
    }
}
