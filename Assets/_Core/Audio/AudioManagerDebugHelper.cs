using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioManagerDebugHelper - Debug tool to test audio playback
/// Add this to any GameObject and use the context menu to test audio
/// </summary>
public class AudioManagerDebugHelper : MonoBehaviour
{
    [ContextMenu("Test Play Music")]
    private void TestPlayMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager.Instance is null!");
            return;
        }

        Debug.Log("=== Testing Music Playback ===");
        AudioManager.Instance.PlayMusic("MainMenuTheme", fadeIn: false);
    }

    [ContextMenu("Test Play SFX Jump")]
    private void TestPlaySFXJump()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager.Instance is null!");
            return;
        }

        Debug.Log("=== Testing SFX Playback (Jump) ===");
        AudioManager.Instance.PlaySFX("Jump");
    }

    [ContextMenu("Check AudioMixer Volumes")]
    private void CheckAudioMixerVolumes()
    {
        AudioOptionsApplier applier = FindObjectOfType<AudioOptionsApplier>();
        if (applier == null)
        {
            Debug.LogError("AudioOptionsApplier not found!");
            return;
        }

        Debug.Log("=== Checking AudioMixer Volumes ===");
        
        // Use reflection to get the mixer
        var mixerField = typeof(AudioOptionsApplier).GetField("mixer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (mixerField == null)
        {
            Debug.LogError("Cannot access mixer field!");
            return;
        }

        AudioMixer mixer = mixerField.GetValue(applier) as AudioMixer;
        if (mixer == null)
        {
            Debug.LogError("AudioMixer is null!");
            return;
        }

        float masterVol, musicVol, sfxVol;
        if (mixer.GetFloat("MasterVol", out masterVol))
        {
            Debug.Log($"MasterVol: {masterVol:F1} dB {(masterVol <= -79f ? "(SILENT!)" : "")}");
        }
        else
        {
            Debug.LogError("MasterVol parameter not found!");
        }

        if (mixer.GetFloat("MusicVol", out musicVol))
        {
            Debug.Log($"MusicVol: {musicVol:F1} dB {(musicVol <= -79f ? "(SILENT!)" : "")}");
        }
        else
        {
            Debug.LogError("MusicVol parameter not found!");
        }

        if (mixer.GetFloat("SFXVol", out sfxVol))
        {
            Debug.Log($"SFXVol: {sfxVol:F1} dB {(sfxVol <= -79f ? "(SILENT!)" : "")}");
        }
        else
        {
            Debug.LogError("SFXVol parameter not found!");
        }

        // Check OptionsManager
        if (OptionsManager.Instance != null)
        {
            var data = OptionsManager.Instance.Data;
            Debug.Log($"OptionsManager Data:");
            Debug.Log($"  - Master Volume: {data.masterVolume:F2}");
            Debug.Log($"  - Music Volume: {data.musicVolume:F2}");
            Debug.Log($"  - SFX Volume: {data.sfxVolume:F2}");
        }
    }

    [ContextMenu("Force Apply Audio Settings")]
    private void ForceApplyAudioSettings()
    {
        AudioOptionsApplier applier = FindObjectOfType<AudioOptionsApplier>();
        if (applier == null)
        {
            Debug.LogError("AudioOptionsApplier not found!");
            return;
        }

        Debug.Log("=== Force Applying Audio Settings ===");
        applier.Apply();
    }
}
