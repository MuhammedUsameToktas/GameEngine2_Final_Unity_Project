using UnityEngine;

/// <summary>
/// Projectile fired by ranged enemies
/// Moves forward and damages player on contact
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f; // Auto-destroy after this time
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 3f;

    [Header("Visual")]
    [SerializeField] private GameObject hitEffect; // Optional VFX on hit

    private Vector3 direction;
    private float spawnTime;
    private Rigidbody rb; // Optional rigidbody for physics

    private void Awake()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
        
        // Get rigidbody if it exists (optional)
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Use kinematic for transform-based movement
        }
    }

    private void Start()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        // Move projectile forward
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        
        // Auto-destroy after lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            DestroyProjectile();
        }
    }

    /// <summary>
    /// Initialize projectile with direction
    /// </summary>
    public void Initialize(Vector3 shootDirection, int projectileDamage = 1)
    {
        direction = shootDirection.normalized;
        damage = projectileDamage;
        
        // Rotate projectile to face direction
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't hit the enemy that fired it
        if (other.CompareTag("Enemy")) return;
        
        // Hit player
        if (other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;
                knockbackDirection.y = 0;
                knockbackDirection.Normalize();
                
                playerHealth.TakeDamage(damage, knockbackDirection, applyKnockback ? knockbackForce : 0f);
            }
            
            // Spawn hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            DestroyProjectile();
        }
        // Hit walls/obstacles
        else if (!other.isTrigger)
        {
            // Spawn hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}
