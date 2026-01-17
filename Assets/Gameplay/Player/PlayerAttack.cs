using UnityEngine;

/// <summary>
/// Player melee attack system
/// Attacks enemies in front of player with knockback
/// One attack kills enemies
/// </summary>
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackAngle = 60f; // Attack cone angle
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private LayerMask enemyLayer = -1;

    [Header("Attack Detection")]
    [SerializeField] private Transform attackPoint; // Optional: specific attack point
    [SerializeField] private float attackPointOffset = 1f; // Forward offset from player center

    [Header("Animation")]
    [SerializeField] private Animator animator;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private PlayerInputHandler input;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    private void Update()
    {
        // Check for attack input
        if (input != null && input.AttackPressed && CanAttack())
        {
            PerformAttack();
        }
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown && !isAttacking;
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        isAttacking = true;

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        // Play punch sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Punch");
        }

        // Detect enemies in attack range
        DetectAndDamageEnemies();

        // Reset attacking state after a short delay (allows animation to play)
        Invoke(nameof(ResetAttackState), 0.1f);
    }

    private void DetectAndDamageEnemies()
    {
        Vector3 attackOrigin = attackPoint != null ? attackPoint.position : transform.position + transform.forward * attackPointOffset;
        
        // Find all enemies in range
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, attackRange, enemyLayer);

        foreach (Collider col in hitColliders)
        {
            // Check if enemy is within attack angle
            Vector3 directionToEnemy = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToEnemy);

            if (angle <= attackAngle * 0.5f)
            {
                // Attack this enemy
                AttackEnemy(col.gameObject);
            }
        }
    }

    private void AttackEnemy(GameObject enemy)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null && !enemyHealth.IsDead)
        {
            // Calculate knockback direction: away from player (opposite of attack direction)
            Vector3 knockbackDirection = (enemy.transform.position - transform.position);
            knockbackDirection.y = 0; // Keep horizontal only - no vertical knockback
            knockbackDirection.Normalize();
            
            // Ensure direction is valid (not zero)
            if (knockbackDirection.magnitude < 0.1f)
            {
                // Fallback: use enemy's forward direction if positions are too close
                knockbackDirection = -transform.forward;
            }
            
            // Apply knockback first (before damage) so enemy bounces back
            EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
            if (enemyKnockback != null)
            {
                enemyKnockback.ApplyKnockback(knockbackDirection, knockbackForce);
            }
            else
            {
                // Fallback to Rigidbody if available
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                }
            }
            
            // Damage enemy (one hit kill) - this will disable attacks and trigger death
            enemyHealth.TakeDamage(999); // Large damage to ensure kill
        }
    }

    private void ResetAttackState()
    {
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Vector3 attackOrigin = attackPoint != null ? attackPoint.position : transform.position + transform.forward * attackPointOffset;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin, attackRange);
        
        // Draw attack angle
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(attackOrigin, leftBoundary * attackRange);
        Gizmos.DrawRay(attackOrigin, rightBoundary * attackRange);
    }
}
