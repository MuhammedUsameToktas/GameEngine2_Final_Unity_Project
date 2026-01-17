# 🎵 Audio System Setup Guide

Complete guide for adding music and SFX to your game using the AudioManager system.

---

## 📋 Table of Contents
1. [Quick Start](#quick-start)
2. [Step-by-Step Setup](#step-by-step-setup)
3. [Adding Music](#adding-music)
4. [Adding SFX](#adding-sfx)
5. [Using AudioManager in Code](#using-audiomanager-in-code)
6. [Best Practices](#best-practices)

---

## 🚀 Quick Start

1. **AudioManager** is already set up in PersistentScene
2. **Assign AudioMixer and Mixer Groups** in AudioManager Inspector
3. **Add AudioClips** to Music Library and SFX Library arrays
4. **Call `AudioManager.Instance.PlayMusic()` or `PlaySFX()`** from anywhere

---

## 🛠 Step-by-Step Setup

### STEP 1: Setup AudioManager in PersistentScene

1. **Open PersistentScene**
   - Navigate to `Scenes/Persistent/PersistentScene.unity`

2. **Find AudioManager GameObject**
   - Should already exist (created earlier)
   - If not, create empty GameObject → Name: `AudioManager`

3. **Add AudioManager Component**
   - Select `AudioManager` GameObject
   - Add Component → `AudioManager`

### STEP 2: Assign AudioMixer and Mixer Groups

1. **Assign AudioMixer**
   - In AudioManager Inspector, find "Audio Mixer" field
   - Drag your `MainAudioMixer` asset into this field
   - Path: `Assets/_Core/Audio/MainAudioMixer.mixer`

2. **Assign Music Mixer Group**
   - Find "Music Mixer Group" field
   - Open your AudioMixer (double-click `MainAudioMixer.mixer`)
   - In AudioMixer window, find the "Music" group
   - Drag "Music" group from AudioMixer window into "Music Mixer Group" field

3. **Assign SFX Mixer Group**
   - Find "SFX Mixer Group" field
   - Drag "SFX" group from AudioMixer window into "SFX Mixer Group" field

**✅ Result**: AudioManager is now connected to your AudioMixer.

---

## 🎵 Adding Music

### Method 1: Add Music to Library Array (Recommended)

1. **Import Your Music Files**
   - Drag music files (`.mp3`, `.wav`, `.ogg`) into Unity Project
   - Recommended location: `Assets/Audio/Music/`

2. **Configure Music Import Settings**
   - Select music file in Project window
   - In Inspector:
     - **Load Type**: `Streaming` (for long tracks) or `Compressed In Memory` (for shorter loops)
     - **Compression Format**: `Vorbis` (good quality/size ratio)
     - **Quality**: `70-90` (higher = better quality, larger file)
     - **Load In Background**: ✅ Checked (prevents stuttering)

3. **Add to Music Library**
   - Select `AudioManager` GameObject
   - In Inspector, find "Music Library" array
   - Set "Size" to number of music tracks
   - Drag each music clip into array slots

4. **Play Music in Code**
   ```csharp
   // Play by name (uses clip.name)
   AudioManager.Instance.PlayMusic("MainMenuTheme");
   
   // With fade in (default)
   AudioManager.Instance.PlayMusic("BattleMusic", fadeIn: true);
   ```

### Method 2: Play Music Directly (No Library)

```csharp
// Get AudioClip reference
AudioClip myMusic = Resources.Load<AudioClip>("Music/MyTrack");

// Play directly
AudioManager.Instance.PlayMusic(myMusic);
```

### Music Tips

- **Looping**: Music automatically loops (set in AudioManager)
- **Fade Transitions**: Music fades between tracks (1 second default)
- **One Track at a Time**: Only one music track plays at once
- **Naming**: Use descriptive names like "MainMenuTheme", "BattleMusic", "VictoryFanfare"

---

## 🔊 Adding SFX

### Method 1: Add SFX to Library Array (Recommended)

1. **Import Your SFX Files**
   - Drag sound effect files into Unity Project
   - Recommended location: `Assets/Audio/SFX/`

2. **Configure SFX Import Settings**
   - Select SFX file in Project window
   - In Inspector:
     - **Load Type**: `Decompress On Load` (for short sounds)
     - **Compression Format**: `PCM` (no compression, instant playback)
     - **Load In Background**: ❌ Unchecked (load immediately)

3. **Add to SFX Library**
   - Select `AudioManager` GameObject
   - In Inspector, find "SFX Library" array
   - Set "Size" to number of sound effects
   - Drag each SFX clip into array slots

4. **Play SFX in Code**
   ```csharp
   // Play by name
   AudioManager.Instance.PlaySFX("Jump");
   AudioManager.Instance.PlaySFX("Hit", volume: 0.8f, pitch: 1.2f);
   
   // Play at 3D position (spatial audio)
   AudioManager.Instance.PlaySFXAtPosition("Explosion", transform.position);
   ```

### Method 2: Play SFX Directly (No Library)

```csharp
// Get AudioClip reference
AudioClip mySFX = Resources.Load<AudioClip>("SFX/MySound");

// Play directly
AudioManager.Instance.PlaySFX(mySFX);
```

### SFX Tips

- **Multiple Simultaneous**: Up to 10 SFX can play at once (configurable)
- **Volume Control**: Each SFX can have custom volume (0-1)
- **Pitch Variation**: Vary pitch (0.5-2.0) to avoid repetition
- **3D Audio**: Use `PlaySFXAtPosition()` for spatial sounds (explosions, footsteps)

---

## 💻 Using AudioManager in Code

### Basic Usage

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // Play music when scene starts
        AudioManager.Instance.PlayMusic("Level1Music");
    }

    void OnPlayerJump()
    {
        // Play jump sound
        AudioManager.Instance.PlaySFX("Jump");
    }

    void OnEnemyHit(Vector3 hitPosition)
    {
        // Play hit sound at position (3D audio)
        AudioManager.Instance.PlaySFXAtPosition("Hit", hitPosition);
    }
}
```

### Music Control

```csharp
// Play music
AudioManager.Instance.PlayMusic("BattleMusic");

// Stop music (with fade out)
AudioManager.Instance.StopMusic(fadeOut: true);

// Pause music
AudioManager.Instance.PauseMusic();

// Resume music
AudioManager.Instance.ResumeMusic();

// Check if playing
if (AudioManager.Instance.IsMusicPlaying())
{
    Debug.Log("Music is playing!");
}
```

### SFX Control

```csharp
// Play SFX with custom volume and pitch
AudioManager.Instance.PlaySFX("Coin", volume: 0.7f, pitch: 1.1f);

// Play 3D SFX (spatial audio)
AudioManager.Instance.PlaySFXAtPosition(
    "Explosion", 
    transform.position,
    volume: 1f,
    pitch: 1f,
    minDistance: 5f,  // Start fading at 5 units
    maxDistance: 50f   // Silent at 50 units
);

// Stop all SFX
AudioManager.Instance.StopAllSFX();
```

### Example: Player Script

```csharp
public class PlayerController : MonoBehaviour
{
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            // Play jump sound
            AudioManager.Instance.PlaySFX("Jump");
            Jump();
        }
    }

    void OnTakeDamage()
    {
        // Play hurt sound
        AudioManager.Instance.PlaySFX("Hurt");
    }

    void OnCollectCoin()
    {
        // Play coin sound with slight pitch variation
        float pitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySFX("Coin", pitch: pitch);
    }
}
```

### Example: Level Manager

```csharp
public class LevelManager : MonoBehaviour
{
    void Start()
    {
        // Play level music
        AudioManager.Instance.PlayMusic("Level1Music");
    }

    void OnBossFightStart()
    {
        // Switch to boss music
        AudioManager.Instance.PlayMusic("BossMusic");
    }

    void OnLevelComplete()
    {
        // Play victory music
        AudioManager.Instance.PlayMusic("VictoryFanfare");
    }
}
```

---

## 🎯 Best Practices

### Music

1. **File Format**
   - Use `.ogg` or `.mp3` for music (smaller file size)
   - Use `.wav` only for short loops that need perfect quality

2. **Import Settings**
   - Long tracks: `Streaming` load type
   - Short loops: `Compressed In Memory`
   - Quality: 70-90 (balance quality vs size)

3. **Naming Convention**
   - Use descriptive names: `MainMenuTheme`, `BattleMusic`, `BossFight`
   - Avoid spaces: Use `CamelCase` or `snake_case`

4. **Transitions**
   - Use fade transitions between tracks (automatic)
   - Don't play same track twice in a row (AudioManager prevents this)

### SFX

1. **File Format**
   - Use `.wav` for short SFX (instant playback, no compression)
   - Use `.ogg` for longer SFX (smaller file size)

2. **Import Settings**
   - Short sounds: `Decompress On Load` + `PCM`
   - Longer sounds: `Compressed In Memory` + `Vorbis`

3. **Volume Levels**
   - Master mix: 0.8-1.0
   - Important sounds: 0.7-0.9
   - Ambient sounds: 0.3-0.5
   - UI sounds: 0.5-0.7

4. **Pitch Variation**
   - Vary pitch slightly (0.9-1.1) to avoid repetition
   - Example: `pitch: Random.Range(0.95f, 1.05f)`

5. **3D Audio**
   - Use `PlaySFXAtPosition()` for:
     - Explosions
     - Footsteps
     - Environmental sounds
     - Enemy sounds
   - Use regular `PlaySFX()` for:
     - UI sounds
     - Player actions (jump, attack)
     - Music stings

### Organization

1. **Folder Structure**
   ```
   Assets/
   ├── Audio/
   │   ├── Music/
   │   │   ├── MainMenuTheme.ogg
   │   │   ├── BattleMusic.ogg
   │   │   └── VictoryFanfare.ogg
   │   └── SFX/
   │       ├── Jump.wav
   │       ├── Hit.wav
   │       └── Coin.wav
   ```

2. **Naming Convention**
   - Music: `[Context][Type]` → `MainMenuTheme`, `BattleMusic`
   - SFX: `[Action][Object]` → `Jump`, `CoinCollect`, `EnemyHit`

---

## 🔧 Troubleshooting

### Problem: Music doesn't play

**Solutions:**
1. Check AudioMixer is assigned in AudioManager
2. Check Music Mixer Group is assigned
3. Check music clip is in Music Library array
4. Check clip name matches exactly (case-sensitive)
5. Check Console for warnings

### Problem: SFX doesn't play

**Solutions:**
1. Check SFX Mixer Group is assigned
2. Check SFX clip is in SFX Library array
3. Check `maxSFXSources` isn't too low (default: 10)
4. Check Console for "No available SFX sources" warning

### Problem: Volume sliders don't affect audio

**Solutions:**
1. Check AudioMixer groups are assigned correctly
2. Check AudioMixer has exposed parameters (MasterVol, MusicVol, SFXVol)
3. Check AudioOptionsApplier is in scene and has AudioMixer assigned
4. Verify AudioSources output to correct Mixer Groups (automatic in AudioManager)

### Problem: Music cuts off abruptly

**Solutions:**
1. Increase `musicFadeTime` in AudioManager (default: 1 second)
2. Check music clip isn't set to "Streaming" if it's short
3. Check music source isn't being destroyed

---

## ✅ Quick Checklist

- [ ] AudioManager component added to AudioManager GameObject
- [ ] AudioMixer assigned in AudioManager
- [ ] Music Mixer Group assigned
- [ ] SFX Mixer Group assigned
- [ ] Music clips added to Music Library array
- [ ] SFX clips added to SFX Library array
- [ ] Music plays when calling `PlayMusic()`
- [ ] SFX plays when calling `PlaySFX()`
- [ ] Volume sliders affect audio
- [ ] Music fades between tracks
- [ ] Multiple SFX can play simultaneously

---

## 🎉 You're Done!

Your audio system is now fully functional. You can:
- ✅ Play music from anywhere: `AudioManager.Instance.PlayMusic("TrackName")`
- ✅ Play SFX from anywhere: `AudioManager.Instance.PlaySFX("SoundName")`
- ✅ Control volume via Options menu
- ✅ Use 3D spatial audio for environmental sounds
- ✅ Fade between music tracks automatically

**Remember**: Add clips to the Library arrays in Inspector, then reference them by name in code!
