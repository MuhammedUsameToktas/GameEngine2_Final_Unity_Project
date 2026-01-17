using UnityEngine;

/// <summary>
/// Handles knockback for enemies
/// Works with transform-based movement systems
/// Keeps enemies on the ground during knockback
/// </summary>
public class EnemyKnockback : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private float groundCheckDistance = 5f; // Increased distance
    [SerializeField] private float groundCheckOffset = 1f; // Start check from above enemy
    [SerializeField] private float minGroundDistance = 0.1f; // Minimum distance to keep from ground

    private Vector3 knockbackVelocity;
    private float knockbackDeceleration = 20f;
    private bool isKnockedBack = false;
    private float enemyHeightOffset = 0f; // Store enemy's height above ground
    private Collider enemyCollider; // Enemy's collider for bounds

    public bool IsKnockedBack => isKnockedBack;

    private void Awake()
    {
        // Default to "Default" layer if groundLayer is not set
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }

        // Get enemy collider for better ground detection
        enemyCollider = GetComponent<Collider>();
        if (enemyCollider == null)
        {
            enemyCollider = GetComponentInChildren<Collider>();
        }

        // Calculate initial height offset from ground
        CalculateGroundHeight();
    }

    /// <summary>
    /// Calculate the enemy's height above ground using multiple raycasts
    /// </summary>
    private void CalculateGroundHeight()
    {
        float bestGroundY = transform.position.y;
        bool foundGround = false;

        // Determine check points based on collider bounds if available
        Vector3[] checkPoints;
        if (enemyCollider != null)
        {
            Bounds bounds = enemyCollider.bounds;
            float checkRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
            checkPoints = new Vector3[]
            {
                bounds.center, // Center
                bounds.center + Vector3.forward * checkRadius, // Forward
                bounds.center - Vector3.forward * checkRadius, // Backward
                bounds.center + Vector3.right * checkRadius, // Right
                bounds.center - Vector3.right * checkRadius, // Left
                bounds.center + (Vector3.forward + Vector3.right) * checkRadius * 0.7f, // Forward-right
                bounds.center + (Vector3.forward - Vector3.right) * checkRadius * 0.7f, // Forward-left
                bounds.center - (Vector3.forward + Vector3.right) * checkRadius * 0.7f, // Backward-right
                bounds.center - (Vector3.forward - Vector3.right) * checkRadius * 0.7f // Backward-left
            };
        }
        else
        {
            // Fallback to simple check points
            checkPoints = new Vector3[]
            {
                transform.position, // Center
                transform.position + transform.forward * 0.3f, // Forward
                transform.position - transform.forward * 0.3f, // Backward
                transform.position + transform.right * 0.3f, // Right
                transform.position - transform.right * 0.3f // Left
            };
        }

        foreach (Vector3 checkPoint in checkPoints)
        {
            RaycastHit hit;
            Vector3 rayOrigin = checkPoint + Vector3.up * groundCheckOffset;
            
            // Cast ray downward
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                if (!foundGround || hit.point.y > bestGroundY)
                {
                    bestGroundY = hit.point.y;
                    foundGround = true;
                }
            }
        }

        if (foundGround)
        {
            float currentY = enemyCollider != null ? enemyCollider.bounds.min.y : transform.position.y;
            enemyHeightOffset = currentY - bestGroundY;
            // Ensure minimum offset
            if (enemyHeightOffset < minGroundDistance)
            {
                enemyHeightOffset = minGroundDistance;
            }
        }
        else
        {
            // If no ground found, try a longer raycast from above
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f, groundLayer))
            {
                float currentY = enemyCollider != null ? enemyCollider.bounds.min.y : transform.position.y;
                enemyHeightOffset = currentY - hit.point.y;
                if (enemyHeightOffset < minGroundDistance)
                {
                    enemyHeightOffset = minGroundDistance;
                }
            }
            else
            {
                // Last resort: assume enemy is at ground level
                enemyHeightOffset = minGroundDistance;
            }
        }
    }

    /// <summary>
    /// Apply knockback force to the enemy
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0; // Keep knockback horizontal
        direction.Normalize();
        knockbackVelocity = direction * force;
        isKnockedBack = true;
        
        // Recalculate ground height when knockback starts
        CalculateGroundHeight();
    }

    private void Update()
    {
        if (isKnockedBack)
        {
            // Calculate new position with knockback (horizontal only)
            Vector3 newPosition = transform.position;
            newPosition.x += knockbackVelocity.x * Time.deltaTime;
            newPosition.z += knockbackVelocity.z * Time.deltaTime;

            // Check ground at new position to keep enemy on ground
            float groundY = GetGroundHeightAt(newPosition);
            
            if (groundY != float.MinValue)
            {
                // Keep enemy at the same height above ground
                newPosition.y = groundY + enemyHeightOffset;
            }
            else
            {
                // If no ground found at new position, check current position
                float currentGroundY = GetGroundHeightAt(transform.position);
                if (currentGroundY != float.MinValue)
                {
                    newPosition.y = currentGroundY + enemyHeightOffset;
                }
                // If still no ground, keep current Y position
            }

            // Apply knockback movement
            transform.position = newPosition;

            // Decelerate knockback
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDeceleration * Time.deltaTime);

            // Stop knockback when velocity is very small
            if (knockbackVelocity.magnitude < 0.1f)
            {
                knockbackVelocity = Vector3.zero;
                isKnockedBack = false;
            }
        }
        else
        {
            // When not knocked back, periodically update ground height (for moving enemies)
            // This ensures height offset stays accurate
            if (Time.frameCount % 10 == 0) // Check every 10 frames for performance
            {
                CalculateGroundHeight();
            }
        }
    }

    /// <summary>
    /// Get ground height at a specific position using multiple raycasts
    /// </summary>
    private float GetGroundHeightAt(Vector3 position)
    {
        float bestGroundY = float.MinValue;
        bool foundGround = false;

        // Determine check points based on collider bounds if available
        Vector3[] checkPoints;
        if (enemyCollider != null)
        {
            Bounds bounds = enemyCollider.bounds;
            float checkRadius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
            Vector3 centerOffset = position - transform.position; // Offset from current position
            Vector3 checkCenter = bounds.center + centerOffset;
            
            checkPoints = new Vector3[]
            {
                checkCenter, // Center
                checkCenter + Vector3.forward * checkRadius, // Forward
                checkCenter - Vector3.forward * checkRadius, // Backward
                checkCenter + Vector3.right * checkRadius, // Right
                checkCenter - Vector3.right * checkRadius // Left
            };
        }
        else
        {
            // Fallback to simple check points
            checkPoints = new Vector3[]
            {
                position, // Center
                position + Vector3.forward * 0.3f, // Forward
                position - Vector3.forward * 0.3f, // Backward
                position + Vector3.right * 0.3f, // Right
                position - Vector3.right * 0.3f // Left
            };
        }

        foreach (Vector3 checkPoint in checkPoints)
        {
            RaycastHit hit;
            Vector3 rayOrigin = checkPoint + Vector3.up * groundCheckOffset;
            
            // Cast ray downward
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                if (!foundGround || hit.point.y > bestGroundY)
                {
                    bestGroundY = hit.point.y;
                    foundGround = true;
                }
            }
        }

        // If no ground found, try a longer raycast from above
        if (!foundGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
            {
                bestGroundY = hit.point.y;
                foundGround = true;
            }
        }

        return foundGround ? bestGroundY : float.MinValue;
    }
}
