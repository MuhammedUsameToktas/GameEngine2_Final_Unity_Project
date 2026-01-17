using UnityEngine;

/// <summary>
/// Unified flying enemy behavior: Patrol → Detect → Follow → Attack → Return to Patrol
/// Works with EnemyPlayerDetector and EnemyAttack
/// Maintains hover height above ground
/// </summary>
[RequireComponent(typeof(EnemyPlayerDetector))]
public class EnemyFlyingBehavior : MonoBehaviour
{
    public enum EnemyState
    {
        Patrolling,
        Following,
        PreparingToAttack,
        Attacking,
        Returning
    }

    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 4f;
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Flying Settings")]
    [SerializeField] private float hoverHeight = 3f;
    [SerializeField] private float minHeightAboveGround = 1f;
    [SerializeField] private LayerMask groundLayer = -1;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;
    [SerializeField] private float idleHoverAmplitude = 0.5f;
    [SerializeField] private float idleHoverSpeed = 1f;

    [Header("Follow Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stopFollowDistance = 15f;
    [SerializeField] private float minDistanceToPlayer = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float attackDiveHeight = 0.5f; // Height above ground when diving to attack
    [SerializeField] private float diveSpeed = 8f; // Speed when diving down to attack
    [SerializeField] private float returnToHoverSpeed = 4f; // Speed when returning to hover height after attack

    [Header("Return Settings")]
    [SerializeField] private float returnToPatrolDistance = 1f;

    private EnemyPlayerDetector detector;
    private EnemyAttack enemyAttack;
    private EnemyState currentState = EnemyState.Patrolling;

    private Vector3 startPosition;
    private Vector3 currentWaypoint;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float waitDuration = 0f;
    private float hoverOffset = 0f;
    private float normalHoverHeight = 0f; // Store the normal hover height to return to
    private bool isDiving = false; // Track if currently diving for attack

    public EnemyState CurrentState => currentState;
    public bool IsWaiting => isWaiting; // Expose waiting state for animator controller

    private void Awake()
    {
        detector = GetComponent<EnemyPlayerDetector>();
        enemyAttack = GetComponent<EnemyAttack>();
        startPosition = transform.position;
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);
        
        // Store initial hover height
        float groundY = GetGroundHeight(transform.position);
        normalHoverHeight = transform.position.y - groundY;
        if (normalHoverHeight < hoverHeight)
        {
            normalHoverHeight = hoverHeight;
        }
    }

    private void Start()
    {
        SetNewWaypoint();
    }

    private void Update()
    {
        UpdateState();
        ExecuteCurrentState();
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                if (detector.IsPlayerDetected)
                {
                    currentState = EnemyState.Following;
                }
                break;

            case EnemyState.Following:
                if (!detector.IsPlayerDetected)
                {
                    currentState = EnemyState.Returning;
                }
                else if (detector.DistanceToPlayer <= attackRange)
                {
                    // Close enough to attack range
                    if (enemyAttack != null && enemyAttack.CanAttack)
                    {
                        // Can attack immediately
                        currentState = EnemyState.Attacking;
                    }
                    else
                    {
                        // In range but can't attack yet (cooldown) - prepare to attack
                        currentState = EnemyState.PreparingToAttack;
                    }
                }
                else if (detector.DistanceToPlayer > stopFollowDistance)
                {
                    currentState = EnemyState.Returning;
                }
                break;

            case EnemyState.PreparingToAttack:
                if (!detector.IsPlayerDetected)
                {
                    // Player left detection range, return to patrol
                    currentState = EnemyState.Returning;
                    isDiving = false;
                }
                else if (detector.DistanceToPlayer > attackRange)
                {
                    // Player moved out of attack range, follow again
                    currentState = EnemyState.Following;
                    isDiving = false;
                }
                else if (enemyAttack != null && enemyAttack.CanAttack)
                {
                    // Can attack now
                    currentState = EnemyState.Attacking;
                }
                // Otherwise stay in PreparingToAttack state
                break;

            case EnemyState.Attacking:
                // Stay in attacking state as long as player is in range
                // Only leave if player moves away or is lost
                
                // Check if we need to return to hover height (for melee attacks)
                if (enemyAttack != null && enemyAttack.IsMelee)
                {
                    float currentGroundY = GetGroundHeight(transform.position);
                    float currentHeightAboveGround = transform.position.y - currentGroundY;
                    float targetHoverY = currentGroundY + hoverHeight;
                    
                    // If we're still at dive height and not attacking, return to hover first
                    if (currentHeightAboveGround < hoverHeight * 0.7f && isDiving && (enemyAttack == null || !enemyAttack.IsAttacking))
                    {
                        // Still returning to hover height, stay in attacking state
                        if (Mathf.Abs(transform.position.y - targetHoverY) > 0.2f)
                        {
                            // Keep in attacking state while returning to hover, but check if should continue attacking
                            if (detector.IsPlayerDetected && detector.DistanceToPlayer <= attackRange && enemyAttack != null && enemyAttack.CanAttack)
                            {
                                // Still in range, will attack again
                                break;
                            }
                        }
                        else
                        {
                            isDiving = false; // Reached hover height
                        }
                    }
                }
                
                // Check if should continue attacking or change state
                if (!detector.IsPlayerDetected || detector.DistanceToPlayer > stopFollowDistance)
                {
                    // Player lost or too far - return to patrol
                    currentState = EnemyState.Returning;
                    isDiving = false;
                }
                else if (detector.DistanceToPlayer > attackRange)
                {
                    // Player moved out of attack range but still detected - follow
                    currentState = EnemyState.Following;
                    isDiving = false;
                }
                else if (enemyAttack != null && !enemyAttack.IsAttacking && !enemyAttack.CanAttack)
                {
                    // Attack finished but on cooldown - prepare for next attack
                    currentState = EnemyState.PreparingToAttack;
                }
                // If still in attack range, stay in Attacking state to continue attacking
                break;

            case EnemyState.Returning:
                // Check 3D distance (including Y) to start position
                Vector3 toStart = startPosition - transform.position;
                float distanceToStart = toStart.magnitude;
                
                // Also check horizontal distance for better accuracy
                toStart.y = 0;
                float horizontalDistanceToStart = toStart.magnitude;
                
                if (horizontalDistanceToStart <= returnToPatrolDistance)
                {
                    // Reached start position, resume patrolling
                    currentState = EnemyState.Patrolling;
                    isWaiting = false; // Reset waiting state
                    SetNewWaypoint();
                }
                else if (detector.IsPlayerDetected && detector.DistanceToPlayer <= stopFollowDistance)
                {
                    // Player detected again while returning, resume following
                    currentState = EnemyState.Following;
                }
                break;
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                ExecutePatrol();
                break;

            case EnemyState.Following:
                ExecuteFollow();
                break;

            case EnemyState.Attacking:
                ExecuteAttack();
                break;

            case EnemyState.Returning:
                ExecuteReturn();
                break;
        }
    }

    private void ExecutePatrol()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                isWaiting = false;
                SetNewWaypoint();
            }
        }
        else
        {
            MoveToWaypoint(patrolSpeed);
        }
    }

    private void ExecuteFollow()
    {
        if (detector.PlayerTransform == null) return;

        Vector3 directionToPlayer = detector.GetDirectionToPlayer();
        float distanceToPlayer = detector.DistanceToPlayer;

        Vector3 playerPos = detector.PlayerTransform.position;
        Vector3 horizontalDirection = directionToPlayer;
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();

        Vector3 targetPosition = playerPos - horizontalDirection * minDistanceToPlayer;

        // Get ground height and set hover height
        float groundY = GetGroundHeight(targetPosition);
        float targetY = groundY + hoverHeight;

        // Smooth height adjustment
        if (transform.position.y > groundY + minHeightAboveGround)
        {
            float currentHeightAboveGround = transform.position.y - groundY;
            if (currentHeightAboveGround > hoverHeight * 0.5f && currentHeightAboveGround < hoverHeight * 1.5f)
            {
                targetY = Mathf.Lerp(transform.position.y, groundY + hoverHeight, 0.1f);
            }
        }

        targetY = Mathf.Max(targetY, groundY + minHeightAboveGround);
        targetPosition.y = targetY;

        // Move towards player
        if (distanceToPlayer > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }

        // Safety check
        float currentGroundY = GetGroundHeight(transform.position);
        float minSafeY = currentGroundY + minHeightAboveGround;
        if (transform.position.y < minSafeY)
        {
            Vector3 safePosition = transform.position;
            safePosition.y = minSafeY;
            transform.position = safePosition;
        }

        // Rotate to face player
        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ExecutePrepareToAttack()
    {
        if (detector.PlayerTransform == null) return;

        // Rotate to face player while preparing to attack
        Vector3 directionToPlayer = detector.GetDirectionToPlayer();
        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Maintain hover height while preparing
        float groundY = GetGroundHeight(transform.position);
        float targetHoverY = groundY + hoverHeight;
        Vector3 hoverPosition = transform.position;
        hoverPosition.y = targetHoverY;
        transform.position = Vector3.MoveTowards(transform.position, hoverPosition, returnToHoverSpeed * Time.deltaTime);
        
        // Don't dive or move, just face the player and wait for attack cooldown
        isDiving = false;
    }

    private void ExecuteAttack()
    {
        if (detector.PlayerTransform == null) return;

        // Get player and ground positions
        Vector3 playerPos = detector.PlayerTransform.position;
        float groundY = GetGroundHeight(playerPos);
        float currentGroundY = GetGroundHeight(transform.position);
        float currentHeightAboveGround = transform.position.y - currentGroundY;
        float targetHoverY = currentGroundY + hoverHeight;
        float attackDiveY = groundY + attackDiveHeight;

        // Check if we need to dive down or return to hover height
        // Only dive for melee attacks, ranged attacks stay at hover height
        bool shouldDive = false;
        bool shouldReturnToHover = false;
        bool isMeleeAttack = enemyAttack != null && enemyAttack.IsMelee;

        if (isMeleeAttack && enemyAttack != null && enemyAttack.IsAttacking)
        {
            // Currently performing melee attack - dive down to attack height
            shouldDive = true;
            isDiving = true;
        }
        else if (isMeleeAttack && enemyAttack != null && enemyAttack.CanAttack && detector.DistanceToPlayer <= attackRange)
        {
            // Can perform melee attack and in range - dive down
            shouldDive = true;
            isDiving = true;
        }
        else if (isDiving && currentHeightAboveGround < hoverHeight * 0.7f)
        {
            // Melee attack finished but still at dive height - return to hover
            shouldReturnToHover = true;
        }
        else
        {
            // Not diving (ranged attack or not attacking) - maintain hover height
            isDiving = false;
        }

        // Handle diving down for melee attack only
        if (shouldDive)
        {
            Vector3 attackPosition = playerPos;
            attackPosition.y = attackDiveY;

            // Move towards attack position (dive down)
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, diveSpeed * Time.deltaTime);

            // Try to attack when close enough
            if (enemyAttack != null && enemyAttack.CanAttack && detector.DistanceToPlayer <= attackRange)
            {
                enemyAttack.TryAttack();
            }
        }
        // For ranged attacks, stay at hover height and attack
        else if (enemyAttack != null && !isMeleeAttack && enemyAttack.CanAttack && detector.DistanceToPlayer <= attackRange)
        {
            // Ranged attack - stay at hover height and attack
            enemyAttack.TryAttack();
        }
        // Handle returning to hover height after attack
        else if (shouldReturnToHover)
        {
            Vector3 hoverPosition = transform.position;
            hoverPosition.y = targetHoverY;

            transform.position = Vector3.MoveTowards(transform.position, hoverPosition, returnToHoverSpeed * Time.deltaTime);
        }
        // Maintain hover height if not diving
        else
        {
            Vector3 hoverPosition = transform.position;
            hoverPosition.y = targetHoverY;

            // Smoothly maintain hover height
            transform.position = Vector3.MoveTowards(transform.position, hoverPosition, returnToHoverSpeed * Time.deltaTime);
        }

        // Safety check - prevent going below ground
        float minSafeY = currentGroundY + minHeightAboveGround;
        if (transform.position.y < minSafeY)
        {
            Vector3 safePosition = transform.position;
            safePosition.y = minSafeY;
            transform.position = safePosition;
        }

        // Continuously rotate to face player (do this last so it's always applied)
        Vector3 directionToPlayer = detector.GetDirectionToPlayer();
        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            // For flying enemies, look at player in 3D space (can look up/down)
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ExecuteReturn()
    {
        Vector3 directionToStart = (startPosition - transform.position);
        float distance = directionToStart.magnitude;

        if (distance > returnToPatrolDistance)
        {
            directionToStart.y = 0;
            directionToStart.Normalize();

            // Calculate target position with proper height
            Vector3 targetPos = transform.position + directionToStart * patrolSpeed * Time.deltaTime;
            float groundY = GetGroundHeight(targetPos);
            targetPos.y = groundY + hoverHeight;

            transform.position = Vector3.MoveTowards(transform.position, targetPos, patrolSpeed * Time.deltaTime);

            // Safety check
            float currentGroundY = GetGroundHeight(transform.position);
            if (transform.position.y < currentGroundY + minHeightAboveGround)
            {
                Vector3 safePosition = transform.position;
                safePosition.y = currentGroundY + minHeightAboveGround;
                transform.position = safePosition;
            }

            // Rotate
            if (directionToStart.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToStart);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Reached start position - ensure we're at proper hover height and reset patrol
            float groundY = GetGroundHeight(startPosition);
            Vector3 finalPosition = startPosition;
            finalPosition.y = groundY + hoverHeight;
            
            // Smoothly move to final position if not already there
            if (Vector3.Distance(transform.position, finalPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, finalPosition, patrolSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = finalPosition;
            }
            
            // Reset waiting state to ensure patrol starts immediately
            isWaiting = false;
        }
    }

    private void SetNewWaypoint()
    {
        // Generate a new waypoint within the patrol radius
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Set waypoint height
        float groundY = GetGroundHeight(currentWaypoint);
        currentWaypoint.y = groundY + hoverHeight;
        
        // Ensure waypoint stays within patrol radius (double-check)
        float distanceFromStart = Vector3.Distance(new Vector3(currentWaypoint.x, startPosition.y, currentWaypoint.z), startPosition);
        if (distanceFromStart > patrolRadius)
        {
            // Clamp waypoint to patrol radius
            Vector3 directionToWaypoint = (currentWaypoint - startPosition);
            directionToWaypoint.y = 0;
            directionToWaypoint.Normalize();
            currentWaypoint = startPosition + directionToWaypoint * patrolRadius;
            currentWaypoint.y = groundY + hoverHeight; // Keep the hover height
        }
    }

    private void MoveToWaypoint(float speed)
    {
        Vector3 direction = (currentWaypoint - transform.position);
        float distance = direction.magnitude;

        if (distance <= waypointReachDistance)
        {
            isWaiting = true;
            waitTimer = 0f;
            waitDuration = Random.Range(minWaitTime, maxWaitTime);
            return;
        }

        // Add gentle hover movement
        float hoverY = currentWaypoint.y + Mathf.Sin(Time.time * idleHoverSpeed + hoverOffset) * idleHoverAmplitude;
        Vector3 targetPos = currentWaypoint;
        targetPos.y = hoverY;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Rotate towards waypoint
        direction.y = 0;
        direction.Normalize();
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Safety check
        float currentGroundY = GetGroundHeight(transform.position);
        if (transform.position.y < currentGroundY + minHeightAboveGround)
        {
            Vector3 safePosition = transform.position;
            safePosition.y = currentGroundY + minHeightAboveGround;
            transform.position = safePosition;
        }
    }

    private float GetGroundHeight(Vector3 position)
    {
        Vector3 rayOrigin = position + Vector3.up * 20f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 50f, groundLayer))
        {
            return hit.point.y;
        }

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 20f, groundLayer))
        {
            return hit.point.y;
        }

        return startPosition.y;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, patrolRadius);

        if (Application.isPlaying && currentState == EnemyState.Patrolling)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }

        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling: return Color.green;
            case EnemyState.Following: return Color.yellow;
            case EnemyState.Attacking: return Color.red;
            case EnemyState.Returning: return Color.blue;
            default: return Color.white;
        }
    }
}
