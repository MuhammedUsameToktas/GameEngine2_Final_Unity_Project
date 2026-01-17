using UnityEngine;

/// <summary>
/// Enemy attack behavior - can perform melee or ranged attacks
/// Works with EnemyPlayerDetector to attack when player is in range
/// </summary>
[RequireComponent(typeof(EnemyPlayerDetector))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackHitTime = 0.4f; // Time when damage is dealt (should match animation hit frame - adjust to match your animation)
    [SerializeField] private bool useAnimationEvents = false; // If true, damage is triggered by Animation Event instead of timer

    [Header("Attack Type")]
    [SerializeField] private bool isMelee = true;
    [SerializeField] private bool isRanged = false;
    [SerializeField] private GameObject projectilePrefab; // For ranged attacks
    [SerializeField] private Transform projectileSpawnPoint; // Where to spawn projectiles

    [Header("Knockback")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 5f;

    private EnemyPlayerDetector detector;
    private Animator animator;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private bool damageDealt = false; // Track if damage has been dealt for this attack

    public bool IsAttacking => isAttacking;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;
    public bool IsMelee => isMelee;
    public bool IsRanged => isRanged;

    private void Awake()
    {
        detector = GetComponent<EnemyPlayerDetector>();
        
        // Auto-find animator
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Auto-find spawn point if not assigned
        if (projectileSpawnPoint == null && isRanged)
        {
            projectileSpawnPoint = transform;
        }
    }

    private void Update()
    {
        // Only auto-attack if not controlled by EnemyBehavior or EnemyFlyingBehavior
        // Those components will call TryAttack() when needed
        if (GetComponent<EnemyBehavior>() == null && GetComponent<EnemyFlyingBehavior>() == null)
        {
            if (detector.IsPlayerDetected && CanAttack)
            {
                float distanceToPlayer = detector.DistanceToPlayer;
                
                if (distanceToPlayer <= attackRange)
                {
                    StartAttack();
                }
            }
        }
    }

    /// <summary>
    /// Public method for EnemyBehavior to trigger attacks
    /// </summary>
    public void TryAttack()
    {
        if (CanAttack && detector.IsPlayerDetected && detector.DistanceToPlayer <= attackRange)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        damageDealt = false;
        lastAttackTime = Time.time;

        // If using animation events, damage will be triggered by the animation
        // Otherwise, use timer to sync with animation hit frame
        if (!useAnimationEvents)
        {
            // Schedule damage to hit at the right time in the animation
            // attackHitTime should match when the attack animation actually hits
            Invoke(nameof(ExecuteAttack), attackHitTime);
        }
        // If useAnimationEvents is true, the damage will be triggered by OnAttackHit() Animation Event
    }

    /// <summary>
    /// Called by Animation Event at the hit frame of the attack animation
    /// Add this to your attack animation: Animation Event at the hit frame calling "OnAttackHit"
    /// </summary>
    public void OnAttackHit()
    {
        if (isAttacking && !damageDealt)
        {
            ExecuteAttack();
        }
    }

    private void ExecuteAttack()
    {
        if (detector.PlayerTransform == null || damageDealt)
        {
            // Don't reset isAttacking here - let it reset after animation completes
            return;
        }

        damageDealt = true;

        if (isMelee)
        {
            PerformMeleeAttack();
        }
        else if (isRanged)
        {
            PerformRangedAttack();
        }

        // Reset attacking state after a short delay to allow animation to finish
        // This ensures the animation plays fully before allowing next attack
        Invoke(nameof(ResetAttackState), 0.1f);
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        damageDealt = false;
    }

    private void PerformMeleeAttack()
    {
        // Check if player is still in range
        float distanceToPlayer = detector.DistanceToPlayer;
        if (distanceToPlayer > attackRange * 1.5f) return; // Allow slight margin

        // Damage player
        var playerHealth = detector.PlayerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Vector3 knockbackDirection = detector.GetDirectionToPlayer();
            playerHealth.TakeDamage(attackDamage, knockbackDirection, applyKnockback ? knockbackForce : 0f);
        }
    }

    private void PerformRangedAttack()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            Debug.LogWarning($"EnemyAttack on {gameObject.name}: Ranged attack enabled but projectile prefab or spawn point not set.");
            return;
        }

        // Spawn projectile
        Vector3 directionToPlayer = detector.GetDirectionToPlayer();
        GameObject projectileObj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(directionToPlayer));
        
        // Initialize projectile with damage and direction
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(directionToPlayer, attackDamage);
        }
        else
        {
            Debug.LogWarning($"EnemyAttack on {gameObject.name}: Projectile prefab doesn't have EnemyProjectile component. Projectile will not work correctly.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
