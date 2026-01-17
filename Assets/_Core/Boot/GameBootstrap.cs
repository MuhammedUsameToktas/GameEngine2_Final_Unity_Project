using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Bootstrap - Handles loading the game after slot selection
/// Per spec: Reads active slot, decides which level to load, loads it
/// This scene acts as a transition point for loading screens, fade transitions, etc.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private void Start()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager not found! Cannot load game.");
            SceneManager.LoadScene("Scenes/Menus/MainMenu");
            return;
        }

        // Read active slot (per spec requirement)
        int slot = SaveManager.Instance.ActiveSlot;

        if (slot <= 0)
        {
            Debug.LogError("No active save slot! Returning to main menu.");
            SceneManager.LoadScene("Scenes/Menus/MainMenu");
            return;
        }

        // Decide new game vs load game (per spec requirement)
        bool isNewGame;
        if (SaveManager.Instance.SlotExists(slot))
        {
            // Load existing game
            isNewGame = false;
            if (!SaveManager.Instance.LoadGame(slot))
            {
                Debug.LogError($"Failed to load game from slot {slot}. Returning to main menu.");
                SceneManager.LoadScene("Scenes/Menus/MainMenu");
                return;
            }
            Debug.Log($"Loading existing game from slot {slot}");
        }
        else
        {
            // Create new game
            isNewGame = true;
            if (!SaveManager.Instance.CreateNewSlot(slot))
            {
                Debug.LogError($"Failed to create new game in slot {slot}. Returning to main menu.");
                SceneManager.LoadScene("Scenes/Menus/MainMenu");
                return;
            }
            Debug.Log($"Creating new game in slot {slot}");
        }

        // Set isNewGame flag in save data
        if (SaveManager.Instance.CurrentSaveData != null)
        {
            SaveManager.Instance.CurrentSaveData.isNewGame = isNewGame;
        }

        // Decide which level to load (per spec requirement)
        string levelToLoad;
        if (isNewGame)
        {
            // New Game → Level_01
            levelToLoad = "Scenes/Levels/Level_01";
            if (SaveManager.Instance.CurrentSaveData != null)
            {
                SaveManager.Instance.CurrentSaveData.currentLevel = "Level_01";
            }
        }
        else
        {
            // Load Game → last saved level
            levelToLoad = SaveManager.Instance.GetTargetLevel();
            if (string.IsNullOrEmpty(levelToLoad))
            {
                levelToLoad = "Scenes/Levels/Level_01"; // Default fallback
            }
            
            // Ensure proper scene path format
            if (!levelToLoad.StartsWith("Scenes/"))
            {
                levelToLoad = $"Scenes/Levels/{levelToLoad}";
            }
            
            // Update current level in save data
            if (SaveManager.Instance.CurrentSaveData != null)
            {
                string levelName = levelToLoad.Replace("Scenes/Levels/", "");
                SaveManager.Instance.CurrentSaveData.currentLevel = levelName;
            }
        }

        // Load it (per spec requirement)
        Debug.Log($"Loading level: {levelToLoad}");
        SceneManager.LoadScene(levelToLoad);
        
        // Fire level loaded event (GameManager will handle setting state to Playing)
        EventBus.OnLevelLoaded?.Invoke(levelToLoad);
        
        // TODO: Implement async loading with loading screen
        // StartCoroutine(LoadAsync(levelToLoad));
    }
}
