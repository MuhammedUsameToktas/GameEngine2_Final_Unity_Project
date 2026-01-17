using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Handles player input using Unity's Input System
/// Provides MoveInput as a Vector2 property for PlayerMovement
/// Reads input directly each frame to avoid reflection issues
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool attackPressed;
    private bool swingPressed;
    private bool swingHeld;
    private bool wasPaused = false;
    private Coroutine resumeInputDelayCoroutine;
    private PlayerHeal playerHeal;

    public Vector2 MoveInput => moveInput;
    public bool JumpPressed => jumpPressed;
    public bool JumpHeld => jumpHeld;
    public bool AttackPressed => attackPressed;
    public bool SwingPressed => swingPressed;
    public bool SwingHeld => swingHeld;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        playerHeal = GetComponent<PlayerHeal>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        // Subscribe to pause events to disable/enable Player action map
        EventBus.OnGamePaused += OnGamePaused;
        EventBus.OnGameResumed += OnGameResumed;
        
        // Check initial pause state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
        {
            OnGamePaused();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.OnGamePaused -= OnGamePaused;
        EventBus.OnGameResumed -= OnGameResumed;
        
        // Dispose input actions
        inputActions?.Dispose();
    }

    private void Update()
    {
        // Check if game is paused
        bool isPaused = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;

        // Don't read input when game is paused
        if (isPaused)
        {
            moveInput = Vector2.zero;
            jumpPressed = false;
            jumpHeld = false;
            attackPressed = false;
            swingPressed = false;
            swingHeld = false;
            // Stop healing if paused
            if (playerHeal != null && playerHeal.IsHealing)
            {
                playerHeal.StopHealing();
            }
            return;
        }

        // Read input directly each frame - avoids reflection/callback issues
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        
        // Read jump input
        jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        jumpHeld = inputActions.Player.Jump.IsPressed();
        
        // Read attack input
        attackPressed = inputActions.Player.Attack.WasPressedThisFrame();
        
        // Read swing input (using Interact action as X button)
        swingPressed = inputActions.Player.Interact.WasPressedThisFrame();
        swingHeld = inputActions.Player.Interact.IsPressed();

        // Handle heal input (hold to heal)
        if (playerHeal != null)
        {
            bool healPressed = inputActions.Player.Heal.IsPressed();
            
            if (healPressed && !playerHeal.IsHealing)
            {
                playerHeal.StartHealing();
            }
            else if (!healPressed && playerHeal.IsHealing)
            {
                playerHeal.StopHealing();
            }
        }
    }

    /// <summary>
    /// Called when game is paused - disable Player action map to prevent input
    /// </summary>
    private void OnGamePaused()
    {
        wasPaused = true;
        // Disable Player action map so it can't read any input while paused
        if (inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    /// <summary>
    /// Called when game is resumed - re-enable Player action map after delay
    /// </summary>
    private void OnGameResumed()
    {
        if (wasPaused)
        {
            wasPaused = false;
            // Stop any existing delay coroutine
            if (resumeInputDelayCoroutine != null)
            {
                StopCoroutine(resumeInputDelayCoroutine);
            }
            // Re-enable Player action map after a short delay to prevent Resume button press from being read as Jump
            resumeInputDelayCoroutine = StartCoroutine(EnablePlayerInputAfterDelay(0.3f));
        }
    }

    /// <summary>
    /// Coroutine to re-enable Player input after a delay
    /// This prevents the Resume button press (X button) from being read as Jump input
    /// </summary>
    private IEnumerator EnablePlayerInputAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Use unscaled time since we just resumed
        
        // Re-enable Player action map
        if (inputActions != null)
        {
            inputActions.Player.Enable();
        }
        
        resumeInputDelayCoroutine = null;
    }
}
