using UnityEngine;

/// <summary>
/// OptionsManager - Global singleton that manages game options
/// Lives in PersistentScene, persists between sessions
/// Single source of truth for all game settings
/// </summary>
public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }

    /// <summary>
    /// Current options data - read/write this from anywhere
    /// </summary>
    public OptionsData Data { get; private set; }

    private const string OPTIONS_KEY = "OPTIONS_DATA";

    private void Awake()
    {
        // Singleton pattern - only one instance allowed
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load options immediately on creation
        LoadOptions();
    }

    /// <summary>
    /// Save options to PlayerPrefs
    /// Call this when user clicks "Apply" or "Save"
    /// </summary>
    public void SaveOptions()
    {
        if (Data == null)
        {
            Debug.LogWarning("OptionsManager: Cannot save - Data is null!");
            return;
        }

        string json = JsonUtility.ToJson(Data, true);
        PlayerPrefs.SetString(OPTIONS_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log("OptionsManager: Options saved successfully");
    }

    /// <summary>
    /// Load options from PlayerPrefs
    /// Creates default options if none exist
    /// </summary>
    private void LoadOptions()
    {
        if (PlayerPrefs.HasKey(OPTIONS_KEY))
        {
            string json = PlayerPrefs.GetString(OPTIONS_KEY);
            try
            {
                Data = JsonUtility.FromJson<OptionsData>(json);
                Debug.Log("OptionsManager: Options loaded from PlayerPrefs");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OptionsManager: Failed to parse saved options: {e.Message}");
                Data = new OptionsData();
                SaveOptions();
            }
        }
        else
        {
            // First time - create default options
            Data = new OptionsData();
            SaveOptions();
            Debug.Log("OptionsManager: Created default options");
        }
    }

    /// <summary>
    /// Reset options to defaults
    /// Useful for "Reset to Defaults" button
    /// </summary>
    public void ResetToDefaults()
    {
        Data = new OptionsData();
        SaveOptions();
        Debug.Log("OptionsManager: Options reset to defaults");
    }

    /// <summary>
    /// Get a copy of current data (for reverting changes)
    /// </summary>
    public OptionsData GetDataCopy()
    {
        return Data?.Clone();
    }

    /// <summary>
    /// Restore data from a cached copy (for cancel functionality)
    /// </summary>
    public void RestoreData(OptionsData dataToRestore)
    {
        if (dataToRestore == null)
        {
            Debug.LogWarning("OptionsManager: Cannot restore - dataToRestore is null!");
            return;
        }

        Data = dataToRestore.Clone();
        Debug.Log("OptionsManager: Data restored from cache");
    }
}
