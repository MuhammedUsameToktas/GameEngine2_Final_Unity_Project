# Attack Animation & Damage Synchronization Guide

This guide explains how to synchronize enemy attack animations with damage dealing.

---

## Problem Description

**Issue 1:** First attack happens without animation (damage dealt immediately, animation plays after)

**Issue 2:** Attack animation plays, but damage is dealt after a delay (not synchronized with animation hit frame)

---

## Solution: Two Methods

### Method 1: Timer-Based (Easier, Less Accurate)

This method uses a timer to delay damage until the animation's hit frame.

#### Setup Steps:

1. **Open your enemy's `EnemyAttack` component in Inspector**

2. **Set `Attack Hit Time`:**
   - This should match when your attack animation actually hits
   - Example: If your attack animation is 1 second long and hits at 0.4 seconds, set `Attack Hit Time` to **0.4**
   - **Default:** 0.4 seconds

3. **Make sure `Use Animation Events` is UNCHECKED**

4. **Test and Adjust:**
   - Play the game
   - Watch when the attack animation hits
   - Adjust `Attack Hit Time` until damage matches the hit frame
   - If damage happens too early → Increase `Attack Hit Time`
   - If damage happens too late → Decrease `Attack Hit Time`

#### How It Works:

- When enemy starts attack → Animation plays immediately
- After `Attack Hit Time` seconds → Damage is dealt
- This should match when the animation visually hits

---

### Method 2: Animation Events (Best, Most Accurate) ⭐ RECOMMENDED

This method uses Unity Animation Events to trigger damage at the exact frame.

#### Setup Steps:

1. **Open your Attack animation clip** in the Project window

2. **Select the animation clip** and open it in the Animation window

3. **Find the hit frame:**
   - Scrub through the animation
   - Find the exact frame where the attack visually hits the player
   - Note the time (e.g., 0.4 seconds)

4. **Add Animation Event:**
   - At the hit frame, click the **"Add Event"** button (or right-click on the timeline)
   - A white marker will appear on the timeline

5. **Set Event Function:**
   - Select the event marker
   - In the Inspector, set **Function:** `OnAttackHit`
   - Leave **Int Parameter:** empty
   - Leave **Float Parameter:** empty
   - Leave **String Parameter:** empty

6. **Enable Animation Events in EnemyAttack:**
   - Select your enemy GameObject
   - Find `EnemyAttack` component
   - Check **"Use Animation Events"** ✅

7. **Test:**
   - Play the game
   - Damage should now happen exactly when the animation hits!

#### Visual Guide:

```
Animation Timeline:
0.0s ──────────── 0.4s ──────────── 1.0s
     [Windup]     [HIT!]     [Recovery]
                    ↑
              Event Here!
```

---

## Fixing the "First Attack Without Animation" Issue

This happens because the enemy attacks immediately when reaching range, before the animation state transitions.

### Solution:

The code has been updated to prevent this. Make sure:

1. **EnemyBehavior** component:
   - Enemy transitions to `PreparingToAttack` state first
   - Then transitions to `Attacking` state when cooldown finishes
   - This ensures animation plays before attack

2. **Check Attack Cooldown:**
   - In `EnemyAttack` component
   - Set `Attack Cooldown` to **> 0** (e.g., 2.0)
   - If cooldown is 0, enemy will skip PrepareToAttack state

3. **Verify Animator Transitions:**
   - Make sure `Chase → PrepareToAttack → Attack` transitions exist
   - Make sure `Has Exit Time` is **UNCHECKED** on these transitions

---

## Adjusting Attack Hit Time

### Finding the Right Value:

1. **Play your attack animation** in the Animation window
2. **Watch for the hit frame:**
   - When does the weapon/claw/fist visually contact the player?
   - Note the time (e.g., 0.35 seconds)
3. **Set `Attack Hit Time` to that value**

### Common Values:

- **Quick attacks:** 0.2 - 0.3 seconds
- **Medium attacks:** 0.4 - 0.5 seconds
- **Slow/heavy attacks:** 0.6 - 0.8 seconds

### Testing:

1. Set `Attack Hit Time` to a value
2. Play the game
3. Watch the attack animation
4. If damage happens:
   - **Too early** → Increase the value
   - **Too late** → Decrease the value
5. Repeat until damage matches the visual hit

---

## Troubleshooting

### Damage happens before animation plays:

**Cause:** Attack is triggered before animation state transitions

**Fix:**
- Make sure `EnemyAttack` component has `Attack Cooldown` > 0
- Check that `PreparingToAttack` state exists and transitions properly
- Verify `Has Exit Time` is unchecked on transitions

### Damage happens too early in animation:

**Cause:** `Attack Hit Time` is too low

**Fix:**
- Increase `Attack Hit Time` value
- Or use Animation Events (Method 2) for precise timing

### Damage happens too late in animation:

**Cause:** `Attack Hit Time` is too high

**Fix:**
- Decrease `Attack Hit Time` value
- Or use Animation Events (Method 2) for precise timing

### Animation Events not working:

**Cause:** Function name mismatch or event not set up correctly

**Fix:**
1. Check that Animation Event function is named exactly `OnAttackHit`
2. Check that `Use Animation Events` is checked in `EnemyAttack` component
3. Make sure the event is on the correct animation clip (Attack state)
4. Verify the event is at the right frame

### First attack still happens without animation:

**Cause:** Enemy attacks immediately when reaching range

**Fix:**
1. Check `EnemyBehavior` → `Attack Range` is set correctly
2. Make sure enemy transitions through states properly:
   - Following → PreparingToAttack → Attacking
3. Verify `EnemyAttack` → `Attack Cooldown` > 0

---

## Recommended Settings

### For Timer-Based (Method 1):

```
EnemyAttack Component:
- Attack Hit Time: 0.4 (adjust to match your animation)
- Use Animation Events: ❌ UNCHECKED
- Attack Cooldown: 2.0
```

### For Animation Events (Method 2):

```
EnemyAttack Component:
- Attack Hit Time: 0.4 (ignored when using events)
- Use Animation Events: ✅ CHECKED
- Attack Cooldown: 2.0

Animation Clip:
- Animation Event at hit frame
- Function: OnAttackHit
```

---

## Step-by-Step: Setting Up Animation Events

1. **Select your attack animation clip** in Project window
2. **Double-click** to open in Animation window
3. **Scrub to the hit frame** (when attack visually hits)
4. **Click "Add Event"** button (or right-click timeline)
5. **Select the event marker**
6. **In Inspector, set Function to:** `OnAttackHit`
7. **Save the animation**
8. **In enemy GameObject, check "Use Animation Events"** in EnemyAttack component
9. **Test in Play mode**

---

## Quick Checklist

- [ ] `Attack Hit Time` is set to match animation hit frame (Method 1)
- [ ] OR Animation Event is added at hit frame with function `OnAttackHit` (Method 2)
- [ ] `Use Animation Events` is checked if using Method 2
- [ ] `Attack Cooldown` > 0 to ensure proper state transitions
- [ ] Animator has proper transitions: Chase → PrepareToAttack → Attack
- [ ] `Has Exit Time` is unchecked on attack transitions
- [ ] Tested in Play mode - damage matches animation hit frame

---

## Summary

**Best Practice:** Use **Animation Events (Method 2)** for perfect synchronization.

**Quick Fix:** Adjust `Attack Hit Time` value until damage matches the visual hit frame.

**Common Issue:** First attack without animation - make sure `Attack Cooldown` > 0 and state transitions are set up correctly.
