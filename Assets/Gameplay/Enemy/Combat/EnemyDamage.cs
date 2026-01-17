using UnityEngine;

/// <summary>
/// Contact damage system for enemies (Souls-like)
/// Applies damage and knockback when player touches the enemy
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 1;
    
    [Header("Knockback Settings")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Don't damage player if enemy is dead or has been hit
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            return; // Enemy is dead, can't damage player
        }

        // Check if this component is disabled (enemy was hit)
        if (!enabled)
        {
            return; // Component disabled, can't damage player
        }

        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Calculate knockback direction (away from enemy)
            Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;
            knockbackDirection.y = 0; // Keep knockback horizontal
            
            health.TakeDamage(damage, knockbackDirection, applyKnockback ? knockbackForce : 0f);
        }
    }
}
