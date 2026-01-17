using UnityEngine;
using System.Collections;

/// <summary>
/// Handles player death, respawn, and soul dropping (Elden Ring style)
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 1.2f;
    [SerializeField] private GameObject lostSoulPrefab;
    [SerializeField] private float soulPickupYOffset = 0.5f; // Y offset above ground for soul pickup

    private GameObject activeLostSoul;
    private PlayerHealth health;
    private PlayerSoul soul;
    private PlayerCurrency currency;
    private CharacterController controller;
    private PlayerMovement movement;
    private bool isDead = false;

    public bool IsDead => isDead;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        soul = GetComponent<PlayerSoul>();
        currency = GetComponent<PlayerCurrency>();
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// Handle player death - drop souls and start respawn routine
    /// </summary>
    public void HandleDeath()
    {
        if (isDead) return;
        
        isDead = true;
        DropSouls();
        StartCoroutine(RespawnRoutine());
    }

    /// <summary>
    /// Drop all collected souls and coins at death location (Elden Ring style)
    /// Always creates a pickup, even if amounts are 0
    /// If player dies again before recovering, the previous pickup is destroyed
    /// </summary>
    private void DropSouls()
    {
        // Destroy previous lost soul if it exists (player died again before recovering)
        if (activeLostSoul != null)
        {
            LostSoulPickup previousPickup = activeLostSoul.GetComponent<LostSoulPickup>();
            if (previousPickup != null)
            {
                previousPickup.OnPlayerDeath();
            }
            else
            {
                // Fallback: destroy directly if component not found
                Destroy(activeLostSoul);
            }
            activeLostSoul = null;
        }

        // Get amounts to drop (can be 0)
        int soulsToDrop = soul != null ? soul.GetSouls() : 0;
        int coinsToDrop = currency != null ? currency.GetCoins() : 0;

        // Try to get prefab from inspector, otherwise load from Resources
        GameObject prefabToSpawn = lostSoulPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("Prefabs/LostSoulPickup");
        }

        if (prefabToSpawn != null)
        {
            // Calculate spawn position with Y offset above ground
            Vector3 spawnPosition = transform.position;
            spawnPosition.y += soulPickupYOffset;
            
            // Always spawn lost soul pickup at death location (even if amounts are 0)
            activeLostSoul = Instantiate(
                prefabToSpawn,
                spawnPosition,
                Quaternion.identity
            );

            LostSoulPickup pickup = activeLostSoul.GetComponent<LostSoulPickup>();
            if (pickup != null)
            {
                pickup.Initialize(soulsToDrop, coinsToDrop); // Can be 0
            }
        }
        else
        {
            Debug.LogWarning("LostSoulPickup prefab not found. Assign it in PlayerDeath inspector or place in Resources/Prefabs/LostSoulPickup");
        }

        // Clear current souls and coins
        if (soul != null)
        {
            soul.ClearSouls();
        }
        if (currency != null)
        {
            currency.ClearCoins();
        }
    }

    /// <summary>
    /// Respawn routine - disable player, wait, then respawn at checkpoint
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        // Ensure game is not paused
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }

        // Disable movement and controller
        if (controller != null)
        {
            controller.enabled = false;
        }
        if (movement != null)
        {
            movement.enabled = false;
        }

        // TODO: Add death animation / VFX / sound

        yield return new WaitForSeconds(respawnDelay);

        // Respawn player at checkpoint
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RespawnPlayer();
        }

        // Reset health
        if (health != null)
        {
            health.ResetHealth();
        }

        // Re-enable movement and controller
        if (controller != null)
        {
            controller.enabled = true;
        }
        if (movement != null)
        {
            movement.enabled = true;
        }

        isDead = false;
    }

    // ========== Lost Soul Persistence ==========

    /// <summary>
    /// Capture lost soul state for saving
    /// </summary>
    public LostSoulSaveData CaptureLostSoul()
    {
        if (activeLostSoul == null)
        {
            return new LostSoulSaveData { exists = false };
        }

        LostSoulPickup pickup = activeLostSoul.GetComponent<LostSoulPickup>();
        if (pickup == null)
        {
            return new LostSoulSaveData { exists = false };
        }

        return new LostSoulSaveData
        {
            exists = true,
            position = activeLostSoul.transform.position,
            amount = pickup.StoredSouls, // For backward compatibility
            souls = pickup.StoredSouls,
            coins = pickup.StoredCoins
        };
    }

    /// <summary>
    /// Restore lost soul from save data
    /// Call this after player spawns (in LevelManager after RestoreAllSaveables)
    /// </summary>
    public void RestoreLostSoul(LostSoulSaveData data)
    {
        if (!data.exists) return;

        // Destroy any existing lost soul first
        if (activeLostSoul != null)
        {
            Destroy(activeLostSoul);
            activeLostSoul = null;
        }

        // Try to get prefab from inspector, otherwise load from Resources
        GameObject prefabToSpawn = lostSoulPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("Prefabs/LostSoulPickup");
        }

        if (prefabToSpawn != null)
        {
            // Spawn lost soul at saved position
            activeLostSoul = Instantiate(
                prefabToSpawn,
                data.position,
                Quaternion.identity
            );

            LostSoulPickup pickup = activeLostSoul.GetComponent<LostSoulPickup>();
            if (pickup != null)
            {
                // Use new format if available, otherwise fallback to old format for backward compatibility
                int soulsToRestore = data.souls > 0 || data.amount > 0 ? (data.souls > 0 ? data.souls : data.amount) : 0;
                int coinsToRestore = data.coins;
                pickup.Initialize(soulsToRestore, coinsToRestore);
            }

            Debug.Log($"Restored lost soul at {data.position} with {data.souls} souls and {data.coins} coins");
        }
        else
        {
            Debug.LogWarning("LostSoulPickup prefab not found. Cannot restore lost soul.");
        }
    }
}
