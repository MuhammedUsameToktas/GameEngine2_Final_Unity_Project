using UnityEngine;

/// <summary>
/// Enemy health system
/// Supports one-hit kills from player attacks
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsStompDeath { get; private set; } // Track if death was from stomp

    private EnemyDeath death;
    private EnemyAnimatorController animatorController;
    private EnemyAttack enemyAttack;
    private EnemyDamage enemyDamage;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        death = GetComponent<EnemyDeath>();
        animatorController = GetComponent<EnemyAnimatorController>();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyDamage = GetComponent<EnemyDamage>();
    }

    public void TakeDamage(int amount = 1)
    {
        TakeDamage(amount, false);
    }

    /// <summary>
    /// Take damage with optional stomp flag
    /// </summary>
    public void TakeDamage(int amount, bool isStomp)
    {
        if (IsDead) return;

        // Set stomp death flag if this is a stomp
        if (isStomp)
        {
            IsStompDeath = true;
        }

        // Immediately disable all damage-dealing components so enemy can't damage player after being hit
        if (enemyAttack != null)
        {
            enemyAttack.enabled = false; // Disable attack component
        }
        
        if (enemyDamage != null)
        {
            enemyDamage.enabled = false; // Disable contact damage component
        }
        
        // Also disable damage colliders/triggers to prevent any accidental damage
        DisableDamageColliders();

        // Trigger take damage animation
        if (animatorController != null)
        {
            animatorController.OnTakeDamage();
        }

        CurrentHealth -= amount;

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            
            // Trigger death animation
            if (animatorController != null)
            {
                animatorController.OnDie();
            }
            
            // Handle death (spawn coins, etc.) - pass stomp flag
            if (death != null)
            {
                death.HandleDeath(IsStompDeath);
            }
        }
    }

    /// <summary>
    /// Disable all colliders that could damage the player
    /// </summary>
    private void DisableDamageColliders()
    {
        // Get all colliders on this enemy and its children
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        
        foreach (Collider col in allColliders)
        {
            // Check if this collider is used for damage (has EnemyDamage component)
            EnemyDamage damageComponent = col.GetComponent<EnemyDamage>();
            if (damageComponent != null)
            {
                // Disable the collider to prevent contact damage
                col.enabled = false;
            }
        }
    }
}
