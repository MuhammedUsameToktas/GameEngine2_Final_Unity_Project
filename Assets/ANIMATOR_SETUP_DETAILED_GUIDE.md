# Detailed Animator Controller Setup Guide

This guide provides **exact step-by-step instructions** with all values you need to set in Unity's Animator window.

---

## Step 1: Verify Parameters (CRITICAL - Must Match Exactly)

Open your Animator Controller and check the **Parameters** tab. You MUST have these **exact** parameters:

### Boolean Parameters (7 total):
1. **IsIdle** - Type: **Bool** - Default: **false**
2. **IsPatrolling** - Type: **Bool** - Default: **false**
3. **IsChasing** - Type: **Bool** - Default: **false**
4. **IsPreparingToAttack** - Type: **Bool** - Default: **false** ⚠️ **MOST IMPORTANT - Often Missing!**
5. **IsAttacking** - Type: **Bool** - Default: **false**
6. **IsMeleeAttack** - Type: **Bool** - Default: **false**
7. **IsRangeAttack** - Type: **Bool** - Default: **false**

### Trigger Parameters (2 total):
8. **TakeDamage** - Type: **Trigger**
9. **Die** - Type: **Trigger**

### Float Parameter (1 total):
10. **Speed** - Type: **Float** - Default: **0**

**⚠️ COMMON MISTAKE #1:** Parameter names are **case-sensitive**! 
- ✅ Correct: `IsPreparingToAttack`
- ❌ Wrong: `IsPreparingToattack`, `isPreparingToAttack`, `PreparingToAttack`

---

## Step 2: Create Animation States

In the **Animator** window, create these states (right-click → `Create State > Empty`):

1. **Idle** (set as default - orange background)
2. **Patrol**
3. **Chase**
4. **PrepareToAttack** ⚠️ **CRITICAL - This is what you're missing!**
5. **Attack**
6. **TakeDamage**
7. **Die**

**⚠️ COMMON MISTAKE #2:** Forgetting to create the `PrepareToAttack` state!

---

## Step 3: Assign Animation Clips

For each state, select it and drag your animation clip into the **Motion** field in the Inspector:

- **Idle** → Your idle animation clip
- **Patrol** → Your walk/patrol animation clip
- **Chase** → Your run/chase animation clip
- **PrepareToAttack** → Your "ready to attack" or "prepare" animation clip (can use idle if you don't have one)
- **Attack** → Your attack animation clip
- **TakeDamage** → Your hit/damage animation clip
- **Die** → Your death animation clip

**⚠️ COMMON MISTAKE #3:** Not assigning animation clips to states!

---

## Step 4: Set Up Transitions (EXACT VALUES)

### Transition: Idle → Patrol

1. Right-click **Idle** state → `Make Transition` → Drag to **Patrol**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:** 
     - `IsPatrolling` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Exit Time:** (ignored)
     - **Transition Duration:** **0.2**
     - **Transition Offset:** **0**
     - **Interruption Source:** **None**

### Transition: Patrol → Idle

1. Right-click **Patrol** state → `Make Transition` → Drag to **Idle**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsIdle` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.2**

### Transition: Patrol → Chase

1. Right-click **Patrol** state → `Make Transition` → Drag to **Chase**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsChasing` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.2**

### Transition: Chase → PrepareToAttack ⚠️ **CRITICAL TRANSITION**

1. Right-click **Chase** state → `Make Transition` → Drag to **PrepareToAttack**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsPreparingToAttack` (Bool) = **true** ⚠️ **Must match exactly!**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED** ⚠️ **Very Important!**
     - **Transition Duration:** **0.1** (fast transition)
     - **Transition Offset:** **0**

**⚠️ COMMON MISTAKE #4:** Having `Has Exit Time` checked on this transition! This causes delay!

### Transition: PrepareToAttack → Attack

1. Right-click **PrepareToAttack** state → `Make Transition` → Drag to **Attack**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsAttacking` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.1**

### Transition: Attack → PrepareToAttack

1. Right-click **Attack** state → `Make Transition` → Drag to **PrepareToAttack**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsPreparingToAttack` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.2**

### Transition: PrepareToAttack → Chase

1. Right-click **PrepareToAttack** state → `Make Transition` → Drag to **Chase**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsChasing` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.2**

### Transition: Chase → Patrol

1. Right-click **Chase** state → `Make Transition` → Drag to **Patrol**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `IsPatrolling` (Bool) = **true**
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.2**

### Transition: Any State → TakeDamage

1. Right-click **Any State** (top-left) → `Make Transition` → Drag to **TakeDamage**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `TakeDamage` (Trigger)
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.1**

### Transition: TakeDamage → Any State

1. Right-click **TakeDamage** state → `Make Transition` → Drag to **Any State**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:** (none - empty)
   - **Settings:**
     - ✅ **Has Exit Time:** **CHECKED** ⚠️ **Important for TakeDamage!**
     - **Exit Time:** **0.9** (plays most of animation)
     - **Transition Duration:** **0.1**

### Transition: Any State → Die

1. Right-click **Any State** → `Make Transition` → Drag to **Die**
2. Select the transition arrow
3. In Inspector:
   - **Conditions:**
     - `Die` (Trigger)
   - **Settings:**
     - ✅ **Has Exit Time:** **UNCHECKED**
     - **Transition Duration:** **0.1**

---

## Step 5: Verify Component Setup

### On Your Enemy GameObject:

1. **EnemyBehavior** component:
   - Check that `Attack Range` is set (e.g., 2.0)
   - Check that `Follow Speed` is set (e.g., 4.0)

2. **EnemyAttack** component:
   - Check that `Attack Cooldown` is set (e.g., 2.0) ⚠️ **This determines when PrepareToAttack shows!**
   - If cooldown is 0, enemy will skip PrepareToAttack state

3. **EnemyAnimatorController** component:
   - Check that `Animator` field is assigned (should auto-find)

4. **Animator** component:
   - Check that `Controller` field has your Animator Controller assigned
   - Check that `Apply Root Motion` is **UNCHECKED** (usually)

---

## Common Mistakes Checklist

### ❌ Mistake #1: Missing Parameter
- **Symptom:** Enemy stays in chase animation
- **Fix:** Add `IsPreparingToAttack` (Bool) parameter

### ❌ Mistake #2: Wrong Parameter Name
- **Symptom:** Transitions don't work
- **Fix:** Check spelling - must be exactly `IsPreparingToAttack` (case-sensitive)

### ❌ Mistake #3: Missing PrepareToAttack State
- **Symptom:** Enemy jumps directly from Chase to Attack
- **Fix:** Create `PrepareToAttack` state and assign animation clip

### ❌ Mistake #4: Has Exit Time Checked
- **Symptom:** Delay before transitioning to PrepareToAttack
- **Fix:** Uncheck `Has Exit Time` on Chase → PrepareToAttack transition

### ❌ Mistake #5: Wrong Transition Conditions
- **Symptom:** Enemy doesn't transition properly
- **Fix:** Verify conditions match exactly:
  - Chase → PrepareToAttack: `IsPreparingToAttack` = true
  - PrepareToAttack → Attack: `IsAttacking` = true

### ❌ Mistake #6: Attack Cooldown = 0
- **Symptom:** Enemy skips PrepareToAttack state
- **Fix:** Set `Attack Cooldown` in EnemyAttack component to > 0 (e.g., 2.0)

### ❌ Mistake #7: Animator Controller Not Assigned
- **Symptom:** No animations play at all
- **Fix:** Assign Animator Controller to Animator component

### ❌ Mistake #8: Multiple Transitions with Same Condition
- **Symptom:** Unpredictable behavior
- **Fix:** Each transition should have unique conditions or priority

---

## Testing Checklist

1. ✅ Enemy starts in **Idle** state
2. ✅ Enemy moves to waypoint → **Patrol** animation
3. ✅ Enemy reaches waypoint → **Idle** animation (waits)
4. ✅ Enemy detects player → **Chase** animation
5. ✅ Enemy reaches attack range → **PrepareToAttack** animation ⚠️ **Check this!**
6. ✅ After cooldown → **Attack** animation
7. ✅ After attack → **PrepareToAttack** animation (if on cooldown)
8. ✅ Player moves away → **Chase** animation

---

## Debugging Tips

### Check Current State in Play Mode:

1. Select your enemy in Hierarchy
2. In Inspector, find **EnemyBehavior** component
3. Look at `Current State` field (read-only) - it shows the current state
4. Check if it shows `PreparingToAttack` when enemy reaches player

### Check Animator Parameters in Play Mode:

1. Open Animator window (`Window > Animation > Animator`)
2. Select your enemy
3. Go to **Parameters** tab
4. Watch the values change in real-time:
   - `IsChasing` should be true when chasing
   - `IsPreparingToAttack` should be true when in range but on cooldown ⚠️ **Check this!**
   - `IsAttacking` should be true when attacking

### If PrepareToAttack Never Shows:

1. Check `EnemyAttack` component → `Attack Cooldown` > 0
2. Check Animator Controller has `IsPreparingToAttack` parameter
3. Check `PrepareToAttack` state exists
4. Check Chase → PrepareToAttack transition exists with correct condition
5. Check `Has Exit Time` is unchecked on that transition

---

## Quick Reference: Transition Settings

| Transition | Condition | Has Exit Time | Duration |
|------------|-----------|---------------|----------|
| Idle → Patrol | `IsPatrolling` = true | ❌ Unchecked | 0.2 |
| Patrol → Idle | `IsIdle` = true | ❌ Unchecked | 0.2 |
| Patrol → Chase | `IsChasing` = true | ❌ Unchecked | 0.2 |
| **Chase → PrepareToAttack** | **`IsPreparingToAttack` = true** | **❌ Unchecked** | **0.1** |
| **PrepareToAttack → Attack** | **`IsAttacking` = true** | **❌ Unchecked** | **0.1** |
| Attack → PrepareToAttack | `IsPreparingToAttack` = true | ❌ Unchecked | 0.2 |
| PrepareToAttack → Chase | `IsChasing` = true | ❌ Unchecked | 0.2 |
| Chase → Patrol | `IsPatrolling` = true | ❌ Unchecked | 0.2 |
| Any State → TakeDamage | `TakeDamage` (Trigger) | ❌ Unchecked | 0.1 |
| TakeDamage → Any State | (none) | ✅ Checked (0.9) | 0.1 |
| Any State → Die | `Die` (Trigger) | ❌ Unchecked | 0.1 |

---

## Final Checklist

Before testing, verify:

- [ ] All 10 parameters exist with exact names
- [ ] `PrepareToAttack` state exists
- [ ] `PrepareToAttack` state has animation clip assigned
- [ ] Chase → PrepareToAttack transition exists
- [ ] `IsPreparingToAttack` condition is set correctly
- [ ] `Has Exit Time` is **UNCHECKED** on Chase → PrepareToAttack
- [ ] EnemyAttack component has `Attack Cooldown` > 0
- [ ] Animator Controller is assigned to Animator component
- [ ] EnemyAnimatorController component is attached

If all these are correct, the enemy should properly show PrepareToAttack animation when reaching the player!
