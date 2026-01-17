# ⚡ Quick Setup Summary

## 🎯 What You Need to Do (In Order)

### 1️⃣ Player Prefab (2 minutes)
- Open `Player.prefab`
- Add `PlayerCurrency` component
- Add `PlayerInventory` component
- Save prefab

### 2️⃣ Create ItemData Assets (5 minutes)
- Create folder: `Assets/Resources/Items/`
- Right-click → **Create → Game → Item**
- Create items like:
  - `GoldKey` → Item ID: `"gold_key"`, Stackable: `false`
  - `WolfPelt` → Item ID: `"wolf_pelt"`, Stackable: `true`
- Assign icons and names

### 3️⃣ Inventory UI Setup (10 minutes)

**A. Structure:**
```
InventoryPanel
├── InventoryUI (Component) ← Add this
└── Content (GameObject)
    └── VerticalLayoutGroup ← Add this
```

**B. Create ItemSlot Prefab:**
1. Create GameObject `ItemSlot`
2. Add UI elements:
   - `Background` (Image, 200x60)
   - `Icon` (Image, 40x40, top-left)
   - `NameText` (TextMeshPro, left of icon)
   - `QuantityText` (TextMeshPro, top-right)
3. Add `ItemSlotUI` component
4. Assign references (Icon, QuantityText, NameText)
5. Save as prefab in `Resources/Prefabs/`

**C. Configure InventoryUI:**
- Select `InventoryPanel`
- In `InventoryUI` component:
  - Drag `Content` to **Content** field
  - Drag `ItemSlot` prefab to **Item Slot Prefab** field

### 4️⃣ Wire Inventory Button (2 minutes)
- Select `PauseMenu` GameObject
- Add `InventoryPanelController` component
- Assign:
  - **Inventory Panel**: Your `InventoryPanel`
  - **Pause Menu Panel**: Your main `PauseMenu` panel
- Select `InventoryButton`
- In Button's **On Click**:
  - Drag `PauseMenu` → Select `InventoryPanelController.OpenInventory()`

### 5️⃣ Test (2 minutes)
- Play game
- Collect a coin → Check console
- Collect item → Open Inventory → See item
- Save/Load → Verify persistence

---

## 📐 Visual Hierarchy

```
Canvas
└── PauseMenu (Panel)
    ├── ResumeButton
    ├── InventoryButton ← Click opens inventory
    ├── OptionsButton
    ├── SaveAndQuitButton
    │
    └── InventoryPanel (Panel) ← Hidden by default
        ├── InventoryUI (Component)
        │   ├── Content: Content (GameObject)
        │   └── Item Slot Prefab: ItemSlot (Prefab)
        │
        ├── Content (GameObject)
        │   └── VerticalLayoutGroup
        │   └── (Item slots spawn here)
        │
        └── BackButton (Optional)
```

---

## 🔧 Component Checklist

**Player Prefab:**
- ✅ `PlayerCurrency`
- ✅ `PlayerInventory`

**InventoryPanel:**
- ✅ `InventoryUI` component
- ✅ `Content` GameObject with LayoutGroup
- ✅ Set to **Inactive** by default

**PauseMenu:**
- ✅ `InventoryPanelController` component
- ✅ References assigned

**InventoryButton:**
- ✅ Wired to `OpenInventory()`

**ItemSlot Prefab:**
- ✅ `ItemSlotUI` component
- ✅ UI elements (Icon, QuantityText, NameText)
- ✅ References assigned

---

## 🎨 ItemSlot Layout Example

```
┌─────────────────────────────┐
│ [Icon] Item Name      x5    │  ← 200x60 pixels
└─────────────────────────────┘
```

**RectTransform Positions:**
- **Icon**: X=10, Y=-10, W=40, H=40
- **NameText**: X=60, Y=-10, W=120, H=20
- **QuantityText**: X=-10, Y=-10, W=30, H=20 (anchored top-right)

---

## 🚨 Common Mistakes

❌ **Forgot to add components to Player prefab**
✅ Add `PlayerCurrency` and `PlayerInventory`

❌ **ItemSlot prefab missing ItemSlotUI component**
✅ Add component and assign references

❌ **InventoryPanel active by default**
✅ Set to inactive, only show when button clicked

❌ **ItemData not in Resources/Items/**
✅ Move ItemData assets to `Resources/Items/` folder

❌ **Button not wired**
✅ Connect InventoryButton to `OpenInventory()`

---

## 📝 ItemData Quick Reference

**Create Item:**
- Right-click in `Resources/Items/` → **Create → Game → Item**

**Required Fields:**
- **Item ID**: Unique string (e.g., `"gold_key"`)
- **Display Name**: What players see
- **Icon**: Sprite image

**Optional:**
- **Stackable**: `true` for materials, `false` for keys
- **Description**: Tooltip text

---

## ✅ Testing Checklist

- [ ] Player has both components
- [ ] ItemData assets created
- [ ] ItemSlot prefab created
- [ ] InventoryUI configured
- [ ] Button wired
- [ ] Collectible has ItemData assigned
- [ ] Inventory shows items
- [ ] Save/Load works

---

**Need more detail?** See `SETUP_GUIDE.md` for complete instructions.
