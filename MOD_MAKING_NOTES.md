# Space Engineers - Mod Making Notes

Consolidated notes for all mods in this workspace. See individual mod CLAUDE.md files for AI assistant context.

---

## Table of Contents
- [InfoLCD - Apex Update / Apex Advanced](#infolcd---apex-update--apex-advanced)
  - [Scrolling Feature - Evaluation](#scrolling-feature---evaluation)
  - [Scrolling Feature - Implementation Reference](#scrolling-feature---implementation-reference)
- [Mod Adjuster Mods](#mod-adjuster-mods)
- [Sturmgrenadier Core Mods](#sturmgrenadier-core-mods)

---

## InfoLCD - Apex Update / Apex Advanced

Both versions are always kept in sync — every change applies to both. See `Mods/InfoLCD - Apex Update/CLAUDE.md` for the AI assistant context file.

---

### Scrolling Feature - Evaluation

Analysis of which screens are compatible with each scrolling approach.

#### ✅ UNIFIED LIST SCROLLING (Approach 1)
Screens with a single scrolling list where all items share the same scroll offset.

| Screen | Compatibility | Status | Notes |
|---|---|---|---|
| **DoorMonitor** | ⭐ Highly Compatible | ✅ Done | Simple door list, each door = one line |
| **DamageMonitor** | ⚠️ Partially | ✅ Done | Category headers scroll away — acceptable |

#### ✅ MULTI-CATEGORY WITH MAXLISTLINES (Approach 2)
Screens with multiple distinct categories needing independent space management.

| Screen | Compatibility | Status | Notes |
|---|---|---|---|
| **Production** | ⭐ Highly Compatible | ✅ Done | Refineries, assemblers, generators, farms, etc. Each category gets MaxListLines limit |
| **GasProduction** | ⭐ Highly Compatible | ✅ Done | Fixed bars (H2/O2/Ice) always visible; generator/farm lists scroll with MaxListLines |
| **LifeSupport** | ⭐ Highly Compatible | ✅ Done | Fixed bars (Battery/O2/H2/Ice) stay pinned; air vents list scrolls. No MaxListLines — position-based space calc |
| **Cargo** | ⭐ Highly Compatible | ✅ Done | Many categories (Ore, Ingots, Components, Ammo, Food, Seeds, etc.). Flat category list; no MaxListLines — position-based space calc |
| **Container** | ⭐ Highly Compatible | ⏳ Pending | List of containers with capacity bars, similar structure to Cargo |
| **Farming** | ⭐ Highly Compatible | ⏳ Pending | Fixed summary + bars + scrolling detail lists (farm plots, irrigation) |

#### ❌ NOT SUITABLE FOR SCROLLING

| Screen | Reason |
|---|---|
| **Weapons** | Complex badge-style visual layout, not a linear list |
| **Systems** | Fixed dashboard design — show all categories at once by design |
| **GridInfo** | Summary stats only, no lists |
| **DetailedInfo** | Single block focus, not a list |
| **AirlockMonitor** | Typically too few items to need scrolling |

#### Approach 1 vs Approach 2 Decision Guide

**Use Unified List (Approach 1) when:**
- Screen shows one primary list (Items, Components, Ingots, etc.)
- All items are the same type/category
- Screen space is dedicated to that single list

**Use Multi-Category with MaxListLines (Approach 2) when:**
- Screen has multiple distinct categories (batteries, solar, wind, reactors)
- Each category needs a separate section with a header
- Need to balance viewing multiple categories vs depth per category
- Users may have dozens of items per category

---

### Scrolling Feature - Implementation Reference

#### Implementation Status

| Screen | Approach | Status |
|---|---|---|
| Items | Approach 1 | ✅ Completed & Tested |
| Power | Approach 2 | ✅ Completed & Tested |
| Components | Approach 1 | ✅ Done |
| Ingots | Approach 1 | ✅ Done |
| Ores | Approach 1 | ✅ Done |
| Ammo | Approach 1 | ✅ Done |
| DoorMonitor | Approach 1 | ✅ Done |
| DamageMonitor | Approach 1 | ✅ Done |
| Production | Approach 2 | ✅ Done |
| GasProduction | Approach 2 | ✅ Done |
| Cargo | Approach 2 (no MaxListLines) | ✅ Done |
| LifeSupport | Approach 2 (no MaxListLines) | ✅ Done |
| Container | Approach 2 | ⏳ Pending |
| Farming | Approach 2 | ⏳ Pending |
| Weapons | — | ⏸ Skipped |
| Systems | — | ⏸ Skipped |
| GridInfo | — | ⏸ Skipped |
| DetailedInfo | — | ⏸ Skipped |
| AirlockMonitor | — | ⏸ Skipped |

---

#### User Guide: Enabling Scrolling on LCD Blocks

**Step 1:** Open the LCD block → Script dropdown → Select InfoLCD script.

**Step 2:** Open CustomData → find the scrolling section (e.g., `; [ POWER - SCROLLING OPTIONS ]`) → edit parameters.

**Step 3:** Config templates:

```ini
; Fast scrolling (~0.5 sec per step)
ToggleScroll=True
ScrollSpeed=30
ScrollLines=1
MaxListLines=5

; Slow scrolling (~2 sec per step)
ToggleScroll=True
ScrollSpeed=120
ScrollLines=1
MaxListLines=5

; Show more per category
ToggleScroll=True
ScrollSpeed=60
ScrollLines=1
MaxListLines=10

; No list limit (use all available space)
ToggleScroll=True
ScrollSpeed=60
ScrollLines=1
MaxListLines=0

; Disable scrolling
ToggleScroll=False
```

**Single List Screens (Approach 1):** Items, Components, Ingots, Ores, Ammo, DoorMonitor, DamageMonitor — no `MaxListLines`.

**Multi-Category Screens (Approach 2):** Power, Production, GasProduction, Cargo, LifeSupport — `MaxListLines` limits items shown per category.

---

#### Code Implementation Reference

**Approach 1: Unified List Scrolling**

State fields:
```csharp
bool toggleScroll = false;
bool reverseDirection = false;
int scrollSpeed = 60;
int scrollLines = 1;
int scrollOffset = 0;
int ticksSinceLastScroll = 0;
```

Config loading:
```csharp
if (config.ContainsKey(CONFIG_SECTION_ID, "ToggleScroll"))
    toggleScroll = config.Get(CONFIG_SECTION_ID, "ToggleScroll").ToBoolean(false);
if (config.ContainsKey(CONFIG_SECTION_ID, "ReverseDirection"))
    reverseDirection = config.Get(CONFIG_SECTION_ID, "ReverseDirection").ToBoolean(false);
if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollSpeed"))
    scrollSpeed = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollSpeed").ToInt32(60));
if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollLines"))
    scrollLines = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollLines").ToInt32(1));
```

Scroll update in Run() — **use `+= 10` for Update10 screens**:
```csharp
if (toggleScroll)
{
    ticksSinceLastScroll += 10;  // Update10 = 10 game ticks per call
    if (ticksSinceLastScroll >= scrollSpeed)
    {
        scrollOffset += reverseDirection ? -scrollLines : scrollLines;
        ticksSinceLastScroll = 0;
    }
}
else { scrollOffset = 0; }
```

Drawing with wraparound:
```csharp
int totalItems = itemList.Count;
int normalizedOffset = ((scrollOffset % totalItems) + totalItems) % totalItems;

for (int i = 0; i < totalItems && linesDrawn < availableLines; i++)
{
    int itemIndex = (normalizedOffset + i) % totalItems;
    // Draw itemList[itemIndex]
    linesDrawn++;
}
```

---

**Approach 2: Multi-Category Scrolling with MaxListLines**

Additional state field:
```csharp
int maxListLines = 5;
```

Additional config loading:
```csharp
if (config.ContainsKey(CONFIG_SECTION_ID, "MaxListLines"))
    maxListLines = Math.Max(0, config.Get(CONFIG_SECTION_ID, "MaxListLines").ToInt32(5));
```

Drawing each category:
```csharp
// Calculate available lines from CURRENT position (not screen top)
float screenHeight = mySurface.SurfaceSize.Y;
float lineHeight = 30 * surfaceData.textSize;
float currentY = position.Y - surfaceData.viewPortOffsetY;
float remainingHeight = screenHeight - currentY;
int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

// Apply user-configured max list lines (0 = no limit)
if (maxListLines > 0)
    availableDataLines = Math.Min(availableDataLines, maxListLines);

// Apply scrolling
int totalDataLines = categoryItems.Count;
int startIndex = 0;
if (toggleScroll && totalDataLines > 0)
{
    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
    startIndex = normalizedOffset;
}

int linesDrawn = 0;
for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
{
    int itemIndex = (startIndex + i) % totalDataLines;
    // Draw categoryItems[itemIndex]
    position += surfaceData.newLine;
    linesDrawn++;
}
```

---

#### Known Issues & Solutions

**Update10 Scroll Speed Bug**
- **Problem:** Using `ticksSinceLastScroll++` on an Update10 screen makes `scrollSpeed=60` take ~10 seconds instead of ~1 second.
- **Root Cause:** Update10 fires once every 10 game ticks. Incrementing by 1 means you're measuring calls, not ticks.
- **Fix:** Always increment by the tick interval: `ticksSinceLastScroll += 10` for Update10, `+= 100` for Update100, `+= 1` for Update1.

**Multi-Category Space Calculation**
- **Problem:** Categories calculating space from screen top caused overlapping content.
- **Fix:** Calculate remaining space from current drawing position: `float currentY = position.Y - surfaceData.viewPortOffsetY;`

**Item Type Collisions in Items Screen**
- **Problem:** Items with same SubtypeId but different TypeId (e.g., `ConsumableItem_Fruit` vs `SeedItem_Fruit`) caused doubled counts.
- **Fix:** Use composite keys: `string cargoKey = $"{typeId}_{subtypeId}";`

---

#### ConfigHelpers.AppendScrollingConfig() Reference

```csharp
public static void AppendScrollingConfig(
    StringBuilder sb,
    bool toggleScroll = false,
    bool reverseDirection = false,
    int scrollSpeed = 5,
    int scrollLines = 1,
    int maxListLines = 5  // Only for multi-category screens
)
```

CustomData options:
- `ToggleScroll` — Enable/disable (default: false)
- `ReverseDirection` — Scroll direction (default: false = up)
- `ScrollSpeed` — Ticks between scrolls (60 ≈ 1 second)
- `ScrollLines` — Lines per scroll step (default: 1)
- `MaxListLines` — Max items per category (default: 5, 0 = unlimited) *[Multi-category only]*

---

## Mod Adjuster Mods

Mods in the `[Mod Name] [Mod Adjuster For SG]` pattern are balance/compatibility adjustments for third-party mods to work with the Sturmgrenadier core mods.

| Mod | Notes |
|---|---|
| Artillery MKII Turret - Goliath | Weapon balance adjustment |
| Dense Colorable Solar Panels | Power output adjustment |
| Federal Industrial - Utilities | Compatibility patch |
| Isy's Dense Solar Panels | Power output adjustment |
| Life'Tech - Algaetechnology | Farming/resource adjustment |
| ModCubeBlocks Refinery x10 | Refinery speed adjustment |
| ModCubeBlocks Upgrade Module | Module balance adjustment |
| More Engineer Characters | Character mod compatibility |
| More Wind Turbines | Wind power output adjustment |
| [Mafoo] More Batteries | Battery capacity/charge rate adjustment |

---

## Sturmgrenadier Core Mods

The SG Core mods form the base gameplay overhaul. Individual notes go here as they accumulate.

| Mod | Purpose |
|---|---|
| Sturmgrenadier Core Mod | Base overhaul — core gameplay changes |
| Sturmgrenadier Core Power | Power system changes |
| Sturmgrenadier Core Production | Production/crafting changes |
| Sturmgrenadier Core Survival | Survival mechanic changes |
| Sturmgrenadier Core Vanilla Combat | Combat balance changes |
| Not Just For Looks | Cosmetic/decorative block changes |

---

*Add new mod-specific notes under their respective sections as they accumulate.*
