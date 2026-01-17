using UnityEngine;

/// <summary>
/// Spider-Man style swinging mechanic
/// Press X while in air to attach to nearest swing point, hold X to continue swinging
/// Release X or touch ground to detach
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    [Tooltip("Maximum distance to detect swing points")]
    [SerializeField] private float maxDetectionDistance = 20f;
    
    [Tooltip("Force applied when swinging (affects swing speed)")]
    [SerializeField] private float swingForce = 15f;
    
    [Tooltip("Damping to prevent infinite swinging")]
    [SerializeField] private float swingDamping = 0.98f;
    
    [Tooltip("Minimum swing speed to maintain (prevents getting stuck)")]
    [SerializeField] private float minSwingSpeed = 2f;
    
    [Tooltip("Momentum multiplier when releasing from swing (Spider-Man style throw)")]
    [SerializeField] private float releaseMomentumMultiplier = 1.2f;
    
    [Tooltip("Rotation speed to face swing direction")]
    [SerializeField] private float swingRotationSpeed = 15f;
    
    [Tooltip("Upward boost multiplier at release (higher = more height)")]
    [SerializeField] private float releaseUpwardBoost = 0.4f;
    
    [Tooltip("Minimum swing speed to get upward boost on release")]
    [SerializeField] private float minSwingSpeedForBoost = 5f;
    
    [Tooltip("Layer mask for swing points")]
    [SerializeField] private LayerMask swingPointLayer = -1;
    
    [Header("Rope/Line Settings")]
    [Tooltip("Material for the swing line/rope")]
    [SerializeField] private Material ropeMaterial;
    
    [Tooltip("Width of the swing line")]
    [SerializeField] private float ropeWidth = 0.1f;
    
    [Tooltip("Origin point for the rope (where it comes from on the player)")]
    [SerializeField] private Transform ropeOrigin;
    
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerGroundCheck groundCheck;
    [SerializeField] private PlayerInputHandler inputHandler;
    
    private CharacterController controller;
    private LineRenderer ropeLine;
    private SwingPoint currentSwingPoint;
    private bool isSwinging;
    private Vector3 swingVelocity;
    private float ropeLength;
    private GameObject ropeObject;
    private Transform camTransform;
    private PlayerJump playerJump;
    private float minAirTimeBeforeSwing = 0.1f; // Minimum time in air before can swing
    private float timeSinceGrounded;
    private Vector3 releaseVelocity; // Velocity to apply when releasing from swing
    private Vector3 initialVelocity; // Player's velocity when starting to swing
    private float swingStartTime; // Time when swing started
    private Vector3 swingStartPosition; // Position when swing started
    
    // Public properties
    public bool IsSwinging => isSwinging;
    public SwingPoint CurrentSwingPoint => currentSwingPoint;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        // Auto-find components if not assigned
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        if (groundCheck == null)
            groundCheck = GetComponent<PlayerGroundCheck>();
        
        if (inputHandler == null)
            inputHandler = GetComponent<PlayerInputHandler>();
        
        if (playerJump == null)
            playerJump = GetComponent<PlayerJump>();
        
        // Cache camera transform
        if (playerMovement != null)
        {
            camTransform = playerMovement.CameraTransform;
        }
        
        // Create rope origin if not assigned
        if (ropeOrigin == null)
        {
            GameObject originObj = new GameObject("RopeOrigin");
            originObj.transform.SetParent(transform);
            originObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Above player center
            ropeOrigin = originObj.transform;
        }
        
        // Create rope line renderer
        CreateRopeLine();
        
        // Default swing point layer to "Everything" if not set
        if (swingPointLayer.value == 0)
        {
            swingPointLayer = -1; // Everything
        }
    }
    
    private void CreateRopeLine()
    {
        ropeObject = new GameObject("SwingRope");
        ropeObject.transform.SetParent(transform);
        
        ropeLine = ropeObject.AddComponent<LineRenderer>();
        ropeLine.startWidth = ropeWidth;
        ropeLine.endWidth = ropeWidth;
        ropeLine.positionCount = 2;
        ropeLine.useWorldSpace = true;
        ropeLine.enabled = false;
        
        // Set material
        if (ropeMaterial == null)
        {
            // Create default material
            ropeMaterial = new Material(Shader.Find("Sprites/Default"));
            ropeMaterial.color = Color.white;
        }
        ropeLine.material = ropeMaterial;
        
        // Set sorting layer to appear in front
        ropeLine.sortingOrder = 10;
    }
    
    private void Update()
    {
        // Track time since grounded
        if (groundCheck != null)
        {
            if (groundCheck.IsGrounded)
            {
                timeSinceGrounded = 0f;
            }
            else
            {
                timeSinceGrounded += Time.deltaTime;
            }
        }
        
        if (isSwinging)
        {
            HandleSwinging();
            UpdateRopeVisual();
            CheckSwingRelease();
        }
        else
        {
            CheckSwingStart();
        }
    }
    
    /// <summary>
    /// Check if player wants to start swinging
    /// </summary>
    private void CheckSwingStart()
    {
        // Only allow swinging when in air
        if (groundCheck != null && groundCheck.IsGrounded)
            return;
        
        // Require minimum air time to prevent swinging immediately after jump
        if (timeSinceGrounded < minAirTimeBeforeSwing)
            return;
        
        // Only start swinging when X is PRESSED (not just held)
        // This prevents swinging if X was held during jump
        bool swingPressed = inputHandler != null && inputHandler.SwingPressed;
        
        if (swingPressed)
        {
            SwingPoint nearestPoint = FindNearestSwingPoint();
            
            if (nearestPoint != null)
            {
                StartSwinging(nearestPoint);
            }
        }
    }
    
    /// <summary>
    /// Get swing input from input handler (for continuing swing)
    /// </summary>
    private bool GetSwingInput()
    {
        if (inputHandler == null)
            return false;
        
        // While swinging, check if button is held
        return inputHandler.SwingHeld;
    }
    
    /// <summary>
    /// Find the nearest swing point within range
    /// </summary>
    private SwingPoint FindNearestSwingPoint()
    {
        SwingPoint nearest = null;
        float nearestDistance = float.MaxValue;
        
        // Find all swing points in scene
        SwingPoint[] allSwingPoints = FindObjectsOfType<SwingPoint>();
        
        foreach (SwingPoint point in allSwingPoints)
        {
            float distance = Vector3.Distance(transform.position, point.Position);
            
            if (distance <= maxDetectionDistance && 
                distance < nearestDistance &&
                point.CanSwingFrom(transform.position))
            {
                nearest = point;
                nearestDistance = distance;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Start swinging from a swing point
    /// </summary>
    private void StartSwinging(SwingPoint swingPoint)
    {
        currentSwingPoint = swingPoint;
        isSwinging = true;
        swingStartTime = Time.time;
        swingStartPosition = transform.position;
        
        // Calculate initial rope length
        ropeLength = Vector3.Distance(ropeOrigin.position, swingPoint.Position);
        
        // Store initial velocity for natural swing physics
        Vector3 currentVel = controller.velocity;
        initialVelocity = currentVel;
        
        // Calculate initial swing velocity based on player's current velocity
        // Project velocity onto the tangent plane of the swing arc
        Vector3 toSwingPoint = (swingPoint.Position - transform.position).normalized;
        Vector3 tangentPlane = Vector3.ProjectOnPlane(currentVel, toSwingPoint);
        
        // If player has significant velocity, use it; otherwise start with forward momentum
        if (tangentPlane.magnitude > 1f)
        {
            swingVelocity = tangentPlane;
        }
        else
        {
            // Start with forward momentum based on camera direction or player facing
            Vector3 forwardDir = Vector3.zero;
            if (camTransform != null)
            {
                forwardDir = camTransform.forward;
                forwardDir.y = 0;
                forwardDir.Normalize();
            }
            else
            {
                forwardDir = transform.forward;
            }
            
            // Project forward direction onto tangent plane
            Vector3 projectedForward = Vector3.ProjectOnPlane(forwardDir, toSwingPoint).normalized;
            swingVelocity = projectedForward * Mathf.Max(initialVelocity.magnitude, 3f);
        }
        
        // Enable rope visual
        if (ropeLine != null)
        {
            ropeLine.enabled = true;
        }
        
        Debug.Log($"Started swinging from point at {swingPoint.Position} with initial velocity: {initialVelocity}");
    }
    
    /// <summary>
    /// Handle swinging physics
    /// </summary>
    private void HandleSwinging()
    {
        if (currentSwingPoint == null)
        {
            StopSwinging();
            return;
        }
        
        Vector3 swingPointPos = currentSwingPoint.Position;
        Vector3 playerPos = transform.position;
        
        // Calculate direction from swing point to player
        Vector3 toPlayer = (playerPos - swingPointPos);
        float currentDistance = toPlayer.magnitude;
        
        // Normalize direction
        Vector3 ropeDirection = toPlayer / currentDistance;
        
        // Get player input for swinging direction
        Vector2 moveInput = inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
        Vector3 inputDirection = Vector3.zero;
        
        // Update camera transform reference
        if (camTransform == null && playerMovement != null)
        {
            camTransform = playerMovement.CameraTransform;
        }
        
        if (moveInput.magnitude > 0.1f && camTransform != null)
        {
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();
            
            inputDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }
        
        // Calculate tangent direction (perpendicular to rope, in the swing plane)
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(ropeDirection, up).normalized;
        if (right.magnitude < 0.1f)
        {
            // If rope is vertical, use forward direction
            right = Vector3.Cross(ropeDirection, camTransform != null ? camTransform.forward : Vector3.forward).normalized;
        }
        Vector3 forward = Vector3.Cross(right, ropeDirection).normalized;
        
        // Tangent is in the plane perpendicular to the rope
        Vector3 tangent = Vector3.Cross(ropeDirection, Vector3.Cross(ropeDirection, up)).normalized;
        if (tangent.magnitude < 0.1f)
        {
            tangent = forward;
        }
        
        // Natural pendulum physics
        // Calculate angle from vertical (for natural pendulum feel)
        // Use ropeDirection which is already calculated above
        float angleFromVertical = Vector3.Angle(ropeDirection, Vector3.down);
        
        // Apply gravity force (perpendicular to rope) - creates natural pendulum motion
        Vector3 gravityDir = Vector3.down;
        Vector3 gravityPerp = Vector3.ProjectOnPlane(gravityDir, ropeDirection).normalized;
        float gravityMagnitude = Mathf.Abs(playerMovement.Gravity);
        
        // Natural pendulum acceleration (gravity component perpendicular to rope)
        // This creates the natural swinging motion
        swingVelocity += gravityPerp * gravityMagnitude * Time.deltaTime;
        
        // Preserve initial momentum for first part of swing (natural carry-over)
        float swingTime = Time.time - swingStartTime;
        if (swingTime < 0.5f && initialVelocity.magnitude > 1f)
        {
            // Blend initial velocity into swing for natural transition
            Vector3 initialTangent = Vector3.ProjectOnPlane(initialVelocity, ropeDirection);
            float blendFactor = 1f - (swingTime / 0.5f); // Fade out over 0.5 seconds
            swingVelocity += initialTangent * blendFactor * 0.3f * Time.deltaTime;
        }
        
        // Apply player input force (perpendicular to rope) - allows player to control swing
        if (inputDirection.magnitude > 0.1f)
        {
            Vector3 inputPerp = Vector3.ProjectOnPlane(inputDirection, ropeDirection).normalized;
            if (inputPerp.magnitude > 0.1f)
            {
                // Natural input response
                swingVelocity += inputPerp * swingForce * Time.deltaTime;
            }
        }
        
        // Project velocity onto tangent plane (circular motion around swing point)
        Vector3 tangentVelocity = Vector3.ProjectOnPlane(swingVelocity, ropeDirection);
        swingVelocity = tangentVelocity;
        
        // Natural damping (air resistance) - preserves momentum better
        swingVelocity *= swingDamping;
        
        // Maintain minimum speed
        if (swingVelocity.magnitude < minSwingSpeed && swingVelocity.magnitude > 0.1f)
        {
            swingVelocity = swingVelocity.normalized * minSwingSpeed;
        }
        
        // Calculate movement along the arc
        Vector3 movement = swingVelocity * Time.deltaTime;
        
        // Move player
        Vector3 newPos = playerPos + movement;
        
        // Constrain to rope length (keep player on sphere around swing point)
        Vector3 toNewPos = newPos - swingPointPos;
        float newDistance = toNewPos.magnitude;
        
        if (Mathf.Abs(newDistance - ropeLength) > 0.01f)
        {
            // Adjust position to maintain rope length
            toNewPos = toNewPos.normalized * ropeLength;
            newPos = swingPointPos + toNewPos;
        }
        
        // Use CharacterController to move
        Vector3 finalMovement = newPos - playerPos;
        controller.Move(finalMovement);
        
        // Update swing velocity based on actual movement
        if (Time.deltaTime > 0)
        {
            swingVelocity = finalMovement / Time.deltaTime;
            // Remove any component along the rope direction
            swingVelocity = Vector3.ProjectOnPlane(swingVelocity, ropeDirection);
        }
        
        // Rotate player to face swing direction (Spider-Man style)
        RotateToSwingDirection();
    }
    
    /// <summary>
    /// Rotate player to face the swing direction (Spider-Man style)
    /// </summary>
    private void RotateToSwingDirection()
    {
        if (swingVelocity.magnitude < 0.5f)
            return; // Don't rotate if not moving much
        
        // Calculate swing direction (tangent to the arc)
        Vector3 swingDirection = swingVelocity.normalized;
        
        // Don't rotate if direction is too vertical
        if (Mathf.Abs(swingDirection.y) > 0.9f)
            return;
        
        // Flatten the direction for rotation (keep it mostly horizontal)
        Vector3 flatDirection = new Vector3(swingDirection.x, 0, swingDirection.z);
        
        if (flatDirection.magnitude > 0.1f)
        {
            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            
            // Smoothly rotate towards swing direction
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                swingRotationSpeed * Time.deltaTime
            );
        }
    }
    
    /// <summary>
    /// Update rope visual line
    /// </summary>
    private void UpdateRopeVisual()
    {
        if (ropeLine == null || currentSwingPoint == null)
            return;
        
        ropeLine.SetPosition(0, ropeOrigin.position);
        ropeLine.SetPosition(1, currentSwingPoint.Position);
    }
    
    /// <summary>
    /// Check if swing should be released
    /// </summary>
    private void CheckSwingRelease()
    {
        // Release if button is not held (check for held, not pressed)
        bool swingHeld = inputHandler != null && inputHandler.SwingHeld;
        
        if (!swingHeld)
        {
            StopSwinging();
            return;
        }
        
        // Release if player touches ground
        if (groundCheck != null && groundCheck.IsGrounded)
        {
            StopSwinging();
            return;
        }
        
        // Release if swing point is too far (rope broke)
        if (currentSwingPoint != null)
        {
            float distance = Vector3.Distance(transform.position, currentSwingPoint.Position);
            if (distance > currentSwingPoint.MaxSwingDistance * 1.5f)
            {
                StopSwinging();
            }
        }
    }
    
    /// <summary>
    /// Stop swinging and apply release momentum (Spider-Man style)
    /// Calculates natural release based on swing state, timing, and position
    /// </summary>
    private void StopSwinging()
    {
        if (!isSwinging)
            return;
        
        isSwinging = false;
        
        // Calculate natural release velocity based on swing state
        if (currentSwingPoint != null)
        {
            Vector3 swingPointPos = currentSwingPoint.Position;
            Vector3 playerPos = transform.position;
            
            // Calculate current swing state
            Vector3 toPlayer = (playerPos - swingPointPos).normalized;
            float angleFromVertical = Vector3.Angle(toPlayer, Vector3.down);
            
            // Get current swing velocity
            float swingSpeed = swingVelocity.magnitude;
            
            // Calculate release velocity based on multiple factors
            releaseVelocity = CalculateReleaseVelocity(swingSpeed, angleFromVertical, toPlayer);
            
            // Apply to player movement system
            if (playerMovement != null)
            {
                ApplyReleaseMomentum(releaseVelocity);
            }
        }
        
        currentSwingPoint = null;
        swingVelocity = Vector3.zero;
        initialVelocity = Vector3.zero;
        
        // Disable rope visual
        if (ropeLine != null)
        {
            ropeLine.enabled = false;
        }
        
        Debug.Log($"Stopped swinging with release velocity: {releaseVelocity}, speed: {releaseVelocity.magnitude}");
    }
    
    /// <summary>
    /// Calculate natural release velocity based on swing state
    /// Factors: swing speed, angle, timing, initial velocity
    /// </summary>
    private Vector3 CalculateReleaseVelocity(float currentSwingSpeed, float angleFromVertical, Vector3 ropeDirection)
    {
        Vector3 releaseVel = Vector3.zero;
        
        // Base release velocity from current swing momentum
        if (swingVelocity.magnitude > 0.1f)
        {
            releaseVel = swingVelocity * releaseMomentumMultiplier;
        }
        
        // Calculate horizontal (forward) component
        Vector3 horizontalVel = new Vector3(releaseVel.x, 0, releaseVel.z);
        float horizontalSpeed = horizontalVel.magnitude;
        
        // Calculate optimal release timing based on swing position
        // Best release is when swinging forward and slightly upward (like Spider-Man)
        float swingTime = Time.time - swingStartTime;
        
        // Upward boost calculation - natural physics based
        float upwardBoost = 0f;
        
        // Factor 1: Swing speed (faster = more height potential)
        if (currentSwingSpeed >= minSwingSpeedForBoost)
        {
            // More speed = more upward potential
            float speedFactor = Mathf.Clamp01((currentSwingSpeed - minSwingSpeedForBoost) / 10f);
            upwardBoost += speedFactor * releaseUpwardBoost * currentSwingSpeed;
        }
        
        // Factor 2: Angle from vertical (optimal release angle gives more height)
        // Best angle is around 30-45 degrees from bottom of swing
        float optimalAngle = 35f;
        float angleFactor = 1f - Mathf.Abs(angleFromVertical - optimalAngle) / 90f;
        angleFactor = Mathf.Clamp01(angleFactor);
        upwardBoost += angleFactor * releaseUpwardBoost * horizontalSpeed * 0.5f;
        
        // Factor 3: Initial velocity contribution (carry momentum from before swing)
        if (initialVelocity.magnitude > 0.5f)
        {
            float initialSpeed = initialVelocity.magnitude;
            upwardBoost += initialSpeed * releaseUpwardBoost * 0.2f;
        }
        
        // Factor 4: Swing duration (longer swings build more momentum)
        float timeFactor = Mathf.Clamp01(swingTime / 2f); // Max benefit at 2 seconds
        upwardBoost *= (1f + timeFactor * 0.3f);
        
        // Clamp upward boost to reasonable values
        upwardBoost = Mathf.Clamp(upwardBoost, 0f, 12f);
        
        // Apply upward component
        releaseVel.y = upwardBoost;
        
        // Boost horizontal momentum for natural forward flight
        if (horizontalSpeed > 0.1f)
        {
            Vector3 horizontalDir = horizontalVel.normalized;
            // Add extra forward momentum based on swing speed
            float forwardBoost = currentSwingSpeed * 0.4f;
            releaseVel += horizontalDir * forwardBoost;
        }
        
        return releaseVel;
    }
    
    /// <summary>
    /// Apply release momentum to player movement (Spider-Man style throw)
    /// </summary>
    private void ApplyReleaseMomentum(Vector3 momentum)
    {
        if (playerMovement == null)
            return;
        
        // Set vertical velocity (upward component) - this gives the upward flight
        playerMovement.SetVerticalVelocity(momentum.y);
        
        // Apply horizontal velocity through knockback system
        // This gives the player forward momentum when releasing
        Vector3 horizontalMomentum = new Vector3(momentum.x, 0, momentum.z);
        if (horizontalMomentum.magnitude > 0.1f)
        {
            // Apply horizontal momentum directly
            // The knockback system will handle deceleration naturally
            float force = horizontalMomentum.magnitude;
            playerMovement.ApplyKnockback(horizontalMomentum.normalized, force);
        }
    }
    
    private void OnDestroy()
    {
        if (ropeObject != null)
        {
            Destroy(ropeObject);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (isSwinging && currentSwingPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(ropeOrigin != null ? ropeOrigin.position : transform.position, 
                currentSwingPoint.Position);
        }
    }
}
