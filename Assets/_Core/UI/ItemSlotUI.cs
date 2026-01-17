using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Item Slot UI - Displays a single inventory item in the UI
/// Shows icon, quantity, and optionally name
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text nameText; // Optional

    private string currentItemID;

    /// <summary>
    /// Setup this slot with item data
    /// </summary>
    public void Setup(string itemID, int quantity)
    {
        currentItemID = itemID;

        // Load ItemData from Resources or use a registry
        ItemData itemData = LoadItemData(itemID);

        if (itemData != null)
        {
            // Set icon
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
            }

            // Set name (optional)
            if (nameText != null)
            {
                nameText.text = itemData.displayName;
            }
        }
        else
        {
            // Fallback: show itemID if no ItemData found
            if (nameText != null)
            {
                nameText.text = itemID;
            }
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
        }

        // Set quantity
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Load ItemData from Resources folder
    /// TODO: Consider using a ScriptableObject registry for better performance
    /// </summary>
    private ItemData LoadItemData(string itemID)
    {
        // Try to load from Resources/Items folder
        ItemData itemData = Resources.Load<ItemData>($"Items/{itemID}");
        
        if (itemData == null)
        {
            // Fallback: search all ItemData assets (slower, but works)
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item.itemID == itemID)
                {
                    return item;
                }
            }
        }

        return itemData;
    }
}
