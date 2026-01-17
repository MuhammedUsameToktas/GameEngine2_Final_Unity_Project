# 🎮 Complete Setup Guide: Inventory & Currency System

This guide walks you through setting up the entire inventory and currency system in Unity, step by step.

---

## 📋 TABLE OF CONTENTS

1. [Player Prefab Setup](#1-player-prefab-setup)
2. [Creating ItemData ScriptableObjects](#2-creating-itemdata-scriptableobjects)
3. [Inventory UI Setup (Detailed)](#3-inventory-ui-setup-detailed)
4. [Creating ItemSlot Prefab](#4-creating-itemslot-prefab)
5. [Wiring the Inventory Button](#5-wiring-the-inventory-button)
6. [Setting Up Collectibles](#6-setting-up-collectibles)
7. [Creating Coin Pickups](#7-creating-coin-pickups)
8. [Testing Everything](#8-testing-everything)

---

## 1. PLAYER PREFAB SETUP

### Step 1.1: Open Player Prefab
1. Navigate to `Assets/Resources/Prefabs/`
2. Find and double-click `Player.prefab` to open it in Prefab Mode

### Step 1.2: Add PlayerCurrency Component
1. Select the root `Player` GameObject in the Hierarchy
2. Click **Add Component** in Inspector
3. Search for `PlayerCurrency`
4. Add the component
5. **No configuration needed** - it starts with 0 coins

### Step 1.3: Add PlayerInventory Component
1. Still on the `Player` GameObject
2. Click **Add Component** again
3. Search for `PlayerInventory`
4. Add the component
5. **No configuration needed** - inventory starts empty

### Step 1.4: Save Prefab
1. Click the **←** arrow in the top-left to exit Prefab Mode
2. Click **Save** when prompted

**✅ Player is now ready!**

---

## 2. CREATING ITEMDATA SCRIPTABLEOBJECTS

### Step 2.1: Create Items Folder
1. In Project window, navigate to `Assets/Resources/`
2. Right-click → **Create → Folder**
3. Name it `Items`

**Why Resources?** The `ItemSlotUI` script automatically loads ItemData from `Resources/Items/` folder.

### Step 2.2: Create Your First Item
1. Right-click in `Assets/Resources/Items/` folder
2. Select **Create → Game → Item**
3. Name it `GoldKey` (or any name you like)

### Step 2.3: Configure ItemData
In the Inspector, set these fields:

**Item Identity:**
- **Item ID**: `"gold_key"` (must be unique, lowercase with underscores)
- **Display Name**: `"Gold Key"` (what players see)

**Visual:**
- **Icon**: Drag a sprite/icon here (create one or use placeholder)
- **Description**: `"A shiny golden key that opens special doors."` (optional)

**Item Properties:**
- **Stackable**: `false` (keys typically don't stack)
- Leave **Description** empty if you don't need tooltips yet

### Step 2.4: Create More Items (Examples)
Create these common item types:

**Keys:**
- `SilverKey` → Item ID: `"silver_key"`, Stackable: `false`
- `AncientKey` → Item ID: `"ancient_key"`, Stackable: `false`

**Materials (stackable):**
- `WolfPelt` → Item ID: `"wolf_pelt"`, Stackable: `true`
- `IronOre` → Item ID: `"iron_ore"`, Stackable: `true`
- `PowerCore` → Item ID: `"power_core"`, Stackable: `true`

**Consumables:**
- `HealthPotion` → Item ID: `"health_potion"`, Stackable: `true`
- `ManaCrystal` → Item ID: `"mana_crystal"`, Stackable: `true`

**✅ Items are ready!**

---

## 3. INVENTORY UI SETUP (DETAILED)

You've already created the Inventory button and panel. Now let's wire everything together.

### Step 3.1: Structure Your Pause Menu

Your pause menu should look like this:

```
Canvas
└── PauseMenu (Panel) ← Main pause menu
    ├── ResumeButton
    ├── InventoryButton ← You added this
    ├── OptionsButton (optional)
    └── SaveAndQuitButton
    └── InventoryPanel (Panel) ← You created this
        ├── InventoryUI (Component)
        └── Content (GameObject with LayoutGroup)
            └── (Item slots will spawn here)
```

### Step 3.2: Setup InventoryPanel Structure

1. **Select your `InventoryPanel` GameObject**

2. **Add InventoryUI Component:**
   - Click **Add Component**
   - Search for `InventoryUI`
   - Add it

3. **Create Content Container:**
   - Right-click `InventoryPanel` → **UI → GameObject** (or create empty GameObject)
   - Name it `Content`
   - Add a **Layout Group** component:
     - **Vertical Layout Group** (recommended for list)
     - OR **Grid Layout Group** (for grid view)
   
   **Vertical Layout Group Settings:**
   - **Child Alignment**: Upper Left
   - **Child Control Size**: ✅ Width, ✅ Height
   - **Child Force Expand**: ❌ Width, ❌ Height
   - **Spacing**: `10` (adjust as needed)
   - **Padding**: Left/Right/Top/Bottom = `10`

   **Grid Layout Group Settings (Alternative):**
   - **Cell Size**: X=`100`, Y=`100`
   - **Spacing**: X=`10`, Y=`10`
   - **Start Corner**: Upper Left
   - **Start Axis**: Horizontal
   - **Child Alignment**: Upper Left

4. **Add ScrollRect (Optional but Recommended):**
   - Select `InventoryPanel`
   - Add Component → **Scroll Rect**
   - Drag `Content` to **Content** field
   - Create a new GameObject under `InventoryPanel` called `Viewport`
   - Set `Viewport` as child of `InventoryPanel`
   - Set `Content` as child of `Viewport`
   - In ScrollRect, drag `Viewport` to **Viewport** field
   - Add **Mask** component to `Viewport` (optional, for clean edges)

### Step 3.3: Configure InventoryUI Component

1. **Select `InventoryPanel`** (the one with InventoryUI component)

2. **In Inspector, find InventoryUI component:**

   - **Content**: Drag your `Content` GameObject here
   - **Item Slot Prefab**: Leave empty for now (we'll create it next)

3. **Set InventoryPanel to Inactive:**
   - In Hierarchy, uncheck the checkbox next to `InventoryPanel`
   - It should only be active when Inventory button is clicked

**✅ InventoryPanel structure is ready!**

---

## 4. CREATING ITEMSLOT PREFAB

This is the visual representation of each item in your inventory.

### Step 4.1: Create ItemSlot GameObject

1. In your scene (or create a temporary scene), create a new GameObject
2. Name it `ItemSlot`
3. Add **RectTransform** (automatically added for UI elements)

### Step 4.2: Add UI Elements

**Option A: Simple Layout (Recommended for beginners)**

1. **Add Image for Background:**
   - Right-click `ItemSlot` → **UI → Image**
   - Name it `Background`
   - Set color to dark gray/black (e.g., `#2A2A2A`)
   - Set **RectTransform**:
     - Width: `200`
     - Height: `60`

2. **Add Image for Icon:**
   - Right-click `ItemSlot` → **UI → Image**
   - Name it `Icon`
   - Set **RectTransform**:
     - Anchor: Top-Left
     - Pos X: `10`, Pos Y: `-10`
     - Width: `40`, Height: `40`
   - Set **Image Type**: Simple
   - Leave **Source Image** empty (will be set by script)

3. **Add Text for Item Name:**
   - Right-click `ItemSlot` → **UI → Text - TextMeshPro** (create TMP if needed)
   - Name it `NameText`
   - Set **RectTransform**:
     - Anchor: Top-Left
     - Pos X: `60`, Pos Y: `-10`
     - Width: `120`, Height: `20`
   - Set **Font Size**: `16`
   - Set **Alignment**: Left, Top
   - Text: `"Item Name"` (placeholder)

4. **Add Text for Quantity:**
   - Right-click `ItemSlot` → **UI → Text - TextMeshPro**
   - Name it `QuantityText`
   - Set **RectTransform**:
     - Anchor: Top-Right
     - Pos X: `-10`, Pos Y: `-10`
     - Width: `30`, Height: `20`
   - Set **Font Size**: `14`
   - Set **Alignment**: Right, Top
   - Text: `"x1"` (placeholder)
   - Set **Color**: Light gray (e.g., `#CCCCCC`)

**Option B: Use a Button (For clickable items later)**

Same as Option A, but:
- Add **Button** component to `ItemSlot`
- Make Background, Icon, NameText, QuantityText children of the Button
- This allows items to be clickable/selectable later

### Step 4.3: Add ItemSlotUI Component

1. Select `ItemSlot` GameObject
2. Add Component → Search `ItemSlotUI`
3. In Inspector, assign references:
   - **Icon Image**: Drag `Icon` GameObject here
   - **Quantity Text**: Drag `QuantityText` GameObject here
   - **Name Text**: Drag `NameText` GameObject here (optional, can be null)

### Step 4.4: Save as Prefab

1. Drag `ItemSlot` from Hierarchy to `Assets/Resources/Prefabs/` folder
2. Name it `ItemSlot`
3. Delete the `ItemSlot` from your scene (we only need the prefab)

**✅ ItemSlot prefab is ready!**

### Step 4.5: Assign Prefab to InventoryUI

1. Go back to your pause menu scene
2. Select `InventoryPanel` (with InventoryUI component)
3. In Inspector, find **InventoryUI** component
4. Drag `ItemSlot` prefab to **Item Slot Prefab** field

**✅ InventoryUI is fully configured!**

---

## 5. WIRING THE INVENTORY BUTTON

### Step 5.1: Add InventoryPanelController

1. Select your **PauseMenu** GameObject (the main pause panel)
2. Add Component → Search `InventoryPanelController`
3. In Inspector, assign:
   - **Inventory Panel**: Drag your `InventoryPanel` GameObject here
   - **Pause Menu Panel**: Drag your main `PauseMenu` panel here

### Step 5.2: Connect Inventory Button

1. Select your **InventoryButton** in Hierarchy
2. In Inspector, find **Button** component
3. Scroll down to **On Click ()** section
4. Click **+** to add a new event
5. Drag your **PauseMenu** GameObject (with InventoryPanelController) to the object field
6. In the dropdown, select: **InventoryPanelController → OpenInventory()**

### Step 5.3: Add Back Button (Optional but Recommended)

1. Inside your `InventoryPanel`, create a **Back Button**
2. Add it as a child of `InventoryPanel`
3. Position it at the top or bottom
4. In Button's **On Click ()**:
   - Drag `PauseMenu` GameObject
   - Select: **InventoryPanelController → CloseInventory()**

**✅ Inventory button is wired!**

---

## 6. SETTING UP COLLECTIBLES

### Step 6.1: Update Existing Collectibles

1. Find a collectible in your scene (or create one)
2. Select the GameObject with `Collectible` component
3. In Inspector, you'll see new fields:
   - **Item Data** (optional)
   - **Item Quantity** (default: 1)

### Step 6.2: Make Collectible Give Item

**Option A: Item Only (No Souls)**
- Set **Soul Value**: `0`
- Assign **Item Data**: Drag an ItemData (e.g., `GoldKey`)
- Set **Item Quantity**: `1`

**Option B: Item + Souls**
- Set **Soul Value**: `10` (or any value)
- Assign **Item Data**: Drag an ItemData
- Set **Item Quantity**: `1`

**Option C: Souls Only (Backward Compatible)**
- Set **Soul Value**: `10`
- Leave **Item Data** empty
- Works exactly as before

### Step 6.3: Test Collectible

1. Play the game
2. Walk into the collectible
3. Open pause menu → Inventory
4. You should see the item appear!

**✅ Collectibles are ready!**

---

## 7. CREATING COIN PICKUPS

### Step 7.1: Create Coin GameObject

1. Create a new GameObject (3D or 2D, depending on your game)
2. Name it `Coin`
3. Add a **Collider** (Sphere Collider for 3D, Circle Collider for 2D)
4. Set collider to **Is Trigger**: ✅

### Step 7.2: Add CoinPickup Component

1. Select `Coin` GameObject
2. Add Component → Search `CoinPickup`
3. In Inspector:
   - **Value**: `1` (or any amount)

### Step 7.3: Add Visual (Optional)

- Add a **Mesh Renderer** with a coin model
- OR add a **Sprite Renderer** with a coin sprite
- Add rotation animation if desired

### Step 7.4: Save as Prefab

1. Drag `Coin` to `Assets/Resources/Prefabs/`
2. Name it `Coin`
3. Place coins in your scene

**✅ Coins are ready!**

---

## 8. TESTING EVERYTHING

### Test Checklist:

**✅ Currency System:**
- [ ] Walk into a coin pickup
- [ ] Check console for "Added X coins" message
- [ ] Save game
- [ ] Load game
- [ ] Coins should persist

**✅ Inventory System:**
- [ ] Walk into a collectible with ItemData assigned
- [ ] Open pause menu
- [ ] Click Inventory button
- [ ] See item appear in inventory panel
- [ ] Save game
- [ ] Load game
- [ ] Items should persist

**✅ UI:**
- [ ] Inventory button opens inventory panel
- [ ] Back button (if added) closes inventory panel
- [ ] Items display with icons and quantities
- [ ] Empty inventory shows nothing (or "Empty" message)

**✅ Multiple Items:**
- [ ] Collect same item twice (if stackable)
- [ ] Quantity should increase
- [ ] Collect different items
- [ ] All should appear in inventory

---

## 🎯 QUICK REFERENCE

### ItemData Naming Convention:
- Use lowercase with underscores: `"gold_key"`, `"wolf_pelt"`
- Keep IDs consistent across your game

### Common Item IDs (Examples):
- Keys: `"gold_key"`, `"silver_key"`, `"ancient_key"`
- Materials: `"iron_ore"`, `"wood_plank"`, `"crystal_shard"`
- Consumables: `"health_potion"`, `"mana_potion"`
- Quest Items: `"wolf_pelt"`, `"bear_claw"`, `"dragon_scale"`

### Folder Structure:
```
Assets/
├── Resources/
│   ├── Items/          ← ItemData ScriptableObjects
│   └── Prefabs/
│       ├── ItemSlot    ← UI prefab
│       └── Coin        ← Coin pickup prefab
├── Gameplay/
│   ├── Player/
│   │   ├── Currency/   ← PlayerCurrency.cs
│   │   └── Inventory/  ← PlayerInventory.cs
│   └── Collectibles/   ← Collectible.cs, CoinPickup.cs
└── _Core/
    └── UI/             ← InventoryUI.cs, ItemSlotUI.cs
```

---

## 🐛 TROUBLESHOOTING

**Problem: Items don't appear in inventory**
- ✅ Check that Player has `PlayerInventory` component
- ✅ Check that Collectible has `ItemData` assigned
- ✅ Check console for errors
- ✅ Verify ItemData has correct `itemID`

**Problem: Icons don't show**
- ✅ Check that ItemData has an icon sprite assigned
- ✅ Check that ItemSlotUI has Icon Image reference
- ✅ Verify sprite is assigned to ItemData

**Problem: Inventory panel doesn't open**
- ✅ Check InventoryPanelController is on PauseMenu
- ✅ Verify button is wired to `OpenInventory()`
- ✅ Check InventoryPanel is inactive by default

**Problem: Coins don't save**
- ✅ Check Player has `PlayerCurrency` component
- ✅ Check SaveManager is saving coins (check console logs)

---

## 🚀 NEXT STEPS

Once everything is working:

1. **Create more ItemData assets** for your game items
2. **Design better UI** (improve ItemSlot appearance)
3. **Add item tooltips** (hover to show description)
4. **Implement shops** (use `inventory.HasItem()` and `currency.CanSpend()`)
5. **Add quests** (check `inventory.HasItem("quest_item", 5)`)
6. **Create item usage** (consumables, keys for doors, etc.)

**Your architecture is now ready for all of this!** 🎉
