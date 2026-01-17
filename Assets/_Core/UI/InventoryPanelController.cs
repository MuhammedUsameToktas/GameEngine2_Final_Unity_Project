using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Inventory Panel Controller - Manages inventory panel visibility with full gamepad support
/// Works with PauseMenuController to show/hide inventory
/// Includes DOTween animations and button selection effects
/// </summary>
public class InventoryPanelController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject pauseMenuPanel; // Reference to main pause menu panel
    [SerializeField] private PauseMenuController pauseMenuController; // Reference to pause menu controller

    [Header("Buttons")]
    [SerializeField] private Button backButton; // Back button in inventory panel (optional - not required)

    [Header("Button Selection Effects")]
    [SerializeField] private float selectionMoveDistance = 30f;
    [SerializeField] private float selectionScaleAmount = 1.1f;
    [SerializeField] private Color selectionColor = new Color(1f, 0.8f, 0.5f, 1f);
    [SerializeField] private float selectionAnimationDuration = 0.2f;

    [Header("Input")]
    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private bool enableGamepadInput = true;

    private bool isInventoryOpen = false;
    private GameObject lastSelectedButton = null;
    private Dictionary<RectTransform, Vector3> originalButtonPositions = new Dictionary<RectTransform, Vector3>();
    private Dictionary<RectTransform, Vector3> originalButtonScales = new Dictionary<RectTransform, Vector3>();
    private Dictionary<RectTransform, Color> originalButtonColors = new Dictionary<RectTransform, Color>();
    private Dictionary<Button, Tweener> activeButtonTweens = new Dictionary<Button, Tweener>();
    private List<Button> inventoryButtons = new List<Button>();

    private void Awake()
    {
        // Auto-find pause menu controller if not assigned
        if (pauseMenuController == null)
        {
            pauseMenuController = FindObjectOfType<PauseMenuController>();
        }

        // Collect all buttons in inventory panel (back button is optional)
        if (inventoryPanel != null)
        {
            inventoryButtons.AddRange(inventoryPanel.GetComponentsInChildren<Button>());
        }

        // Add back button if found (optional - not required)
        if (backButton != null && !inventoryButtons.Contains(backButton))
        {
            inventoryButtons.Add(backButton);
        }
    }

    private void Start()
    {
        // Store original button states
        StoreOriginalButtonStates();

        // Setup button navigation
        SetupButtonNavigation();

        // Ensure inventory panel is hidden on start
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Handle button selection effects when inventory is open
        if (isInventoryOpen)
        {
            HandleButtonSelectionEffects();
            HandleCancelInput();
        }
    }

    private void OnDestroy()
    {
        // Clean up tweens
        foreach (var tween in activeButtonTweens.Values)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        activeButtonTweens.Clear();
    }

    /// <summary>
    /// Store original button positions, scales, and colors
    /// </summary>
    private void StoreOriginalButtonStates()
    {
        originalButtonPositions.Clear();
        originalButtonScales.Clear();
        originalButtonColors.Clear();

        foreach (var button in inventoryButtons)
        {
            if (button != null)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    originalButtonPositions[buttonRect] = buttonRect.anchoredPosition;
                    originalButtonScales[buttonRect] = buttonRect.localScale;

                    // Get Image component
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage == null && button.targetGraphic != null)
                    {
                        buttonImage = button.targetGraphic.GetComponent<Image>();
                    }
                    if (buttonImage == null)
                    {
                        buttonImage = button.GetComponentInChildren<Image>();
                    }
                    
                    if (buttonImage != null)
                    {
                        originalButtonColors[buttonRect] = buttonImage.color;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Setup button navigation for gamepad support
    /// </summary>
    private void SetupButtonNavigation()
    {
        foreach (var button in inventoryButtons)
        {
            if (button != null)
            {
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.Automatic;
                button.navigation = nav;
            }
        }
    }

    /// <summary>
    /// Handle Cancel input (Circle button on gamepad) and Start/Options button
    /// </summary>
    private void HandleCancelInput()
    {
        if (!isInventoryOpen) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Paused) return;

        // Check gamepad Circle button - returns to pause menu
        if (enableGamepadInput)
        {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                // Circle button - return to pause menu
                if (gamepad.buttonEast.wasPressedThisFrame) // Circle = buttonEast
                {
                    CloseInventory();
                    return; // Return to pause menu, don't close pause menu
                }
                
                // Start/Options button - return directly to game (close pause menu)
                if (gamepad.startButton.wasPressedThisFrame)
                {
                    // Close inventory first
                    CloseInventory();
                    
                    // Then close pause menu (return to game)
                    if (pauseMenuController != null)
                    {
                        pauseMenuController.ClosePause();
                    }
                    return;
                }
            }
        }

        // Check keyboard Esc - returns to pause menu
        if (enableKeyboardInput)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }
        }
    }

    /// <summary>
    /// Handle button selection effects
    /// </summary>
    private void HandleButtonSelectionEffects()
    {
        if (EventSystem.current == null) return;

        GameObject currentlySelected = EventSystem.current.currentSelectedGameObject;
        
        // If selection changed
        if (currentlySelected != lastSelectedButton)
        {
            // Reset previously selected button
            if (lastSelectedButton != null)
            {
                ResetButtonSelectionEffect(lastSelectedButton);
            }

            // Apply effect to newly selected button
            if (currentlySelected != null && inventoryButtons.Exists(b => b != null && b.gameObject == currentlySelected))
            {
                ApplyButtonSelectionEffect(currentlySelected);
            }

            lastSelectedButton = currentlySelected;
        }
    }

    /// <summary>
    /// Apply selection effect to a button
    /// </summary>
    private void ApplyButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        Button button = buttonObject.GetComponent<Button>();
        if (button != null && activeButtonTweens.ContainsKey(button))
        {
            activeButtonTweens[button].Kill();
            activeButtonTweens.Remove(button);
        }

        // Get original scale
        Vector3 originalScale = originalButtonScales.ContainsKey(buttonRect) 
            ? originalButtonScales[buttonRect] 
            : buttonRect.localScale;

        // Calculate target scale (grows from center)
        Vector3 targetScale = originalScale * selectionScaleAmount;

        // Animate scale - use SetUpdate(true) to work when timeScale is 0
        // Scale animates from center by default (pivot point)
        Tweener scaleTween = buttonRect.DOScale(targetScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // Critical: works when paused

        // Animate color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null && button != null && button.targetGraphic != null)
        {
            buttonImage = button.targetGraphic.GetComponent<Image>();
        }
        if (buttonImage == null)
        {
            buttonImage = buttonObject.GetComponentInChildren<Image>();
        }

        if (buttonImage != null)
        {
            Color originalColor = originalButtonColors.ContainsKey(buttonRect)
                ? originalButtonColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(selectionColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        // Store tween reference
        if (button != null)
        {
            activeButtonTweens[button] = scaleTween;
        }
    }

    /// <summary>
    /// Reset selection effect for a button
    /// </summary>
    private void ResetButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        Button button = buttonObject.GetComponent<Button>();
        if (button != null && activeButtonTweens.ContainsKey(button))
        {
            activeButtonTweens[button].Kill();
            activeButtonTweens.Remove(button);
        }

        // Get original scale
        Vector3 originalScale = originalButtonScales.ContainsKey(buttonRect) 
            ? originalButtonScales[buttonRect] 
            : buttonRect.localScale;

        // Animate back to original scale
        buttonRect.DOScale(originalScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        // Reset color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null && button != null && button.targetGraphic != null)
        {
            buttonImage = button.targetGraphic.GetComponent<Image>();
        }
        if (buttonImage == null)
        {
            buttonImage = buttonObject.GetComponentInChildren<Image>();
        }

        if (buttonImage != null)
        {
            Color originalColor = originalButtonColors.ContainsKey(buttonRect)
                ? originalButtonColors[buttonRect]
                : buttonImage.color;
            
            buttonImage.DOColor(originalColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// Reset all button selection effects
    /// </summary>
    private void ResetAllButtonSelectionEffects()
    {
        foreach (var button in inventoryButtons)
        {
            if (button != null)
            {
                ResetButtonSelectionEffect(button.gameObject);
            }
        }
        lastSelectedButton = null;
    }

    /// <summary>
    /// Select first button in inventory panel (back button is optional)
    /// </summary>
    private void SelectFirstButton()
    {
        // Don't select anything if no buttons (back button is optional)
        if (inventoryButtons.Count == 0)
        {
            // Clear selection - no buttons to select
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            return;
        }

        // Select first available button (back button if exists, otherwise first button in list)
        Button firstButton = backButton != null ? backButton : inventoryButtons[0];
        
        if (firstButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            lastSelectedButton = firstButton.gameObject;
            ApplyButtonSelectionEffect(firstButton.gameObject);
        }
    }

    /// <summary>
    /// Open inventory panel (hides pause menu, shows inventory)
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("InventoryPanelController: Inventory panel not assigned!");
            return;
        }

        // Ensure game is paused
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Paused)
        {
            GameManager.Instance.PauseGame();
        }

        // Hide pause menu panel
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Show inventory panel
        inventoryPanel.SetActive(true);
        isInventoryOpen = true;

        // Refresh original button states
        StoreOriginalButtonStates();

        // Refresh inventory UI
        InventoryUI inventoryUI = inventoryPanel.GetComponentInChildren<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.Refresh();
        }

        // Select first button after a frame (to ensure UI is ready)
        Invoke(nameof(SelectFirstButton), 0.1f);
    }

    /// <summary>
    /// Close inventory panel (shows pause menu, hides inventory)
    /// </summary>
    public void CloseInventory()
    {
        if (inventoryPanel == null) return;

        // Reset button effects
        ResetAllButtonSelectionEffects();

        // Hide inventory panel
        inventoryPanel.SetActive(false);
        isInventoryOpen = false;

        // Show pause menu panel
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        // Restore pause menu button selection
        // Use a small delay to ensure UI is ready
        if (pauseMenuController != null)
        {
            Invoke(nameof(RestorePauseMenuSelection), 0.1f);
        }
    }

    /// <summary>
    /// Restore pause menu button selection
    /// </summary>
    private void RestorePauseMenuSelection()
    {
        if (pauseMenuController != null && pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            // Call public method on PauseMenuController to restore selection
            pauseMenuController.RestoreButtonSelection();
        }
    }

    /// <summary>
    /// Toggle inventory panel
    /// </summary>
    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    /// <summary>
    /// Check if inventory is currently open
    /// </summary>
    public bool IsInventoryOpen => isInventoryOpen;
}
