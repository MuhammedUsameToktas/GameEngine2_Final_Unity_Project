using UnityEngine;

/// <summary>
/// Unified enemy behavior: Patrol → Detect → Follow → Attack → Return to Patrol
/// Works with EnemyPlayerDetector and EnemyAttack
/// </summary>
[RequireComponent(typeof(EnemyPlayerDetector))]
public class EnemyBehavior : MonoBehaviour
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
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;

    [Header("Follow Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stopFollowDistance = 15f; // If player runs this far, return to patrol

    [Header("Return Settings")]
    [SerializeField] private float returnToPatrolDistance = 1f; // Distance from start to consider "returned"

    private EnemyPlayerDetector detector;
    private EnemyAttack enemyAttack;
    private EnemyKnockback knockback;
    private EnemyState currentState = EnemyState.Patrolling;

    private Vector3 startPosition;
    private Vector3 currentWaypoint;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float waitDuration = 0f;

    public EnemyState CurrentState => currentState;
    public bool IsWaiting => isWaiting; // Expose waiting state for animator controller

    private void Awake()
    {
        detector = GetComponent<EnemyPlayerDetector>();
        enemyAttack = GetComponent<EnemyAttack>();
        knockback = GetComponent<EnemyKnockback>();
        startPosition = transform.position;
    }

    private void Start()
    {
        // Ensure we start in a moving state, not waiting
        isWaiting = false;
        waitTimer = 0f;
        waitDuration = 0f;
        SetNewWaypoint();
        
        // If waypoint is too close to start position, generate a new one
        float distanceToWaypoint = Vector3.Distance(new Vector3(currentWaypoint.x, startPosition.y, currentWaypoint.z), startPosition);
        if (distanceToWaypoint < waypointReachDistance * 2f)
        {
            // Waypoint too close, generate a new one
            SetNewWaypoint();
        }
    }

    private void Update()
    {
        UpdateState();
        ExecuteCurrentState();
    }

    private void UpdateState()
    {
        // State machine logic
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
                    // Player left detection range, return to patrol
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
                    // Player ran too far away, return to patrol
                    currentState = EnemyState.Returning;
                }
                break;

            case EnemyState.PreparingToAttack:
                if (!detector.IsPlayerDetected)
                {
                    // Player left detection range, return to patrol
                    currentState = EnemyState.Returning;
                }
                else if (detector.DistanceToPlayer > attackRange)
                {
                    // Player moved out of attack range, follow again
                    currentState = EnemyState.Following;
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
                
                if (!detector.IsPlayerDetected || detector.DistanceToPlayer > stopFollowDistance)
                {
                    // Player lost or too far - return to patrol
                    currentState = EnemyState.Returning;
                }
                else if (detector.DistanceToPlayer > attackRange)
                {
                    // Player moved out of attack range but still detected - follow
                    currentState = EnemyState.Following;
                }
                else if (enemyAttack != null && !enemyAttack.IsAttacking && !enemyAttack.CanAttack)
                {
                    // Attack finished but on cooldown - prepare for next attack
                    currentState = EnemyState.PreparingToAttack;
                }
                // If still in attack range, stay in Attacking state to continue attacking
                break;

            case EnemyState.Returning:
                float distanceToStart = Vector3.Distance(transform.position, startPosition);
                if (distanceToStart <= returnToPatrolDistance)
                {
                    // Returned to start, resume patrol
                    currentState = EnemyState.Patrolling;
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
        // Don't execute movement if enemy is being knocked back
        if (knockback != null && knockback.IsKnockedBack)
        {
            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrolling:
                ExecutePatrol();
                break;

            case EnemyState.Following:
                ExecuteFollow();
                break;

            case EnemyState.PreparingToAttack:
                ExecutePrepareToAttack();
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
        Vector3 targetPosition = detector.PlayerTransform.position;

        // Don't get too close (stop at attack range)
        float distanceToPlayer = detector.DistanceToPlayer;
        if (distanceToPlayer > attackRange)
        {
            // Move towards player but stop at attack range
            Vector3 direction = directionToPlayer;
            direction.y = 0;
            direction.Normalize();

            Vector3 moveDirection = direction * followSpeed * Time.deltaTime;
            transform.position += moveDirection;

            // Rotate towards player
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Close enough, just rotate to face player
            Vector3 direction = directionToPlayer;
            direction.y = 0;
            direction.Normalize();

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void ExecutePrepareToAttack()
    {
        // Rotate to face player while preparing to attack
        if (detector.PlayerTransform != null)
        {
            Vector3 directionToPlayer = detector.GetDirectionToPlayer();
            Vector3 direction = directionToPlayer;
            direction.y = 0;
            direction.Normalize();

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        // Don't move, just face the player and wait for attack cooldown
    }

    private void ExecuteAttack()
    {
        // Rotate to face player while attacking
        if (detector.PlayerTransform != null)
        {
            Vector3 directionToPlayer = detector.GetDirectionToPlayer();
            Vector3 direction = directionToPlayer;
            direction.y = 0;
            direction.Normalize();

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Only trigger attack if not already attacking (prevents multiple attacks)
        // The attack will be triggered once when entering Attacking state
        if (enemyAttack != null && !enemyAttack.IsAttacking && enemyAttack.CanAttack && detector.DistanceToPlayer <= attackRange)
        {
            enemyAttack.TryAttack();
        }
    }

    private void ExecuteReturn()
    {
        Vector3 directionToStart = (startPosition - transform.position);
        directionToStart.y = 0;
        float distance = directionToStart.magnitude;

        if (distance > returnToPatrolDistance)
        {
            directionToStart.Normalize();
            transform.position += directionToStart * patrolSpeed * Time.deltaTime;

            // Rotate towards start position
            if (directionToStart.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToStart);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void SetNewWaypoint()
    {
        // Generate a new waypoint within the patrol radius
        // Ensure it's far enough from current position to avoid getting stuck
        int attempts = 0;
        const int maxAttempts = 10;
        float minDistance = waypointReachDistance * 2f; // Minimum distance to avoid immediate "reached" state
        
        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            currentWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Ensure waypoint is on ground
            RaycastHit hit;
            if (Physics.Raycast(currentWaypoint + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                currentWaypoint.y = hit.point.y;
            }
            
            // Ensure waypoint stays within patrol radius (double-check)
            float distanceFromStart = Vector3.Distance(new Vector3(currentWaypoint.x, startPosition.y, currentWaypoint.z), startPosition);
            if (distanceFromStart > patrolRadius)
            {
                // Clamp waypoint to patrol radius
                Vector3 directionToWaypoint = (currentWaypoint - startPosition);
                directionToWaypoint.y = 0;
                directionToWaypoint.Normalize();
                currentWaypoint = startPosition + directionToWaypoint * patrolRadius;
                currentWaypoint.y = hit.point.y; // Keep the ground height
            }
            
            // Check if waypoint is far enough from current position
            float distanceToWaypoint = Vector3.Distance(new Vector3(currentWaypoint.x, transform.position.y, currentWaypoint.z), transform.position);
            if (distanceToWaypoint >= minDistance)
            {
                break; // Good waypoint found
            }
            
            attempts++;
        } while (attempts < maxAttempts);
        
        // If we couldn't find a good waypoint, ensure it's at least at the edge of patrol radius
        if (attempts >= maxAttempts)
        {
            Vector3 directionFromStart = (transform.position - startPosition);
            directionFromStart.y = 0;
            if (directionFromStart.magnitude < 0.1f)
            {
                // At start position, pick a random direction
                directionFromStart = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            }
            else
            {
                directionFromStart.Normalize();
            }
            
            currentWaypoint = startPosition + directionFromStart * (patrolRadius * 0.8f);
            RaycastHit hit;
            if (Physics.Raycast(currentWaypoint + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                currentWaypoint.y = hit.point.y;
            }
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

        direction.y = 0;
        direction.Normalize();

        transform.position += direction * speed * Time.deltaTime;

        // Rotate towards waypoint
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw patrol area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, patrolRadius);

        // Draw current waypoint
        if (Application.isPlaying && currentState == EnemyState.Patrolling)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }

        // Draw state indicator
        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling: return Color.green;
            case EnemyState.Following: return Color.yellow;
            case EnemyState.PreparingToAttack: return Color.magenta;
            case EnemyState.Attacking: return Color.red;
            case EnemyState.Returning: return Color.blue;
            default: return Color.white;
        }
    }
}
