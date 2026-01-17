using UnityEngine;

/// <summary>
/// Kill zone - environment hazard that instantly kills the player on contact
/// Place on trigger colliders (spikes, lava, bottomless pits, etc.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool requirePlayerTag = true;

    private void Start()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"KillZone on {gameObject.name}: Collider was not set to trigger. Auto-fixed.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (requirePlayerTag && !other.CompareTag("Player"))
        {
            return;
        }

        // Try to get PlayerHealth component
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            // Kill the player
            playerHealth.TakeDamage(999); // Massive damage to ensure death
        }
    }
}
