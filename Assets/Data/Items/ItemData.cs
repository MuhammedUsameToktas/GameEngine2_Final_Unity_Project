using UnityEngine;

/// <summary>
/// Item Data - ScriptableObject definition for items
/// Used by inventory, shop, quests, and UI
/// Pure data - no scene references
/// </summary>
[CreateAssetMenu(menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Item Identity")]
    [Tooltip("Unique identifier for this item (e.g., 'gold_key', 'wolf_pelt')")]
    public string itemID;
    
    [Tooltip("Display name shown in UI")]
    public string displayName;
    
    [Header("Visual")]
    [Tooltip("Icon shown in inventory/shop UI")]
    public Sprite icon;
    
    [Header("Item Properties")]
    [Tooltip("Can this item stack? (e.g., keys don't stack, materials do)")]
    public bool stackable = true;
    
    [Tooltip("Optional description for tooltips")]
    [TextArea(2, 4)]
    public string description;
}
