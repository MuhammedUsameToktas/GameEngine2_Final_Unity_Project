using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player Inventory System - Manages player's items
/// Stores Item IDs + quantities (NOT GameObjects or scene references)
/// Quest-ready, shop-ready, crafting-ready architecture
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    /// <summary>
    /// Add item to inventory
    /// </summary>
    public void AddItem(string itemID, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("PlayerInventory: Attempted to add item with null/empty ID");
            return;
        }

        if (amount < 0)
        {
            Debug.LogWarning($"PlayerInventory: Attempted to add negative amount ({amount}) of {itemID}");
            return;
        }

        if (items.ContainsKey(itemID))
        {
            items[itemID] += amount;
        }
        else
        {
            items[itemID] = amount;
        }

        Debug.Log($"PlayerInventory: Added {amount}x {itemID}. Total: {items[itemID]}");
    }

    /// <summary>
    /// Check if player has item(s)
    /// </summary>
    public bool HasItem(string itemID, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemID)) return false;
        
        return items.ContainsKey(itemID) && items[itemID] >= amount;
    }

    /// <summary>
    /// Get quantity of a specific item
    /// </summary>
    public int GetItemQuantity(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return 0;
        
        return items.ContainsKey(itemID) ? items[itemID] : 0;
    }

    /// <summary>
    /// Remove item from inventory
    /// </summary>
    public bool RemoveItem(string itemID, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("PlayerInventory: Attempted to remove item with null/empty ID");
            return false;
        }

        if (!HasItem(itemID, amount))
        {
            Debug.LogWarning($"PlayerInventory: Cannot remove {amount}x {itemID}. Player has {GetItemQuantity(itemID)}");
            return false;
        }

        items[itemID] -= amount;
        
        if (items[itemID] <= 0)
        {
            items.Remove(itemID);
        }

        Debug.Log($"PlayerInventory: Removed {amount}x {itemID}");
        return true;
    }

    /// <summary>
    /// Get all items in inventory (for UI display)
    /// </summary>
    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(items);
    }

    /// <summary>
    /// Clear all items (for testing or new game)
    /// </summary>
    public void Clear()
    {
        items.Clear();
        Debug.Log("PlayerInventory: Cleared all items");
    }
}
