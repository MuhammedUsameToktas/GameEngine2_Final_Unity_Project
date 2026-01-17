using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Cutscene
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    private GameState currentState = GameState.Menu;

    [Header("Settings")]
    [SerializeField] private bool pauseOnFocusLoss = true;

    [Header("Playtime Tracking")]
    private float sessionPlayTime = 0f; // Accumulated playtime for current session

    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to events
        EventBus.OnGameStarted += HandleGameStarted;
        EventBus.OnLevelLoaded += HandleLevelLoaded;
    }

    private void Start()
    {
        // Load MainMenu additively (PersistentScene stays loaded)
        // This is the professional way: PersistentScene is a container, not a screen
        SceneManager.LoadScene("Scenes/Menus/MainMenu", LoadSceneMode.Additive);
    }

    private void OnDestroy()
    {
        EventBus.OnGameStarted -= HandleGameStarted;
        EventBus.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void Update()
    {
        // Track playtime only when actively playing (not in menu, paused, or cutscene)
        if (currentState == GameState.Playing)
        {
            sessionPlayTime += Time.unscaledDeltaTime;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && pauseOnFocusLoss && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Called when the application is about to quit (build or editor)
    /// </summary>
    private void OnApplicationQuit()
    {
        TrySaveOnExit();
    }

    /// <summary>
    /// Called when the application is paused (mobile background, Alt+Tab, etc.)
    /// </summary>
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            TrySaveOnExit();
        }
    }

    /// <summary>
    /// Emergency save on exit - saves game if there's an active slot
    /// </summary>
    private void TrySaveOnExit()
    {
        if (SaveManager.Instance == null)
            return;

        if (!SaveManager.Instance.HasActiveSlot)
            return;

        // Prepare save data (accumulate session playtime)
        PrepareSaveData();
        
        // Save the game
        SaveManager.Instance.SaveGame();
    }

    /// <summary>
    /// Start game from a save slot (unified entry point)
    /// Per spec: Sets active slot and loads GameBootstrap scene
    /// GameBootstrap will handle deciding new game vs load game
    /// </summary>
    public void StartGame(int slotIndex)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager not found. Cannot start game.");
            return;
        }

        // Set active slot (per spec requirement)
        SaveManager.Instance.SetActiveSlot(slotIndex);

        // Load GameBootstrap scene (per spec requirement)
        // Single mode unloads MainMenu but PersistentScene stays due to DontDestroyOnLoad
        SceneManager.LoadScene("Scenes/System/GameBootstrap", LoadSceneMode.Single);
    }

    /// <summary>
    /// Start a new game (legacy method - kept for compatibility)
    /// </summary>
    public void StartNewGame(int saveSlot)
    {
        StartGame(saveSlot);
    }

    /// <summary>
    /// Load an existing game (legacy method - kept for compatibility)
    /// </summary>
    public void LoadGame(int saveSlot)
    {
        StartGame(saveSlot);
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            EventBus.OnGamePaused?.Invoke();
        }
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            EventBus.OnGameResumed?.Invoke();
        }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMenu()
    {
        // Accumulate session playtime before returning to menu
        PrepareSaveData();
        
        Time.timeScale = 1f;
        ChangeState(GameState.Menu);
        // Load additively so PersistentScene stays loaded
        SceneManager.LoadScene("Scenes/Menus/MainMenu", LoadSceneMode.Additive);
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        // Accumulate session playtime before saving and quitting
        PrepareSaveData();
        
        // Save before quitting if needed
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentSaveSlot() > 0)
        {
            SaveManager.Instance.SaveGame(SaveManager.Instance.GetCurrentSaveSlot());
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Change game state
    /// </summary>
    private void ChangeState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;

        // Reset session playtime when transitioning from Menu to Playing (new game/load)
        if (previousState == GameState.Menu && newState == GameState.Playing)
        {
            ResetSessionPlayTime();
        }

        Debug.Log($"Game state changed: {previousState} -> {newState}");
    }

    /// <summary>
    /// Handle game started event
    /// </summary>
    private void HandleGameStarted()
    {
        // Additional logic when game starts
    }

    /// <summary>
    /// Handle level loaded event - set state to Playing
    /// </summary>
    private void HandleLevelLoaded(string levelName)
    {
        // Set state to Playing when a level loads (if we're not in menu)
        if (currentState == GameState.Menu)
        {
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// Set cutscene state
    /// </summary>
    public void SetCutsceneState(bool inCutscene)
    {
        if (inCutscene)
        {
            ChangeState(GameState.Cutscene);
        }
        else if (currentState == GameState.Cutscene)
        {
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// Prepare save data by accumulating session playtime
    /// Call this before saving, quitting, or changing scenes
    /// Returns the session playtime that was accumulated (for cases where currentSaveData is null)
    /// </summary>
    public float PrepareSaveData()
    {
        float accumulatedTime = sessionPlayTime;
        
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            // Accumulate session playtime to save data
            SaveManager.Instance.CurrentSaveData.playTime += sessionPlayTime;
        }
        
        // Reset session counter (time is now being saved)
        sessionPlayTime = 0f;
        
        return accumulatedTime;
    }

    /// <summary>
    /// Reset session playtime (call when starting/loading a game)
    /// </summary>
    public void ResetSessionPlayTime()
    {
        sessionPlayTime = 0f;
    }

    /// <summary>
    /// Format playtime in HH:MM:SS format
    /// </summary>
    public static string FormatPlayTime(float seconds)
    {
        int hours = Mathf.FloorToInt(seconds / 3600f);
        int minutes = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{hours:D2}:{minutes:D2}:{secs:D2}";
    }
}
