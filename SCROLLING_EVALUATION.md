# Scrolling Feature Compatibility Analysis

## Executive Summary
This document evaluates which remaining InfoLCD screens are compatible with the two implemented scrolling approaches.

---

## ✅ UNIFIED LIST SCROLLING (Approach 1)

### Compatible Screens
Screens with **single scrolling lists** where all items share the same scroll offset.

#### 1. **DoorMonitor** - HIGHLY COMPATIBLE ⭐
- **Structure**: Single list of doors with status indicators
- **Why it works**: Simple door list, each door = one line
- **Implementation**: Straightforward list scrolling
- **User benefit**: View status of many doors across large ships

#### 2. **DamageMonitor** - PARTIALLY COMPATIBLE ⚠️
- **Structure**: List of damaged blocks grouped by category
- **Complexity**: Has category headers but primarily a list
- **Why it works**: Can scroll through damaged blocks
- **Consideration**: Category headers might scroll away (acceptable)
- **User benefit**: Track damage across large battle ships

---

## ✅ MULTI-CATEGORY WITH MAXLISTLINES (Approach 2)

### Compatible Screens
Screens with **multiple distinct categories** needing independent space management.

#### 3. **Production** - HIGHLY COMPATIBLE ⭐ ✅ COMPLETE
- **Structure**: Multiple categories
  - Refineries (show queue, active/total)
  - Assemblers (show queue, active/total) 
  - Generators (active/total)
  - Oxygen Farms (active/total)
  - Food Processors (active/total)
  - Irrigation Systems (active/total)
  - Algae Farms (active/total)
- **Why it works**: Similar to Power screen with multiple production types
- **Implementation**: Each category gets MaxListLines limit, all share scroll offset
- **User benefit**: Balance viewing multiple production types vs depth per type

#### 4. **GasProduction** - HIGHLY COMPATIBLE ⭐
- **Structure**: Multiple categories
  - Hydrogen bar
  - Oxygen bar
  - Ice bar
  - Generators list (multiple generators with status)
  - Oxygen Farms list (multiple farms with status)
- **Why it works**: Fixed bars at top + scrolling lists below
- **Implementation**: Bars always visible, generator/farm lists scroll with MaxListLines
- **User benefit**: See all gas types + scroll through many generators

#### 5. **LifeSupport** - HIGHLY COMPATIBLE ⭐
- **Structure**: Multiple categories
  - Battery bars (multiple batteries)
  - Hydrogen tanks bar
  - Oxygen tanks bar
  - Ice inventory bar
  - Air vents status
- **Why it works**: Multiple resource bars + lists
- **Implementation**: Each category limited by MaxListLines, shared scroll
- **User benefit**: Balance viewing multiple life support systems

#### 6. **Cargo** - HIGHLY COMPATIBLE ⭐
- **Structure**: Multiple cargo categories
  - Ore
  - Ingots
  - Components
  - ProtoComponents
  - Ammo (multiple types)
  - HandAmmo
  - Weapons (Rifle, Pistol, Launcher)
  - Tools
  - Kits
  - Bottles
  - Food (RawFood, CookedFood)
  - Drinks
  - Seeds
  - Miscellaneous
- **Why it works**: Many categories, user wants overview of all types
- **Implementation**: MaxListLines prevents any one category from dominating
- **User benefit**: See status across all cargo types, scroll within each category

#### 7. **Container** - HIGHLY COMPATIBLE ⭐
- **Structure**: List of containers with capacity bars
  - Production containers
  - Storage containers
  - Each shows fill level bar + current/max volume
- **Why it works**: List of container blocks, similar to Power's battery list
- **Implementation**: MaxListLines limits containers shown, scroll cycles through all
- **User benefit**: Monitor all containers without one dominating the screen

#### 8. **Farming** - HIGHLY COMPATIBLE ⭐  
- **Structure**: Multiple farming categories
  - Summary (working/total farms and irrigation)
  - Ice bar
  - Water bar  
  - Water production rate
  - Farm plot details (if space)
  - Irrigation details (if space)
- **Why it works**: Fixed summary + bars + scrolling details
- **Implementation**: Summary/bars always visible, detail lists scroll with MaxListLines
- **User benefit**: Overview + ability to see many individual farm plots

---

## ❌ NOT SUITABLE FOR SCROLLING

### Screens that should NOT have scrolling

#### 9. **Weapons** - NOT SUITABLE
- **Structure**: Complex multi-section layout
  - Badge-style status indicators (using centered text within brackets)
  - Weapon categories have different visual styles
  - Detailed ammo breakdowns per weapon type
  - Mixed content: totals, status badges, inventory counts
- **Why it doesn't work**: Content is designed for visual overview, not linear scrolling
- **Alternative**: Current design works well for weapons overview

#### 10. **Systems** - NOT SUITABLE
- **Structure**: Fixed dashboard layout
  - Grid integrity summary
  - Categorical damage overview (Movement, Communications, Tanks, Power, etc.)
  - Designed for at-a-glance status check
- **Why it doesn't work**: Dashboard design meant to show all categories at once
- **Alternative**: Current design optimal for ship status overview

#### 11. **GridInfo** - NOT SUITABLE
- **Structure**: Stats dashboard
  - Grid name
  - Mass (base + cargo)
  - Block count
  - PCU usage
  - Grid type (ship/station)
- **Why it doesn't work**: Few lines of summary stats, no lists to scroll
- **Alternative**: No benefit from scrolling

#### 12. **AirlockMonitor** - MAYBE NOT SUITABLE ⚠️
- **Structure**: Airlock status display
  - Lists airlocks with door states
  - Pressurization status
- **Why uncertain**: Depends on implementation depth
- **Consideration**: If it's just a few airlocks = no scrolling needed
- **If many airlocks**: Could use Unified List Scrolling (Approach 1)
- **Recommendation**: Review implementation before deciding

#### 13. **DetailedInfo** - NOT SUITABLE
- **Structure**: Detailed block information display
  - Shows DetailedInfo property of a single block
  - Block-specific statistics and status
- **Why it doesn't work**: Single block focus, not a list
- **Alternative**: Not designed for scrolling

---

## Implementation Priority Recommendations

### HIGH PRIORITY (Maximum User Benefit)
1. **DoorMonitor** (Approach 1) - Essential for large ships with many doors
2. **Production** (Approach 2) - Critical for factory ships with many machines
3. **Cargo** (Approach 2) - Essential for cargo ships with diverse storage

### MEDIUM PRIORITY (Significant Benefit)
4. **GasProduction** (Approach 2) - Useful for ships with many generators
5. **Container** (Approach 2) - Useful for organized storage systems
6. **LifeSupport** (Approach 2) - Helpful for life support monitoring

### LOW PRIORITY (Nice to Have)
7. **Farming** (Approach 2) - Helpful for dedicated farming ships
8. **DamageMonitor** (Approach 1) - Useful during/after combat

### SKIP
- Weapons (complex layout)
- Systems (dashboard design)
- GridInfo (stats only)
- DetailedInfo (single block)
- AirlockMonitor (pending review - likely few items)

---

## Technical Notes

### Approach 1 (Unified List) Implementation Notes:
- Simpler than Approach 2
- All items scroll together as one list
- Best for screens with homogeneous content
- Examples: DoorMonitor, potentially DamageMonitor

### Approach 2 (Multi-Category with MaxListLines) Implementation Notes:
- More complex but more powerful
- Each category calculates remaining space from current position
- MaxListLines config prevents category domination
- All categories share same scroll offset
- Critical for screens with multiple distinct sections
- Examples: Production, GasProduction, LifeSupport, Cargo, Container, Farming

### Common Implementation Pattern:
Both approaches share:
- Config fields: `toggleScroll`, `reverseDirection`, `scrollSpeed`, `scrollLines`
- Approach 2 adds: `maxListLines` (default 5, 0 = unlimited)
- Config helper: `ConfigHelpers.AppendScrollingConfig(sb)`
- Scroll update logic in `Run()` method
- Wraparound list drawing in drawing method

---

## Summary Table

| Screen | Approach | Priority | Benefit | Notes |
|--------|----------|----------|---------|-------|
| DoorMonitor | 1 (Unified List) | HIGH | Essential | Large ships with many doors |
| DamageMonitor | 1 (Unified List) | LOW | Helpful | Combat damage tracking |
| Production | 2 (Multi-Category) | HIGH | Critical | Factory ships |
| GasProduction | 2 (Multi-Category) | MEDIUM | Significant | Many generators |
| LifeSupport | 2 (Multi-Category) | MEDIUM | Significant | Life support systems |
| Cargo | 2 (Multi-Category) | HIGH | Essential | Diverse cargo |
| Container | 2 (Multi-Category) | MEDIUM | Significant | Organized storage |
| Farming | 2 (Multi-Category) | LOW | Helpful | Farming operations |
| Weapons | ❌ | SKIP | N/A | Complex layout |
| Systems | ❌ | SKIP | N/A | Dashboard design |
| GridInfo | ❌ | SKIP | N/A | Stats only |
| DetailedInfo | ❌ | SKIP | N/A | Single block |
| AirlockMonitor | ❓ | PENDING | Unknown | Review needed |

---

## Next Steps

1. **Implement High Priority Screens First**
   - DoorMonitor (Approach 1)
   - Production (Approach 2)
   - Cargo (Approach 2)

2. **Test and Validate**
   - Verify scrolling works with hidden categories
   - Confirm MaxListLines behavior
   - Test edge cases (0 or 1 items per category)

3. **Move to Medium Priority**
   - GasProduction, Container, LifeSupport

4. **Consider Low Priority Based on User Feedback**
   - Farming, DamageMonitor

5. **Review AirlockMonitor Implementation**
   - Determine if scrolling is beneficial
   - Choose approach if applicable
