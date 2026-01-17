using UnityEngine;

/// <summary>
/// Collectible - World item that can be collected once and persists across saves
/// Gives souls to player when collected
/// Uses unique ID system for save persistence (Hollow Knight / Dark Souls style)
/// </summary>
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour, ISaveable
{
    [Header("Collectible Settings")]
    [SerializeField] private string collectibleID;
    [SerializeField] private int soulValue = 10;
    
    [Header("Inventory Item (Optional)")]
    [Tooltip("If assigned, this collectible will add an item to inventory instead of/in addition to souls")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int itemQuantity = 1;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectEffect; // Optional VFX on collection

    private bool collected = false;
    private bool isRegistered = false;

    private void Awake()
    {
        // Register early in Awake to ensure we're registered before RestoreAllSaveables is called
        RegisterWithSaveSystem();
    }

    private void Start()
    {
        // Ensure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"Collectible on {gameObject.name}: Collider was not set to trigger. Auto-fixed.");
        }

        // Validate ID
        if (string.IsNullOrEmpty(collectibleID))
        {
            Debug.LogError($"Collectible on {gameObject.name} has no ID! Assign a unique ID in the Inspector.");
            return;
        }

        // Ensure we're registered (in case SaveManager wasn't ready in Awake)
        RegisterWithSaveSystem();
    }

    /// <summary>
    /// Register with save system
    /// </summary>
    private void RegisterWithSaveSystem()
    {
        if (isRegistered) return;
        
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
            isRegistered = true;
        }
    }

    private void OnDestroy()
    {
        // Unregister from save system
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        Collect(other);
    }

    /// <summary>
    /// Collect this item
    /// </summary>
    private void Collect(Collider player)
    {
        collected = true;

        // Give souls to player (if soulValue > 0)
        if (soulValue > 0)
        {
            PlayerSoul playerSoul = player.GetComponent<PlayerSoul>();
            if (playerSoul != null)
            {
                playerSoul.AddSouls(soulValue);
            }
        }

        // Add item to inventory (if itemData is assigned)
        if (itemData != null)
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddItem(itemData.itemID, itemQuantity);
            }
            else
            {
                Debug.LogWarning($"Collectible {collectibleID}: Player does not have PlayerInventory component!");
            }
        }

        // Spawn collect effect if assigned
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Hide the collectible (don't destroy - needed for save system)
        gameObject.SetActive(false);
    }

    // ========== ISaveable Implementation ==========

    /// <summary>
    /// Get unique ID for this collectible
    /// </summary>
    public string GetUniqueID()
    {
        return $"Collectible_{collectibleID}";
    }

    /// <summary>
    /// Capture state for saving
    /// </summary>
    public object CaptureState()
    {
        // Return a wrapper class because JsonUtility requires a class/struct, not a primitive
        return new CollectibleState { collected = this.collected };
    }

    /// <summary>
    /// Restore state from save
    /// </summary>
    public void RestoreState(object state)
    {
        if (state is CollectibleState savedState)
        {
            collected = savedState.collected;
            gameObject.SetActive(!collected);
            
            if (collected)
            {
                Debug.Log($"Collectible {collectibleID} restored as collected (hidden)");
            }
        }
        else if (state is bool wasCollected)
        {
            // Fallback for old save format
            collected = wasCollected;
            gameObject.SetActive(!collected);
        }
        else
        {
            Debug.LogWarning($"Collectible {collectibleID}: Failed to restore state - invalid type: {state?.GetType()}");
        }
    }
    
    // Wrapper class for serialization (JsonUtility requires a class/struct)
    [System.Serializable]
    private class CollectibleState
    {
        public bool collected;
    }
}
