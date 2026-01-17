using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main Menu Controller - Handles all main menu UI animations and navigation
/// 
/// SETUP INSTRUCTIONS:
/// 1. Attach this script to a GameObject in your Main Menu scene
/// 2. Assign all panel GameObjects (Press to Begin, Main Menu, Play Slots, Options, Credits)
/// 3. For Press to Begin panel:
///    - The script will AUTO-FIND CanvasGroup, Text, and RectTransform from the panel
///    - OR manually assign them if you want specific components
///    - CanvasGroup: Should be on the Press to Begin panel GameObject
///    - Text: Should be a child of the Press to Begin panel (the "Press to Begin" text)
///    - RectTransform: Will use the panel's RectTransform automatically
/// 4. For Main Menu panel:
///    - Assign all 4 buttons (Play, Options, Credits, Quit)
///    - Assign the background image RectTransform (the one that animates from right)
///    - The buttons list will auto-populate from the assigned buttons
/// 5. For Play Slots panel:
///    - Assign all 3 save slot button RectTransforms
/// 6. (Optional) Assign the InputActionAsset if you want to use the new Input System
///    - If not assigned, the script will try to load it from Resources
///    - The script also has fallback input detection
/// 
/// ANIMATION FLOW:
/// - On Start: Press to Begin panel fades in, text animates with DOTween
/// - On Input: Press to Begin fades out and scales up, then Main Menu opens
/// - Main Menu: Buttons slide in from left (top to bottom), image slides in from right
/// - Play Button: Opens Play Slots panel with save slots animating from top to bottom
/// - Cancel (Circle on gamepad): Closes Play Slots panel and returns to Main Menu
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pressToBeginPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject playSlotsPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject deleteConfirmPopup;

    [Header("Press to Begin")]
    [SerializeField] private CanvasGroup pressToBeginCanvasGroup;
    [SerializeField] private TextMeshProUGUI pressToBeginText;
    [SerializeField] private RectTransform pressToBeginRectTransform;

    [Header("Main Menu")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private RectTransform mainMenuImage; // The image that comes from right to left
    [SerializeField] private List<RectTransform> mainMenuButtons; // Will be populated with buttons

    [Header("Play Slots")]
    [SerializeField] private List<RectTransform> saveSlotButtons; // 3 save slots
    [SerializeField] private float[] saveSlotDefaultPositions = { 150f, -100f, -350f }; // Default Y positions for slots 1, 2, 3
    [SerializeField] private float saveSlotStartY = 800f; // Starting Y position (off-screen)
    private List<Button> saveSlotButtonComponents = new List<Button>(); // Button components for navigation

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private float buttonAnimationDuration = 0.5f;
    [SerializeField] private float buttonDelay = 0.02f; // Very fast sequential animation (overlapping)
    [SerializeField] private float textAnimationDuration = 1f;
    [SerializeField] private float saveSlotDelay = 0.15f;
    
    [Header("Main Menu Animation Positions")]
    [SerializeField] private float buttonStartX = -400f; // Buttons start position
    [SerializeField] private float buttonEndX = 450f; // Buttons end position
    [SerializeField] private float imageStartX = 500f; // Image start position
    [SerializeField] private float imageEndX = -500f; // Image end position

    [Header("Button Selection Effects")]
    [SerializeField] private float selectionMoveDistance = 30f; // How much to move right when selected
    [SerializeField] private float selectionScaleAmount = 1.1f; // Scale multiplier when selected
    [SerializeField] private Color selectionColor = new Color(1f, 0.8f, 0.5f, 1f); // Color when selected
    [SerializeField] private float selectionAnimationDuration = 0.2f; // Duration of selection animation

    [Header("Input")]
    [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActionAsset;
    private UnityEngine.InputSystem.InputActionMap uiActionMap;
    private UnityEngine.InputSystem.InputAction submitAction;
    private UnityEngine.InputSystem.InputAction cancelAction;
    
    [Header("Delete Confirmation Popup")]
    [SerializeField] private Button deleteConfirmYesButton;
    [SerializeField] private Button deleteConfirmNoButton;
    [SerializeField] private CanvasGroup deleteConfirmCanvasGroup;
    [SerializeField] private RectTransform deleteConfirmPanel;
    [SerializeField] private GameObject deleteConfirmBackground; // Background dim (separate from window)
    
    private enum MenuState
    {
        PressToBegin,
        MainMenu,
        SaveSlots,
        DeleteConfirm,
        Options,
        Credits
    }
    
    private MenuState currentMenuState = MenuState.PressToBegin;
    private int selectedSlotIndex = 0; // Track which slot is currently selected (1-3)
    
    private bool isPressToBeginActive = true;
    private bool isMainMenuActive = false;
    private bool isPlaySlotsActive = false;
    private bool isOptionsActive = false;
    private bool isCreditsActive = false;

    private Sequence pressToBeginTextSequence;
    private Dictionary<RectTransform, Vector3> originalButtonPositions = new Dictionary<RectTransform, Vector3>();
    private Dictionary<RectTransform, Vector3> originalButtonScales = new Dictionary<RectTransform, Vector3>(); // Store original button scales
    private Dictionary<RectTransform, Color> originalButtonColors = new Dictionary<RectTransform, Color>(); // Store original button colors
    private Vector3 originalImagePosition;
    private float pressToBeginInputCooldown = 0f; // Cooldown to prevent immediate re-triggering
    private Vector3 originalTextPosition; // Store original text position for hover animation
    private Dictionary<RectTransform, Vector3> originalSlotPositions = new Dictionary<RectTransform, Vector3>(); // Store original slot positions
    private Dictionary<RectTransform, Vector3> originalSlotScales = new Dictionary<RectTransform, Vector3>(); // Store original slot scales
    private Dictionary<RectTransform, Color> originalSlotColors = new Dictionary<RectTransform, Color>(); // Store original slot colors
    private GameObject lastSelectedButton = null; // Track last selected button
    private GameObject lastSelectedSlotButton = null; // Track last selected slot button
    private GameObject lastSelectedDeleteButton = null; // Track last selected delete popup button
    private Dictionary<Button, Tweener> activeButtonTweens = new Dictionary<Button, Tweener>(); // Track active button tweens
    private Dictionary<Button, Tweener> activeSlotButtonTweens = new Dictionary<Button, Tweener>(); // Track active slot button tweens
    private Dictionary<Button, Tweener> activeDeleteButtonTweens = new Dictionary<Button, Tweener>(); // Track active delete popup button tweens
    private Dictionary<RectTransform, Vector3> originalDeleteButtonScales = new Dictionary<RectTransform, Vector3>(); // Store original delete button scales
    private Dictionary<RectTransform, Color> originalDeleteButtonColors = new Dictionary<RectTransform, Color>(); // Store original delete button colors

    private void Awake()
    {
        // Auto-populate Press to Begin components if not assigned
        if (pressToBeginPanel != null)
        {
            if (pressToBeginCanvasGroup == null)
                pressToBeginCanvasGroup = pressToBeginPanel.GetComponent<CanvasGroup>();
            
            if (pressToBeginText == null)
                pressToBeginText = pressToBeginPanel.GetComponentInChildren<TextMeshProUGUI>();
            
            if (pressToBeginRectTransform == null)
                pressToBeginRectTransform = pressToBeginPanel.GetComponent<RectTransform>();
        }

        // Initialize input actions
        if (inputActionAsset != null)
        {
            uiActionMap = inputActionAsset.FindActionMap("UI");
            if (uiActionMap != null)
            {
                submitAction = uiActionMap.FindAction("Submit");
                cancelAction = uiActionMap.FindAction("Cancel");
            }
        }
        
        // If no asset assigned, try to find it in resources or create actions manually
        if (inputActionAsset == null)
        {
            inputActionAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
            if (inputActionAsset != null)
            {
                uiActionMap = inputActionAsset.FindActionMap("UI");
                if (uiActionMap != null)
                {
                    submitAction = uiActionMap.FindAction("Submit");
                    cancelAction = uiActionMap.FindAction("Cancel");
                }
            }
        }
        
        // Setup button references
        if (mainMenuButtons == null || mainMenuButtons.Count == 0)
        {
            mainMenuButtons = new List<RectTransform>();
            if (playButton != null) mainMenuButtons.Add(playButton.GetComponent<RectTransform>());
            if (optionsButton != null) mainMenuButtons.Add(optionsButton.GetComponent<RectTransform>());
            if (creditsButton != null) mainMenuButtons.Add(creditsButton.GetComponent<RectTransform>());
            if (quitButton != null) mainMenuButtons.Add(quitButton.GetComponent<RectTransform>());
        }

        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsButtonClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        
        // Auto-find delete confirmation popup if not assigned
        if (deleteConfirmPopup == null)
        {
            // Try to find by name
            GameObject foundPopup = GameObject.Find("DeleteConfirmPopup");
            if (foundPopup == null)
                foundPopup = GameObject.Find("Delete Confirm Popup");
            if (foundPopup == null)
                foundPopup = GameObject.Find("DeletePopup");
            if (foundPopup == null)
                foundPopup = GameObject.Find("PopupWindow");
            
            if (foundPopup != null)
            {
                deleteConfirmPopup = foundPopup;
                Debug.Log("[Delete] Auto-found deleteConfirmPopup: " + foundPopup.name);
            }
        }
        
        // Auto-find delete confirmation background if not assigned
        if (deleteConfirmBackground == null)
        {
            GameObject foundBackground = GameObject.Find("DeleteConfirmBackground");
            if (foundBackground == null)
                foundBackground = GameObject.Find("Delete Confirm Background");
            if (foundBackground == null)
                foundBackground = GameObject.Find("BackgroundDim");
            if (foundBackground == null)
                foundBackground = GameObject.Find("Background");
            
            if (foundBackground != null)
            {
                deleteConfirmBackground = foundBackground;
                Debug.Log("[Delete] Auto-found deleteConfirmBackground: " + foundBackground.name);
            }
        }
        
        // Auto-find delete popup components if not assigned
        if (deleteConfirmPopup != null)
        {
            // Auto-find CanvasGroup
            if (deleteConfirmCanvasGroup == null)
            {
                deleteConfirmCanvasGroup = deleteConfirmPopup.GetComponent<CanvasGroup>();
                if (deleteConfirmCanvasGroup == null)
                {
                    deleteConfirmCanvasGroup = deleteConfirmPopup.GetComponentInChildren<CanvasGroup>();
                }
            }
            
            // Auto-find Panel RectTransform
            if (deleteConfirmPanel == null)
            {
                // Look for a child named "Panel" or use the popup itself
                Transform panelTransform = deleteConfirmPopup.transform.Find("Panel");
                if (panelTransform == null)
                    panelTransform = deleteConfirmPopup.transform;
                deleteConfirmPanel = panelTransform.GetComponent<RectTransform>();
            }
            
            // Auto-find Yes button
            if (deleteConfirmYesButton == null)
            {
                Transform yesButtonTransform = deleteConfirmPopup.transform.Find("YesButton");
                if (yesButtonTransform == null)
                    yesButtonTransform = deleteConfirmPopup.transform.Find("Yes Button");
                if (yesButtonTransform == null)
                    yesButtonTransform = deleteConfirmPopup.transform.Find("ConfirmButton");
                if (yesButtonTransform != null)
                {
                    deleteConfirmYesButton = yesButtonTransform.GetComponent<Button>();
                }
            }
            
            // Auto-find No button
            if (deleteConfirmNoButton == null)
            {
                Transform noButtonTransform = deleteConfirmPopup.transform.Find("NoButton");
                if (noButtonTransform == null)
                    noButtonTransform = deleteConfirmPopup.transform.Find("No Button");
                if (noButtonTransform == null)
                    noButtonTransform = deleteConfirmPopup.transform.Find("CancelButton");
                if (noButtonTransform != null)
                {
                    deleteConfirmNoButton = noButtonTransform.GetComponent<Button>();
                }
            }
        }
        
        // Setup delete confirmation popup buttons
        if (deleteConfirmYesButton != null)
            deleteConfirmYesButton.onClick.AddListener(ConfirmDelete);
        if (deleteConfirmNoButton != null)
            deleteConfirmNoButton.onClick.AddListener(CancelDelete);
        
        // Initialize delete popup as hidden
        if (deleteConfirmPopup != null)
            deleteConfirmPopup.SetActive(false);
        else
        {
            Debug.LogWarning("[Delete] deleteConfirmPopup not found and could not be auto-found. " +
                           "Please create a GameObject named 'DeleteConfirmPopup' in your MainMenu scene, " +
                           "or assign it manually in the MainMenuController inspector.");
        }

        // Ensure buttons are set up for navigation
        SetupButtonNavigation();
        
        // Setup slot button references
        SetupSlotButtonReferences();
    }
    
    private void SetupSlotButtonReferences()
    {
        saveSlotButtonComponents.Clear();
        if (saveSlotButtons != null)
        {
            foreach (var slotRect in saveSlotButtons)
            {
                if (slotRect != null)
                {
                    Button slotButton = slotRect.GetComponent<Button>();
                    if (slotButton != null)
                    {
                        saveSlotButtonComponents.Add(slotButton);
                    }
                }
            }
        }
    }

    private void SetupButtonNavigation()
    {
        // Ensure all buttons have navigation enabled for gamepad support
        // Unity's UI system will handle up/down navigation automatically
        // Make sure buttons are in the correct order in the hierarchy for proper navigation
        
        Button[] buttons = { playButton, optionsButton, creditsButton, quitButton };
        foreach (var button in buttons)
        {
            if (button != null)
            {
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.Automatic; // Allows automatic navigation between buttons
                button.navigation = nav;
            }
        }
    }

    private void OnEnable()
    {
        if (submitAction != null)
        {
            submitAction.Enable();
            submitAction.performed += OnSubmitPressed;
        }
        if (cancelAction != null)
        {
            cancelAction.Enable();
            cancelAction.performed += OnCancelPressed;
        }
    }

    private void OnDisable()
    {
        if (submitAction != null)
        {
            submitAction.performed -= OnSubmitPressed;
            submitAction.Disable();
        }
        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPressed;
            cancelAction.Disable();
        }
    }

    private void Start()
    {
        InitializeMenu();
    }

    private void Update()
    {
        // Update cooldown timer
        if (pressToBeginInputCooldown > 0f)
        {
            pressToBeginInputCooldown -= Time.deltaTime;
        }

        // Handle button selection effects in main menu
        if (isMainMenuActive)
        {
            HandleButtonSelectionEffects();
        }
        
        // Handle slot button selection effects in play slots panel
        if (isPlaySlotsActive)
        {
            HandleSlotButtonSelectionEffects();
            
            // Handle delete input (Triangle button on gamepad) - only when slots panel is active
            // Check that we're not in delete confirm state
            if (currentMenuState != MenuState.DeleteConfirm)
            {
                HandleDeleteInput();
            }
        }
        
        // Handle Circle button (Cancel) in delete popup
        if (currentMenuState == MenuState.DeleteConfirm)
        {
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) // Circle = buttonEast
            {
                Debug.Log("[Delete] Circle button pressed - closing popup");
                CancelDelete();
            }
            
            // Handle delete popup button selection effects
            HandleDeleteButtonSelectionEffects();
        }

        // Fallback input detection using new Input System
        // This allows the menu to work even if Input System actions aren't configured
        if (isPressToBeginActive && pressToBeginInputCooldown <= 0f)
        {
            // Check gamepad input
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || 
                                   gamepad.buttonEast.wasPressedThisFrame ||
                                   gamepad.buttonWest.wasPressedThisFrame ||
                                   gamepad.buttonNorth.wasPressedThisFrame))
            {
                TransitionToMainMenu();
                return;
            }
            
            // Check keyboard input using new Input System
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                TransitionToMainMenu();
            }
        }
    }

    private void InitializeMenu()
    {
        // Set initial states
        if (pressToBeginPanel != null)
        {
            pressToBeginPanel.SetActive(true);
            if (pressToBeginCanvasGroup != null)
            {
                pressToBeginCanvasGroup.alpha = 0f;
                pressToBeginCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
            }
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (playSlotsPanel != null)
            playSlotsPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        // Start press to begin text animation
        StartPressToBeginTextAnimation();

        // Position main menu elements off screen
        PositionMainMenuElementsOffScreen();
    }

    private void StartPressToBeginTextAnimation()
    {
        if (pressToBeginText == null) return;

        // Get the RectTransform
        RectTransform textRect = pressToBeginText.transform as RectTransform;
        if (textRect == null) return;

        // Store original position if not already stored
        if (originalTextPosition == Vector3.zero)
        {
            originalTextPosition = textRect.anchoredPosition;
        }

        // Kill any existing animation
        pressToBeginTextSequence?.Kill();

        // Create a looping hover animation (move up and down smoothly)
        pressToBeginTextSequence = DOTween.Sequence();
        
        Vector3 upPos = originalTextPosition;
        upPos.y += 10f; // Move up 10 pixels
        
        Vector3 downPos = originalTextPosition;
        downPos.y -= 10f; // Move down 10 pixels
        
        // Smooth hover: up -> down -> center -> repeat
        pressToBeginTextSequence.Append(textRect.DOAnchorPos(upPos, textAnimationDuration).SetEase(Ease.InOutSine));
        pressToBeginTextSequence.Append(textRect.DOAnchorPos(downPos, textAnimationDuration).SetEase(Ease.InOutSine));
        pressToBeginTextSequence.Append(textRect.DOAnchorPos(originalTextPosition, textAnimationDuration).SetEase(Ease.InOutSine));
        pressToBeginTextSequence.SetLoops(-1);
    }

    private void PositionMainMenuElementsOffScreen()
    {
        // Store original button positions, scales, and colors
        originalButtonPositions.Clear();
        originalButtonScales.Clear();
        originalButtonColors.Clear();
        
        foreach (var button in mainMenuButtons)
        {
            if (button != null)
            {
                // Store original position (we'll preserve Y position)
                originalButtonPositions[button] = button.anchoredPosition;
                
                // Store original scale
                originalButtonScales[button] = button.localScale;
                
                // Store original color (get from button's Image component)
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    originalButtonColors[button] = buttonImage.color;
                }
                
                // Set start position for animation (keep Y, set X to -400)
                Vector3 pos = button.anchoredPosition;
                pos.x = buttonStartX; // -400
                button.anchoredPosition = pos;
            }
        }

        // Store original image position (preserve Y, but we'll animate X from 500 to -500)
        if (mainMenuImage != null)
        {
            originalImagePosition = mainMenuImage.anchoredPosition;
            Vector3 pos = mainMenuImage.anchoredPosition;
            pos.x = imageStartX; // 500
            mainMenuImage.anchoredPosition = pos;
        }

        // Store original slot positions and position save slots off screen
        if (saveSlotButtons != null)
        {
            originalSlotPositions.Clear();
            for (int i = 0; i < saveSlotButtons.Count; i++)
            {
                if (saveSlotButtons[i] != null)
                {
                    // Store original position
                    originalSlotPositions[saveSlotButtons[i]] = saveSlotButtons[i].anchoredPosition;
                    
                    // Set to off-screen position (Y = 800)
                    Vector3 pos = saveSlotButtons[i].anchoredPosition;
                    pos.y = saveSlotStartY; // 800
                    saveSlotButtons[i].anchoredPosition = pos;
                }
            }
        }
    }

    private void OnSubmitPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isPressToBeginActive && pressToBeginInputCooldown <= 0f)
        {
            TransitionToMainMenu();
        }
        // Other submit handling is done by Unity's button system
    }

    private void OnCancelPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (currentMenuState == MenuState.DeleteConfirm)
        {
            Debug.Log("[Delete] Cancel pressed - closing popup");
            CancelDelete();
        }
        else if (isPlaySlotsActive)
        {
            ClosePlaySlotsPanel();
        }
        else if (isOptionsActive)
        {
            CloseOptionsPanel();
        }
        else if (isCreditsActive)
        {
            CloseCreditsPanel();
        }
        else if (isMainMenuActive)
        {
            // Go back to press to begin panel
            ReturnToPressToBegin();
        }
    }
    

    private void TransitionToMainMenu()
    {
        if (!isPressToBeginActive) return;

        isPressToBeginActive = false;
        isMainMenuActive = true;
        currentMenuState = MenuState.MainMenu;

        // Play main menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("MainMenuTheme");
        }

        // Stop text animation
        pressToBeginTextSequence?.Kill();

        // Fade out and scale up press to begin panel
        Sequence transitionSequence = DOTween.Sequence();
        
        if (pressToBeginCanvasGroup != null)
        {
            transitionSequence.Append(pressToBeginCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }

        if (pressToBeginRectTransform != null)
        {
            transitionSequence.Join(pressToBeginRectTransform.DOScale(2f, scaleDuration).SetEase(Ease.InBack));
        }

        transitionSequence.OnComplete(() =>
        {
            if (pressToBeginPanel != null)
                pressToBeginPanel.SetActive(false);
            
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                AnimateMainMenuIn();
                // Note: Button selection and effects are applied after animation completes
                // This is handled in AnimateMainMenuIn's OnComplete callback
            }
        });
    }

    private void AnimateMainMenuIn()
    {
        Sequence mainMenuSequence = DOTween.Sequence();

        // Reset buttons and image to default positions first
        ResetMainMenuElementsToDefault();

        // Animate buttons from -400 to 450, rapidly one after another (overlapping)
        for (int i = 0; i < mainMenuButtons.Count; i++)
        {
            if (mainMenuButtons[i] != null)
            {
                // Ensure button is at default start position (-400)
                Vector3 currentPos = mainMenuButtons[i].anchoredPosition;
                if (originalButtonPositions.ContainsKey(mainMenuButtons[i]))
                {
                    currentPos.y = originalButtonPositions[mainMenuButtons[i]].y; // Preserve original Y
                }
                currentPos.x = buttonStartX; // -400 (default position)
                mainMenuButtons[i].anchoredPosition = currentPos;

                // Animate to end position (450) - preserve Y position
                Vector3 endPos = currentPos;
                endPos.x = buttonEndX; // 450
                
                // Use Join instead of Append so animations overlap and appear rapidly
                if (i == 0)
                {
                    // First button starts immediately
                    mainMenuSequence.Append(mainMenuButtons[i].DOAnchorPos(endPos, buttonAnimationDuration)
                        .SetEase(Ease.OutBack));
                }
                else
                {
                    // Subsequent buttons start with small delay (overlapping, not waiting for completion)
                    mainMenuSequence.Join(mainMenuButtons[i].DOAnchorPos(endPos, buttonAnimationDuration)
                        .SetEase(Ease.OutBack)
                        .SetDelay(i * buttonDelay)); // Very small delay for rapid sequential appearance
                }
            }
        }

        // Animate teddy image from 500 to -500 at the same time as first button
        if (mainMenuImage != null)
        {
            // Ensure image is at default start position (500)
            Vector3 currentPos = mainMenuImage.anchoredPosition;
            currentPos.y = originalImagePosition.y; // Preserve original Y
            currentPos.x = imageStartX; // 500 (default position)
            mainMenuImage.anchoredPosition = currentPos;

            // Animate to end position (-500) - preserve Y position
            // Start at the same time as first button (same speed)
            Vector3 endPos = currentPos;
            endPos.x = imageEndX; // -500
            mainMenuSequence.Join(mainMenuImage.DOAnchorPos(endPos, buttonAnimationDuration)
                .SetEase(Ease.OutBack));
        }

        // Update stored positions after animation completes (for selection effects)
        mainMenuSequence.OnComplete(() =>
        {
            // Update stored positions to final animated positions
            foreach (var button in mainMenuButtons)
            {
                if (button != null && originalButtonPositions.ContainsKey(button))
                {
                    originalButtonPositions[button] = button.anchoredPosition;
                }
            }
            
            // Select first button and apply selection effects after animation completes
            SelectFirstButton();
            if (playButton != null)
            {
                ApplyButtonSelectionEffect(playButton.gameObject);
            }
        });

        // Note: First button selection happens immediately when panel opens
        // This ensures gamepad navigation works right away
    }

    private void ResetMainMenuElementsToDefault()
    {
        // Reset buttons to default position (-400)
        foreach (var button in mainMenuButtons)
        {
            if (button != null && originalButtonPositions.ContainsKey(button))
            {
                Vector3 pos = originalButtonPositions[button];
                pos.x = buttonStartX; // -400 (default position)
                button.anchoredPosition = pos;
            }
        }

        // Reset teddy image to default position (500)
        if (mainMenuImage != null)
        {
            Vector3 pos = originalImagePosition;
            pos.x = imageStartX; // 500 (default position)
            mainMenuImage.anchoredPosition = pos;
        }
    }

    private void SelectFirstButton()
    {
        // Select the first button so gamepad navigation works
        if (playButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            lastSelectedButton = playButton.gameObject;
        }
    }

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

            // Apply selection effect to new button
            if (currentlySelected != null)
            {
                ApplyButtonSelectionEffect(currentlySelected);
            }

            lastSelectedButton = currentlySelected;
        }
    }

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

        // Get original position and scale
        Vector3 originalPos = originalButtonPositions.ContainsKey(buttonRect) 
            ? originalButtonPositions[buttonRect] 
            : buttonRect.anchoredPosition;
        
        Vector3 originalScale = originalButtonScales.ContainsKey(buttonRect) 
            ? originalButtonScales[buttonRect] 
            : buttonRect.localScale;

        // Calculate target position (move right)
        Vector3 targetPos = originalPos;
        targetPos.x += selectionMoveDistance;

        // Calculate target scale
        Vector3 targetScale = originalScale * selectionScaleAmount;

        // Animate position
        Tweener posTween = buttonRect.DOAnchorPos(targetPos, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate scale
        Tweener scaleTween = buttonRect.DOScale(targetScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = originalButtonColors.ContainsKey(buttonRect)
                ? originalButtonColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(selectionColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad);
        }

        // Store tween reference
        if (button != null)
        {
            activeButtonTweens[button] = posTween;
        }
    }

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

        // Get original position and scale
        Vector3 originalPos = originalButtonPositions.ContainsKey(buttonRect) 
            ? originalButtonPositions[buttonRect] 
            : buttonRect.anchoredPosition;
        
        Vector3 originalScale = originalButtonScales.ContainsKey(buttonRect) 
            ? originalButtonScales[buttonRect] 
            : buttonRect.localScale;

        // Animate back to original position
        Tweener posTween = buttonRect.DOAnchorPos(originalPos, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate back to original scale
        Tweener scaleTween = buttonRect.DOScale(originalScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate back to original color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = originalButtonColors.ContainsKey(buttonRect)
                ? originalButtonColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(originalColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    private void ResetAllButtonSelectionEffects()
    {
        // Reset all buttons to their original state
        foreach (var button in mainMenuButtons)
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

    private void ReturnToPressToBegin()
    {
        if (!isMainMenuActive) return;

        isMainMenuActive = false;
        isPressToBeginActive = true;
        currentMenuState = MenuState.PressToBegin;

        // Set input cooldown to prevent immediate re-triggering
        pressToBeginInputCooldown = 0.5f; // 0.5 second cooldown

        // Reset all button selection effects before hiding
        ResetAllButtonSelectionEffects();

        // Reset main menu elements to default positions before hiding
        ResetMainMenuElementsToDefault();

        // Hide main menu
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Show and reset press to begin panel (as if it was never pressed)
        if (pressToBeginPanel != null)
        {
            pressToBeginPanel.SetActive(true);
            
            // Reset press to begin panel to initial state
            if (pressToBeginCanvasGroup != null)
            {
                pressToBeginCanvasGroup.alpha = 1f; // Already visible, no fade needed
            }

            if (pressToBeginRectTransform != null)
            {
                pressToBeginRectTransform.localScale = Vector3.one; // Reset scale
            }

            // Reset text position if needed
            if (pressToBeginText != null && pressToBeginText.transform is RectTransform textRect && originalTextPosition != Vector3.zero)
            {
                textRect.anchoredPosition = originalTextPosition;
            }

            // Restart text hover animation
            StartPressToBeginTextAnimation();
        }
    }

    private void OnPlayButtonClicked()
    {
        OpenPlaySlotsPanel();
    }

    private void OnOptionsButtonClicked()
    {
        // Handle options panel opening
        if (optionsPanel != null)
        {
            // Reset all button selection effects before hiding
            ResetAllButtonSelectionEffects();
            
            // Reset main menu elements to default positions before hiding
            ResetMainMenuElementsToDefault();
            mainMenuPanel?.SetActive(false);
            optionsPanel.SetActive(true);
            isMainMenuActive = false;
            isOptionsActive = true;
            currentMenuState = MenuState.Options;
        }
    }

    private void OnCreditsButtonClicked()
    {
        // Handle credits panel opening
        if (creditsPanel != null)
        {
            // Reset all button selection effects before hiding
            ResetAllButtonSelectionEffects();
            
            // Reset main menu elements to default positions before hiding
            ResetMainMenuElementsToDefault();
            mainMenuPanel?.SetActive(false);
            creditsPanel.SetActive(true);
            isMainMenuActive = false;
            isCreditsActive = true;
            currentMenuState = MenuState.Credits;
        }
    }

    private void CloseOptionsPanel()
    {
        if (!isOptionsActive) return;

        isOptionsActive = false;
        isMainMenuActive = true;
        currentMenuState = MenuState.MainMenu;

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // Reset main menu elements to default positions
        ResetMainMenuElementsToDefault();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            // Re-animate main menu in
            // Note: Button selection and effects are applied after animation completes
            AnimateMainMenuIn();
        }
    }

    private void CloseCreditsPanel()
    {
        if (!isCreditsActive) return;

        isCreditsActive = false;
        isMainMenuActive = true;
        currentMenuState = MenuState.MainMenu;

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        // Reset main menu elements to default positions
        ResetMainMenuElementsToDefault();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            // Re-animate main menu in
            // Note: Button selection and effects are applied after animation completes
            AnimateMainMenuIn();
        }
    }

    private void OnQuitButtonClicked()
    {
        // Handle quit
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OpenPlaySlotsPanel()
    {
        if (isPlaySlotsActive) return;

        isMainMenuActive = false;
        isPlaySlotsActive = true;
        currentMenuState = MenuState.SaveSlots;
        selectedSlotIndex = 0; // Reset selection

        // Reset all button selection effects before hiding
        ResetAllButtonSelectionEffects();

        // Hide main menu
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Show play slots panel
        if (playSlotsPanel != null)
        {
            playSlotsPanel.SetActive(true);
            
            // Initialize save slot buttons with data
            InitializeSaveSlotButtons();
            
            AnimateSaveSlotsIn();
        }
    }

    /// <summary>
    /// Initialize save slot buttons with slot data
    /// </summary>
    private void InitializeSaveSlotButtons()
    {
        if (saveSlotButtons == null || saveSlotButtons.Count == 0) return;

        // Store scales and colors for slot buttons (positions already stored in PositionMainMenuElementsOffScreen)
        // But we need to update positions to use the final animated positions (default positions)
        originalSlotScales.Clear();
        originalSlotColors.Clear();
        
        for (int i = 0; i < saveSlotButtons.Count; i++)
        {
            if (saveSlotButtons[i] != null)
            {
                // Store final position (default position after animation)
                // Use the default positions array, preserving X from original
                if (originalSlotPositions.ContainsKey(saveSlotButtons[i]))
                {
                    Vector3 finalPos = originalSlotPositions[saveSlotButtons[i]];
                    if (i < saveSlotDefaultPositions.Length)
                    {
                        finalPos.y = saveSlotDefaultPositions[i]; // Use default Y position
                    }
                    originalSlotPositions[saveSlotButtons[i]] = finalPos;
                }
                else
                {
                    // Fallback: store current position if not already stored
                    Vector3 pos = saveSlotButtons[i].anchoredPosition;
                    if (i < saveSlotDefaultPositions.Length)
                    {
                        pos.y = saveSlotDefaultPositions[i];
                    }
                    originalSlotPositions[saveSlotButtons[i]] = pos;
                }
                
                // Store original scale
                originalSlotScales[saveSlotButtons[i]] = saveSlotButtons[i].localScale;
                
                // Store original color
                Image slotImage = saveSlotButtons[i].GetComponent<Image>();
                if (slotImage != null)
                {
                    originalSlotColors[saveSlotButtons[i]] = slotImage.color;
                }
                
                SaveSlotButton slotButton = saveSlotButtons[i].GetComponent<SaveSlotButton>();
                if (slotButton != null)
                {
                    slotButton.SetSlotIndex(i + 1); // Slots are 1-indexed
                    slotButton.RefreshSlotDisplay();
                }
            }
        }
        
        // Setup navigation for slot buttons
        SetupSlotButtonNavigation();
    }
    
    /// <summary>
    /// Setup navigation for save slot buttons
    /// </summary>
    private void SetupSlotButtonNavigation()
    {
        foreach (var button in saveSlotButtonComponents)
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
    /// Handle slot button selection effects
    /// </summary>
    private void HandleSlotButtonSelectionEffects()
    {
        if (EventSystem.current == null) return;

        GameObject currentlySelected = EventSystem.current.currentSelectedGameObject;
        
        // If selection changed
        if (currentlySelected != lastSelectedSlotButton)
        {
            // Reset previously selected slot button
            if (lastSelectedSlotButton != null)
            {
                ResetSlotButtonSelectionEffect(lastSelectedSlotButton);
            }

            // Apply selection effect to new slot button
            if (currentlySelected != null && IsSlotButton(currentlySelected))
            {
                ApplySlotButtonSelectionEffect(currentlySelected);
                
                // Update selected slot index when selection changes
                SaveSlotButton slotButton = currentlySelected.GetComponent<SaveSlotButton>();
                if (slotButton != null)
                {
                    SetSelectedSlot(slotButton.GetSlotIndex());
                }
            }

            lastSelectedSlotButton = currentlySelected;
        }
    }
    
    /// <summary>
    /// Check if a GameObject is a slot button
    /// </summary>
    private bool IsSlotButton(GameObject obj)
    {
        if (obj == null) return false;
        return saveSlotButtonComponents.Exists(btn => btn != null && btn.gameObject == obj);
    }
    
    /// <summary>
    /// Apply selection effect to a slot button
    /// </summary>
    private void ApplySlotButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        // Kill any existing tween for this button
        Button button = buttonObject.GetComponent<Button>();
        if (button != null && activeSlotButtonTweens.ContainsKey(button))
        {
            activeSlotButtonTweens[button].Kill();
            activeSlotButtonTweens.Remove(button);
        }

        // Get original position and scale
        Vector3 originalPos = originalSlotPositions.ContainsKey(buttonRect) 
            ? originalSlotPositions[buttonRect] 
            : buttonRect.anchoredPosition;
        
        Vector3 originalScale = originalSlotScales.ContainsKey(buttonRect) 
            ? originalSlotScales[buttonRect] 
            : buttonRect.localScale;

        // Calculate target position (move right)
        Vector3 targetPos = originalPos;
        targetPos.x += selectionMoveDistance;

        // Calculate target scale
        Vector3 targetScale = originalScale * selectionScaleAmount;

        // Animate position
        Tweener posTween = buttonRect.DOAnchorPos(targetPos, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate scale
        Tweener scaleTween = buttonRect.DOScale(targetScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = originalSlotColors.ContainsKey(buttonRect)
                ? originalSlotColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(selectionColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad);
        }

        // Store tween reference
        if (button != null)
        {
            activeSlotButtonTweens[button] = posTween;
        }
    }
    
    /// <summary>
    /// Reset selection effect for a slot button
    /// </summary>
    private void ResetSlotButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        // Kill any existing tween for this button
        Button button = buttonObject.GetComponent<Button>();
        if (button != null && activeSlotButtonTweens.ContainsKey(button))
        {
            activeSlotButtonTweens[button].Kill();
            activeSlotButtonTweens.Remove(button);
        }

        // Get original position and scale
        Vector3 originalPos = originalSlotPositions.ContainsKey(buttonRect) 
            ? originalSlotPositions[buttonRect] 
            : buttonRect.anchoredPosition;
        
        Vector3 originalScale = originalSlotScales.ContainsKey(buttonRect) 
            ? originalSlotScales[buttonRect] 
            : buttonRect.localScale;

        // Animate back to original position
        Tweener posTween = buttonRect.DOAnchorPos(originalPos, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate back to original scale
        Tweener scaleTween = buttonRect.DOScale(originalScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate back to original color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = originalSlotColors.ContainsKey(buttonRect)
                ? originalSlotColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(originalColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// Select the first slot button
    /// </summary>
    private void SelectFirstSlotButton()
    {
        if (saveSlotButtonComponents.Count > 0 && saveSlotButtonComponents[0] != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(saveSlotButtonComponents[0].gameObject);
            lastSelectedSlotButton = saveSlotButtonComponents[0].gameObject;
            ApplySlotButtonSelectionEffect(saveSlotButtonComponents[0].gameObject);
            
            // Track first slot selection
            SaveSlotButton slotButton = saveSlotButtonComponents[0].GetComponent<SaveSlotButton>();
            if (slotButton != null)
            {
                SetSelectedSlot(slotButton.GetSlotIndex());
            }
        }
    }

    private void AnimateSaveSlotsIn()
    {
        Sequence saveSlotsSequence = DOTween.Sequence();

        // Animate save slots from Y = 800 to their default positions (overlapping like closing animation)
        for (int i = 0; i < saveSlotButtons.Count; i++)
        {
            if (saveSlotButtons[i] != null)
            {
                // Get original position to preserve X
                Vector3 originalPos = originalSlotPositions.ContainsKey(saveSlotButtons[i]) 
                    ? originalSlotPositions[saveSlotButtons[i]] 
                    : saveSlotButtons[i].anchoredPosition;
                
                // Set start position (Y = 800, preserve X)
                Vector3 startPos = originalPos;
                startPos.y = saveSlotStartY; // 800
                saveSlotButtons[i].anchoredPosition = startPos;

                // Set target position (default Y position, preserve X)
                Vector3 targetPos = originalPos;
                if (i < saveSlotDefaultPositions.Length)
                {
                    targetPos.y = saveSlotDefaultPositions[i]; // Use default position: 150, -100, -350
                }

                // Use Join for overlapping animations (same style as closing)
                if (i == 0)
                {
                    // First slot starts immediately
                    saveSlotsSequence.Append(saveSlotButtons[i].DOAnchorPosY(targetPos.y, buttonAnimationDuration * 0.7f)
                        .SetEase(Ease.OutBack));
                }
                else
                {
                    // Subsequent slots overlap with delay (same style as closing)
                    saveSlotsSequence.Join(saveSlotButtons[i].DOAnchorPosY(targetPos.y, buttonAnimationDuration * 0.7f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(i * saveSlotDelay * 0.5f));
                }
            }
        }
        
        // Select first slot button after animation completes
        saveSlotsSequence.OnComplete(() =>
        {
            SelectFirstSlotButton();
        });
    }

    private void ClosePlaySlotsPanel()
    {
        if (!isPlaySlotsActive) return;

        isPlaySlotsActive = false;
        isMainMenuActive = true;
        currentMenuState = MenuState.MainMenu;
        selectedSlotIndex = 0; // Reset selection

        // Reset all slot button selection effects before hiding
        ResetAllSlotButtonSelectionEffects();

        // Animate save slots back to Y = 800
        Sequence closeSequence = DOTween.Sequence();

        for (int i = 0; i < saveSlotButtons.Count; i++)
        {
            if (saveSlotButtons[i] != null)
            {
                Vector3 currentPos = saveSlotButtons[i].anchoredPosition;
                Vector3 endPos = currentPos;
                endPos.y = saveSlotStartY; // 800

                closeSequence.Join(saveSlotButtons[i].DOAnchorPosY(endPos.y, buttonAnimationDuration * 0.7f)
                    .SetEase(Ease.InBack)
                    .SetDelay(i * saveSlotDelay * 0.5f));
            }
        }

        closeSequence.OnComplete(() =>
        {
            if (playSlotsPanel != null)
                playSlotsPanel.SetActive(false);
            
            // Reset main menu elements to default positions
            ResetMainMenuElementsToDefault();
            
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                // Re-animate main menu in
                // Note: Button selection and effects are applied after animation completes
                AnimateMainMenuIn();
            }
        });
    }
    
    /// <summary>
    /// Reset all slot button selection effects
    /// </summary>
    private void ResetAllSlotButtonSelectionEffects()
    {
        // Reset all slot buttons to their original state
        foreach (var slotRect in saveSlotButtons)
        {
            if (slotRect != null)
            {
                ResetSlotButtonSelectionEffect(slotRect.gameObject);
            }
        }

        // Clear selection tracking
        lastSelectedSlotButton = null;

        // Clear EventSystem selection
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Kill all active slot button tweens
        foreach (var tween in activeSlotButtonTweens.Values)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        activeSlotButtonTweens.Clear();
    }

    /// <summary>
    /// Set the currently selected slot index (called by SaveSlotButton)
    /// </summary>
    public void SetSelectedSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
    }
    
    /// <summary>
    /// Handle delete input (Triangle button on gamepad)
    /// </summary>
    private void HandleDeleteInput()
    {
        // Don't handle delete if popup is already open
        if (currentMenuState == MenuState.DeleteConfirm)
            return;
            
        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        if (gamepad == null)
            return;
            
        // Check for Triangle button (buttonNorth)
        if (gamepad.buttonNorth.wasPressedThisFrame) // Triangle = buttonNorth (not buttonWest!)
        {
            Debug.Log($"[Delete] Triangle pressed! State: {currentMenuState}, SelectedSlot: {selectedSlotIndex}");
            TryOpenDeleteConfirmation();
        }
    }
    
    /// <summary>
    /// Try to open delete confirmation popup
    /// </summary>
    private void TryOpenDeleteConfirmation()
    {
        Debug.Log($"[Delete] TryOpenDeleteConfirmation called. selectedSlotIndex: {selectedSlotIndex}");
        
        // Get currently selected slot from EventSystem if selectedSlotIndex is not set
        if (selectedSlotIndex <= 0 || selectedSlotIndex > 3)
        {
            Debug.Log("[Delete] selectedSlotIndex not set, trying EventSystem...");
            // Try to get selection from EventSystem
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                Debug.Log($"[Delete] EventSystem selected: {EventSystem.current.currentSelectedGameObject.name}");
                SaveSlotButton slotButton = EventSystem.current.currentSelectedGameObject.GetComponent<SaveSlotButton>();
                if (slotButton != null)
                {
                    selectedSlotIndex = slotButton.GetSlotIndex();
                    Debug.Log($"[Delete] Got slot index from EventSystem: {selectedSlotIndex}");
                }
                else
                {
                    Debug.LogWarning("[Delete] Selected object is not a SaveSlotButton. Cannot delete.");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[Delete] No slot selected in EventSystem. Cannot delete.");
                return;
            }
        }
            
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[Delete] SaveManager not found. Cannot delete slot.");
            return;
        }
            
        // Only allow deletion if slot exists
        if (!SaveManager.Instance.SlotExists(selectedSlotIndex))
        {
            Debug.Log($"[Delete] Slot {selectedSlotIndex} is empty. Cannot delete.");
            return;
        }
        
        Debug.Log($"[Delete] Opening delete confirmation for slot {selectedSlotIndex}");
        // Open confirmation popup
        ShowDeletePopup();
    }
    
    /// <summary>
    /// Show delete confirmation popup
    /// </summary>
    private void ShowDeletePopup()
    {
        Debug.Log("[Delete] ShowDeletePopup called");
        
        if (deleteConfirmPopup == null)
        {
            Debug.LogError("[Delete] deleteConfirmPopup is null! Please assign it in the inspector.");
            return;
        }
        
        currentMenuState = MenuState.DeleteConfirm;
        Debug.Log($"[Delete] Menu state changed to: {currentMenuState}");
        
        // Disable slot navigation while popup is open
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // Show background immediately (no animation)
        if (deleteConfirmBackground != null)
        {
            deleteConfirmBackground.SetActive(true);
        }
        
        // Reset any previous button states first (in case popup was closed with effects still applied)
        ResetAllDeleteButtonSelectionEffects();
        
        // Show popup window
        deleteConfirmPopup.SetActive(true);
        Debug.Log("[Delete] Popup set to active");
        
        // Store original button states for selection effects (fresh start)
        StoreDeleteButtonOriginalStates();
        
        // Animate only the popup window (panel), not the background
        if (deleteConfirmPanel != null)
        {
            // Start from slightly smaller scale
            deleteConfirmPanel.localScale = Vector3.one * 0.9f;
            // Animate to full scale
            deleteConfirmPanel.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);
        }
        else
        {
            Debug.LogWarning("[Delete] deleteConfirmPanel is null. Popup will appear without scale animation.");
        }
        
        // Fade in the window if CanvasGroup is on the panel
        if (deleteConfirmCanvasGroup != null)
        {
            deleteConfirmCanvasGroup.alpha = 0f;
            deleteConfirmCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }
        
        // Setup button navigation
        SetupDeletePopupNavigation();
        
        // Select "Yes" button by default - do this after a small delay to ensure popup is active
        StartCoroutine(SelectDeletePopupButton());
    }
    
    /// <summary>
    /// Store original states of delete popup buttons for selection effects
    /// </summary>
    private void StoreDeleteButtonOriginalStates()
    {
        originalDeleteButtonScales.Clear();
        originalDeleteButtonColors.Clear();
        
        if (deleteConfirmYesButton != null)
        {
            RectTransform yesRect = deleteConfirmYesButton.GetComponent<RectTransform>();
            if (yesRect != null)
            {
                originalDeleteButtonScales[yesRect] = yesRect.localScale;
                Image yesImage = deleteConfirmYesButton.GetComponent<Image>();
                if (yesImage != null)
                {
                    originalDeleteButtonColors[yesRect] = yesImage.color;
                }
            }
        }
        
        if (deleteConfirmNoButton != null)
        {
            RectTransform noRect = deleteConfirmNoButton.GetComponent<RectTransform>();
            if (noRect != null)
            {
                originalDeleteButtonScales[noRect] = noRect.localScale;
                Image noImage = deleteConfirmNoButton.GetComponent<Image>();
                if (noImage != null)
                {
                    originalDeleteButtonColors[noRect] = noImage.color;
                }
            }
        }
    }
    
    /// <summary>
    /// Handle delete popup button selection effects
    /// </summary>
    private void HandleDeleteButtonSelectionEffects()
    {
        if (EventSystem.current == null) return;

        GameObject currentlySelected = EventSystem.current.currentSelectedGameObject;
        
        // If selection changed
        if (currentlySelected != lastSelectedDeleteButton)
        {
            // Reset previously selected button
            if (lastSelectedDeleteButton != null)
            {
                ResetDeleteButtonSelectionEffect(lastSelectedDeleteButton);
            }

            // Apply selection effect to new button
            if (currentlySelected != null && IsDeleteButton(currentlySelected))
            {
                ApplyDeleteButtonSelectionEffect(currentlySelected);
            }

            lastSelectedDeleteButton = currentlySelected;
        }
    }
    
    /// <summary>
    /// Check if a GameObject is a delete popup button
    /// </summary>
    private bool IsDeleteButton(GameObject obj)
    {
        if (obj == null) return false;
        return (deleteConfirmYesButton != null && obj == deleteConfirmYesButton.gameObject) ||
               (deleteConfirmNoButton != null && obj == deleteConfirmNoButton.gameObject);
    }
    
    /// <summary>
    /// Apply selection effect to a delete popup button (scale from center, color change, no position movement)
    /// </summary>
    private void ApplyDeleteButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        // Kill any existing tween for this button
        Button button = buttonObject.GetComponent<Button>();
        if (button != null && activeDeleteButtonTweens.ContainsKey(button))
        {
            activeDeleteButtonTweens[button].Kill();
            activeDeleteButtonTweens.Remove(button);
        }

        // Get original scale
        Vector3 originalScale = originalDeleteButtonScales.ContainsKey(buttonRect) 
            ? originalDeleteButtonScales[buttonRect] 
            : buttonRect.localScale;

        // Calculate target scale (scale from center, no position movement)
        Vector3 targetScale = originalScale * selectionScaleAmount;

        // Animate scale (from center, no position change)
        Tweener scaleTween = buttonRect.DOScale(targetScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = originalDeleteButtonColors.ContainsKey(buttonRect)
                ? originalDeleteButtonColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(selectionColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad);
        }

        // Store tween reference
        if (button != null)
        {
            activeDeleteButtonTweens[button] = scaleTween;
        }
    }
    
    /// <summary>
    /// Reset selection effect for a delete popup button
    /// </summary>
    private void ResetDeleteButtonSelectionEffect(GameObject buttonObject)
    {
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        // Kill any existing tween for this button
        Button button = buttonObject.GetComponent<Button>();
        if (button != null && activeDeleteButtonTweens.ContainsKey(button))
        {
            activeDeleteButtonTweens[button].Kill();
            activeDeleteButtonTweens.Remove(button);
        }

        // Get original scale
        Vector3 originalScale = originalDeleteButtonScales.ContainsKey(buttonRect) 
            ? originalDeleteButtonScales[buttonRect] 
            : buttonRect.localScale;

        // Animate back to original scale
        Tweener scaleTween = buttonRect.DOScale(originalScale, selectionAnimationDuration)
            .SetEase(Ease.OutQuad);

        // Animate back to original color
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = originalDeleteButtonColors.ContainsKey(buttonRect)
                ? originalDeleteButtonColors[buttonRect]
                : buttonImage.color;
            
            Tweener colorTween = buttonImage.DOColor(originalColor, selectionAnimationDuration)
                .SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// Setup navigation for delete popup buttons
    /// </summary>
    private void SetupDeletePopupNavigation()
    {
        if (deleteConfirmYesButton != null && deleteConfirmNoButton != null)
        {
            // Setup Yes button navigation - can navigate to No in all directions
            Navigation yesNav = deleteConfirmYesButton.navigation;
            yesNav.mode = Navigation.Mode.Explicit;
            yesNav.selectOnRight = deleteConfirmNoButton;
            yesNav.selectOnLeft = deleteConfirmNoButton;
            yesNav.selectOnUp = deleteConfirmNoButton;
            yesNav.selectOnDown = deleteConfirmNoButton;
            deleteConfirmYesButton.navigation = yesNav;
            
            // Setup No button navigation - can navigate to Yes in all directions
            Navigation noNav = deleteConfirmNoButton.navigation;
            noNav.mode = Navigation.Mode.Explicit;
            noNav.selectOnRight = deleteConfirmYesButton;
            noNav.selectOnLeft = deleteConfirmYesButton;
            noNav.selectOnUp = deleteConfirmYesButton;
            noNav.selectOnDown = deleteConfirmYesButton;
            deleteConfirmNoButton.navigation = noNav;
            
            Debug.Log("[Delete] Button navigation setup complete - Yes ↔ No");
        }
        else
        {
            // Fallback: Use automatic navigation if buttons exist
            if (deleteConfirmYesButton != null)
            {
                Navigation nav = deleteConfirmYesButton.navigation;
                nav.mode = Navigation.Mode.Automatic;
                deleteConfirmYesButton.navigation = nav;
            }
            
            if (deleteConfirmNoButton != null)
            {
                Navigation nav = deleteConfirmNoButton.navigation;
                nav.mode = Navigation.Mode.Automatic;
                deleteConfirmNoButton.navigation = nav;
            }
            
            Debug.LogWarning("[Delete] Using automatic navigation - explicit navigation not possible (one button missing)");
        }
    }
    
    /// <summary>
    /// Coroutine to select popup button after popup is shown
    /// </summary>
    private System.Collections.IEnumerator SelectDeletePopupButton()
    {
        yield return new WaitForEndOfFrame();
        
        // Select "Yes" button by default
        if (deleteConfirmYesButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(deleteConfirmYesButton.gameObject);
            Debug.Log("[Delete] Selected 'Yes' button");
        }
        else
        {
            Debug.LogWarning("[Delete] Could not select 'Yes' button - button or EventSystem is null");
        }
    }
    
    /// <summary>
    /// Close delete confirmation popup
    /// </summary>
    private void CloseDeletePopup()
    {
        if (deleteConfirmPopup == null) return;
        
        // Immediately reset ALL button selection effects (no animation, instant reset)
        ResetAllDeleteButtonSelectionEffects();
        
        // Animate only the popup window out (not background)
        Sequence closeSequence = DOTween.Sequence();
        
        if (deleteConfirmCanvasGroup != null)
        {
            closeSequence.Join(deleteConfirmCanvasGroup.DOFade(0f, fadeDuration * 0.5f).SetEase(Ease.InQuad));
        }
        
        if (deleteConfirmPanel != null)
        {
            closeSequence.Join(deleteConfirmPanel.DOScale(Vector3.one * 0.9f, fadeDuration * 0.5f).SetEase(Ease.InBack));
        }
        
        closeSequence.OnComplete(() =>
        {
            deleteConfirmPopup.SetActive(false);
            
            // Hide background
            if (deleteConfirmBackground != null)
            {
                deleteConfirmBackground.SetActive(false);
            }
            
            currentMenuState = MenuState.SaveSlots;
            
            // Restore slot selection
            if (saveSlotButtonComponents.Count > 0 && selectedSlotIndex > 0 && selectedSlotIndex <= saveSlotButtonComponents.Count)
            {
                int buttonIndex = selectedSlotIndex - 1; // Convert to 0-based
                if (buttonIndex >= 0 && buttonIndex < saveSlotButtonComponents.Count && saveSlotButtonComponents[buttonIndex] != null)
                {
                    if (EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(saveSlotButtonComponents[buttonIndex].gameObject);
                    }
                }
            }
        });
    }
    
    /// <summary>
    /// Reset all delete button selection effects immediately (instant, no animation)
    /// </summary>
    private void ResetAllDeleteButtonSelectionEffects()
    {
        // Kill all active tweens
        foreach (var tween in activeDeleteButtonTweens.Values)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        activeDeleteButtonTweens.Clear();
        
        // Reset Yes button immediately
        if (deleteConfirmYesButton != null)
        {
            RectTransform yesRect = deleteConfirmYesButton.GetComponent<RectTransform>();
            if (yesRect != null)
            {
                // Reset scale immediately - use stored original or default to Vector3.one
                if (originalDeleteButtonScales.ContainsKey(yesRect))
                {
                    yesRect.localScale = originalDeleteButtonScales[yesRect];
                }
                else
                {
                    // If no original stored, reset to default scale
                    yesRect.localScale = Vector3.one;
                }
                
                // Reset color immediately - use stored original or get current as fallback
                Image yesImage = deleteConfirmYesButton.GetComponent<Image>();
                if (yesImage != null)
                {
                    if (originalDeleteButtonColors.ContainsKey(yesRect))
                    {
                        yesImage.color = originalDeleteButtonColors[yesRect];
                    }
                    // If no original stored, we'll let StoreDeleteButtonOriginalStates handle it next time
                }
            }
        }
        
        // Reset No button immediately
        if (deleteConfirmNoButton != null)
        {
            RectTransform noRect = deleteConfirmNoButton.GetComponent<RectTransform>();
            if (noRect != null)
            {
                // Reset scale immediately - use stored original or default to Vector3.one
                if (originalDeleteButtonScales.ContainsKey(noRect))
                {
                    noRect.localScale = originalDeleteButtonScales[noRect];
                }
                else
                {
                    // If no original stored, reset to default scale
                    noRect.localScale = Vector3.one;
                }
                
                // Reset color immediately - use stored original or get current as fallback
                Image noImage = deleteConfirmNoButton.GetComponent<Image>();
                if (noImage != null)
                {
                    if (originalDeleteButtonColors.ContainsKey(noRect))
                    {
                        noImage.color = originalDeleteButtonColors[noRect];
                    }
                    // If no original stored, we'll let StoreDeleteButtonOriginalStates handle it next time
                }
            }
        }
        
        // Clear selection tracking
        lastSelectedDeleteButton = null;
        
        // Clear EventSystem selection
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
    
    /// <summary>
    /// Confirm delete action
    /// </summary>
    private void ConfirmDelete()
    {
        if (selectedSlotIndex <= 0 || selectedSlotIndex > 3)
            return;
            
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager not found. Cannot delete slot.");
            CloseDeletePopup();
            return;
        }
        
        // Delete the slot
        if (SaveManager.Instance.DeleteSlot(selectedSlotIndex))
        {
            Debug.Log($"Slot {selectedSlotIndex} deleted successfully");
        }
        else
        {
            Debug.LogError($"Failed to delete slot {selectedSlotIndex}");
        }
        
        // Close popup and refresh slots
        CloseDeletePopup();
        RefreshSaveSlots();
    }
    
    /// <summary>
    /// Cancel delete action
    /// </summary>
    private void CancelDelete()
    {
        CloseDeletePopup();
    }
    
    /// <summary>
    /// Refresh save slot displays after deletion
    /// </summary>
    private void RefreshSaveSlots()
    {
        if (saveSlotButtons == null) return;
        
        foreach (var slotRect in saveSlotButtons)
        {
            if (slotRect != null)
            {
                SaveSlotButton slotButton = slotRect.GetComponent<SaveSlotButton>();
                if (slotButton != null)
                {
                    slotButton.RefreshSlotDisplay();
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up tweens
        DOTween.KillAll();
        pressToBeginTextSequence?.Kill();
    }
}
