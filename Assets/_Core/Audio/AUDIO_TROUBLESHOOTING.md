# 🔧 Audio Troubleshooting Guide

If you're not hearing any sound in your game, follow these steps:

## ✅ Quick Checklist

1. **Check Console for Errors**
   - Open Unity Console (Window → General → Console)
   - Look for red errors or yellow warnings
   - Common messages:
     - "SFX 'Jump' not found in library!" → Clip name mismatch
     - "Music Mixer Group not assigned!" → Missing mixer group assignment
     - "No available SFX sources!" → Too many sounds playing at once

2. **Verify AudioManager Setup**
   - Open PersistentScene
   - Select AudioManager GameObject
   - Check Inspector:
     - ✅ Audio Mixer is assigned
     - ✅ Music Mixer Group is assigned
     - ✅ SFX Mixer Group is assigned
     - ✅ Music Library has clips
     - ✅ SFX Library has clips

3. **Check Clip Names**
   - Clip names in library MUST match exactly what you call in code
   - Case-sensitive!
   - Examples:
     - Code: `PlaySFX("Jump")` → Clip name must be exactly "Jump"
     - Code: `PlayMusic("MainMenuTheme")` → Clip name must be exactly "MainMenuTheme"

4. **Check Volume Settings**
   - Open Options menu
   - Check volume sliders are not at 0
   - Master Volume should be > 0
   - Music Volume should be > 0 (for music)
   - SFX Volume should be > 0 (for sound effects)

5. **Check AudioMixer Parameters**
   - Open MainAudioMixer asset
   - Verify these parameters are exposed:
     - `MasterVol`
     - `MusicVol`
     - `SFXVol`
   - Check parameter names match exactly (case-sensitive)

## 🔍 Detailed Troubleshooting

### Problem: No Sound at All

**Possible Causes:**
1. AudioManager.Instance is null
2. AudioMixer not assigned
3. Mixer Groups not assigned
4. Volume sliders at 0

**Solution:**
1. Check Console for "AudioManager.Instance is null" warnings
2. Verify AudioManager GameObject exists in PersistentScene
3. Assign AudioMixer and Mixer Groups in Inspector
4. Check Options menu volume sliders

### Problem: SFX Not Playing

**Possible Causes:**
1. Clip name mismatch
2. SFX Mixer Group not assigned
3. SFX volume at 0
4. Clip not in SFX Library array

**Solution:**
1. Check Console for "SFX 'X' not found in library!" warning
2. Verify clip name matches exactly (case-sensitive)
3. Check SFX Library array has the clip assigned
4. Verify SFX Mixer Group is assigned in AudioManager
5. Check SFX Volume slider in Options menu

**Debug:**
- Console will show: "Available: Jump, Punch, Coin, Run" if clip not found
- This tells you what clips ARE in the library

### Problem: Music Not Playing

**Possible Causes:**
1. Clip name mismatch
2. Music Mixer Group not assigned
3. Music volume at 0
4. Clip not in Music Library array

**Solution:**
1. Check Console for "Music 'X' not found in library!" warning
2. Verify clip name matches exactly (case-sensitive)
3. Check Music Library array has the clip assigned
4. Verify Music Mixer Group is assigned in AudioManager
5. Check Music Volume slider in Options menu

### Problem: Sounds Play But Are Silent

**Possible Causes:**
1. AudioMixer volume parameters set to -80dB (silent)
2. Volume sliders at 0
3. Mixer Groups not connected properly

**Solution:**
1. Check Options menu volume sliders
2. Open AudioMixer window
3. Check exposed parameters (MasterVol, MusicVol, SFXVol) are not at -80dB
4. Verify AudioOptionsApplier is applying settings correctly

### Problem: Clip Name Mismatch

**How to Fix:**
1. Select your audio clip in Project window
2. Check the name in Inspector (top of Inspector panel)
3. Make sure it matches EXACTLY what you call in code:
   - Code: `PlaySFX("Jump")` → Clip name: "Jump" ✅
   - Code: `PlaySFX("Jump")` → Clip name: "jump" ❌ (wrong case)
   - Code: `PlaySFX("Jump")` → Clip name: "Jump " ❌ (extra space)

**Common Mistakes:**
- Extra spaces: "Jump " vs "Jump"
- Wrong case: "jump" vs "Jump"
- Different names: "PlayerJump" vs "Jump"

## 🧪 Testing Steps

1. **Test AudioManager Initialization**
   - Play game
   - Check Console for: "AudioManager: Built audio libraries. Music: X, SFX: Y"
   - If you see this, AudioManager is working

2. **Test SFX**
   - Press Jump button
   - Check Console for: "AudioManager: Playing SFX 'Jump'"
   - If you see this but no sound, check volume/mixer

3. **Test Music**
   - Open Main Menu
   - Check Console for: "AudioManager: Playing music 'MainMenuTheme'"
   - If you see this but no sound, check volume/mixer

4. **Test Volume**
   - Open Options menu
   - Move volume sliders
   - Check Console for: "AudioOptionsApplier: Applied audio settings"
   - Sound should change immediately

## 📝 Common Issues & Solutions

### Issue: "SFX 'Jump' not found in library!"

**Solution:**
1. Check clip name is exactly "Jump" (no spaces, correct case)
2. Verify clip is in SFX Library array in AudioManager Inspector
3. Check Console shows "Available: ..." to see what clips ARE found

### Issue: "Music Mixer Group not assigned!"

**Solution:**
1. Open PersistentScene
2. Select AudioManager GameObject
3. In Inspector, find "Music Mixer Group" field
4. Open AudioMixer window (double-click MainAudioMixer)
5. Drag "Music" group from AudioMixer into the field

### Issue: "No available SFX sources!"

**Solution:**
1. Too many sounds playing at once
2. Increase "Max SFX Sources" in AudioManager Inspector (default: 10)
3. Or wait for sounds to finish

### Issue: Sounds play but are very quiet

**Solution:**
1. Check volume sliders in Options menu
2. Check AudioMixer parameters are not at -80dB
3. Verify AudioOptionsApplier is applying settings

## 🎯 Quick Fixes

**No sound at all:**
1. Check AudioManager exists in PersistentScene
2. Assign AudioMixer and Mixer Groups
3. Check volume sliders

**SFX not working:**
1. Check clip names match exactly
2. Verify clips in SFX Library array
3. Check SFX Mixer Group assigned

**Music not working:**
1. Check clip names match exactly
2. Verify clips in Music Library array
3. Check Music Mixer Group assigned

**Volume not working:**
1. Check Options menu volume sliders
2. Verify AudioOptionsApplier is in scene
3. Check AudioMixer parameters are exposed

## 📞 Still Not Working?

1. **Check Console** - Look for any errors or warnings
2. **Check Inspector** - Verify all fields are assigned
3. **Check Names** - Clip names must match exactly
4. **Check Volume** - Sliders must be > 0
5. **Check Mixer** - Groups must be assigned

The debug logs will tell you exactly what's wrong!
