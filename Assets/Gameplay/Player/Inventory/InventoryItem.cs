using System;

/// <summary>
/// Inventory Item - Serializable struct for save system
/// Represents an item in the player's inventory
/// </summary>
[Serializable]
public struct InventoryItem
{
    public string itemID;
    public int quantity;
}
