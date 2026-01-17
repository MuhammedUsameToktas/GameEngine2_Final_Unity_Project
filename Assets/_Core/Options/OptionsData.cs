using UnityEngine;

/// <summary>
/// OptionsData - Pure serializable data class for game settings
/// This class contains NO Unity references and can be saved/loaded anytime
/// </summary>
[System.Serializable]
public class OptionsData
{
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Graphics Settings")]
    public int resolutionIndex = 0;
    public bool fullscreen = true;
    public int qualityLevel = 2; // 0 = Low, 1 = Medium, 2 = High, etc.

    [Header("Gameplay Settings")]
    [Range(0.1f, 5f)]
    public float cameraSensitivity = 1f;

    /// <summary>
    /// Creates a copy of this OptionsData
    /// Useful for reverting changes
    /// </summary>
    public OptionsData Clone()
    {
        return new OptionsData
        {
            masterVolume = this.masterVolume,
            musicVolume = this.musicVolume,
            sfxVolume = this.sfxVolume,
            resolutionIndex = this.resolutionIndex,
            fullscreen = this.fullscreen,
            qualityLevel = this.qualityLevel,
            cameraSensitivity = this.cameraSensitivity
        };
    }
}
