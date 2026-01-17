using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Save Slot Button - Displays slot info and handles slot selection
/// Responsibilities:
/// - Display slot info (empty vs used)
/// - Handle click to start game
/// - Update UI based on slot state
/// - Notify MainMenuController when selected
/// </summary>
public class SaveSlotButton : MonoBehaviour, ISelectHandler
{
    [Header("Slot Configuration")]
    [SerializeField] private int slotIndex = 1; // 1, 2, or 3

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI slotTitleText;
    [SerializeField] private TextMeshProUGUI slotInfoText;
    [SerializeField] private Button slotButton;

    private SaveSlotInfo slotInfo;

    private void Awake()
    {
        // Auto-find components if not assigned
        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (slotTitleText == null)
            slotTitleText = GetComponentInChildren<TextMeshProUGUI>();

        // Setup button listener
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    private void Start()
    {
        RefreshSlotDisplay();
    }

    /// <summary>
    /// Get the slot index (1-3)
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }
    
    /// <summary>
    /// Set the slot index (1-3)
    /// </summary>
    public void SetSlotIndex(int index)
    {
        slotIndex = Mathf.Clamp(index, 1, 3);
        RefreshSlotDisplay();
    }

    /// <summary>
    /// Refresh the slot display with current data
    /// </summary>
    public void RefreshSlotDisplay()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager not found. Cannot refresh slot display.");
            return;
        }

        slotInfo = SaveManager.Instance.GetSlotInfo(slotIndex);
        UpdateUI();
    }

    /// <summary>
    /// Update UI based on slot state
    /// </summary>
    private void UpdateUI()
    {
        if (slotInfo == null)
        {
            slotInfo = new SaveSlotInfo { hasSave = false };
        }

        if (slotTitleText != null)
        {
            if (slotInfo.hasSave)
            {
                slotTitleText.text = $"Slot {slotIndex}";
            }
            else
            {
                slotTitleText.text = "New Game";
            }
        }

        if (slotInfoText != null)
        {
            if (slotInfo.hasSave)
            {
                // Format play time using GameManager utility
                string timeString = GameManager.FormatPlayTime(slotInfo.playTime);

                slotInfoText.text = $"Level: {slotInfo.lastLevel}\n" +
                                    $"Time: {timeString}\n" +
                                    $"Saved: {slotInfo.lastSaveDate}";
            }
            else
            {
                slotInfoText.text = "Empty Slot";
            }
        }
    }

    /// <summary>
    /// Handle slot button click
    /// Per spec: SaveSlotButton -> SaveManager.SelectSlot() -> GameManager.StartGame()
    /// </summary>
    private void OnSlotClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager not found. Cannot select slot.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found. Cannot start game.");
            return;
        }

        // Step 1: Select slot in SaveManager
        SaveManager.Instance.SelectSlot(slotIndex);

        // Step 2: Start game via GameManager
        GameManager.Instance.StartGame(slotIndex);
    }
    
    /// <summary>
    /// Called when this button is selected (for gamepad navigation)
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        // Notify MainMenuController that this slot is selected
        // We need to find MainMenuController - it should be in the scene
        MainMenuController menuController = FindObjectOfType<MainMenuController>();
        if (menuController != null)
        {
            menuController.SetSelectedSlot(slotIndex);
        }
    }
}
