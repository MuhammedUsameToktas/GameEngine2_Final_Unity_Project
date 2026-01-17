using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const int MAX_SAVE_SLOTS = 3;
    private const string SAVE_DIRECTORY = "Saves";
    private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY);

    // ========== REQUIRED DATA (per spec) ==========
    // Private fields for internal use (must be declared before properties)
    private int currentSaveSlot = -1;
    private SaveData currentSaveData;

    // Public properties for external access
    public int ActiveSlot 
    { 
        get => currentSaveSlot; 
        private set => currentSaveSlot = value; 
    }
    
    public SaveData CurrentSaveData 
    { 
        get => currentSaveData; 
        private set => currentSaveData = value; 
    }
    
    /// <summary>
    /// Check if there is an active save slot
    /// </summary>
    public bool HasActiveSlot => currentSaveSlot > 0;

    private List<ISaveable> saveableObjects = new List<ISaveable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure save directory exists
        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }

        // Subscribe to save events
        EventBus.OnSaveRequested += SaveGame;
    }

    private void OnDestroy()
    {
        EventBus.OnSaveRequested -= SaveGame;
    }

    /// <summary>
    /// Register an object that can be saved/loaded
    /// </summary>
    public void RegisterSaveable(ISaveable saveable)
    {
        if (!saveableObjects.Contains(saveable))
        {
            saveableObjects.Add(saveable);
        }
    }

    /// <summary>
    /// Unregister a saveable object
    /// </summary>
    public void UnregisterSaveable(ISaveable saveable)
    {
        saveableObjects.Remove(saveable);
    }

    /// <summary>
    /// Save game to a specific slot (1-3)
    /// </summary>
    public bool SaveGame(int slot)
    {
        if (slot < 1 || slot > MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slot}. Must be between 1 and {MAX_SAVE_SLOTS}");
            return false;
        }

        ActiveSlot = slot;

        // Prepare save data (accumulate session playtime into currentSaveData if it exists)
        // Returns the session time that was accumulated (important if currentSaveData is null)
        float accumulatedSessionTime = 0f;
        if (GameManager.Instance != null)
        {
            accumulatedSessionTime = GameManager.Instance.PrepareSaveData();
        }

        // Preserve playtime and checkpoint before creating new SaveData
        float preservedPlayTime = 0f;
        Vector3 preservedCheckpointPosition = Vector3.zero;
        if (currentSaveData != null)
        {
            // Playtime was already accumulated in PrepareSaveData()
            preservedPlayTime = currentSaveData.playTime;
            // Preserve checkpoint position
            preservedCheckpointPosition = currentSaveData.checkpointPosition;
        }
        else
        {
            // No current save data, try to load existing save to preserve playtime and checkpoint
            SaveData existingData = GetSaveMetadata(slot);
            if (existingData != null)
            {
                preservedPlayTime = existingData.playTime;
                preservedCheckpointPosition = existingData.checkpointPosition;
            }
            // Add accumulated session time (since currentSaveData was null, it wasn't added automatically)
            preservedPlayTime += accumulatedSessionTime;
        }
        
        // Fallback: If checkpoint is zero but LevelManager has a checkpoint, use it
        if (preservedCheckpointPosition == Vector3.zero && LevelManager.Instance != null)
        {
            Vector3 levelManagerCheckpoint = LevelManager.Instance.GetCheckpointPosition();
            if (levelManagerCheckpoint != Vector3.zero)
            {
                preservedCheckpointPosition = levelManagerCheckpoint;
            }
        }

        // Create new save data
        currentSaveData = new SaveData();
        CurrentSaveData = currentSaveData;
        currentSaveData.playerData = new PlayerSaveData();
        currentSaveData.worldData = new WorldSaveData();
        
        // Restore preserved playtime (includes accumulated session time)
        currentSaveData.playTime = preservedPlayTime;
        // Restore preserved checkpoint position
        currentSaveData.checkpointPosition = preservedCheckpointPosition;
        
        // CRITICAL: Always check LevelManager's current checkpoint and use it if available
        // This ensures checkpoint is saved even if SetCheckpoint() was called when CurrentSaveData was null
        if (LevelManager.Instance != null)
        {
            Vector3 levelManagerCheckpoint = LevelManager.Instance.GetCheckpointPosition();
            if (levelManagerCheckpoint != Vector3.zero)
            {
                currentSaveData.checkpointPosition = levelManagerCheckpoint;
                Debug.Log($"Using LevelManager checkpoint position: {levelManagerCheckpoint}");
            }
        }

        // Capture state from all saveable objects
        foreach (var saveable in saveableObjects)
        {
            string id = saveable.GetUniqueID();
            object state = saveable.CaptureState();
            
            if (state != null)
            {
                currentSaveData.worldData.StoreObject(id, state);
                
                // Special handling for player: also save to PlayerSaveData
                if (id.StartsWith("Player_") && state is Vector3 playerPosition)
                {
                    currentSaveData.playerData.position = playerPosition;
                }
            }
        }

        // Capture lost soul state (if player exists)
        GameObject player = null;
        if (LevelManager.Instance != null)
        {
            player = LevelManager.Instance.GetPlayerInstance();
        }
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            PlayerDeath playerDeath = player.GetComponent<PlayerDeath>();
            if (playerDeath != null)
            {
                currentSaveData.lostSoul = playerDeath.CaptureLostSoul();
            }
            
            // Save currency
            PlayerCurrency playerCurrency = player.GetComponent<PlayerCurrency>();
            if (playerCurrency != null)
            {
                currentSaveData.coins = playerCurrency.Coins;
                currentSaveData.playerData.coins = playerCurrency.Coins; // Also save to PlayerSaveData for backward compatibility
            }
            
            // Save inventory
            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                currentSaveData.inventory.Clear();
                foreach (var item in playerInventory.GetAllItems())
                {
                    currentSaveData.inventory.Add(new InventoryItem 
                    { 
                        itemID = item.Key, 
                        quantity = item.Value 
                    });
                }
            }
        }

        // Add metadata
        currentSaveData.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        currentSaveData.lastLevel = sceneName;
        currentSaveData.currentLevel = sceneName; // Per spec: update current level
        currentSaveData.isNewGame = false; // Per spec: no longer a new game after first save

        // Save to file
        string filePath = GetSaveFilePath(slot);
        string json = JsonUtility.ToJson(currentSaveData, true);

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"Game saved to slot {slot}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save game using current slot
    /// </summary>
    public void SaveGame()
    {
        if (currentSaveSlot > 0)
        {
            SaveGame(currentSaveSlot);
        }
    }

    /// <summary>
    /// Load game from a specific slot (1-3)
    /// </summary>
    public bool LoadGame(int slot)
    {
        if (slot < 1 || slot > MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slot}. Must be between 1 and {MAX_SAVE_SLOTS}");
            return false;
        }

        string filePath = GetSaveFilePath(slot);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save file found at slot {slot}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            CurrentSaveData = currentSaveData;
            ActiveSlot = slot;

            // Restore state to all saveable objects
            foreach (var saveable in saveableObjects)
            {
                string id = saveable.GetUniqueID();
                
                if (currentSaveData.worldData.HasObject(id))
                {
                    // We need to get the type information from the save data
                    // For now, we'll pass the JSON string and let the saveable handle deserialization
                    var saveData = currentSaveData.worldData.savedObjects.Find(x => x.id == id);
                    if (saveData != null)
                    {
                        // Try to deserialize based on type
                        try
                        {
                            Type objectType = Type.GetType(saveData.dataType);
                            if (objectType != null)
                            {
                                object state = JsonUtility.FromJson(saveData.dataJson, objectType);
                                saveable.RestoreState(state);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to restore state for {id}: {e.Message}");
                        }
                    }
                }
            }

            Debug.Log($"Game loaded from slot {slot}");
            
            // Reset session playtime when loading a game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSessionPlayTime();
            }
            
            EventBus.OnLoadCompleted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a save slot has data
    /// </summary>
    public bool HasSaveData(int slot)
    {
        if (slot < 1 || slot > MAX_SAVE_SLOTS) return false;
        return File.Exists(GetSaveFilePath(slot));
    }

    /// <summary>
    /// Get save metadata without loading the full game
    /// </summary>
    public SaveData GetSaveMetadata(int slot)
    {
        if (slot < 1 || slot > MAX_SAVE_SLOTS) return null;

        string filePath = GetSaveFilePath(slot);
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read save metadata: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete a save slot
    /// </summary>
    public bool DeleteSave(int slot)
    {
        if (slot < 1 || slot > MAX_SAVE_SLOTS) return false;

        string filePath = GetSaveFilePath(slot);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"Save slot {slot} deleted");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Delete a save slot and clear cached data (for UI deletion)
    /// </summary>
    public bool DeleteSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slotIndex}. Must be between 1 and {MAX_SAVE_SLOTS}");
            return false;
        }

        string filePath = GetSaveFilePath(slotIndex);
        
        // Delete file if it exists
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"Save slot {slotIndex} deleted");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save slot {slotIndex}: {e.Message}");
                return false;
            }
        }

        // Clear cached data if this slot was active
        ClearCachedSlotData(slotIndex);
        
        return true;
    }

    /// <summary>
    /// Clear cached slot data (for when slot is deleted)
    /// </summary>
    private void ClearCachedSlotData(int slotIndex)
    {
        // Reset in-memory data if this slot was loaded
        if (ActiveSlot == slotIndex)
        {
            ActiveSlot = -1;
            CurrentSaveData = null;
            currentSaveData = null;
        }
    }

    /// <summary>
    /// Get the file path for a save slot
    /// </summary>
    private string GetSaveFilePath(int slot)
    {
        return Path.Combine(SavePath, $"save_slot_{slot}.json");
    }

    /// <summary>
    /// Get current save slot
    /// </summary>
    public int GetCurrentSaveSlot()
    {
        return currentSaveSlot;
    }

    /// <summary>
    /// Get current save data
    /// </summary>
    public SaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }

    // ========== SLOT AWARENESS METHODS (FOR MENU UI) ==========

    /// <summary>
    /// Get slot info for menu display (metadata only, doesn't load full game)
    /// </summary>
    public SaveSlotInfo GetSlotInfo(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > MAX_SAVE_SLOTS)
        {
            return new SaveSlotInfo { hasSave = false };
        }

        SaveData metadata = GetSaveMetadata(slotIndex);
        
        if (metadata == null)
        {
            return new SaveSlotInfo { hasSave = false };
        }

        // Calculate completion percent (placeholder - implement based on your game logic)
        int completionPercent = 0; // TODO: Calculate based on game progress

        return new SaveSlotInfo
        {
            hasSave = true,
            playTime = metadata.playTime,
            lastLevel = metadata.lastLevel ?? "Unknown",
            lastSaveDate = metadata.saveDate ?? "Unknown",
            completionPercent = completionPercent
        };
    }

    /// <summary>
    /// Check if a slot exists (alias for HasSaveData for clarity)
    /// </summary>
    public bool SlotExists(int slotIndex)
    {
        return HasSaveData(slotIndex);
    }

    /// <summary>
    /// Create a new save slot with initial data (for new game)
    /// </summary>
    public bool CreateNewSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slotIndex}. Must be between 1 and {MAX_SAVE_SLOTS}");
            return false;
        }

        // Create initial save data
        currentSaveData = new SaveData();
        currentSaveData.playerData = new PlayerSaveData();
        currentSaveData.worldData = new WorldSaveData();
        currentSaveData.playTime = 0f;
        currentSaveData.lastLevel = "Level_01"; // Default starting level
        currentSaveData.currentLevel = "Level_01"; // Per spec: current level
        currentSaveData.isNewGame = true; // Per spec: new game flag
        currentSaveData.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        CurrentSaveData = currentSaveData;

        // Save to file
        string filePath = GetSaveFilePath(slotIndex);
        string json = JsonUtility.ToJson(currentSaveData, true);

        try
        {
            File.WriteAllText(filePath, json);
            ActiveSlot = slotIndex;
            
            // Reset session playtime when creating a new game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSessionPlayTime();
            }
            
            Debug.Log($"New save slot {slotIndex} created");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create new save slot: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Set the active slot (without loading it yet)
    /// </summary>
    public void SetActiveSlot(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Invalid save slot: {slotIndex}. Must be between 1 and {MAX_SAVE_SLOTS}");
            return;
        }

        ActiveSlot = slotIndex;

        // If slot exists, load metadata (but not full game state)
        if (SlotExists(slotIndex))
        {
            currentSaveData = GetSaveMetadata(slotIndex);
            CurrentSaveData = currentSaveData;
        }
        else
        {
            currentSaveData = null;
            CurrentSaveData = null;
        }
    }

    /// <summary>
    /// Select a slot (alias for SetActiveSlot - per spec requirement)
    /// </summary>
    public void SelectSlot(int slotIndex)
    {
        SetActiveSlot(slotIndex);
    }

    /// <summary>
    /// Check if save data has been loaded
    /// </summary>
    public bool HasLoadedData => CurrentSaveData != null && ActiveSlot > 0;
    
    /// <summary>
    /// Restore all registered saveable objects (called by LevelManager after level loads)
    /// Per spec: LevelManager asks SaveManager to restore data
    /// </summary>
    public void RestoreAllSaveables()
    {
        if (CurrentSaveData == null)
        {
            Debug.LogWarning("No save data to restore");
            return;
        }
        
        // Restore state to all saveable objects
        foreach (var saveable in saveableObjects)
        {
            string id = saveable.GetUniqueID();
            
            if (CurrentSaveData.worldData.HasObject(id))
            {
                var saveData = CurrentSaveData.worldData.savedObjects.Find(x => x.id == id);
                if (saveData != null)
                {
                    try
                    {
                        Type objectType = Type.GetType(saveData.dataType);
                        if (objectType != null)
                        {
                            object state = JsonUtility.FromJson(saveData.dataJson, objectType);
                            saveable.RestoreState(state);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to restore state for {id}: {e.Message}");
                    }
                }
            }
            else
            {
                // Collectible not in save data - means it was never collected, so it should be active
                // This is fine, just leave it as is
            }
        }
        
        // Restore lost soul (after player spawns)
        RestoreLostSoul();
        
        // Restore currency and inventory (after player spawns)
        RestoreCurrencyAndInventory();
        
        Debug.Log($"Restored state for {saveableObjects.Count} saveable objects");
    }

    /// <summary>
    /// Restore currency and inventory from save data
    /// Called after player spawns
    /// </summary>
    private void RestoreCurrencyAndInventory()
    {
        if (CurrentSaveData == null) return;

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try LevelManager
            if (LevelManager.Instance != null)
            {
                player = LevelManager.Instance.GetPlayerInstance();
            }
        }

        if (player == null) return;

        // Restore currency
        PlayerCurrency playerCurrency = player.GetComponent<PlayerCurrency>();
        if (playerCurrency != null)
        {
            // Use coins from SaveData (preferred) or fallback to PlayerSaveData
            int coinsToRestore = CurrentSaveData.coins;
            if (coinsToRestore == 0 && CurrentSaveData.playerData != null)
            {
                coinsToRestore = CurrentSaveData.playerData.coins;
            }
            playerCurrency.RestoreCoins(coinsToRestore);
        }

        // Restore inventory
        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.Clear(); // Clear any existing items
            if (CurrentSaveData.inventory != null)
            {
                foreach (var item in CurrentSaveData.inventory)
                {
                    playerInventory.AddItem(item.itemID, item.quantity);
                }
            }
        }
    }

    /// <summary>
    /// Restore lost soul from save data
    /// Called after player spawns
    /// </summary>
    private void RestoreLostSoul()
    {
        if (CurrentSaveData == null) return;

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try LevelManager
            if (LevelManager.Instance != null)
            {
                player = LevelManager.Instance.GetPlayerInstance();
            }
        }

        if (player != null)
        {
            PlayerDeath playerDeath = player.GetComponent<PlayerDeath>();
            if (playerDeath != null)
            {
                playerDeath.RestoreLostSoul(CurrentSaveData.lostSoul);
            }
        }
    }

    /// <summary>
    /// Get the target level to load (for GameBootstrap)
    /// Per spec: Returns currentLevel if available, otherwise lastLevel, otherwise Level_01
    /// </summary>
    public string GetTargetLevel()
    {
        if (CurrentSaveData == null)
        {
            return "Level_01"; // Default starting level
        }

        // Per spec: prefer currentLevel over lastLevel
        if (!string.IsNullOrEmpty(CurrentSaveData.currentLevel))
        {
            return CurrentSaveData.currentLevel;
        }

        return string.IsNullOrEmpty(CurrentSaveData.lastLevel) ? "Level_01" : CurrentSaveData.lastLevel;
    }
    
    /// <summary>
    /// Check if current game is a new game (per spec requirement)
    /// </summary>
    public bool IsNewGame()
    {
        if (CurrentSaveData == null)
        {
            return true; // No save data = new game
        }
        
        return CurrentSaveData.isNewGame;
    }
}
