using UnityEngine;

/// <summary>
/// Detects player within range and line of sight
/// Shared component for all enemy types that need player detection
/// </summary>
public class EnemyPlayerDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float detectionAngle = 120f; // Field of view angle
    [SerializeField] private LayerMask obstacleLayer = -1; // Layers that block line of sight
    [SerializeField] private bool requireLineOfSight = true;

    private Transform playerTransform;
    private bool playerDetected = false;
    private float lastDetectionTime = 0f;

    public bool IsPlayerDetected => playerDetected;
    public Transform PlayerTransform => playerDetected ? playerTransform : null;
    public float DistanceToPlayer => playerDetected ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        // Continuously try to find player if not found (in case player spawns dynamically)
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        bool wasDetected = playerDetected;
        playerDetected = CheckPlayerDetection();

        if (playerDetected && !wasDetected)
        {
            lastDetectionTime = Time.time;
        }
    }

    private void FindPlayer()
    {
        // Try multiple methods to find player
        var player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            // Try finding via LevelManager
            if (LevelManager.Instance != null)
            {
                GameObject playerInstance = LevelManager.Instance.GetPlayerInstance();
                if (playerInstance != null)
                {
                    player = playerInstance;
                }
            }
        }

        if (player == null)
        {
            // Try FindObjectOfType as last resort
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                player = playerHealth.gameObject;
            }
        }

        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private bool CheckPlayerDetection()
    {
        if (playerTransform == null) return false;

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distance = directionToPlayer.magnitude;

        // Check range
        if (distance > detectionRange) return false;

        // Check angle (field of view)
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > detectionAngle * 0.5f) return false;

        // Check line of sight
        if (requireLineOfSight)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = directionToPlayer.normalized;
            
            // Cast ray from enemy to player
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, distance, obstacleLayer))
            {
                // If we hit something that's not the player, line of sight is blocked
                if (!hit.collider.CompareTag("Player"))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Get direction to player (normalized)
    /// </summary>
    public Vector3 GetDirectionToPlayer()
    {
        if (!playerDetected || playerTransform == null) return Vector3.zero;
        return (playerTransform.position - transform.position).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = playerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw detection angle
        if (playerTransform != null)
        {
            Gizmos.color = playerDetected ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        // Draw field of view
        Gizmos.color = Color.cyan;
        Vector3 leftBoundary = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, detectionAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
    }
}
