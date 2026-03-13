# Space Engineers Modding - AI Assistant Guide

## Project Overview
This workspace contains Space Engineers mods. For the **InfoLCD Apex LCD mod**, see [Mods/InfoLCD - Apex Update/CLAUDE.md](Mods/InfoLCD - Apex Update/CLAUDE.md) for mod-specific patterns, screen inventory, and scrolling implementation details.

Specific mod details and active projects can be found in the respective mod folders.

## Mod Making Notes

[MOD_MAKING_NOTES.md](MOD_MAKING_NOTES.md) is the central notes file for all mods in this workspace. **Everything gets written down here.**

**What it contains:**
- Per-mod sections for implementation notes, decisions, and lessons learned
- InfoLCD scrolling feature — full evaluation (screen compatibility) and implementation reference (code templates, config patterns, known issues)
- Mod Adjuster mods list with balance notes
- Sturmgrenadier Core mods list and notes

**How to use it:**
- When implementing a new feature (e.g., scrolling on a new screen), record the approach, any issues hit, and how they were solved
- When making a design decision (e.g., why a screen was skipped for scrolling), write it down so future-you knows why
- Each mod has its own section — add sub-sections as needed
- Keep implementation status tables up to date (e.g., mark screens as ✅ Done when complete)
- AI assistants should read this file when working on any mod to get full context on past decisions

## Space Engineers Mod SDK Location
The official Space Engineers Mod SDK is located at:
```
D:\SteamLibrary\steamapps\common\SpaceEngineersModSDK\
```

### SDK Structure & Contents

**Bin64_Profile/** - Game DLLs with XML Documentation
- `Sandbox.Common.dll/.xml` - Core game systems and utilities
- `Sandbox.Game.dll/.xml` - Main game logic, blocks, and gameplay systems
- `SpaceEngineers.Game.dll/.xml` - Space Engineers-specific implementations
- `SpaceEngineers.ObjectBuilders.dll/.xml` - Block, item, and component definitions
- `VRage.Game.dll/.xml` - VRage game engine core functionality
- `VRage.Math.dll/.xml` - Mathematics library (vectors, matrices, etc.)
- `VRage.Scripting.dll/.xml` - Scripting system and sandboxing
- `VRage.Library.dll/.xml` - Core utilities and extensions

**OriginalContent/** - Vanilla Game Assets
- `Models/Items/` - Item 3D models and definitions (e.g., SeedPack_fruit.xml, FoodContainer_mushroom.xml)
- `Models/Cubes/` - Block 3D models and structural definitions
- `Models/Components/` - Component models (displays, buttons, etc.)
- `Materials/` - Material definitions and textures

**Tools/** - Modding Utilities
- `VRageEditor/` - Suite of editing tools
  - `ModelViewer.bat` - View and inspect 3D models
  - `AnimationController.bat` - Animation editing
  - `BehaviorTree.bat` - AI behavior editing
  - `VisualScripting.bat` - Visual scripting editor

### How to Use the SDK

**1. API Reference (IntelliSense)**
- Reference DLLs from `Bin64_Profile/` in your Visual Studio project
- XML files provide full IntelliSense documentation
- Example: Reference `Sandbox.Game.dll` to get autocomplete for `IMyTerminalBlock`, `MyTextSurfaceScriptBase`, etc.

**2. Source Code Study (Decompilation)**
- Use tools like dnSpy or ILSpy to decompile DLLs
- Study vanilla implementations of text surface scripts, block logic, inventory systems
- Understand internal APIs and best practices from Keen's own code
- Reference implementations help debug complex interactions

**3. Asset Structure Reference**
- Examine XML model definitions in `OriginalContent/Models/` to understand item structure
- Check how Keen defines items you work with (seeds, food containers, components)
- Use as templates for custom items or to understand item properties

**4. Testing & Validation**
- Use `ModelViewer.bat` to preview 3D assets
- Test custom models before deployment
- Verify LOD (Level of Detail) configurations

### SDK Best Practices
- **Always check SDK first** when implementing new features - don't reinvent the wheel
- **Decompile sparingly** - Start with XML docs, only decompile when you need implementation details
- **Version compatibility** - SDK version should match your game version for accurate reference
- **Keep SDK updated** - Steam auto-updates keep SDK in sync with game patches

## Key Modding Resources

### Official Documentation
- **Primary Reference**: [Space Engineers Modding Wiki](https://spaceengineers.wiki.gg/wiki/Modding/Reference)
- **API Documentation**: [Space Engineers Mod API Reference](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html)
- **SDK Files**: Use local SDK folder for decompiled source and implementation examples

### Important Modding Concepts

#### Programmable Blocks vs Text Surface Scripts
- **Programmable Blocks**: User-editable C# scripts with ingame API (sandboxed)
- **Text Surface Scripts**: Mod-provided scripts that run directly on LCD screens (full API access)
  - Inherit from `MyTextSurfaceScriptBase`
  - NO access to Programmable Block ingame API
  - Full access to game internals (blocks, inventories, components)

#### Key Namespaces & Classes
**Core Interfaces:**
- `IMyTerminalBlock` - Base interface for all controllable blocks
- `IMyTextSurface` - LCD/screen rendering interface
- `IMyInventory` - Inventory access
- `IMyCubeGrid` - Grid/ship structure
- `IMyPowerProducer` - Power generation blocks
- `IMyBatteryBlock` - Battery storage and I/O

**Mod API:**
- `Sandbox.ModAPI` - Full modding API
- `VRage.Game.ModAPI` - Core game objects
- `Sandbox.Game.EntityComponents` - Block components (e.g., `MyResourceSourceComponent`)
- `MyTextSurfaceScriptBase` - Base class for LCD scripts

**Drawing & UI:**
- `MySpriteDrawFrame` - Sprite rendering
- `MySprite` - Individual drawable elements
- `Color` - VRageMath color system

#### Common Modding Patterns

**1. Block Discovery & Filtering**
```csharp
// Get all blocks from grid
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
grid.GetFatBlocks(blocks);

// Filter by type
List<IMyPowerProducer> powerBlocks = new List<IMyPowerProducer>();
grid.GetFatBlocks(powerBlocks);
```

**2. Inventory Scanning**
```csharp
// Pattern: Iterate inventories and parse item types
var typeId = item.Type.TypeId.Split('_')[1];  // "MyObjectBuilder_Component" → "Component"
var subtypeId = item.Type.SubtypeId;           // Specific item name
var amount = item.Amount.ToIntSafe();          // Convert VRage.MyFixedPoint to int
```

**3. Configuration via CustomData**
```csharp
// Pattern: Use MyIni parser for block CustomData
MyIni config = new MyIni();
config.TryParse(terminalBlock.CustomData, "SectionName", out result);
bool value = config.Get("SectionName", "Key").ToBoolean();
```

**4. Scrolling Lists**
```csharp
// Pattern: Calculate available space from current position
float currentY = position.Y - surfaceData.viewPortOffsetY;
float remainingHeight = screenHeight - currentY;
int availableLines = Math.Max(1, (int)(remainingHeight / lineHeight));
int startIndex = (scrollOffset % totalItems + totalItems) % totalItems;
```

**5. Subgrid Caching**
```csharp
// Pattern: Cache subgrid scans to reduce performance impact
if (scanSubgrids && tick >= updateFrequency) {
    // Full scan: main + subgrids
    var allBlocks = GetBlocks(grid, includeSubgrids: true);
    // Cache only subgrid portion
    subgridCache = allBlocks.Except(mainBlocks);
}
// Merge main (always fresh) + cached subgrids
blocks = mainBlocks.Concat(subgridCache);
```

#### Known Quirks & Gotchas

**Item Type Collision**
- Items can share SubtypeIds but have different TypeIds
- Example: `ConsumableItem_Fruit` vs `SeedItem_Fruit`
- **Solution**: Use composite key `"{typeId}_{subtypeId}"` for dictionaries

**VRage.MyFixedPoint**
- Inventory amounts are `MyFixedPoint`, not `int` or `float`
- Use `.ToIntSafe()` extension method for conversion
- Required for any arithmetic operations

**Block Components**
- Access via `block.Components.Get<ComponentType>()`
- Example: `MyResourceSourceComponent` for power generation details
- Returns null if component doesn't exist on block

**Dedicated Server Performance**
- Always check: `if (MyAPIGateway.Utilities?.IsDedicated ?? false) return;`
- Text surface scripts should not run on dedicated servers
- Prevents server performance issues

#### Typical Mod File Structure
```
ModFolder/
├── Data/
│   ├── Scripts/
│   │   └── YourNamespace/
│   │       └── YourScripts.cs
│   ├── CubeBlocks/          # Block definitions (optional)
│   ├── TextSurfaceScripts.sbc   # LCD script registration (if applicable)
│   └── *.sbc                # Other definition files
├── Models/                  # 3D models (optional)
├── Textures/                # Textures (optional)
├── metadata.mod             # Mod metadata
└── thumb.png               # Workshop thumbnail
```

#### Debugging & Testing
- Use `MyLog.Default.WriteLine()` for logging
- Logs appear in Space Engineers log files
- Test in Creative mode with various LCD sizes
- Check both compact (corner LCD) and standard modes
- Test with hidden categories and scrolling edge cases

#### Workshop Publishing
- Ensure `metadata.mod` has correct ModId and version
- Test with clean game install (no other mods)
- Verify performance with many blocks/items
- Include clear usage instructions for CustomData configuration

## Development Notes

### ⚠️ Public Release Considerations (Steam Workshop)

- **Backward Compatibility is CRITICAL**: Users have existing save games and configurations
- **Never break existing CustomData**: Add new config options, don't remove or rename existing ones
- **Support old config formats**: Check for both old and new config keys when updating
- **Graceful degradation**: New features should be optional; old saves should continue working
- **Test with existing configs**: Before releasing, verify old CustomData configurations still work
- **Version documentation**: Note breaking changes clearly in mod descriptions

### Coding Principles
1. **Backward Compatibility**: Never break existing user configurations or save games
2. **Performance First**: Cache subgrid scans, minimize LINQ
3. **Null Safety**: Always check block/component existence
4. **Explicit Config**: Never rely on default values for critical settings
5. **Position-Based Layout**: Calculate remaining space for proper multi-section displays
6. **Defensive Parsing**: Try/catch around DetailedInfo parsing and type conversions

## Additional Resources

Project-specific documentation and implementation details can be found in individual mod folders.
