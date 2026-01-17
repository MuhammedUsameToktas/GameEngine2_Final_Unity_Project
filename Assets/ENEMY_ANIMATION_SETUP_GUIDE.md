# Enemy Animation Setup Guide

This guide provides step-by-step instructions for setting up animations for enemies in your Unity project.

## Overview

The `EnemyAnimatorController` component is already implemented and handles all animation parameter updates automatically. You just need to:
1. Add the component to enemy GameObjects
2. Set up the Animator Controller with the required parameters
3. Create animation states and transitions
4. Assign animation clips to states

---

## Part 1: Adding EnemyAnimatorController Component

### Step 1: Select Your Enemy GameObject
1. In the Hierarchy, select an enemy GameObject (or open an enemy prefab)
2. Make sure the enemy has an `Animator` component (if not, add one via `Add Component > Animator`)

### Step 2: Add EnemyAnimatorController Component
1. With the enemy selected, click `Add Component` in the Inspector
2. Search for `EnemyAnimatorController`
3. Click to add it

### Step 3: Configure the Component
- The component will automatically find the `Animator` component
- If your Animator is on a child object, you can manually assign it in the `Animator` field
- The component will automatically detect:
  - `EnemyBehavior` or `EnemyFlyingBehavior` (for state information)
  - `EnemyAttack` (for attack type information)
  - `EnemyHealth` (for damage/death events)

**Note:** The component works automatically once added - no additional configuration needed!

---

## Part 2: Setting Up the Animator Controller

### Step 1: Open/Create Animator Controller
1. Navigate to `Assets/Resources/Animations/Enemy_Animations/`
2. Either:
   - Open an existing enemy controller (e.g., `AC_Enemy_ChestMosnter.controller`)
   - Or create a new one: Right-click → `Create > Animator Controller`
   - Name it appropriately (e.g., `AC_Enemy_[EnemyName].controller`)

### Step 2: Add Required Parameters

Open the Animator window (`Window > Animation > Animator`) and select your controller.

In the **Parameters** tab (top-left), click the **+** button to add each parameter:

#### Boolean Parameters:
1. **IsIdle** (Bool)
   - Type: Bool
   - Default: false

2. **IsPatrolling** (Bool)
   - Type: Bool
   - Default: false

3. **IsChasing** (Bool)
   - Type: Bool
   - Default: false

4. **IsPreparingToAttack** (Bool)
   - Type: Bool
   - Default: false

5. **IsAttacking** (Bool)
   - Type: Bool
   - Default: false

6. **IsMeleeAttack** (Bool)
   - Type: Bool
   - Default: false

7. **IsRangeAttack** (Bool)
   - Type: Bool
   - Default: false

#### Trigger Parameters:
8. **TakeDamage** (Trigger)
   - Type: Trigger

9. **Die** (Trigger)
   - Type: Trigger

#### Float Parameter:
10. **Speed** (Float)
   - Type: Float
   - Default: 0

**Important:** Parameter names must match exactly (case-sensitive):
- `IsIdle`, `IsPatrolling`, `IsChasing`, `IsPreparingToAttack`, `IsAttacking`
- `IsMeleeAttack`, `IsRangeAttack`
- `TakeDamage`, `Die`
- `Speed`

---

## Part 3: Creating Animation States

### Step 1: Create Base States

In the Animator window, right-click in the empty space and create states:

1. **Idle** (default state - orange background)
   - Right-click → `Create State > Empty`
   - Name it: `Idle`
   - Right-click on it → `Set as Layer Default State` (orange background)

2. **Patrol**
   - Right-click → `Create State > Empty`
   - Name it: `Patrol`

3. **Chase**
   - Right-click → `Create State > Empty`
   - Name it: `Chase`

4. **PrepareToAttack**
   - Right-click → `Create State > Empty`
   - Name it: `PrepareToAttack`

5. **Attack**
   - Right-click → `Create State > Empty`
   - Name it: `Attack`

6. **TakeDamage**
   - Right-click → `Create State > Empty`
   - Name it: `TakeDamage`

7. **Die**
   - Right-click → `Create State > Empty`
   - Name it: `Die`

### Step 2: Assign Animation Clips to States

For each state:
1. Select the state in the Animator window
2. In the Inspector, find the `Motion` field
3. Drag your animation clip from the Project window into the `Motion` field
   - Example: `Idle` state → `Idle.anim` clip
   - Example: `Patrol` state → `Walk.anim` or `Patrol.anim` clip
   - Example: `Chase` state → `Run.anim` or `Chase.anim` clip
   - Example: `PrepareToAttack` state → `PrepareAttack.anim` or `ReadyToAttack.anim` clip
   - Example: `Attack` state → `Attack.anim` or `MeleeAttack.anim` clip
   - Example: `TakeDamage` state → `Hit.anim` or `TakeDamage.anim` clip
   - Example: `Die` state → `Death.anim` or `Die.anim` clip

**Note:** If you don't have animation clips yet, you can:
- Import them from your 3D model package
- Create placeholder animations
- Use the same clip for multiple states temporarily

---

## Part 4: Setting Up Transitions

### Understanding the State Flow

The enemy state machine follows this logic:
- **Idle** → When `IsIdle` is true (enemy is waiting at a waypoint after reaching it)
- **Patrol** → When `IsPatrolling` is true (enemy is moving to a waypoint)
- **Chase** → When `IsChasing` is true (enemy detected player and is following)
- **PrepareToAttack** → When `IsPreparingToAttack` is true (enemy reached player but is waiting for attack cooldown)
- **Attack** → When `IsAttacking` is true (enemy is performing an attack)
- **TakeDamage** → When `TakeDamage` trigger is fired (can interrupt any state)
- **Die** → When `Die` trigger is fired (can interrupt any state)

**Important:** The enemy automatically switches between Idle and Patrol during patrolling:
- When enemy reaches a waypoint → **Idle** state (waits for 1-3 seconds)
- After waiting → **Patrol** state (moves to next waypoint)
- This creates natural patrol behavior where enemies pause at each location

### Creating Transitions

#### From Any State → TakeDamage
1. Right-click on **Any State** (top-left)
2. Select `Make Transition`
3. Drag to **TakeDamage** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `TakeDamage` (Trigger)
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.1 (for quick response)

#### From Any State → Die
1. Right-click on **Any State**
2. Select `Make Transition`
3. Drag to **Die** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `Die` (Trigger)
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.1

#### From Idle → Patrol
1. Right-click on **Idle** state
2. Select `Make Transition`
3. Drag to **Patrol** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsPatrolling` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

**Note:** This transition happens automatically when the enemy finishes waiting at a waypoint and starts moving to the next one.

#### From Patrol → Idle
1. Right-click on **Patrol** state
2. Select `Make Transition`
3. Drag to **Idle** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsIdle` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

**Note:** This transition happens automatically when the enemy reaches a waypoint and starts waiting. The enemy will idle for 1-3 seconds (configurable in `EnemyBehavior` component) before continuing to the next waypoint.

#### From Patrol → Chase
1. Right-click on **Patrol** state
2. Select `Make Transition`
3. Drag to **Chase** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsChasing` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

#### From Chase → Patrol
1. Right-click on **Chase** state
2. Select `Make Transition`
3. Drag to **Patrol** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsPatrolling` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

#### From Chase → PrepareToAttack
1. Right-click on **Chase** state
2. Select `Make Transition`
3. Drag to **PrepareToAttack** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsPreparingToAttack` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

**Note:** This transition happens when the enemy reaches attack range but is on cooldown (can't attack yet).

#### From PrepareToAttack → Attack
1. Right-click on **PrepareToAttack** state
2. Select `Make Transition`
3. Drag to **Attack** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsAttacking` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.1

**Note:** This transition happens when the attack cooldown finishes and the enemy can attack.

#### From Attack → PrepareToAttack
1. Right-click on **Attack** state
2. Select `Make Transition`
3. Drag to **PrepareToAttack** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsPreparingToAttack` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

**Note:** This transition happens after an attack finishes but the enemy is still on cooldown.

#### From PrepareToAttack → Chase
1. Right-click on **PrepareToAttack** state
2. Select `Make Transition`
3. Drag to **Chase** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsChasing` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

**Note:** This transition happens if the player moves out of attack range while the enemy is preparing.

#### From Attack → Chase
1. Right-click on **Attack** state
2. Select `Make Transition`
3. Drag to **Chase** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsChasing` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

#### From Attack → Patrol
1. Right-click on **Attack** state
2. Select `Make Transition`
3. Drag to **Patrol** state
4. Select the transition arrow
5. In Inspector, set conditions:
   - Condition: `IsPatrolling` (Bool) = true
6. Uncheck `Has Exit Time`
7. Set `Transition Duration`: 0.2

#### From TakeDamage → Previous State
1. Right-click on **TakeDamage** state
2. Select `Make Transition`
3. Drag to **Any State**
4. Select the transition arrow
5. In Inspector:
   - Check `Has Exit Time`
   - Set `Exit Time`: 0.9 (plays most of the damage animation)
   - Set `Transition Duration`: 0.1

**Note:** The `TakeDamage` state will automatically return to the previous state after playing. You might want to add specific transitions back to Idle, Patrol, or Chase based on your needs.

### Transition Settings Summary

For most transitions:
- **Has Exit Time**: Unchecked (except for TakeDamage → return)
- **Transition Duration**: 0.1-0.2 seconds
- **Interruption Source**: None (default)

For TakeDamage and Die:
- **Has Exit Time**: Unchecked (immediate)
- **Transition Duration**: 0.1 seconds
- These can interrupt any state

---

## Part 5: Advanced Setup (Optional)

### Using Speed Parameter for Blend Trees

If you want smooth speed-based animations:

1. Create a **Blend Tree**:
   - Right-click in Animator → `Create State > From New Blend Tree`
   - Name it: `Locomotion`

2. Configure Blend Tree:
   - Select the Blend Tree state
   - In Inspector, click the Blend Tree name to edit it
   - Set `Blend Type`: `1D`
   - Set `Parameter`: `Speed`
   - Add motions:
     - Motion 0: `Idle.anim` at Threshold: 0
     - Motion 1: `Walk.anim` at Threshold: 0.5
     - Motion 2: `Run.anim` at Threshold: 1.0

3. Replace Idle/Patrol/Chase states with transitions to/from the Blend Tree

### Attack Type Variations

If you have separate melee and ranged attack animations:

1. Create two attack states:
   - `MeleeAttack`
   - `RangeAttack`

2. Set up transitions:
   - From Chase → MeleeAttack (when `IsAttacking` = true AND `IsMeleeAttack` = true)
   - From Chase → RangeAttack (when `IsAttacking` = true AND `IsRangeAttack` = true)

3. Add conditions with multiple parameters:
   - Condition 1: `IsAttacking` = true
   - Condition 2: `IsMeleeAttack` = true (or `IsRangeAttack` = true)

---

## Part 6: Assigning Controller to Enemy

### Step 1: Select Enemy GameObject/Prefab
1. Select your enemy in the Hierarchy or open the prefab

### Step 2: Assign Animator Controller
1. Find the `Animator` component in the Inspector
2. In the `Controller` field, drag your Animator Controller from the Project window
   - Example: `AC_Enemy_ChestMosnter.controller`

### Step 3: Verify Setup
1. Make sure `EnemyAnimatorController` component is attached
2. Make sure `Animator` component has the controller assigned
3. Play the game and test:
   - Enemy should idle when spawned
   - Enemy should patrol when moving
   - Enemy should chase when player is detected
   - Enemy should attack when in range
   - Enemy should play damage animation when hit
   - Enemy should play death animation when killed

---

## Part 7: Player Attack Animation Setup

The player already has the `Attack` trigger parameter set up. To verify:

1. Open `Assets/Resources/Animations/Player_Animations/Player_AC.controller`
2. Check Parameters tab - you should see:
   - `Attack` (Trigger) ✓
   - `Speed` (Float) ✓
   - `IsMoving` (Bool) ✓
   - `IdleVariant` (Trigger) ✓

3. If `Attack` parameter is missing:
   - Click **+** in Parameters tab
   - Select `Trigger`
   - Name it: `Attack`

4. Create/assign attack animation:
   - Create an `Attack` state in the Animator
   - Assign your attack animation clip
   - Add transition from any state → Attack (when `Attack` trigger fires)
   - Add transition from Attack → back to locomotion (with exit time)

---

## Troubleshooting

### Enemy Not Animating
- ✅ Check that `EnemyAnimatorController` component is attached
- ✅ Check that `Animator` component has a controller assigned
- ✅ Check that parameter names match exactly (case-sensitive)
- ✅ Check that animation clips are assigned to states
- ✅ Check that transitions have correct conditions

### Wrong Animation Playing
- ✅ Verify state conditions are set correctly
- ✅ Check that `Has Exit Time` is unchecked for responsive transitions
- ✅ Verify parameter values in Animator window during play (use Debug mode)

### Attack Animation Not Triggering
- ✅ Check that `EnemyAttack` component is attached
- ✅ Check that attack range is appropriate
- ✅ Verify `IsAttacking` parameter is being set (check Animator window during play)

### Damage/Death Animations Not Playing
- ✅ Check that `EnemyHealth` component is attached
- ✅ Verify `TakeDamage` and `Die` triggers are set up correctly
- ✅ Check that transitions from `Any State` are configured

### Speed Parameter Not Working
- ✅ Verify `Speed` parameter is a Float type
- ✅ Check that `EnemyAnimatorController` is updating the speed value
- ✅ If using Blend Trees, verify thresholds are set correctly

---

## Quick Reference: Parameter Usage

| Parameter | Type | When Set | Used For |
|-----------|------|----------|----------|
| `IsIdle` | Bool | Enemy reaches waypoint and is waiting | Idle animation (enemy pauses at waypoint) |
| `IsPatrolling` | Bool | Enemy is moving to waypoint | Walk/patrol animation |
| `IsChasing` | Bool | Enemy detected player and is following | Run/chase animation |
| `IsPreparingToAttack` | Bool | Enemy reached player but waiting for attack cooldown | Prepare/ready to attack animation |
| `IsAttacking` | Bool | Enemy is performing an attack | Attack animation |
| `IsMeleeAttack` | Bool | Enemy is doing melee attack | Melee-specific animation |
| `IsRangeAttack` | Bool | Enemy is doing ranged attack | Ranged-specific animation |
| `TakeDamage` | Trigger | Enemy takes damage | Hit/damage animation |
| `Die` | Trigger | Enemy dies | Death animation |
| `Speed` | Float | Based on movement speed | Speed-based blending (0-1, 0 when idle/preparing) |

**Patrol Behavior:**
- Enemy moves to a random waypoint within patrol radius → **Patrol** animation
- Enemy reaches waypoint → **Idle** animation (waits 1-3 seconds)
- After waiting → **Patrol** animation (moves to next waypoint)
- This cycle repeats, creating natural patrol behavior

**Combat Behavior:**
- Enemy detects player → **Chase** animation (runs toward player)
- Enemy reaches attack range but on cooldown → **PrepareToAttack** animation (faces player, ready stance)
- Attack cooldown finishes → **Attack** animation (performs attack)
- After attack, if still on cooldown → **PrepareToAttack** animation (waits for next attack)
- If player moves away → **Chase** animation (follows player again)

---

## Summary Checklist

- [ ] `EnemyAnimatorController` component added to enemy
- [ ] Animator Controller created/opened
- [ ] All 9 parameters added to Animator Controller
- [ ] Animation states created (Idle, Patrol, Chase, Attack, TakeDamage, Die)
- [ ] Animation clips assigned to states
- [ ] Transitions created between states
- [ ] Transition conditions set correctly
- [ ] Animator Controller assigned to enemy's Animator component
- [ ] Tested in Play mode

---

## Additional Notes

- The `EnemyAnimatorController` script automatically updates all parameters based on enemy state
- You don't need to manually set parameters in code - it's all handled automatically
- The component works with both ground enemies (`EnemyBehavior`) and flying enemies (`EnemyFlyingBehavior`)
- Attack type (melee/ranged) is automatically detected from the `EnemyAttack` component
- Damage and death animations are triggered automatically when `EnemyHealth` takes damage

For questions or issues, refer to the `EnemyAnimatorController.cs` script comments for implementation details.
