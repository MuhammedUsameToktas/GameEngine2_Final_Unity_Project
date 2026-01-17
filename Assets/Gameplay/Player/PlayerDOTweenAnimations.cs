using UnityEngine;
using DG.Tweening;

/// <summary>
/// Adds DOTween visual animations to the player (scale, position, rotation)
/// Works alongside Animator animations for enhanced visual feedback
/// Handles jump, fall, land, and run animations
/// </summary>
public class PlayerDOTweenAnimations : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform targetTransform; // The transform to animate (usually the visual/model, not the root)
    
    [Header("Jump Animation")]
    [SerializeField] private bool enableJumpAnimation = true;
    [SerializeField] private float jumpScaleAmount = 1.15f;
    [SerializeField] private float jumpScaleDuration = 0.2f;
    [SerializeField] private Ease jumpScaleEase = Ease.OutQuad;
    
    [Header("Fall Animation")]
    [SerializeField] private bool enableFallAnimation = true;
    [SerializeField] private float fallSquashAmount = 0.95f;
    [SerializeField] private float fallSquashDuration = 0.3f;
    [SerializeField] private Ease fallSquashEase = Ease.InQuad;
    
    [Header("Land Animation")]
    [SerializeField] private bool enableLandAnimation = true;
    [SerializeField] private float landSquashAmount = 0.85f;
    [SerializeField] private float landSquashDuration = 0.1f;
    [SerializeField] private float landBounceBackDuration = 0.15f;
    [SerializeField] private Ease landSquashEase = Ease.OutQuad;
    [SerializeField] private Ease landBounceEase = Ease.OutBack;
    
    [Header("Run Animation")]
    [SerializeField] private bool enableRunAnimation = true;
    [SerializeField] private float runBobAmount = 0.1f;
    [SerializeField] private float runBobSpeed = 10f;
    [SerializeField] private Ease runBobEase = Ease.InOutSine;
    
    [Header("Turn Lean Animation")]
    [SerializeField] private bool enableTurnLeanAnimation = true;
    [SerializeField] private float maxLeanAngle = 15f; // Maximum lean angle in degrees
    [SerializeField] private float leanDuration = 0.2f; // How fast the lean animates
    [SerializeField] private Ease leanEase = Ease.OutQuad;
    [SerializeField] private LeanAxis leanAxis = LeanAxis.Z; // Which axis to rotate around (Z for top-down, X for side-view)
    
    public enum LeanAxis
    {
        X, // Rotate around X-axis (for side-view games)
        Z  // Rotate around Z-axis (for top-down games)
    }
    
    private PlayerJump playerJump;
    private PlayerMovement playerMovement;
    private PlayerGroundCheck groundCheck;
    private PlayerInputHandler inputHandler;
    private Transform cameraTransform;
    
    private Vector3 originalScale;
    private Vector3 originalLocalPosition;
    
    // Active tweens
    private Tween jumpTween;
    private Tween fallTween;
    private Tween landTween;
    private Tween runTween;
    private Tween leanTween;
    
    // State tracking
    private bool wasGrounded;
    private bool isFalling;
    private bool wasRunning;
    private float currentLeanAngle = 0f;
    private Vector3 originalLocalRotation;
    
    private void Awake()
    {
        playerJump = GetComponent<PlayerJump>();
        playerMovement = GetComponent<PlayerMovement>();
        groundCheck = GetComponent<PlayerGroundCheck>();
        inputHandler = GetComponent<PlayerInputHandler>();
        
        // Auto-find camera if not assigned (same logic as PlayerMovement)
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        // Auto-find target transform if not assigned (use child with renderer or self)
        if (targetTransform == null)
        {
            // Try to find a child with a renderer
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                targetTransform = renderer.transform;
            }
            else
            {
                // Fallback to self
                targetTransform = transform;
            }
        }
        
        // Store original values
        originalScale = targetTransform.localScale;
        originalLocalPosition = targetTransform.localPosition;
        originalLocalRotation = targetTransform.localEulerAngles;
    }
    
    private void OnEnable()
    {
        // Subscribe to jump events
        if (playerJump != null)
        {
            playerJump.OnJumpStarted += OnJumpStarted;
            playerJump.OnJumpEnded += OnJumpEnded;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        if (playerJump != null)
        {
            playerJump.OnJumpStarted -= OnJumpStarted;
            playerJump.OnJumpEnded -= OnJumpEnded;
        }
        
        // Kill all active tweens
        KillAllTweens();
    }
    
    private void Start()
    {
        wasGrounded = groundCheck != null && groundCheck.IsGrounded;
    }
    
    private void Update()
    {
        if (groundCheck == null || playerMovement == null)
            return;
        
        // Track falling state
        bool currentlyGrounded = groundCheck.IsGrounded;
        bool currentlyFalling = !currentlyGrounded && playerMovement.VerticalVelocity < -0.5f;
        
        // Handle fall animation
        if (enableFallAnimation)
        {
            if (currentlyFalling && !isFalling)
            {
                StartFallAnimation();
            }
            else if (!currentlyFalling && isFalling)
            {
                StopFallAnimation();
            }
        }
        
        // Handle run animation (only when grounded and moving)
        if (enableRunAnimation)
        {
            bool currentlyRunning = currentlyGrounded && playerMovement.IsMoving() && 
                                    (playerJump == null || !playerJump.IsJumping);
            
            if (currentlyRunning && !wasRunning)
            {
                StartRunAnimation();
            }
            else if (!currentlyRunning && wasRunning)
            {
                StopRunAnimation();
            }
            
            wasRunning = currentlyRunning;
        }
        
        // Detect landing for land animation
        if (enableLandAnimation && groundCheck.JustLanded)
        {
            OnLanded();
        }
        
        // Handle turn lean animation
        if (enableTurnLeanAnimation && inputHandler != null && playerMovement != null)
        {
            HandleTurnLeanAnimation();
        }
        
        wasGrounded = currentlyGrounded;
    }
    
    /// <summary>
    /// Called when jump starts
    /// </summary>
    private void OnJumpStarted()
    {
        if (!enableJumpAnimation)
            return;
        
        // Kill any existing jump tween
        if (jumpTween != null && jumpTween.IsActive())
        {
            jumpTween.Kill();
        }
        
        // Stop fall animation if active
        StopFallAnimation();
        
        // Stop run animation if active
        StopRunAnimation();
        
        // Animate scale up for jump
        jumpTween = targetTransform.DOScale(
            originalScale * jumpScaleAmount,
            jumpScaleDuration
        )
        .SetEase(jumpScaleEase)
        .OnComplete(() =>
        {
            // Return to original scale
            targetTransform.DOScale(originalScale, jumpScaleDuration * 0.5f)
                .SetEase(Ease.InQuad);
        });
    }
    
    /// <summary>
    /// Called when jump ends (reached peak or started falling)
    /// </summary>
    private void OnJumpEnded()
    {
        // Jump animation will naturally complete, no special handling needed
    }
    
    /// <summary>
    /// Called when player lands
    /// </summary>
    private void OnLanded()
    {
        if (!enableLandAnimation)
            return;
        
        // Kill any existing land tween
        if (landTween != null && landTween.IsActive())
        {
            landTween.Kill();
        }
        
        // Stop fall animation
        StopFallAnimation();
        
        // Create landing sequence: squash down then bounce back
        Sequence landSequence = DOTween.Sequence();
        
        // Squash down
        landSequence.Append(
            targetTransform.DOScale(
                new Vector3(originalScale.x * landSquashAmount, originalScale.y * landSquashAmount, originalScale.z * landSquashAmount),
                landSquashDuration
            )
            .SetEase(landSquashEase)
        );
        
        // Bounce back (slightly overshoot then settle)
        landSequence.Append(
            targetTransform.DOScale(originalScale, landBounceBackDuration)
            .SetEase(landBounceEase)
        );
        
        landTween = landSequence;
    }
    
    /// <summary>
    /// Start fall animation (squash effect while falling)
    /// </summary>
    private void StartFallAnimation()
    {
        isFalling = true;
        
        // Kill existing fall tween
        if (fallTween != null && fallTween.IsActive())
        {
            fallTween.Kill();
        }
        
        // Animate to fall squash
        fallTween = targetTransform.DOScale(
            new Vector3(originalScale.x * fallSquashAmount, originalScale.y * fallSquashAmount, originalScale.z * fallSquashAmount),
            fallSquashDuration
        )
        .SetEase(fallSquashEase);
    }
    
    /// <summary>
    /// Stop fall animation (return to normal scale)
    /// </summary>
    private void StopFallAnimation()
    {
        isFalling = false;
        
        if (fallTween != null && fallTween.IsActive())
        {
            fallTween.Kill();
        }
        
        // Return to original scale smoothly
        targetTransform.DOScale(originalScale, fallSquashDuration * 0.5f)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// Start run animation (vertical bob)
    /// </summary>
    private void StartRunAnimation()
    {
        // Kill existing run tween
        if (runTween != null && runTween.IsActive())
        {
            runTween.Kill();
        }
        
        // Create looping vertical bob animation
        Vector3 bobPosition = originalLocalPosition + Vector3.up * runBobAmount;
        
        runTween = targetTransform.DOLocalMoveY(
            bobPosition.y,
            1f / runBobSpeed
        )
        .SetEase(runBobEase)
        .SetLoops(-1, LoopType.Yoyo);
    }
    
    /// <summary>
    /// Stop run animation (return to original position)
    /// </summary>
    private void StopRunAnimation()
    {
        if (runTween != null && runTween.IsActive())
        {
            runTween.Kill();
        }
        
        // Return to original position
        targetTransform.DOLocalMove(originalLocalPosition, 0.2f)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// Handle turn lean animation based on player turning relative to camera forward
    /// </summary>
    private void HandleTurnLeanAnimation()
    {
        if (cameraTransform == null || !playerMovement.IsMoving())
        {
            // No camera or not moving - return to neutral
            if (Mathf.Abs(currentLeanAngle) > 0.1f)
            {
                ApplyLeanAnimation(0f);
            }
            return;
        }
        
        Vector2 moveInput = inputHandler.MoveInput;
        Vector3 inputVector = new Vector3(moveInput.x, 0, moveInput.y);
        
        // Calculate movement direction relative to camera (same logic as PlayerMovement)
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        
        Vector3 moveDirection = (camForward * inputVector.z + camRight * inputVector.x).normalized;
        
        // Get player's current forward direction (flattened to horizontal plane)
        Vector3 playerForward = transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();
        
        // Calculate the signed angle between player forward and movement direction
        // Positive angle = turning right, Negative angle = turning left
        float turnAngle = Vector3.SignedAngle(playerForward, moveDirection, Vector3.up);
        
        // Calculate target lean angle based on turn angle
        // Clamp to maxLeanAngle and scale proportionally
        float targetLeanAngle = 0f;
        
        if (Mathf.Abs(turnAngle) > 1f) // Only lean if there's a meaningful turn
        {
            // Normalize turn angle to -1 to 1 range (using max turn angle of 90 degrees as reference)
            float normalizedTurn = Mathf.Clamp(turnAngle / 90f, -1f, 1f);
            // Apply lean proportional to turn, clamped to maxLeanAngle
            targetLeanAngle = normalizedTurn * maxLeanAngle;
        }
        
        // Only animate if the target angle is different from current
        if (Mathf.Abs(targetLeanAngle - currentLeanAngle) > 0.1f)
        {
            ApplyLeanAnimation(targetLeanAngle);
        }
    }
    
    /// <summary>
    /// Apply lean animation using DOTween
    /// </summary>
    private void ApplyLeanAnimation(float targetAngle)
    {
        // Kill existing lean tween
        if (leanTween != null && leanTween.IsActive())
        {
            leanTween.Kill();
        }
        
        // Get current rotation
        Vector3 currentRotation = targetTransform.localEulerAngles;
        
        // Create target rotation based on selected axis
        Vector3 targetRotation = originalLocalRotation;
        
        if (leanAxis == LeanAxis.Z)
        {
            // Rotate around Z-axis (for top-down view)
            targetRotation.z = originalLocalRotation.z + targetAngle;
        }
        else // LeanAxis.X
        {
            // Rotate around X-axis (for side view)
            targetRotation.x = originalLocalRotation.x + targetAngle;
        }
        
        // Update current lean angle to target
        currentLeanAngle = targetAngle;
        
        // Animate to target rotation
        leanTween = targetTransform.DOLocalRotate(
            targetRotation,
            leanDuration
        )
        .SetEase(leanEase);
    }
    
    /// <summary>
    /// Kill all active tweens
    /// </summary>
    private void KillAllTweens()
    {
        if (jumpTween != null && jumpTween.IsActive())
            jumpTween.Kill();
        
        if (fallTween != null && fallTween.IsActive())
            fallTween.Kill();
        
        if (landTween != null && landTween.IsActive())
            landTween.Kill();
        
        if (runTween != null && runTween.IsActive())
            runTween.Kill();
        
        if (leanTween != null && leanTween.IsActive())
            leanTween.Kill();
        
        // Reset to original values
        if (targetTransform != null)
        {
            targetTransform.localScale = originalScale;
            targetTransform.localPosition = originalLocalPosition;
            targetTransform.localEulerAngles = originalLocalRotation;
        }
        
        currentLeanAngle = 0f;
    }
    
    private void OnDestroy()
    {
        KillAllTweens();
    }
}
