using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AudioManager - Centralized audio system for music and SFX
/// Handles music playback (one track at a time) and SFX (multiple simultaneous)
/// Connects to AudioMixer groups for volume control
/// 
/// SETUP:
/// 1. Attach to AudioManager GameObject in PersistentScene
/// 2. Assign AudioMixer and Mixer Groups in Inspector
/// 3. Create AudioClip references for music and SFX
/// 4. Call AudioManager.Instance.PlayMusic() or PlaySFX() from anywhere
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource; // Single music source
    [SerializeField] private float musicFadeTime = 1f; // Time to fade between tracks

    [Header("SFX Settings")]
    [SerializeField] private int maxSFXSources = 10; // Max simultaneous SFX
    [SerializeField] private Transform sfxSourceParent; // Parent for SFX AudioSources (optional)

    [Header("Music Library")]
    [Tooltip("Add your music tracks here. Access by name: PlayMusic(\"TrackName\")")]
    [SerializeField] private AudioClip[] musicLibrary;

    [Header("SFX Library")]
    [Tooltip("Add your sound effects here. Access by name: PlaySFX(\"SoundName\")")]
    [SerializeField] private AudioClip[] sfxLibrary;

    // Runtime data
    private Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();
    private Queue<AudioSource> availableSFXSources = new Queue<AudioSource>();
    private List<AudioSource> activeSFXSources = new List<AudioSource>();
    private Coroutine currentMusicFade;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup music source if not assigned
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 1f; // Set volume to 1 (mixer will control actual volume)
        }

        // Connect music source to mixer group
        if (musicMixerGroup != null)
        {
            musicSource.outputAudioMixerGroup = musicMixerGroup;
        }
        else
        {
            Debug.LogWarning("AudioManager: Music Mixer Group not assigned! Music may not play correctly.");
        }

        // Setup SFX sources pool
        SetupSFXPool();

        // Build audio clip dictionaries
        BuildAudioDictionaries();
        
        // Debug: Check initial volume settings
        Debug.Log("AudioManager: Initialization complete");
        Debug.Log($"  - Music Source: {(musicSource != null ? "OK" : "NULL")}");
        Debug.Log($"  - Music Mixer Group: {(musicMixerGroup != null ? musicMixerGroup.name : "NULL")}");
        Debug.Log($"  - SFX Mixer Group: {(sfxMixerGroup != null ? sfxMixerGroup.name : "NULL")}");
        Debug.Log($"  - Audio Mixer: {(audioMixer != null ? "OK" : "NULL")}");
        
        // Check if AudioOptionsApplier exists and has applied settings
        AudioOptionsApplier applier = FindObjectOfType<AudioOptionsApplier>();
        if (applier == null)
        {
            Debug.LogWarning("AudioManager: AudioOptionsApplier not found! Volume settings may not be applied.");
        }
    }

    /// <summary>
    /// Setup pool of AudioSources for SFX
    /// </summary>
    private void SetupSFXPool()
    {
        GameObject sfxParent = sfxSourceParent != null ? sfxSourceParent.gameObject : gameObject;
        
        for (int i = 0; i < maxSFXSources; i++)
        {
            GameObject sfxObj = new GameObject($"SFXSource_{i}");
            sfxObj.transform.SetParent(sfxParent.transform);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            
            // Connect to SFX mixer group
            if (sfxMixerGroup != null)
            {
                source.outputAudioMixerGroup = sfxMixerGroup;
            }
            else
            {
                Debug.LogWarning("AudioManager: SFX Mixer Group not assigned! SFX may not play correctly.");
            }
            
            availableSFXSources.Enqueue(source);
        }
    }

    /// <summary>
    /// Build dictionaries from audio clip arrays for fast lookup
    /// </summary>
    private void BuildAudioDictionaries()
    {
        // Build music dictionary
        musicDict.Clear();
        foreach (var clip in musicLibrary)
        {
            if (clip != null && !musicDict.ContainsKey(clip.name))
            {
                musicDict[clip.name] = clip;
                Debug.Log($"AudioManager: Added music '{clip.name}' to library");
            }
        }

        // Build SFX dictionary
        sfxDict.Clear();
        foreach (var clip in sfxLibrary)
        {
            if (clip != null && !sfxDict.ContainsKey(clip.name))
            {
                sfxDict[clip.name] = clip;
                Debug.Log($"AudioManager: Added SFX '{clip.name}' to library");
            }
        }

        Debug.Log($"AudioManager: Built audio libraries. Music: {musicDict.Count}, SFX: {sfxDict.Count}");
    }

    #region Music Methods

    /// <summary>
    /// Play music track by name
    /// </summary>
    public void PlayMusic(string musicName, bool fadeIn = true)
    {
        if (!musicDict.ContainsKey(musicName))
        {
            Debug.LogWarning($"AudioManager: Music '{musicName}' not found in library! Available: {string.Join(", ", musicDict.Keys)}");
            return;
        }

        Debug.Log($"AudioManager: Playing music '{musicName}'");
        PlayMusic(musicDict[musicName], fadeIn);
    }

    /// <summary>
    /// Play music track by AudioClip
    /// </summary>
    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Cannot play null music clip!");
            return;
        }

        // If same clip is already playing, do nothing
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        // Stop current fade if any
        if (currentMusicFade != null)
        {
            StopCoroutine(currentMusicFade);
        }

        // Fade out current music, then fade in new music
        if (fadeIn && musicSource.isPlaying)
        {
            currentMusicFade = StartCoroutine(FadeMusic(clip));
        }
        else
        {
            // Immediate switch
            musicSource.clip = clip;
            musicSource.volume = 1f; // Ensure volume is set
            musicSource.Play();
            
            // Debug info
            Debug.Log($"AudioManager: Music '{clip.name}' started playing.");
            Debug.Log($"  - IsPlaying: {musicSource.isPlaying}");
            Debug.Log($"  - Volume: {musicSource.volume}");
            Debug.Log($"  - Output Group: {(musicSource.outputAudioMixerGroup != null ? musicSource.outputAudioMixerGroup.name : "NULL!")}");
            Debug.Log($"  - Clip Length: {clip.length}");
            Debug.Log($"  - Clip Load Type: {clip.loadType}");
            
            // Check if actually playing after a frame
            StartCoroutine(CheckMusicPlaying(clip.name));
        }
    }

    /// <summary>
    /// Check if music is actually playing after a frame
    /// </summary>
    private IEnumerator CheckMusicPlaying(string clipName)
    {
        yield return new WaitForSeconds(0.1f);
        if (musicSource != null)
        {
            Debug.Log($"AudioManager: Music check after 0.1s - IsPlaying: {musicSource.isPlaying}, Time: {musicSource.time}");
            if (!musicSource.isPlaying)
            {
                Debug.LogError($"AudioManager: Music '{clipName}' is NOT playing! Check AudioMixer volume settings.");
            }
        }
    }

    /// <summary>
    /// Stop music (with optional fade out)
    /// </summary>
    public void StopMusic(bool fadeOut = true)
    {
        if (currentMusicFade != null)
        {
            StopCoroutine(currentMusicFade);
        }

        if (fadeOut && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Pause music
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Resume music
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
        }
    }

    /// <summary>
    /// Fade between music tracks
    /// </summary>
    private IEnumerator FadeMusic(AudioClip newClip)
    {
        // Fade out current music
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeOutMusic());
        }

        // Switch clip
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new music
        yield return StartCoroutine(FadeInMusic());
    }

    /// <summary>
    /// Fade out current music
    /// </summary>
    private IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < musicFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume; // Restore volume
    }

    /// <summary>
    /// Fade in current music
    /// </summary>
    private IEnumerator FadeInMusic()
    {
        float targetVolume = musicSource.volume;
        musicSource.volume = 0f;
        float elapsed = 0f;

        while (elapsed < musicFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / musicFadeTime);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    #endregion

    #region SFX Methods

    /// <summary>
    /// Play SFX by name
    /// </summary>
    public void PlaySFX(string sfxName, float volume = 1f, float pitch = 1f)
    {
        if (!sfxDict.ContainsKey(sfxName))
        {
            Debug.LogWarning($"AudioManager: SFX '{sfxName}' not found in library! Available: {string.Join(", ", sfxDict.Keys)}");
            return;
        }

        Debug.Log($"AudioManager: Playing SFX '{sfxName}'");
        PlaySFX(sfxDict[sfxName], volume, pitch);
    }

    /// <summary>
    /// Play SFX by AudioClip
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Cannot play null SFX clip!");
            return;
        }

        // Get available source from pool
        AudioSource source = GetAvailableSFXSource();
        if (source == null)
        {
            Debug.LogWarning("AudioManager: No available SFX sources! Increase maxSFXSources.");
            return;
        }

        // Configure and play
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        Debug.Log($"AudioManager: SFX '{clip.name}' started playing.");
        Debug.Log($"  - IsPlaying: {source.isPlaying}");
        Debug.Log($"  - Volume: {source.volume}");
        Debug.Log($"  - Pitch: {source.pitch}");
        Debug.Log($"  - Output Group: {(source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : "NULL!")}");
        Debug.Log($"  - Clip Length: {clip.length}");
        
        if (!source.isPlaying)
        {
            Debug.LogError($"AudioManager: SFX '{clip.name}' is NOT playing! Check AudioMixer volume settings.");
        }

        // Return to pool when done
        StartCoroutine(ReturnSFXSourceToPool(source, clip.length / pitch));
    }

    /// <summary>
    /// Play SFX at specific position (3D spatial audio)
    /// </summary>
    public void PlaySFXAtPosition(string sfxName, Vector3 position, float volume = 1f, float pitch = 1f, float minDistance = 1f, float maxDistance = 500f)
    {
        if (!sfxDict.ContainsKey(sfxName))
        {
            Debug.LogWarning($"AudioManager: SFX '{sfxName}' not found in library!");
            return;
        }

        PlaySFXAtPosition(sfxDict[sfxName], position, volume, pitch, minDistance, maxDistance);
    }

    /// <summary>
    /// Play SFX at specific position (3D spatial audio)
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float minDistance = 1f, float maxDistance = 500f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Cannot play null SFX clip!");
            return;
        }

        AudioSource source = GetAvailableSFXSource();
        if (source == null)
        {
            Debug.LogWarning("AudioManager: No available SFX sources!");
            return;
        }

        // Configure for 3D audio
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 1f; // Full 3D
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.transform.position = position;
        source.Play();

        StartCoroutine(ReturnSFXSourceToPool(source, clip.length / pitch));
    }

    /// <summary>
    /// Stop all SFX
    /// </summary>
    public void StopAllSFX()
    {
        foreach (var source in activeSFXSources)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
                ReturnSFXSourceToPool(source);
            }
        }
        activeSFXSources.Clear();
    }

    /// <summary>
    /// Get available SFX source from pool
    /// </summary>
    private AudioSource GetAvailableSFXSource()
    {
        // Try to get from queue
        if (availableSFXSources.Count > 0)
        {
            AudioSource source = availableSFXSources.Dequeue();
            activeSFXSources.Add(source);
            return source;
        }

        // If no available sources, try to reuse oldest active source
        if (activeSFXSources.Count > 0)
        {
            AudioSource oldest = activeSFXSources[0];
            activeSFXSources.RemoveAt(0);
            if (oldest != null)
            {
                oldest.Stop();
                activeSFXSources.Add(oldest);
                return oldest;
            }
        }

        return null;
    }

    /// <summary>
    /// Return SFX source to pool after clip finishes
    /// </summary>
    private IEnumerator ReturnSFXSourceToPool(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnSFXSourceToPool(source);
    }

    /// <summary>
    /// Return SFX source to pool immediately
    /// </summary>
    private void ReturnSFXSourceToPool(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
        source.spatialBlend = 0f; // Reset to 2D
        source.transform.position = transform.position; // Reset position

        if (activeSFXSources.Contains(source))
        {
            activeSFXSources.Remove(source);
        }

        if (!availableSFXSources.Contains(source))
        {
            availableSFXSources.Enqueue(source);
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Check if music is playing
    /// </summary>
    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }

    /// <summary>
    /// Get current music clip name
    /// </summary>
    public string GetCurrentMusicName()
    {
        return musicSource != null && musicSource.clip != null ? musicSource.clip.name : "";
    }

    /// <summary>
    /// Add music clip to library at runtime
    /// </summary>
    public void AddMusicToLibrary(AudioClip clip)
    {
        if (clip != null && !musicDict.ContainsKey(clip.name))
        {
            musicDict[clip.name] = clip;
        }
    }

    /// <summary>
    /// Add SFX clip to library at runtime
    /// </summary>
    public void AddSFXToLibrary(AudioClip clip)
    {
        if (clip != null && !sfxDict.ContainsKey(clip.name))
        {
            sfxDict[clip.name] = clip;
        }
    }

    #endregion

    #region Editor Helpers

    /// <summary>
    /// Refresh audio dictionaries (call when clips are added in inspector)
    /// </summary>
    [ContextMenu("Refresh Audio Libraries")]
    private void RefreshAudioLibraries()
    {
        BuildAudioDictionaries();
        Debug.Log($"AudioManager: Refreshed libraries. Music: {musicDict.Count}, SFX: {sfxDict.Count}");
    }

    #endregion
}
