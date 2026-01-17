# 🎮 Options System - Complete Unity Setup Guide

## 📋 Table of Contents
1. [Overview](#overview)
2. [Step-by-Step Setup](#step-by-step-setup)
3. [AudioMixer Setup](#audiomixer-setup)
4. [UI Setup](#ui-setup)
5. [Testing](#testing)
6. [Troubleshooting](#troubleshooting)

---

## 🧠 Overview

This options system follows a **4-layer architecture**:

1. **OptionsData** → Pure serializable data (no Unity references)
2. **OptionsManager** → Singleton that persists data (lives in PersistentScene)
3. **OptionsAppliers** → Apply settings to game systems (Audio, Graphics)
4. **OptionsUI** → UI that reads/writes to OptionsManager

### ✅ Key Principles
- ✅ Options persist between sessions (PlayerPrefs)
- ✅ Works in Main Menu AND Gameplay
- ✅ UI does NOT store values (only displays/edits)
- ✅ Systems subscribe to changes, don't poll

---

## 🛠 Step-by-Step Setup

### STEP 1: Add OptionsManager to PersistentScene

1. **Open PersistentScene**
   - Navigate to `Scenes/Persistent/PersistentScene.unity`

2. **Create OptionsManager GameObject**
   - Right-click in Hierarchy → Create Empty
   - Name it: `OptionsManager`
   - **Important**: Make it a child of the root (same level as GameManager)

3. **Add OptionsManager Component**
   - Select `OptionsManager` GameObject
   - Click "Add Component" in Inspector
   - Search for: `OptionsManager`
   - Add it

4. **Verify Setup**
   - OptionsManager should have `DontDestroyOnLoad` behavior (handled by script)
   - It will automatically load options on `Awake()`

**✅ Result**: OptionsManager now exists and loads options on game start.

---

### STEP 2: Setup AudioMixer (If You Don't Have One)

#### Option A: Create New AudioMixer

1. **Create AudioMixer Asset**
   - Right-click in Project window → `Create → Audio → Audio Mixer`
   - Name it: `MainAudioMixer`
   - Save in: `Assets/Settings/` (or wherever you keep settings)

2. **Open AudioMixer Window**
   - Double-click `MainAudioMixer` asset
   - Window → Audio → Audio Mixer (if not already open)

3. **Create Mixer Groups**
   - In AudioMixer window, you should see "Master" group
   - Right-click "Master" → `Add child group`
   - Name it: `Music`
   - Right-click "Master" → `Add child group`
   - Name it: `SFX`

4. **Expose Volume Parameters**
   - Select "Master" group
   - In Inspector, find "Volume" parameter
   - Right-click "Volume" → `Expose`
   - Rename exposed parameter to: `MasterVol`
   - Repeat for "Music" group → Expose as `MusicVol`
   - Repeat for "SFX" group → Expose as `SFXVol`

5. **Save AudioMixer**
   - Click "Save" in AudioMixer window

#### Option B: Use Existing AudioMixer

If you already have an AudioMixer:
- Note the names of your exposed volume parameters
- You'll use these in Step 3

---

### STEP 3: Add AudioOptionsApplier

1. **Find or Create AudioManager GameObject**
   - In PersistentScene, look for `AudioManager` GameObject
   - If it doesn't exist, create it:
     - Right-click in Hierarchy → Create Empty
     - Name it: `AudioManager`

2. **Add AudioOptionsApplier Component**
   - Select `AudioManager` GameObject
   - Add Component → `AudioOptionsApplier`

3. **Assign AudioMixer**
   - In Inspector, find "Mixer" field
   - Drag your `MainAudioMixer` asset into this field

4. **Verify Parameter Names**
   - Check "Master Volume Param" = `MasterVol` (or your parameter name)
   - Check "Music Volume Param" = `MusicVol` (or your parameter name)
   - Check "SFX Volume Param" = `SFXVol` (or your parameter name)

5. **Test Validation** (Optional)
   - Right-click `AudioOptionsApplier` component in Inspector
   - Click "Validate Mixer Parameters"
   - Should see "All AudioMixer parameters are valid!" in Console

**✅ Result**: Audio settings will now apply automatically when options change.

---

### STEP 4: Add GraphicsOptionsApplier

1. **Create GraphicsManager GameObject** (or use existing)
   - In PersistentScene, create empty GameObject
   - Name it: `GraphicsManager`

2. **Add GraphicsOptionsApplier Component**
   - Select `GraphicsManager` GameObject
   - Add Component → `GraphicsOptionsApplier`

**✅ Result**: Graphics settings will now apply automatically when options change.

---

### STEP 5: Connect Audio Sources to Mixer Groups

Your AudioSources need to output to the correct Mixer Groups:

1. **For Each AudioSource in Your Game:**
   - Select the AudioSource GameObject
   - In Inspector, find "Output" dropdown
   - Set to appropriate group:
     - Music → `Music` group
     - Sound Effects → `SFX` group
     - Master → `Master` group

2. **For AudioSources Created at Runtime:**
   ```csharp
   AudioSource source = gameObject.AddComponent<AudioSource>();
   source.outputAudioMixerGroup = musicMixerGroup; // Assign in code
   ```

**✅ Result**: Volume sliders will now control actual game audio.

---

### STEP 6: Create Options UI (Main Menu)

#### 6A: Create Options Panel

1. **In MainMenu Scene:**
   - Open `Scenes/Menus/MainMenu.unity`

2. **Create Options Panel**
   - Right-click Canvas → UI → Panel
   - Name it: `OptionsPanel`
   - Set it inactive initially (uncheck checkbox in Inspector)

3. **Add OptionsUIController**
   - Select `OptionsPanel`
   - Add Component → `OptionsUIController`

#### 6B: Create Audio UI Elements

1. **Create Audio Section**
   - Right-click `OptionsPanel` → UI → Text - TextMeshPro (for label)
   - Name it: `AudioLabel`
   - Text: "Audio Settings"

2. **Master Volume Slider**
   - Right-click `OptionsPanel` → UI → Slider
   - Name it: `MasterVolumeSlider`
   - Add label: Right-click slider → UI → Text - TextMeshPro
   - Label text: "Master Volume"
   - Position slider below AudioLabel

3. **Music Volume Slider**
   - Duplicate `MasterVolumeSlider` (Ctrl+D)
   - Name it: `MusicVolumeSlider`
   - Update label: "Music Volume"
   - Position below MasterVolumeSlider

4. **SFX Volume Slider**
   - Duplicate again
   - Name it: `SFXVolumeSlider`
   - Update label: "SFX Volume"
   - Position below MusicVolumeSlider

5. **Assign to OptionsUIController**
   - Select `OptionsPanel`
   - In Inspector, find `OptionsUIController` component
   - Drag sliders into respective fields:
     - Master Volume Slider → `Master Volume Slider`
     - Music Volume Slider → `Music Volume Slider`
     - SFX Volume Slider → `SFX Volume Slider`

#### 6C: Create Graphics UI Elements

1. **Resolution Dropdown**
   - Right-click `OptionsPanel` → UI → Dropdown - TextMeshPro
   - Name it: `ResolutionDropdown`
   - Add label: "Resolution"
   - Assign to `OptionsUIController` → `Resolution Dropdown`

2. **Fullscreen Toggle**
   - Right-click `OptionsPanel` → UI → Toggle
   - Name it: `FullscreenToggle`
   - Add label: "Fullscreen"
   - Assign to `OptionsUIController` → `Fullscreen Toggle`

3. **Quality Dropdown**
   - Right-click `OptionsPanel` → UI → Dropdown - TextMeshPro
   - Name it: `QualityDropdown`
   - Add label: "Quality"
   - Assign to `OptionsUIController` → `Quality Dropdown`

#### 6D: Create Gameplay UI Elements

1. **Camera Sensitivity Slider**
   - Right-click `OptionsPanel` → UI → Slider
   - Name it: `CameraSensitivitySlider`
   - Add label: "Camera Sensitivity"
   - Min: 0.1, Max: 5
   - Assign to `OptionsUIController` → `Camera Sensitivity Slider`

#### 6E: Create Buttons

1. **Apply Button**
   - Right-click `OptionsPanel` → UI → Button - TextMeshPro
   - Name it: `ApplyButton`
   - Text: "Apply"
   - Assign to `OptionsUIController` → `Apply Button`

2. **Cancel Button**
   - Duplicate Apply Button
   - Name it: `CancelButton`
   - Text: "Cancel"
   - Assign to `OptionsUIController` → `Cancel Button`

3. **Reset Button**
   - Duplicate again
   - Name it: `ResetButton`
   - Text: "Reset to Defaults"
   - Assign to `OptionsUIController` → `Reset Button`

#### 6F: Create "Options" Button in Main Menu

1. **Add Options Button to Main Menu**
   - Find your main menu buttons
   - Create button: "Options"
   - Add OnClick event:
     - Drag `OptionsPanel` GameObject
     - Function: `GameObject.SetActive`
     - Check the checkbox (to set active)

2. **Add Close Button to Options Panel**
   - In OptionsPanel, create "Close" or "Back" button
   - OnClick: Set `OptionsPanel` inactive

**✅ Result**: Options menu accessible from main menu.

---

### STEP 7: Add Options to Pause Menu

**Important**: You can reuse the same `OptionsUIController` logic!

1. **In Your Pause Menu:**
   - Open your pause menu scene/panel
   - Create `OptionsPanel` (same as Step 6)
   - Add `OptionsUIController` component
   - Wire up UI elements (same as Step 6)

2. **Or Use Prefab:**
   - Make `OptionsPanel` a prefab
   - Instantiate in both Main Menu and Pause Menu

**✅ Result**: Options accessible from both Main Menu and Pause Menu.

---

## 🎧 AudioMixer Setup (Detailed)

### Understanding AudioMixer Parameters

AudioMixer uses **decibels (dB)** for volume, but humans think in **linear 0-1** scale.

**Conversion Formula:**
```
dB = 20 * log10(volume)
```

- Volume 1.0 = 0 dB (full volume)
- Volume 0.5 = -6 dB (half volume)
- Volume 0.1 = -20 dB (quiet)
- Volume 0.0 = -80 dB (silent)

The `AudioOptionsApplier` handles this conversion automatically.

### Exposing Parameters (Step-by-Step)

1. **Open AudioMixer Window**
   - Double-click your AudioMixer asset

2. **Select a Group** (e.g., "Master")

3. **In Inspector:**
   - Find "Volume" parameter
   - Right-click it → `Expose`
   - It appears in "Exposed Parameters" list

4. **Rename Exposed Parameter:**
   - Click the exposed parameter
   - Rename to: `MasterVol` (or your preferred name)

5. **Repeat for Music and SFX groups**

### Common Issues

**❌ Problem**: "Parameter not found" error
- **Solution**: Make sure parameter name matches exactly (case-sensitive)
- Check in AudioMixer window → Exposed Parameters

**❌ Problem**: Volume doesn't change
- **Solution**: Make sure AudioSources output to correct Mixer Groups
- Check AudioSource "Output" field in Inspector

---

## 🖥 UI Setup (Detailed)

### Layout Recommendations

```
OptionsPanel
├── Title (Text)
├── AudioSection
│   ├── MasterVolumeSlider
│   ├── MusicVolumeSlider
│   └── SFXVolumeSlider
├── GraphicsSection
│   ├── ResolutionDropdown
│   ├── FullscreenToggle
│   └── QualityDropdown
├── GameplaySection
│   └── CameraSensitivitySlider
└── Buttons
    ├── ApplyButton
    ├── CancelButton
    └── ResetButton
```

### Slider Setup

For each volume slider:
- **Min Value**: 0
- **Max Value**: 1
- **Whole Numbers**: Unchecked
- **Value**: 1 (default)

### Dropdown Setup

Resolution and Quality dropdowns are populated automatically by `OptionsUIController.Start()`.

### Toggle Setup

Fullscreen toggle:
- **Is On**: Checked (default fullscreen)

---

## 🧪 Testing

### Test 1: Options Persist

1. **Start Game**
2. **Open Options Menu**
3. **Change Master Volume to 0.5**
4. **Click Apply**
5. **Quit Game**
6. **Restart Game**
7. **Open Options Menu**
8. **Verify**: Master Volume should be 0.5

**✅ Pass**: Options persist between sessions.

### Test 2: Options Work in Gameplay

1. **Start Game**
2. **Open Options Menu** (Main Menu)
3. **Change SFX Volume to 0.3**
4. **Click Apply**
5. **Start Gameplay**
6. **Play Sound Effect**
7. **Verify**: Sound is quieter

**✅ Pass**: Options apply to gameplay.

### Test 3: Options Work in Pause Menu

1. **Start Gameplay**
2. **Open Pause Menu**
3. **Open Options**
4. **Change Resolution**
5. **Click Apply**
6. **Verify**: Resolution changes immediately

**✅ Pass**: Options work from pause menu.

### Test 4: Cancel Reverts Changes

1. **Open Options Menu**
2. **Change Master Volume to 0.2**
3. **Click Cancel** (don't click Apply)
4. **Reopen Options Menu**
5. **Verify**: Master Volume is back to original value

**✅ Pass**: Cancel works correctly.

---

## 🔧 Troubleshooting

### Problem: OptionsManager.Instance is null

**Cause**: OptionsManager not in PersistentScene, or PersistentScene not loaded first.

**Solution**:
1. Check PersistentScene has OptionsManager GameObject
2. Verify PersistentScene is loaded first in Build Settings
3. Check OptionsManager has `DontDestroyOnLoad` (automatic)

---

### Problem: Audio doesn't change when moving slider

**Cause**: AudioOptionsApplier not applying, or AudioMixer not assigned.

**Solution**:
1. Check AudioOptionsApplier exists in scene
2. Verify AudioMixer is assigned in Inspector
3. Check parameter names match exactly
4. Verify AudioSources output to correct Mixer Groups

---

### Problem: Resolution dropdown is empty

**Cause**: Dropdown not assigned, or OptionsUIController not running Start().

**Solution**:
1. Assign resolutionDropdown in Inspector
2. Check OptionsUIController is enabled
3. Verify GraphicsOptionsApplier exists (needed for GetResolutionNames())

---

### Problem: Options don't save

**Cause**: SaveOptions() not being called, or PlayerPrefs issue.

**Solution**:
1. Make sure "Apply" button calls `OnApplyPressed()`
2. Check Console for "Options saved successfully" message
3. Verify PlayerPrefs is not being cleared elsewhere

---

### Problem: Graphics changes don't apply

**Cause**: GraphicsOptionsApplier not in scene, or invalid resolution index.

**Solution**:
1. Add GraphicsOptionsApplier to PersistentScene
2. Check Console for warnings about invalid resolution index
3. Verify Screen.resolutions array is not empty

---

## 📝 Quick Reference

### Accessing Options from Code

```csharp
// Read current options
float masterVol = OptionsManager.Instance.Data.masterVolume;

// Change options
OptionsManager.Instance.Data.masterVolume = 0.5f;

// Save options
OptionsManager.Instance.SaveOptions();

// Apply audio settings
FindObjectOfType<AudioOptionsApplier>()?.Apply();

// Apply graphics settings
FindObjectOfType<GraphicsOptionsApplier>()?.Apply();
```

### Adding New Options

1. **Add to OptionsData.cs:**
   ```csharp
   public float newOption = 1f;
   ```

2. **Add UI element** (Slider, Toggle, etc.)

3. **Wire up in OptionsUIController:**
   ```csharp
   private void OnNewOptionChanged(float value)
   {
       OptionsManager.Instance.Data.newOption = value;
   }
   ```

4. **Create Applier** (if needed):
   ```csharp
   public class NewOptionApplier : MonoBehaviour
   {
       void Start() { Apply(); }
       public void Apply()
       {
           var data = OptionsManager.Instance.Data;
           // Apply newOption to your system
       }
   }
   ```

---

## ✅ Final Checklist

- [ ] OptionsManager in PersistentScene
- [ ] AudioOptionsApplier in PersistentScene with AudioMixer assigned
- [ ] GraphicsOptionsApplier in PersistentScene
- [ ] AudioMixer has exposed parameters (MasterVol, MusicVol, SFXVol)
- [ ] AudioSources output to correct Mixer Groups
- [ ] Options UI created in Main Menu
- [ ] Options UI created in Pause Menu (optional)
- [ ] All UI elements assigned in OptionsUIController
- [ ] Apply button wired up
- [ ] Tested: Options persist between sessions
- [ ] Tested: Options work in gameplay
- [ ] Tested: Cancel reverts changes

---

## 🎉 You're Done!

Your options system is now fully functional. It will:
- ✅ Persist between sessions
- ✅ Work in Main Menu and Gameplay
- ✅ Apply settings automatically
- ✅ Never need touching again (if done right!)

**Remember**: Options are GAME SETTINGS DATA, not UI logic. UI is just a viewer/editor of that data.
