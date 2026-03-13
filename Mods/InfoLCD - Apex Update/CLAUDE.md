# InfoLCD - Apex LCD Mod - AI Assistant Guide

## Mod Overview
InfoLCD is a Space Engineers **text surface script mod** that adds information LCD screens to the game. It comes in two versions that must always be kept in sync:

- **InfoLCD - Apex Update** — For the Apex Update modpack  
- **InfoLCD - Apex Advanced** — For the Apex Advanced modpack

Both versions share identical logic. **Every change must be made to both files.**

> For general Space Engineers modding guidance (SDK, API, common patterns, namespaces), see the root [CLAUDE.md](../../CLAUDE.md).

---

## File Locations

Scripts live at:
```
Mods/InfoLCD - Apex Update/Data/Scripts/SG/MahLCDs_Summary_[ScreenName].cs
Mods/InfoLCD - Apex Advanced/Data/Scripts/SG/MahLCDs_Summary_[ScreenName].cs
```

---

## Screen Inventory

### ✅ Scrolling Implemented
| Screen | Approach | Notes |
|--------|----------|-------|
| Items | Approach 1 | Composite key fix for item type collisions |
| Power | Approach 2 | MaxListLines, space calculation fix |
| Components | Approach 1 | |
| Ingots | Approach 1 | |
| Ores | Approach 1 | |
| Ammo | Approach 1 | |
| DoorMonitor | Approach 1 | ✅ TESTED & WORKING |
| DamageMonitor | Approach 1 | |
| Production | Approach 2 | MaxListLines |
| GasProduction | Approach 2 | MaxListLines |
| Cargo | Approach 1 | No MaxListLines — position-based space calc |
| LifeSupport | Approach 1 | Only air vents scroll; fixed bars stay pinned |

### ⏳ Scrolling Pending
- **Container** — Similar structure to Cargo
- **Farming** — Farm plot/irrigation detail lists

### ⏸ Scrolling Skipped (not suitable)
- **Weapons** — Complex badge-style visual layout
- **Systems** — Fixed dashboard design
- **GridInfo** — Stats only, no lists
- **DetailedInfo** — Single block focus
- **AirlockMonitor** — Pending review (likely too few items)

See [MOD_MAKING_NOTES.md](../../MOD_MAKING_NOTES.md) for full scrolling compatibility analysis, implementation reference, code templates, and user documentation.

---

## Key Architecture

### Dual-Version Rule
Every code change must be made to **both** files simultaneously:
- `InfoLCD - Apex Update/Data/Scripts/SG/MahLCDs_Summary_[Name].cs`
- `InfoLCD - Apex Advanced/Data/Scripts/SG/MahLCDs_Summary_[Name].cs`

The Update version sometimes uses `ConfigHelpers.AppendScrollingConfig(sb)` for config output; the Advanced version always uses inline `sb.AppendLine(...)`. Check the existing CreateConfig() pattern in each file before adding new config fields.

### Update10 = +10 (Critical)
All InfoLCD screens use `ScriptUpdate.Update10`. The scroll counter must always increment by **10** per call:
```csharp
ticksSinceLastScroll += 10;  // NOT ticksSinceLastScroll++
```
This keeps `scrollSpeed=60` meaning ~1 real second regardless of update frequency. **Never copy `++` from other code without checking.**

### Config Pattern
All config uses `MyIni` parser with a per-screen section constant (`CONFIG_SECTION_ID`). Scrolling fields always go **last** in the config section, after all screen-specific options. Section header format:
```
; [ SCREENNAME - SCROLLING OPTIONS ]
```

### Position-Based Space Calculation
When calculating available lines for a scrolling list, always measure from the **current drawing position**, not from screen top:
```csharp
float screenHeight = mySurface.SurfaceSize.Y;
float lineHeight = 30 * surfaceData.textSize;
float currentY = position.Y - surfaceData.viewPortOffsetY;
float remainingHeight = screenHeight - currentY;
int availableLines = Math.Max(1, (int)(remainingHeight / lineHeight));
```

---

## Scrolling Approaches

### Approach 1 — Flat List (no MaxListLines)
Used for screens with a single scrolling list (Items, Cargo, LifeSupport air vents, etc.)

**State fields:**
```csharp
bool toggleScroll = false;
bool reverseDirection = false;
int scrollSpeed = 60;
int scrollLines = 1;
int scrollOffset = 0;
int ticksSinceLastScroll = 0;
```

**Run() scroll update:**
```csharp
if (toggleScroll)
{
    ticksSinceLastScroll += 10;
    if (ticksSinceLastScroll >= scrollSpeed)
    {
        ticksSinceLastScroll = 0;
        if (reverseDirection) scrollOffset -= scrollLines;
        else scrollOffset += scrollLines;
    }
}
else { scrollOffset = 0; ticksSinceLastScroll = 0; }
```

**Drawing with wraparound:**
```csharp
int totalItems = itemList.Count;
int startIndex = toggleScroll && totalItems > 0
    ? ((scrollOffset % totalItems) + totalItems) % totalItems
    : 0;

int linesDrawn = 0;
for (int i = 0; i < totalItems && linesDrawn < availableLines; i++)
{
    int idx = (startIndex + i) % totalItems;
    // Draw itemList[idx]
    linesDrawn++;
}
```

### Approach 2 — Multi-Category with MaxListLines
Used for screens with multiple distinct categories (Power, Production, GasProduction).

Adds one extra state field:
```csharp
int maxListLines = 5;  // 0 = unlimited
```

When drawing each category, apply the cap after calculating position-based space:
```csharp
if (maxListLines > 0)
    availableLines = Math.Min(availableLines, maxListLines);
```

---

## Common Gotchas

### Item Type Collision
Items can share SubtypeIds but have different TypeIds (e.g., `ConsumableItem_Fruit` vs `SeedItem_Fruit`). Always use a composite key:
```csharp
string key = $"{typeId}_{subtypeId}";
```

### Multi-Category Screen Position
Each category must calculate remaining space from its own current Y position, not from screen top. Hidden/empty categories change how much space is actually available when the next category starts drawing.

### Dedicated Server Guard
Text surface scripts must bail out on dedicated servers — they should not be running LCD drawing logic there:
```csharp
if (MyAPIGateway.Utilities?.IsDedicated ?? false) return;
```
