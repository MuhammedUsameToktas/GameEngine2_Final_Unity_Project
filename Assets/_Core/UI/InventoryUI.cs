using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inventory UI - Displays player inventory in pause menu
/// Automatically refreshes when enabled
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform content; // Parent for item slots (e.g., VerticalLayoutGroup)
    [SerializeField] private GameObject itemSlotPrefab;

    private PlayerInventory inventory;

    private void OnEnable()
    {
        // Small delay to ensure player is found (in case player spawns dynamically)
        Invoke(nameof(Refresh), 0.1f);
    }

    private void OnDisable()
    {
        // Cancel any pending refresh
        CancelInvoke(nameof(Refresh));
    }

    /// <summary>
    /// Refresh the inventory display
    /// </summary>
    public void Refresh()
    {
        // Find player inventory
        if (inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                inventory = player.GetComponent<PlayerInventory>();
            }

            // Fallback: try LevelManager
            if (inventory == null && LevelManager.Instance != null)
            {
                GameObject playerInstance = LevelManager.Instance.GetPlayerInstance();
                if (playerInstance != null)
                {
                    inventory = playerInstance.GetComponent<PlayerInventory>();
                }
            }

            // Final fallback: FindObjectOfType
            if (inventory == null)
            {
                inventory = FindObjectOfType<PlayerInventory>();
            }
        }

        if (inventory == null)
        {
            Debug.LogWarning("InventoryUI: Could not find PlayerInventory component!");
            return;
        }

        if (content == null)
        {
            Debug.LogWarning("InventoryUI: Content transform not assigned!");
            return;
        }

        if (itemSlotPrefab == null)
        {
            Debug.LogWarning("InventoryUI: Item slot prefab not assigned!");
            return;
        }

        // Clear existing slots
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Create slots for each item
        Dictionary<string, int> allItems = inventory.GetAllItems();
        
        if (allItems.Count == 0)
        {
            // Optional: Show "Inventory Empty" message
            return;
        }

        foreach (var item in allItems)
        {
            GameObject slot = Instantiate(itemSlotPrefab, content);
            ItemSlotUI slotUI = slot.GetComponent<ItemSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(item.Key, item.Value);
            }
            else
            {
                Debug.LogWarning($"InventoryUI: Item slot prefab does not have ItemSlotUI component!");
            }
        }
    }
}
