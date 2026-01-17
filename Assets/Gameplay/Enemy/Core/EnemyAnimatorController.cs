using UnityEngine;

/// <summary>
/// Controls enemy animations based on state
/// Handles idle, patrol, chase, attack, take damage, and die animations
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    // Animator parameter hashes
    private static readonly int IsIdleHash = Animator.StringToHash("IsIdle");
    private static readonly int IsPatrollingHash = Animator.StringToHash("IsPatrolling");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");
    private static readonly int IsPreparingToAttackHash = Animator.StringToHash("IsPreparingToAttack");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsMeleeAttackHash = Animator.StringToHash("IsMeleeAttack");
    private static readonly int IsRangeAttackHash = Animator.StringToHash("IsRangeAttack");
    private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private EnemyBehavior enemyBehavior;
    private EnemyFlyingBehavior flyingBehavior;
    private EnemyAttack enemyAttack;
    private EnemyHealth enemyHealth;
    private EnemyDeath enemyDeath;

    private bool isDead = false;
    private float currentSpeed = 0f;

    private void Awake()
    {
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        enemyBehavior = GetComponent<EnemyBehavior>();
        flyingBehavior = GetComponent<EnemyFlyingBehavior>();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyDeath = GetComponent<EnemyDeath>();

        if (animator == null)
        {
            Debug.LogWarning($"EnemyAnimatorController on {gameObject.name}: No Animator found. Animation will not work.");
        }
    }

    private void Update()
    {
        if (animator == null || isDead) return;

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        // Reset all state bools
        animator.SetBool(IsIdleHash, false);
        animator.SetBool(IsPatrollingHash, false);
        animator.SetBool(IsChasingHash, false);
        animator.SetBool(IsPreparingToAttackHash, false);
        animator.SetBool(IsAttackingHash, false);
        animator.SetBool(IsMeleeAttackHash, false);
        animator.SetBool(IsRangeAttackHash, false);

        // Determine state based on behavior components
        if (enemyBehavior != null)
        {
            UpdateGroundEnemyAnimations();
        }
        else if (flyingBehavior != null)
        {
            UpdateFlyingEnemyAnimations();
        }
        else
        {
            // Fallback: use idle if no behavior component
            animator.SetBool(IsIdleHash, true);
        }

        // Update speed
        animator.SetFloat(SpeedHash, currentSpeed);
    }

    private void UpdateGroundEnemyAnimations()
    {
        EnemyBehavior.EnemyState state = enemyBehavior.CurrentState;

        switch (state)
        {
            case EnemyBehavior.EnemyState.Patrolling:
                // If waiting at waypoint, use idle animation; otherwise use patrol animation
                if (enemyBehavior.IsWaiting)
                {
                    animator.SetBool(IsIdleHash, true);
                    currentSpeed = 0f; // Not moving when idle
                }
                else
                {
                    animator.SetBool(IsPatrollingHash, true);
                    currentSpeed = 0.5f; // Walking speed
                }
                break;

            case EnemyBehavior.EnemyState.Following:
                animator.SetBool(IsChasingHash, true);
                currentSpeed = 1f; // Running speed
                break;

            case EnemyBehavior.EnemyState.PreparingToAttack:
                animator.SetBool(IsPreparingToAttackHash, true);
                currentSpeed = 0f; // Not moving, preparing to attack
                break;

            case EnemyBehavior.EnemyState.Attacking:
                animator.SetBool(IsAttackingHash, true);
                currentSpeed = 0f;

                // Set attack type if enemy has attack component
                if (enemyAttack != null)
                {
                    if (enemyAttack.IsMelee)
                    {
                        animator.SetBool(IsMeleeAttackHash, true);
                    }
                    if (enemyAttack.IsRanged)
                    {
                        animator.SetBool(IsRangeAttackHash, true);
                    }
                }
                break;

            case EnemyBehavior.EnemyState.Returning:
                animator.SetBool(IsPatrollingHash, true);
                currentSpeed = 0.5f;
                break;
        }
    }

    private void UpdateFlyingEnemyAnimations()
    {
        EnemyFlyingBehavior.EnemyState state = flyingBehavior.CurrentState;

        switch (state)
        {
            case EnemyFlyingBehavior.EnemyState.Patrolling:
                // If waiting at waypoint, use idle animation; otherwise use patrol animation
                if (flyingBehavior.IsWaiting)
                {
                    animator.SetBool(IsIdleHash, true);
                    currentSpeed = 0f; // Not moving when idle
                }
                else
                {
                    animator.SetBool(IsPatrollingHash, true);
                    currentSpeed = 0.3f; // Flying patrol speed
                }
                break;

            case EnemyFlyingBehavior.EnemyState.Following:
                animator.SetBool(IsChasingHash, true);
                currentSpeed = 1f;
                break;

            case EnemyFlyingBehavior.EnemyState.PreparingToAttack:
                animator.SetBool(IsPreparingToAttackHash, true);
                currentSpeed = 0f; // Not moving, preparing to attack
                break;

            case EnemyFlyingBehavior.EnemyState.Attacking:
                animator.SetBool(IsAttackingHash, true);
                currentSpeed = 0f;

                if (enemyAttack != null)
                {
                    if (enemyAttack.IsMelee)
                    {
                        animator.SetBool(IsMeleeAttackHash, true);
                    }
                    if (enemyAttack.IsRanged)
                    {
                        animator.SetBool(IsRangeAttackHash, true);
                    }
                }
                break;

            case EnemyFlyingBehavior.EnemyState.Returning:
                animator.SetBool(IsPatrollingHash, true);
                currentSpeed = 0.3f;
                break;
        }
    }

    /// <summary>
    /// Trigger take damage animation
    /// Called by EnemyHealth when enemy takes damage
    /// </summary>
    public void OnTakeDamage()
    {
        if (animator != null && !isDead)
        {
            animator.SetTrigger(TakeDamageHash);
        }
    }

    /// <summary>
    /// Trigger death animation
    /// Called by EnemyHealth when enemy dies
    /// </summary>
    public void OnDie()
    {
        if (animator != null && !isDead)
        {
            isDead = true;
            animator.SetTrigger(DieHash);
        }
    }

    private void OnEnable()
    {
        // Subscribe to enemy health events if available
        if (enemyHealth != null)
        {
            // We'll need to modify EnemyHealth to call these methods
            // For now, we'll check in Update
        }
    }
}
