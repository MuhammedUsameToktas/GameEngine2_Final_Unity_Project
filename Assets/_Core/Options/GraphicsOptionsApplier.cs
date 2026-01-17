using UnityEngine;

/// <summary>
/// GraphicsOptionsApplier - Applies graphics settings from OptionsManager
/// Attach this to a GameObject in PersistentScene
/// </summary>
public class GraphicsOptionsApplier : MonoBehaviour
{
    private void Start()
    {
        // Apply settings when scene starts
        Apply();
    }

    /// <summary>
    /// Apply graphics settings from OptionsManager
    /// Call this after changing options and clicking "Apply"
    /// </summary>
    public void Apply()
    {
        if (OptionsManager.Instance == null)
        {
            Debug.LogError("GraphicsOptionsApplier: OptionsManager.Instance is null!");
            return;
        }

        var data = OptionsManager.Instance.Data;

        // Validate resolution index
        if (data.resolutionIndex < 0 || data.resolutionIndex >= Screen.resolutions.Length)
        {
            Debug.LogWarning($"GraphicsOptionsApplier: Invalid resolution index {data.resolutionIndex}. Using current resolution.");
            // Find closest matching resolution
            data.resolutionIndex = FindClosestResolutionIndex();
        }

        // Apply resolution and fullscreen
        Resolution targetResolution = Screen.resolutions[data.resolutionIndex];
        Screen.SetResolution(targetResolution.width, targetResolution.height, data.fullscreen, targetResolution.refreshRate);
        
        // Apply quality level
        if (data.qualityLevel >= 0 && data.qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(data.qualityLevel);
        }
        else
        {
            Debug.LogWarning($"GraphicsOptionsApplier: Invalid quality level {data.qualityLevel}. Using default.");
            data.qualityLevel = QualitySettings.GetQualityLevel();
        }

        Debug.Log($"GraphicsOptionsApplier: Applied graphics settings (Resolution: {targetResolution.width}x{targetResolution.height}, Fullscreen: {data.fullscreen}, Quality: {QualitySettings.names[data.qualityLevel]})");
    }

    /// <summary>
    /// Find the closest matching resolution index to current screen resolution
    /// </summary>
    private int FindClosestResolutionIndex()
    {
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        int closestIndex = 0;
        int smallestDiff = int.MaxValue;

        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            int diff = Mathf.Abs(Screen.resolutions[i].width - currentWidth) + 
                      Mathf.Abs(Screen.resolutions[i].height - currentHeight);
            
            if (diff < smallestDiff)
            {
                smallestDiff = diff;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// Get array of resolution display names for UI dropdown
    /// </summary>
    public static string[] GetResolutionNames()
    {
        Resolution[] resolutions = Screen.resolutions;
        string[] names = new string[resolutions.Length];

        for (int i = 0; i < resolutions.Length; i++)
        {
            names[i] = $"{resolutions[i].width} x {resolutions[i].height} ({resolutions[i].refreshRate} Hz)";
        }

        return names;
    }

    /// <summary>
    /// Get array of quality level names for UI dropdown
    /// </summary>
    public static string[] GetQualityNames()
    {
        return QualitySettings.names;
    }
}
