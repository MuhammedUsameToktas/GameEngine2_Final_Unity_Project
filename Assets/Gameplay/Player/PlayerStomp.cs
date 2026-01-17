using UnityEngine;

/// <summary>
/// Player stomp attack - jump on enemies to kill them (Mario 64 style)
/// Player bounces like a trampoline when landing on enemies
/// </summary>
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerGroundCheck))]
public class PlayerStomp : MonoBehaviour
{
    [Header("Stomp Settings")]
    [SerializeField] private float stompDetectionRadius = 1f;
    [SerializeField] private float stompDetectionHeight = 0.5f; // How far below player to check
    [SerializeField] private float bounceForce = 12f; // How high player bounces after stomping
    [SerializeField] private LayerMask enemyLayer = -1;

    [Header("Stomp Detection")]
    [SerializeField] private float detectionCooldown = 0.1f; // Prevent multiple stomps in quick succession

    private PlayerJump playerJump;
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private float lastStompTime = 0f;
    private bool wasGrounded = false;

    private void Awake()
    {
        playerJump = GetComponent<PlayerJump>();
        groundCheck = GetComponent<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        bool currentlyGrounded = groundCheck != null && groundCheck.IsGrounded;
        
        // Check for stomp while falling (before landing) to prevent contact damage
        // This allows us to kill the enemy before the player takes damage
        if (!currentlyGrounded && movement != null && movement.VerticalVelocity < 0f)
        {
            // Player is falling - check for enemies below
            CheckForStompWhileFalling();
        }
        // Also check when landing as backup
        else if (!wasGrounded && currentlyGrounded)
        {
            CheckForStomp();
        }
        
        wasGrounded = currentlyGrounded;
    }

    /// <summary>
    /// Check for stomp while falling - detects enemies before landing to prevent contact damage
    /// </summary>
    private void CheckForStompWhileFalling()
    {
        // Cooldown to prevent multiple stomps
        if (Time.time < lastStompTime + detectionCooldown)
            return;

        // Check for enemies directly below player while falling
        Vector3 detectionOrigin = transform.position;
        
        // Cast a ray downward to find enemies
        RaycastHit[] hits = Physics.SphereCastAll(
            detectionOrigin,
            stompDetectionRadius,
            Vector3.down,
            stompDetectionHeight * 2f, // Check further down while falling
            enemyLayer
        );

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null)
            {
                GameObject enemy = hit.collider.gameObject;
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                
                // Only stomp if enemy is alive and below player
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    float enemyY = enemy.transform.position.y;
                    float playerY = transform.position.y;
                    
                    if (enemyY < playerY)
                    {
                        float distance = hit.distance;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestEnemy = enemy;
                        }
                    }
                }
            }
        }

        if (closestEnemy != null)
        {
            StompEnemy(closestEnemy);
        }
    }

    /// <summary>
    /// Check for stomp when landing (backup method)
    /// </summary>
    private void CheckForStomp()
    {
        // Cooldown to prevent multiple stomps
        if (Time.time < lastStompTime + detectionCooldown)
            return;

        // Check for enemies below player using a larger detection area
        Vector3 detectionOrigin = transform.position;
        detectionOrigin.y -= stompDetectionHeight;

        // Use a larger radius and check all colliders
        Collider[] hitColliders = Physics.OverlapSphere(detectionOrigin, stompDetectionRadius, enemyLayer);

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in hitColliders)
        {
            // Check if enemy is actually below player (not just nearby)
            float enemyY = col.transform.position.y;
            float playerY = transform.position.y;
            
            if (enemyY < playerY)
            {
                EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    // Check distance to find closest enemy
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = col.gameObject;
                    }
                }
            }
        }

        if (closestEnemy != null)
        {
            StompEnemy(closestEnemy);
        }
    }

    private void StompEnemy(GameObject enemy)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null && !enemyHealth.IsDead)
        {
            lastStompTime = Time.time;

            // IMPORTANT: Disable enemy damage BEFORE killing it to prevent contact damage
            // This ensures player doesn't take damage when stomping
            EnemyDamage enemyDamage = enemy.GetComponent<EnemyDamage>();
            if (enemyDamage != null)
            {
                enemyDamage.enabled = false;
            }
            
            // Also disable damage colliders immediately
            Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
            foreach (Collider col in enemyColliders)
            {
                EnemyDamage damageComp = col.GetComponent<EnemyDamage>();
                if (damageComp != null)
                {
                    col.enabled = false;
                }
            }

            // Kill enemy with stomp flag (this will trigger squish animation, no knockback)
            enemyHealth.TakeDamage(999, true); // true = is stomp death

            // Bounce player upward (like trampoline) - do this after killing enemy
            if (movement != null)
            {
                movement.SetVerticalVelocity(bounceForce);
            }

            // Trigger enemy death animation with DOTween (handled by EnemyDeath)
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw stomp detection area
        Vector3 detectionOrigin = transform.position;
        detectionOrigin.y -= stompDetectionHeight;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(detectionOrigin, stompDetectionRadius);
        
        // Draw line from player to detection point
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, detectionOrigin);
    }
}
