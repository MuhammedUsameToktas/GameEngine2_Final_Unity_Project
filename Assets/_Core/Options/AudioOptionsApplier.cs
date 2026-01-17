using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioOptionsApplier - Applies audio settings from OptionsManager to AudioMixer
/// Attach this to a GameObject in PersistentScene (or any scene that has an AudioMixer)
/// </summary>
public class AudioOptionsApplier : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    [SerializeField] private AudioMixer mixer;

    [Header("Mixer Parameter Names")]
    [Tooltip("Name of the exposed Master volume parameter in AudioMixer")]
    [SerializeField] private string masterVolumeParam = "MasterVol";
    
    [Tooltip("Name of the exposed Music volume parameter in AudioMixer")]
    [SerializeField] private string musicVolumeParam = "MusicVol";
    
    [Tooltip("Name of the exposed SFX volume parameter in AudioMixer")]
    [SerializeField] private string sfxVolumeParam = "SFXVol";

    private void Start()
    {
        // Apply settings when scene starts
        // Use a small delay to ensure OptionsManager is ready
        StartCoroutine(ApplyDelayed());
    }

    private System.Collections.IEnumerator ApplyDelayed()
    {
        // Wait a frame to ensure OptionsManager is initialized
        yield return null;
        
        // Wait for OptionsManager if it's not ready yet
        int maxWait = 10;
        int waitCount = 0;
        while (OptionsManager.Instance == null && waitCount < maxWait)
        {
            yield return new WaitForSeconds(0.1f);
            waitCount++;
        }

        if (OptionsManager.Instance == null)
        {
            Debug.LogError("AudioOptionsApplier: OptionsManager.Instance is still null after waiting!");
            yield break;
        }

        Debug.Log("AudioOptionsApplier: Applying audio settings on Start");
        Apply();
    }

    /// <summary>
    /// Apply audio settings from OptionsManager to AudioMixer
    /// Call this after changing options and clicking "Apply"
    /// </summary>
    public void Apply()
    {
        if (OptionsManager.Instance == null)
        {
            Debug.LogError("AudioOptionsApplier: OptionsManager.Instance is null!");
            return;
        }

        if (mixer == null)
        {
            Debug.LogWarning("AudioOptionsApplier: AudioMixer is not assigned!");
            return;
        }

        var data = OptionsManager.Instance.Data;

        // Convert linear 0-1 volume to decibels (logarithmic scale)
        // Human hearing is logarithmic, so this is industry standard
        // Formula: dB = 20 * log10(volume)
        // When volume = 0, we set to -80dB (effectively silent)
        
        float masterDB = data.masterVolume > 0.0001f 
            ? Mathf.Log10(data.masterVolume) * 20f 
            : -80f;
        
        float musicDB = data.musicVolume > 0.0001f 
            ? Mathf.Log10(data.musicVolume) * 20f 
            : -80f;
        
        float sfxDB = data.sfxVolume > 0.0001f 
            ? Mathf.Log10(data.sfxVolume) * 20f 
            : -80f;

        // Apply to mixer
        bool masterSet = mixer.SetFloat(masterVolumeParam, masterDB);
        bool musicSet = mixer.SetFloat(musicVolumeParam, musicDB);
        bool sfxSet = mixer.SetFloat(sfxVolumeParam, sfxDB);

        Debug.Log($"AudioOptionsApplier: Applied audio settings");
        Debug.Log($"  - Master: {data.masterVolume:F2} ({masterDB:F1} dB) - Set: {masterSet}");
        Debug.Log($"  - Music: {data.musicVolume:F2} ({musicDB:F1} dB) - Set: {musicSet}");
        Debug.Log($"  - SFX: {data.sfxVolume:F2} ({sfxDB:F1} dB) - Set: {sfxSet}");
        
        // Verify the values were actually set
        float verifyMaster, verifyMusic, verifySFX;
        if (mixer.GetFloat(masterVolumeParam, out verifyMaster))
        {
            Debug.Log($"  - Verified MasterVol: {verifyMaster:F1} dB");
        }
        if (mixer.GetFloat(musicVolumeParam, out verifyMusic))
        {
            Debug.Log($"  - Verified MusicVol: {verifyMusic:F1} dB");
        }
        if (mixer.GetFloat(sfxVolumeParam, out verifySFX))
        {
            Debug.Log($"  - Verified SFXVol: {verifySFX:F1} dB");
        }
    }

    /// <summary>
    /// Validate that mixer parameters exist
    /// Call this in editor to check setup
    /// </summary>
    [ContextMenu("Validate Mixer Parameters")]
    private void ValidateMixerParameters()
    {
        if (mixer == null)
        {
            Debug.LogError("AudioMixer is not assigned!");
            return;
        }

        bool allValid = true;

        if (!HasParameter(masterVolumeParam))
        {
            Debug.LogError($"AudioMixer parameter '{masterVolumeParam}' not found!");
            allValid = false;
        }

        if (!HasParameter(musicVolumeParam))
        {
            Debug.LogError($"AudioMixer parameter '{musicVolumeParam}' not found!");
            allValid = false;
        }

        if (!HasParameter(sfxVolumeParam))
        {
            Debug.LogError($"AudioMixer parameter '{sfxVolumeParam}' not found!");
            allValid = false;
        }

        if (allValid)
        {
            Debug.Log("All AudioMixer parameters are valid!");
        }
    }

    private bool HasParameter(string paramName)
    {
        if (mixer == null) return false;
        
        // Unity's AudioMixer.GetFloat returns true if parameter exists, false otherwise
        // This is the standard way to check if an exposed parameter exists
        float testValue;
        return mixer.GetFloat(paramName, out testValue);
    }
}
