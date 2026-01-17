using UnityEngine;

/// <summary>
/// Flying enemy that detects and follows the player
/// Moves in 3D space (can fly at different heights)
/// </summary>
[RequireComponent(typeof(EnemyPlayerDetector))]
public class EnemyFlyingFollow : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float flySpeed = 4f;
    [SerializeField] private float followSpeed = 6f; // Speed when following player
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float hoverHeight = 3f; // Height above ground to maintain
    [SerializeField] private float minDistanceToPlayer = 2f; // Stop following when this close
    [SerializeField] private float minHeightAboveGround = 1f; // Minimum height to prevent going below ground
    [SerializeField] private LayerMask groundLayer = -1; // Layers that count as ground

    [Header("Idle Behavior")]
    [SerializeField] private float idleHoverSpeed = 1f;
    [SerializeField] private float idleHoverAmplitude = 0.5f;
    [SerializeField] private Vector3 idlePatrolArea = new Vector3(5f, 2f, 5f); // Area to patrol when idle

    private EnemyPlayerDetector detector;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float hoverOffset = 0f;

    private void Awake()
    {
        detector = GetComponent<EnemyPlayerDetector>();
        startPosition = transform.position;
        hoverOffset = Random.Range(0f, Mathf.PI * 2f); // Randomize hover phase
    }

    private void Update()
    {
        if (detector.IsPlayerDetected)
        {
            FollowPlayer();
        }
        else
        {
            IdleHover();
        }
    }

    private void FollowPlayer()
    {
        if (detector.PlayerTransform == null) return;

        Vector3 directionToPlayer = detector.GetDirectionToPlayer();
        float distanceToPlayer = detector.DistanceToPlayer;

        // Calculate target position (horizontal position relative to player)
        Vector3 playerPos = detector.PlayerTransform.position;
        Vector3 horizontalDirection = directionToPlayer;
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();
        
        targetPosition = playerPos - horizontalDirection * minDistanceToPlayer;
        
        // Get ground height at target position
        float groundY = GetGroundHeight(targetPosition);
        
        // Calculate target Y: maintain hover height above ground, but don't go below current Y if it's safe
        float targetY = groundY + hoverHeight;
        
        // If we're already at a safe height, try to maintain it (with small adjustments)
        if (transform.position.y > groundY + minHeightAboveGround)
        {
            // Smoothly adjust height instead of forcing it
            float currentHeightAboveGround = transform.position.y - groundY;
            if (currentHeightAboveGround > hoverHeight * 0.5f && currentHeightAboveGround < hoverHeight * 1.5f)
            {
                // We're in a reasonable range, use current height as base
                targetY = Mathf.Lerp(transform.position.y, groundY + hoverHeight, 0.1f);
            }
        }
        
        // Ensure minimum height
        targetY = Mathf.Max(targetY, groundY + minHeightAboveGround);
        targetPosition.y = targetY;

        // Move towards player
        float currentSpeed = distanceToPlayer > minDistanceToPlayer * 2f ? followSpeed : flySpeed;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        // Final safety check - prevent going below ground
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

    /// <summary>
    /// Get ground height at a given position using raycast
    /// </summary>
    private float GetGroundHeight(Vector3 position)
    {
        // Cast from reasonable height above position (not too high to avoid issues)
        Vector3 rayOrigin = position + Vector3.up * 20f;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 50f, groundLayer))
        {
            return hit.point.y;
        }
        
        // If no ground found, try casting from current position
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 20f, groundLayer))
        {
            return hit.point.y;
        }
        
        // Fallback: use start position Y if raycast fails
        return startPosition.y;
    }

    private void IdleHover()
    {
        // Random patrol within area
        Vector3 patrolTarget = startPosition + new Vector3(
            Mathf.Sin(Time.time * 0.5f) * idlePatrolArea.x * 0.5f,
            0,
            Mathf.Cos(Time.time * 0.5f) * idlePatrolArea.z * 0.5f
        );
        
        // Get ground height and add hover height
        float groundY = GetGroundHeight(patrolTarget);
        float hoverY = groundY + hoverHeight;
        
        // Add gentle up/down movement
        float newY = hoverY + Mathf.Sin(Time.time * idleHoverSpeed + hoverOffset) * idleHoverAmplitude;
        patrolTarget.y = newY;

        transform.position = Vector3.MoveTowards(transform.position, patrolTarget, flySpeed * Time.deltaTime);
        
        // Safety check - prevent going below ground
        float currentGroundY = GetGroundHeight(transform.position);
        if (transform.position.y < currentGroundY + minHeightAboveGround)
        {
            Vector3 safePosition = transform.position;
            safePosition.y = currentGroundY + minHeightAboveGround;
            transform.position = safePosition;
        }
    }
}
