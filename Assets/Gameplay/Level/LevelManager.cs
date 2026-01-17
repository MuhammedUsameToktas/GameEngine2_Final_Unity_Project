using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// LevelManager - Handles level setup, player spawning, and save data restoration
/// Per spec: Spawns player, restores saved data, handles level restart, supports checkpoints
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Current Level")]
    [SerializeField] private LevelConfig currentLevelConfig;
    [SerializeField] private string currentLevelName;

    [Header("Player Spawn")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private GameObject playerPrefab; // Player prefab to spawn

    [Header("Camera")]
    [SerializeField] private CinemachineVirtualDynamic cinemachineCamera; // Cinemachine camera to follow player

    private GameObject playerInstance;
    private Vector3 currentSpawnPoint; // Per spec: current spawn point (checkpoint or default)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Subscribe to events
        EventBus.OnLevelLoaded += HandleLevelLoaded;
    }
    
    private void OnDestroy()
    {
        EventBus.OnLevelLoaded -= HandleLevelLoaded;
    }
    
    private void Start()
    {
        // Initialize spawn point to default
        currentSpawnPoint = defaultSpawnPoint != null ? defaultSpawnPoint.position : Vector3.zero;
        
        // Initialize level when scene loads
        InitializeLevel();
    }
    
    /// <summary>
    /// Initialize level: spawn player and restore saved data
    /// Per spec: LevelManager coordinates gameplay setup
    /// </summary>
    private void InitializeLevel()
    {
        // Get current level name from active scene
        currentLevelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Load level config if available
        LoadLevelConfig(currentLevelName);
        
        // Load checkpoint position from save data
        LoadCheckpointFromSave();
        
        // Determine spawn position based on new game vs loaded game
        Vector3 spawnPosition = DetermineSpawnPosition();
        
        // Spawn player
        SpawnPlayerAtPosition(spawnPosition);
        
        // Restore saved data after a frame to ensure all saveables are registered
        // Per spec: LevelManager asks SaveManager to restore data
        StartCoroutine(RestoreSaveDataDelayed());
    }
    
    /// <summary>
    /// Restore save data after a frame delay to ensure all saveables are registered
    /// </summary>
    private System.Collections.IEnumerator RestoreSaveDataDelayed()
    {
        yield return null; // Wait one frame for all Awake() calls to complete
        
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RestoreAllSaveables();
        }
    }
    
    /// <summary>
    /// Determine spawn position based on game state
    /// Per spec: New Game → Default spawn, Load Game → Saved checkpoint
    /// </summary>
    private Vector3 DetermineSpawnPosition()
    {
        if (SaveManager.Instance == null)
        {
            return currentSpawnPoint != Vector3.zero ? currentSpawnPoint : GetPlayerSpawnPoint(); // Fallback to default
        }
        
        // Per spec: New Game → Default spawn
        if (SaveManager.Instance.IsNewGame())
        {
            return currentSpawnPoint != Vector3.zero ? currentSpawnPoint : GetPlayerSpawnPoint();
        }
        
        // Per spec: Load Game → Saved checkpoint (if exists)
        if (currentSpawnPoint != Vector3.zero)
        {
            return currentSpawnPoint; // Use checkpoint from save data
        }
        
        // Fallback: Try saved player position
        if (SaveManager.Instance.CurrentSaveData != null && SaveManager.Instance.CurrentSaveData.playerData != null)
        {
            Vector3 savedPosition = SaveManager.Instance.CurrentSaveData.playerData.position;
            if (savedPosition != Vector3.zero)
            {
                return savedPosition;
            }
        }
        
        // Final fallback: Default spawn
        return GetPlayerSpawnPoint();
    }
    
    /// <summary>
    /// Handle level loaded event
    /// </summary>
    private void HandleLevelLoaded(string levelName)
    {
        // Level was loaded, initialize it
        InitializeLevel();
    }

    /// <summary>
    /// Load a level by name
    /// </summary>
    public void LoadLevel(string levelName)
    {
        string scenePath = $"Scenes/Levels/{levelName}";
        
        SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
        
        currentLevelName = levelName;
        
        // Find level config if it exists
        LoadLevelConfig(levelName);
    }

    /// <summary>
    /// Load a level using a LevelConfig
    /// </summary>
    public void LoadLevel(LevelConfig config)
    {
        if (config == null)
        {
            Debug.LogError("Cannot load level: LevelConfig is null");
            return;
        }

        currentLevelConfig = config;
        LoadLevel(config.levelName);
    }

    /// <summary>
    /// Restart the current level
    /// Per spec: LevelManager handles level restart
    /// </summary>
    public void RestartLevel()
    {
        if (!string.IsNullOrEmpty(currentLevelName))
        {
            // Reset checkpoint to default spawn
            currentSpawnPoint = defaultSpawnPoint != null ? defaultSpawnPoint.position : Vector3.zero;
            
            // Reload the scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    /// <summary>
    /// Unload the current level
    /// </summary>
    public void UnloadCurrentLevel()
    {
        if (!string.IsNullOrEmpty(currentLevelName))
        {
            SceneManager.UnloadSceneAsync($"Scenes/Levels/{currentLevelName}");
            currentLevelName = null;
            currentLevelConfig = null;
        }
    }

    /// <summary>
    /// Get the spawn point for the player
    /// </summary>
    public Vector3 GetPlayerSpawnPoint()
    {
        if (currentLevelConfig != null && currentLevelConfig.playerSpawnPoint != null)
        {
            return currentLevelConfig.playerSpawnPoint.position;
        }

        if (defaultSpawnPoint != null)
        {
            return defaultSpawnPoint.position;
        }

        // Fallback to origin
        return Vector3.zero;
    }

    /// <summary>
    /// Spawn the player at the spawn point
    /// Per spec: Player prefab is spawned by LevelManager, never placed manually
    /// </summary>
    public void SpawnPlayer(GameObject playerPrefab)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Cannot spawn player: Player prefab is null");
            return;
        }

        Vector3 spawnPosition = GetPlayerSpawnPoint();
        SpawnPlayerAtPosition(spawnPosition);
    }
    
    /// <summary>
    /// Spawn player at specific position
    /// Per spec: LevelManager spawns player
    /// </summary>
    private void SpawnPlayerAtPosition(Vector3 position)
    {
        // Use prefab reference if available, otherwise try to load from Resources
        GameObject prefabToSpawn = playerPrefab;
        
        if (prefabToSpawn == null)
        {
            // Try to load from Resources as fallback
            prefabToSpawn = Resources.Load<GameObject>("Prefabs/Player");
        }
        
        if (prefabToSpawn == null)
        {
            Debug.LogError("Cannot spawn player: Player prefab is null. Assign it in LevelManager or place in Resources/Prefabs/Player");
            return;
        }

        // Destroy existing player instance if any
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }

        playerInstance = Instantiate(prefabToSpawn, position, Quaternion.identity);
        Debug.Log($"Player spawned at position: {position}");
        
        // Assign camera target after spawning player
        AssignCameraToPlayer();
    }
    
    /// <summary>
    /// Assign Cinemachine camera to follow the spawned player
    /// Per spec: LevelManager connects camera to player after spawning
    /// </summary>
    private void AssignCameraToPlayer()
    {
        if (playerInstance == null)
        {
            Debug.LogWarning("Cannot assign camera: Player instance is null");
            return;
        }

        // Find camera if not assigned in inspector
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindObjectOfType<CinemachineVirtualDynamic>();
        }

        if (cinemachineCamera == null)
        {
            Debug.LogWarning("Cannot assign camera: CinemachineVirtualDynamic not found. Assign it in LevelManager inspector or add it to the scene.");
            return;
        }

        // Assign the player as the camera target
        cinemachineCamera.AssignTarget(playerInstance);
    }
    
    /// <summary>
    /// Set checkpoint position (per spec: checkpoint updates spawn point)
    /// </summary>
    public void SetCheckpoint(Vector3 newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
        
        // Save checkpoint position to save data
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            SaveManager.Instance.CurrentSaveData.checkpointPosition = newSpawnPoint;
        }
        
        Debug.Log($"Checkpoint set at: {newSpawnPoint}");
    }
    
    /// <summary>
    /// Load checkpoint position from save data
    /// </summary>
    private void LoadCheckpointFromSave()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            Vector3 savedCheckpoint = SaveManager.Instance.CurrentSaveData.checkpointPosition;
            // Only update if we have a valid checkpoint (not zero)
            // Note: We check != Vector3.zero because zero means no checkpoint saved
            if (savedCheckpoint != Vector3.zero)
            {
                currentSpawnPoint = savedCheckpoint;
                Debug.Log($"Checkpoint loaded from save: {currentSpawnPoint}");
            }
            else
            {
                Debug.Log("No checkpoint found in save data, using default spawn point");
            }
        }
        else
        {
            Debug.Log("SaveManager or CurrentSaveData is null, using default spawn point");
        }
    }
    
    /// <summary>
    /// Get current checkpoint position
    /// </summary>
    public Vector3 GetCheckpointPosition()
    {
        return currentSpawnPoint;
    }

    /// <summary>
    /// Load level config from Resources or ScriptableObjects folder
    /// </summary>
    private void LoadLevelConfig(string levelName)
    {
        // Try to load from Resources
        LevelConfig config = Resources.Load<LevelConfig>($"LevelConfigs/{levelName}");
        
        if (config != null)
        {
            currentLevelConfig = config;
        }
        else
        {
            Debug.LogWarning($"No LevelConfig found for level: {levelName}");
        }
    }

    /// <summary>
    /// Get current level config
    /// </summary>
    public LevelConfig GetCurrentLevelConfig()
    {
        return currentLevelConfig;
    }

    /// <summary>
    /// Get current level name
    /// </summary>
    public string GetCurrentLevelName()
    {
        return currentLevelName;
    }
    
    /// <summary>
    /// Get player instance (for external access)
    /// </summary>
    public GameObject GetPlayerInstance()
    {
        return playerInstance;
    }

    /// <summary>
    /// Respawn player at current checkpoint
    /// Per spec: Clean reset without scene reload
    /// </summary>
    public void RespawnPlayer()
    {
        if (playerInstance == null)
        {
            Debug.LogWarning("Cannot respawn player: Player instance is null");
            return;
        }

        // Reset position to checkpoint
        playerInstance.transform.position = currentSpawnPoint;
        playerInstance.transform.rotation = Quaternion.identity;

        // Reset velocity (if using Rigidbody)
        Rigidbody rb = playerInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset CharacterController velocity
        CharacterController controller = playerInstance.GetComponent<CharacterController>();
        if (controller != null)
        {
            // CharacterController doesn't have velocity, but we can ensure it's enabled
            controller.enabled = true;
        }

        Debug.Log($"Player respawned at checkpoint: {currentSpawnPoint}");
    }
}

