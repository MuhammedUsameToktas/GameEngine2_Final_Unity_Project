using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OptionsUIController - Example UI controller for Options menu
/// This can be used in both Main Menu and Pause Menu
/// Attach this to your Options Panel GameObject
/// </summary>
public class OptionsUIController : MonoBehaviour
{
    [Header("Audio UI Elements")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Graphics UI Elements")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Gameplay UI Elements")]
    [SerializeField] private Slider cameraSensitivitySlider;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button resetButton;

    private OptionsData cachedData; // For reverting changes

    private void OnEnable()
    {
        // Load current options into UI when panel opens
        LoadOptionsToUI();
        
        // Cache current data for cancel functionality
        if (OptionsManager.Instance != null)
        {
            cachedData = OptionsManager.Instance.GetDataCopy();
        }
    }

    private void Start()
    {
        // Setup UI elements
        SetupAudioUI();
        SetupGraphicsUI();
        SetupGameplayUI();
        SetupButtons();

        // Load current options
        LoadOptionsToUI();
    }

    #region Setup Methods

    private void SetupAudioUI()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void SetupGraphicsUI()
    {
        // Setup resolution dropdown
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            string[] resolutionNames = GraphicsOptionsApplier.GetResolutionNames();
            resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(resolutionNames));
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        // Setup fullscreen toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // Setup quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            string[] qualityNames = GraphicsOptionsApplier.GetQualityNames();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(qualityNames));
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }
    }

    private void SetupGameplayUI()
    {
        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.minValue = 0.1f;
            cameraSensitivitySlider.maxValue = 5f;
            cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);
        }
    }

    private void SetupButtons()
    {
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyPressed);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelPressed);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetPressed);
        }
    }

    #endregion

    #region UI Event Handlers

    private void OnMasterVolumeChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.masterVolume = value;
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.musicVolume = value;
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.sfxVolume = value;
        }
    }

    private void OnResolutionChanged(int index)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.resolutionIndex = index;
        }
    }

    private void OnFullscreenChanged(bool value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.fullscreen = value;
        }
    }

    private void OnQualityChanged(int index)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.qualityLevel = index;
        }
    }

    private void OnCameraSensitivityChanged(float value)
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.Data.cameraSensitivity = value;
        }
    }

    #endregion

    #region Button Actions

    /// <summary>
    /// Apply button - Save options and apply to game systems
    /// </summary>
    private void OnApplyPressed()
    {
        if (OptionsManager.Instance == null)
        {
            Debug.LogError("OptionsManager.Instance is null!");
            return;
        }

        // Save to PlayerPrefs
        OptionsManager.Instance.SaveOptions();

        // Apply audio settings
        AudioOptionsApplier audioApplier = FindObjectOfType<AudioOptionsApplier>();
        if (audioApplier != null)
        {
            audioApplier.Apply();
        }
        else
        {
            Debug.LogWarning("AudioOptionsApplier not found in scene!");
        }

        // Apply graphics settings
        GraphicsOptionsApplier graphicsApplier = FindObjectOfType<GraphicsOptionsApplier>();
        if (graphicsApplier != null)
        {
            graphicsApplier.Apply();
        }
        else
        {
            Debug.LogWarning("GraphicsOptionsApplier not found in scene!");
        }

        // Update cached data (changes are now permanent)
        cachedData = OptionsManager.Instance.GetDataCopy();

        Debug.Log("Options applied successfully!");
    }

    /// <summary>
    /// Cancel button - Revert UI to cached values
    /// </summary>
    private void OnCancelPressed()
    {
        if (cachedData != null && OptionsManager.Instance != null)
        {
            // Restore cached data
            OptionsManager.Instance.RestoreData(cachedData);
            LoadOptionsToUI();
        }
    }

    /// <summary>
    /// Reset button - Reset to default values
    /// </summary>
    private void OnResetPressed()
    {
        if (OptionsManager.Instance == null) return;

        // Reset to defaults
        OptionsManager.Instance.ResetToDefaults();
        
        // Reload UI
        LoadOptionsToUI();
        
        // Update cache
        cachedData = OptionsManager.Instance.GetDataCopy();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Load current options from OptionsManager into UI elements
    /// </summary>
    private void LoadOptionsToUI()
    {
        if (OptionsManager.Instance == null)
        {
            Debug.LogWarning("OptionsManager.Instance is null! Cannot load options to UI.");
            return;
        }

        var data = OptionsManager.Instance.Data;

        // Load audio
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = data.masterVolume;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = data.musicVolume;
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = data.sfxVolume;

        // Load graphics
        if (resolutionDropdown != null)
        {
            if (data.resolutionIndex >= 0 && data.resolutionIndex < resolutionDropdown.options.Count)
            {
                resolutionDropdown.value = data.resolutionIndex;
            }
        }

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = data.fullscreen;

        if (qualityDropdown != null)
        {
            if (data.qualityLevel >= 0 && data.qualityLevel < qualityDropdown.options.Count)
            {
                qualityDropdown.value = data.qualityLevel;
            }
        }

        // Load gameplay
        if (cameraSensitivitySlider != null)
            cameraSensitivitySlider.value = data.cameraSensitivity;
    }

    #endregion
}
