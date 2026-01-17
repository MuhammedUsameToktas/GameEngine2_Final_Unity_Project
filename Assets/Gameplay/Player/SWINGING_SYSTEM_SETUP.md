# Spider-Man Style Swinging System Setup Guide

This guide will help you set up and use the swinging mechanic in your game.

## Overview

The swinging system allows players to swing from designated points in the air, similar to Spider-Man's web-swinging. The player can:
- Press X (Interact button) while in the air to attach to the nearest swing point
- Hold X to continue swinging
- Use movement input (WASD/Left Stick) to control swing direction
- Release X or touch the ground to detach

## Components

### 1. PlayerSwing.cs
Main component that handles swinging mechanics. Attach this to your player GameObject.

### 2. SwingPoint.cs
Component that marks locations where players can swing from. Place this on GameObjects in your scene.

## Setup Instructions

### Step 1: Add PlayerSwing to Your Player

1. Select your Player GameObject in the scene
2. Add the `PlayerSwing` component (Component → Scripts → PlayerSwing)
3. The component will auto-find required references (PlayerMovement, PlayerGroundCheck, PlayerInputHandler)
4. Configure the settings:

   **Swing Settings:**
   - `Max Detection Distance`: How far the player can detect swing points (default: 20)
   - `Swing Force`: How much force is applied when swinging (default: 15) - Higher = faster swinging
   - `Swing Damping`: Reduces swing speed over time (default: 0.95) - Lower = more damping
   - `Min Swing Speed`: Minimum speed to maintain (default: 2) - Prevents getting stuck
   - `Swing Point Layer`: Layer mask for swing points (default: Everything)

   **Rope/Line Settings:**
   - `Rope Material`: Material for the swing line visual (optional - creates default if not set)
   - `Rope Width`: Thickness of the swing line (default: 0.1)
   - `Rope Origin`: Transform where the rope comes from on the player (auto-created if not set)

### Step 2: Create Swing Points in Your Scene

1. Create empty GameObjects where you want swing points (GameObject → Create Empty)
2. Position them in the air where players should be able to swing from
3. Add the `SwingPoint` component to each GameObject (Component → Scripts → SwingPoint)
4. Configure each swing point:

   **Swing Point Settings:**
   - `Max Swing Distance`: Maximum distance player can be to attach (default: 15)
   - `Visual Indicator`: Optional visual object (auto-created sphere if not set)
   - `Obstacle Layer`: Layer mask for obstacles that block the swing line (default: Everything)

### Step 3: Configure Input

The swinging system uses the **Interact** action from your Input System, which is mapped to the X button by default.

**To verify/change the input:**
1. Open your Input System asset (InputSystem_Actions.inputactions)
2. Find the "Interact" action in the Player action map
3. Ensure it's mapped to the X button (or your preferred key)
4. The system uses:
   - `WasPressedThisFrame()` to detect initial press
   - `IsPressed()` to detect holding

**Note:** If you want to use a different button, you can modify `PlayerInputHandler.cs` to use a different action (like Attack) or add a dedicated Swing action.

### Step 4: Test the System

1. Play the game
2. Jump into the air
3. Press and hold X (Interact button) while near a swing point
4. You should see a rope/line appear connecting you to the swing point
5. Use movement input (WASD) to control swing direction
6. Release X to detach

## Customization

### Adjusting Swing Feel

**Make swinging faster:**
- Increase `Swing Force` in PlayerSwing component
- Decrease `Swing Damping`

**Make swinging slower:**
- Decrease `Swing Force`
- Increase `Swing Damping`

**Make swinging more responsive:**
- Increase `Swing Force`
- Adjust `Min Swing Speed` to prevent getting stuck

### Visual Customization

**Rope Material:**
1. Create a new Material in your project
2. Assign it to the `Rope Material` field in PlayerSwing
3. You can use any shader, but "Sprites/Default" or "Unlit/Color" work well

**Rope Width:**
- Adjust `Rope Width` in PlayerSwing component
- Smaller values = thinner rope, larger = thicker

**Swing Point Visual:**
- The SwingPoint component auto-creates a semi-transparent orange sphere
- You can assign a custom GameObject to `Visual Indicator` for a different look
- Or disable the visual by setting it to null (you'll need to modify the script)

### Swing Point Placement Tips

1. **Height:** Place swing points high enough that players can swing underneath them
2. **Spacing:** Space swing points so players can chain swings together
3. **Range:** Ensure `Max Swing Distance` is appropriate for your level design
4. **Obstacles:** Configure `Obstacle Layer` to prevent swinging through walls

## Example Scene Setup

Here's a typical setup:

```
Scene Hierarchy:
├── Player
│   ├── PlayerMovement (component)
│   ├── PlayerJump (component)
│   ├── PlayerSwing (component) ← Add this
│   └── ... other player components
│
└── SwingPoints (empty parent)
    ├── SwingPoint_01 (GameObject with SwingPoint component)
    ├── SwingPoint_02 (GameObject with SwingPoint component)
    ├── SwingPoint_03 (GameObject with SwingPoint component)
    └── ... more swing points
```

## Troubleshooting

### Player doesn't attach to swing points
- Check that swing points are within `Max Detection Distance`
- Verify swing points have the `SwingPoint` component
- Ensure the player is in the air (not grounded)
- Check that X button (Interact) is properly mapped

### Rope doesn't appear
- Check that `Rope Material` is assigned (or let it auto-create)
- Verify `Rope Origin` transform exists
- Check LineRenderer component on the rope GameObject

### Swinging feels too slow/fast
- Adjust `Swing Force` in PlayerSwing component
- Adjust `Swing Damping` to control speed decay
- Check that gravity settings are appropriate

### Player gets stuck while swinging
- Increase `Min Swing Speed`
- Increase `Swing Force`
- Check that swing point isn't too close to the player

### Can't swing through obstacles
- Configure `Obstacle Layer` on SwingPoint components
- Ensure walls/obstacles are on the correct layers
- Adjust `Max Swing Distance` if needed

## Advanced Usage

### Chaining Swings
Players can chain multiple swings by:
1. Swinging from one point
2. Releasing X while still in the air
3. Immediately pressing X again to attach to the next swing point

### Integration with Other Systems
The swinging system automatically:
- Disables normal movement while swinging (handled by PlayerMovement)
- Respects ground detection (releases when grounded)
- Works with existing jump and movement systems

## Code Notes

- `PlayerSwing` uses `CharacterController.Move()` for movement during swinging
- Normal `PlayerMovement` is disabled while `IsSwinging` is true
- The rope visual uses Unity's `LineRenderer` component
- Swing physics use pendulum mechanics with player input for control

## Support

If you encounter issues:
1. Check the Unity Console for error messages
2. Verify all required components are present on the player
3. Ensure swing points are properly configured
4. Test with default settings first, then customize
