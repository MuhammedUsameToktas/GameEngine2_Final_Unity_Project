using UnityEngine;

/// <summary>
/// Soul Zone - Continuously gives souls to player while inside (for testing)
/// Simulates soul farming areas or soul collection zones
/// </summary>
[RequireComponent(typeof(Collider))]
public class SoulZone : MonoBehaviour
{
    [Header("Soul Settings")]
    [SerializeField] private int soulsPerTick = 5;
    [SerializeField] private float gainInterval = 0.5f;

    private float timer;

    private void Start()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"SoulZone on {gameObject.name}: Collider was not set to trigger. Auto-fixed.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PlayerSoul playerSoul = other.GetComponent<PlayerSoul>();
            if (playerSoul != null)
            {
                playerSoul.AddSouls(soulsPerTick);
                timer = gainInterval;
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
