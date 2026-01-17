using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Pause Menu Controller - Handles pause menu UI and interactions with full controller support
/// 
/// SETUP INSTRUCTIONS:
/// 1. Attach this script to a GameObject in your gameplay scene (or PersistentScene)
/// 2. Create a Canvas with a Panel named "PauseMenu" (disabled by default)
/// 3. Assign the pausePanel GameObject reference
/// 4. Create buttons: Resume, Options (optional), Save & Quit
/// 5. Assign button references in inspector
/// 6. Buttons will automatically be set up for navigation and selection effects
/// 7. The pause input (Esc/Start) and Cancel (Circle) are handled automatically
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel; // Options panel (optional)

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button inventoryButton; // Inventory button
    [SerializeField] private Button optionsButton; // Optional
    [SerializeField] private Button saveAndQuitButton;

    [Header("Button Selection Effects")]
    [SerializeField] private float selectionMoveDistance = 30f; // How much to move right when selected
    [SerializeField] private float selectionScaleAmount = 1.1f; // Scale multiplier when selected
    [SerializeField] private Color selectionColor = new Color(1f, 0.8f, 0.5f, 1f); // Color when selected
    [SerializeField] private float selectionAnimationDuration = 0.2f; // Duration of selection animation

    [Header("Input")]
    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private bool enableGamepadInput = true;
    [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActionAsset;
    private UnityEngine.InputSystem.InputActionMap uiActionMap;
    private UnityEngine.InputSystem.InputAction cancelAction;

    // Button tracking
    private List<Button> pauseMenuButtons = new List<Button>();
    private Dictionary<RectTransform, Vector3> originalButtonPositions = new Dictionary<RectTransform, Vector3>();
    private Dictionary<RectTransform, Vector3> originalButtonScales = new Dictionary<RectTransform, Vector3>();
    private Dictionary<RectTransform, Color> originalButtonColors = new Dictionary<RectTransform, Color>();
    private Dictionary<Button, Tweener> activeButtonTweens = new Dictionary<Button, Tweener>();
    private GameObject lastSelectedButton = null;
    private bool isPauseMenuActive = false;
    private bool isOptionsOpen = false; // Track if options panel is open
    private float resumeInputCooldown = 0f; // Cooldown to prevent jump input after resuming

    private void Awake()
    {
        // Initialize input actions
        if (inputActionAsset != null)
        {
            uiActionMap = inputActionAsset.FindActionMap("UI");
            if (uiActionMap != null)
            {
                cancelAction = uiActionMap.FindAction("Cancel");
            }
        }
        
        // If no asset assigned, try to find it in resources
        if (inputActionAsset == null)
        {
            inputActionAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
            if (inputActionAsset != null)
            {
                uiActionMap = inputActionAsset.FindActionMap("UI");
                if (uiActionMap != null)
                {
                    cancelAction = uiActionMap.FindAction("Cancel");
                }
            }
        }

        // Build button list
        pauseMenuButtons.Clear();
        if (resumeButton != null) pauseMenuButtons.Add(resumeButton);
        if (inventoryButton != null) pauseMenuButtons.Add(inventoryButton);
        if (optionsButton != null) pauseMenuButtons.Add(optionsButton);
        if (saveAndQuitButton != null) pauseMenuButtons.Add(saveAndQuitButton);

        // Setup button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ClosePause);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OpenOptions);
        }

        if (saveAndQuitButton != null)
        {
            saveAndQuitButton.onClick.AddListener(SaveAndQuit);
        }

        // Store original button states
        StoreOriginalButtonStates();
    }

    private void OnEnable()
    {
        if (cancelAction != null)
        {
            cancelAction.Enable();
            cancelAction.performed += OnCancelPressed;
        }
    }

    private void OnDisable()
    {
        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPressed;
            cancelAction.Disable();
        }
    }

    private void Start()
    {
        // Ensure pause panel is hidden on start
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Ensure options panel is hidden on start
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        // Setup button navigation
        SetupButtonNavigation();

        // Subscribe to pause events
        EventBus.OnGamePaused += HandleGamePaused;
        EventBus.OnGameResumed += HandleGameResumed;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.OnGamePaused -= HandleGamePaused;
        EventBus.OnGameResumed -= HandleGameResumed;

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

    private void Update()
    {
        // Update resume input cooldown
        if (resumeInputCooldown > 0f)
        {
            resumeInputCooldown -= Time.unscaledDeltaTime;
        }

        // Handle pause input (toggle pause - works both when playing and when paused)
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameState.Playing || 
                GameManager.Instance.CurrentState == GameState.Paused)
            {
                HandlePauseInput();
            }
        }

        // Handle button selection effects when pause menu is active
        if (isPauseMenuActive)
        {
            HandleButtonSelectionEffects();
        }

        // Handle Cancel input (Circle button) when pause menu is active
        if (isPauseMenuActive && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
        {
            HandleCancelInput();
        }
    }

    /// <summary>
    /// Store original button positions, scales, and colors
    /// </summary>
    private void StoreOriginalButtonStates()
    {
        originalButtonPositions.Clear();
        originalButtonScales.Clear();
        originalButtonColors.Clear();

        foreach (var button in pauseMenuButtons)
        {
            if (button != null)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    originalButtonPositions[buttonRect] = buttonRect.anchoredPosition;
                    originalButtonScales[buttonRect] = buttonRect.localScale;

                    // Get Image component - check button itself, targetGraphic, or children
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
                    else
                    {
                        Debug.LogWarning($"PauseMenuController: Button '{button.name}' has no Image component. Color changes will not work.");
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
        foreach (var button in pauseMenuButtons)
        {
            if (button != null)
            {
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.Automatic; // Allows automatic navigation between buttons
                button.navigation = nav;
            }
        }
    }

    /// <summary>
    /// Handle pause input (Esc for keyboard, Start button for gamepad)
    /// Toggles pause state - opens when playing, closes when paused
    /// </summary>
    private void HandlePauseInput()
    {
        // Check if inventory is open - inventory handles Start button differently
        InventoryPanelController inventoryController = FindObjectOfType<InventoryPanelController>();
        if (inventoryController != null && inventoryController.IsInventoryOpen)
        {
            // Inventory panel handles Start button to close pause menu directly
            // We don't handle pause input here when inventory is open
            return;
        }

        bool pausePressed = false;

        // Keyboard input (Esc)
        if (enableKeyboardInput)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                pausePressed = true;
            }
        }

        // Gamepad input (Start/Options button)
        if (enableGamepadInput && !pausePressed)
        {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                pausePressed = true;
            }
        }

        // Toggle pause if button was pressed
        if (pausePressed)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Handle Cancel button (Circle on gamepad, Esc when menu is open)
    /// </summary>
    private void OnCancelPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // If options panel is open, close it instead of closing pause menu
        if (isOptionsOpen && optionsPanel != null && optionsPanel.activeSelf)
        {
            CloseOptions();
            return;
        }

        // Only handle cancel when pause menu is active
        if (isPauseMenuActive && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
        {
            ClosePause();
        }
    }

    /// <summary>
    /// Handle Cancel input directly (fallback if Input System action doesn't work)
    /// </summary>
    private void HandleCancelInput()
        {
        // Don't handle cancel if inventory is open (inventory handles its own cancel)
        InventoryPanelController inventoryController = FindObjectOfType<InventoryPanelController>();
        if (inventoryController != null && inventoryController.IsInventoryOpen)
        {
            return; // Let inventory panel handle its own cancel
        }

        // If options panel is open, close it instead of closing pause menu
        if (isOptionsOpen && optionsPanel != null && optionsPanel.activeSelf)
        {
            // Check gamepad Circle button (buttonEast) or keyboard Esc
            bool cancelPressed = false;
            
            if (enableGamepadInput)
            {
                var gamepad = UnityEngine.InputSystem.Gamepad.current;
                if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) // Circle = buttonEast
                {
                    cancelPressed = true;
                }
            }
            
            if (enableKeyboardInput && !cancelPressed)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && 
                    UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    cancelPressed = true;
                }
            }
            
            if (cancelPressed)
            {
                CloseOptions();
                return;
            }
        }

        // Check gamepad Circle button (buttonEast)
        if (enableGamepadInput)
        {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) // Circle = buttonEast
            {
                ClosePause();
            }
        }

        // Check keyboard Esc (but only if menu is open - Esc when menu is closed should open menu)
        // This is handled by HandlePauseInput when not paused, so we don't need it here
    }

    /// <summary>
    /// Handle button selection effects (like main menu)
    /// </summary>
    private void HandleButtonSelectionEffects()
    {
        if (EventSystem.current == null) return;

        GameObject currentlySelected = EventSystem.current.currentSelectedGameObject;
        
        // If selection changed or if we have a selection but no last selected (initial selection)
        if (currentlySelected != lastSelectedButton)
        {
            // Reset previously selected button
            if (lastSelectedButton != null)
            {
                ResetButtonSelectionEffect(lastSelectedButton);
            }

            // Apply selection effect to new button
            if (currentlySelected != null && IsPauseMenuButton(currentlySelected))
            {
                ApplyButtonSelectionEffect(currentlySelected);
            }

            lastSelectedButton = currentlySelected;
        }
        // If we have a selection but it's the same, make sure effect is applied (in case it wasn't applied initially)
        else if (currentlySelected != null && IsPauseMenuButton(currentlySelected) && lastSelectedButton == currentlySelected)
        {
            // Check if effect is already applied by checking if we have an active tween
            Button button = currentlySelected.GetComponent<Button>();
            if (button != null && !activeButtonTweens.ContainsKey(button))
            {
                // Effect not applied, apply it now
                ApplyButtonSelectionEffect(currentlySelected);
            }
        }
    }

    /// <summary>
    /// Check if a GameObject is a pause menu button
    /// </summary>
    private bool IsPauseMenuButton(GameObject obj)
    {
        if (obj == null) return false;
        return pauseMenuButtons.Exists(btn => btn != null && btn.gameObject == obj);
    }

    /// <summary>
    /// Apply selection effect to a button
    /// </summary>
    private void ApplyButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        // Kill any existing tween for this button
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

        // Animate color - match MainMenuController exactly
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
            
            // Animate color - use SetUpdate(true) to work when timeScale is 0
            Tweener colorTween = buttonImage.DOColor(selectionColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true); // Critical: works when paused
        }
        else
        {
            Debug.LogWarning($"PauseMenuController: Button '{buttonObject.name}' has no Image component for color animation!");
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

        // Kill any existing tween for this button
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

        // Animate back to original scale - use SetUpdate(true) to work when timeScale is 0
        Tweener scaleTween = buttonRect.DOScale(originalScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // Critical: works when paused

        // Animate back to original color - match MainMenuController exactly
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
            
            // Animate color back - use SetUpdate(true) to work when timeScale is 0
            Tweener colorTween = buttonImage.DOColor(originalColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true); // Critical: works when paused
        }
    }

    /// <summary>
    /// Reset all button selection effects
    /// </summary>
    private void ResetAllButtonSelectionEffects()
    {
        // Reset all buttons to their original state
        foreach (var button in pauseMenuButtons)
        {
            if (button != null)
            {
                ResetButtonSelectionEffect(button.gameObject);
            }
        }

        // Clear selection tracking
        lastSelectedButton = null;

        // Clear EventSystem selection
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Kill all active button tweens
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
    /// Reset all buttons to their TRUE original state (immediately, no animation)
    /// This prevents accumulation when reopening the menu
    /// </summary>
    private void ResetAllButtonsToOriginalState()
    {
        // Kill all active tweens first
        foreach (var tween in activeButtonTweens.Values)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        activeButtonTweens.Clear();

        // Immediately reset all buttons to original positions/scales/colors
        foreach (var button in pauseMenuButtons)
        {
            if (button != null)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (buttonRect != null && originalButtonPositions.ContainsKey(buttonRect))
                {
                    // Reset position immediately
                    buttonRect.anchoredPosition = originalButtonPositions[buttonRect];
                    
                    // Reset scale immediately
                    if (originalButtonScales.ContainsKey(buttonRect))
                    {
                        buttonRect.localScale = originalButtonScales[buttonRect];
                    }
                    
                    // Reset color immediately
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage == null && button.targetGraphic != null)
                    {
                        buttonImage = button.targetGraphic.GetComponent<Image>();
                    }
                    if (buttonImage == null)
                    {
                        buttonImage = button.GetComponentInChildren<Image>();
                    }
                    
                    if (buttonImage != null && originalButtonColors.ContainsKey(buttonRect))
                    {
                        buttonImage.color = originalButtonColors[buttonRect];
                    }
                }
            }
        }

        // Clear selection tracking
        lastSelectedButton = null;
    }

    /// <summary>
    /// Select first button (Resume)
    /// </summary>
    private void SelectFirstButton()
    {
        if (resumeButton != null && EventSystem.current != null)
        {
            // Clear last selected to ensure effect is applied
            lastSelectedButton = null;
            
            // Select the button
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            
            // Update last selected
            lastSelectedButton = resumeButton.gameObject;
            
            // Explicitly apply selection effect (like MainMenuController does)
            ApplyButtonSelectionEffect(resumeButton.gameObject);
        }
    }

    /// <summary>
    /// Public method to restore pause menu button selection (called by InventoryPanelController)
    /// </summary>
    public void RestoreButtonSelection()
    {
        if (isPauseMenuActive && pausePanel != null && pausePanel.activeSelf)
        {
            SelectFirstButton();
            StartCoroutine(SelectFirstButtonDelayed());
        }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            OpenPause();
        }
        else if (GameManager.Instance.CurrentState == GameState.Paused)
        {
            ClosePause();
        }
    }

    /// <summary>
    /// Open pause menu
    /// </summary>
    public void OpenPause()
    {
        if (GameManager.Instance == null) return;

        // Only allow pausing when playing
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        isPauseMenuActive = true;

        // CRITICAL: Reset all buttons to their TRUE original state BEFORE storing
        // This prevents accumulation of transforms when reopening the menu
        ResetAllButtonsToOriginalState();

        // Show pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Refresh original button states when menu opens (now from true original positions)
        StoreOriginalButtonStates();

        // Pause the game (GameManager handles time scale)
        GameManager.Instance.PauseGame();

        // Select first button immediately and after a frame (to ensure UI is ready)
        // Do it immediately first to ensure selection happens
        SelectFirstButton();
        
        // Also do it after a frame to ensure it sticks (works even when time is paused)
        StartCoroutine(SelectFirstButtonDelayed());
    }

    /// <summary>
    /// Coroutine to select first button after pause menu opens
    /// Uses WaitForSecondsRealtime to work even when timeScale is 0
    /// </summary>
    private IEnumerator SelectFirstButtonDelayed()
    {
        yield return new WaitForSecondsRealtime(0.01f); // Small delay to ensure UI is ready
        SelectFirstButton();
    }

    /// <summary>
    /// Close pause menu
    /// </summary>
    public void ClosePause()
    {
        if (GameManager.Instance == null) return;

        // Don't close if inventory is open - close inventory first
        InventoryPanelController inventoryController = FindObjectOfType<InventoryPanelController>();
        if (inventoryController != null && inventoryController.IsInventoryOpen)
        {
            inventoryController.CloseInventory();
            return; // Inventory close will restore pause menu, but we don't want to resume yet
        }

        // Don't close if options is open - close options first
        if (isOptionsOpen && optionsPanel != null && optionsPanel.activeSelf)
        {
            CloseOptions();
            return; // Options close will restore pause menu, but we don't want to resume yet
        }

        isPauseMenuActive = false;
        isOptionsOpen = false;

        // Reset all button selection effects
        ResetAllButtonSelectionEffects();

        // Hide pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Hide options panel if it's open
        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            optionsPanel.SetActive(false);
        }

        // Clear EventSystem selection
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Resume the game (GameManager handles time scale)
        GameManager.Instance.ResumeGame();

        // Set cooldown to prevent jump input from being processed immediately after resuming
        // This prevents the Resume button press from also triggering Jump
        resumeInputCooldown = 0.2f; // 200ms cooldown

        // Start coroutine to clear jump input buffer after resume
        StartCoroutine(ClearJumpInputAfterResume());
    }

    /// <summary>
    /// Coroutine to clear jump input buffer after resuming
    /// This prevents the button press that triggered Resume from also triggering Jump
    /// </summary>
    private IEnumerator ClearJumpInputAfterResume()
    {
        // Wait a frame to ensure input is cleared
        yield return new WaitForEndOfFrame();
        
        // The PlayerInputHandler will already ignore input when paused,
        // but we add this extra safety to ensure jump buffer is cleared
        // The cooldown in Update() will prevent any immediate jump input
    }

    /// <summary>
    /// Open options panel (hides pause menu, shows options)
    /// </summary>
    public void OpenOptions()
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("PauseMenuController: Options panel not assigned!");
            return;
        }

        // Ensure game is paused
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Paused)
        {
            GameManager.Instance.PauseGame();
        }

        // Hide pause menu panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Show options panel
        optionsPanel.SetActive(true);
        isOptionsOpen = true;

        // Refresh options UI if OptionsUIController exists
        OptionsUIController optionsController = optionsPanel.GetComponent<OptionsUIController>();
        if (optionsController == null)
        {
            optionsController = optionsPanel.GetComponentInChildren<OptionsUIController>();
        }
        
        // OptionsUIController will handle loading options in OnEnable()
    }

    /// <summary>
    /// Close options panel (shows pause menu, hides options)
    /// </summary>
    public void CloseOptions()
    {
        if (optionsPanel == null) return;

        // Hide options panel
        optionsPanel.SetActive(false);
        isOptionsOpen = false;

        // Show pause menu panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Restore pause menu button selection
        // Use a small delay to ensure UI is ready
        Invoke(nameof(RestoreButtonSelection), 0.1f);
    }

    /// <summary>
    /// Save game and return to main menu
    /// </summary>
    public void SaveAndQuit()
    {
        if (GameManager.Instance == null) return;

        // Save the game if there's an active slot
        if (SaveManager.Instance != null && SaveManager.Instance.HasActiveSlot)
        {
            SaveManager.Instance.SaveGame();
        }

        // Reset button effects
        ResetAllButtonSelectionEffects();
        isPauseMenuActive = false;

        // Resume time before loading main menu (important!)
        GameManager.Instance.ResumeGame();

        // Return to main menu
        GameManager.Instance.ReturnToMenu();
    }

    /// <summary>
    /// Handle game paused event
    /// </summary>
    private void HandleGamePaused()
    {
        // Ensure pause panel is visible when game is paused
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            isPauseMenuActive = true;
        }
    }

    /// <summary>
    /// Handle game resumed event
    /// </summary>
    private void HandleGameResumed()
    {
        // Hide pause panel when game resumes
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            isPauseMenuActive = false;
        }

        // Reset button effects
        ResetAllButtonSelectionEffects();
    }
}

