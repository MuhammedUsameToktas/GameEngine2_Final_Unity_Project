using UnityEngine;

/// <summary>
/// Damage Zone - Continuously damages player while inside (for testing)
/// Simulates spikes, acid, lava, or enemy contact areas
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private int damageAmount = 1;

    private float timer;

    private void Start()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"DamageZone on {gameObject.name}: Collider was not set to trigger. Auto-fixed.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(damageAmount);
                timer = damageInterval;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            timer = 0f; // Reset timer when player leaves
        }
    }
}
