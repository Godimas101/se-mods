using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace MahrianeIndustries.LCDInfo
{
    [MyTextSurfaceScript("LCDInfoScreenProductionSummary", "$IOS LCD - Production")]
    public class LCDProductionSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsProductionStatus";

        string searchId = "*";
        bool showInactive = false;
        bool showRefineries = true;
        bool showAssemblers = true;
        bool showGenerators = true;
        bool showOxygenFarms = true;
    // New categories
    bool showFoodProcessors = true;
    bool showIrrigationSystems = true;
    bool showAlgaeFarms = true;

        // Scrolling state
        bool toggleScroll = false;
        bool reverseDirection = false;
        int scrollSpeed = 60;
        int scrollLines = 1;
        int maxListLines = 5;
        int scrollOffset = 0;
        int ticksSinceLastScroll = 0;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .45f : .35f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 104,
                ratioOffset = 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = true,
                showBars = true,
                showSubgrids = false,
                showDocked = false,
                useColors = true
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            config.Clear();

            // Build custom formatted config with section headers
            StringBuilder sb = new StringBuilder();
            
            // Always preserve existing CustomData (from other mods/apps)
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
            }

            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; [ PRODUCTION - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendExcludeIdsConfig(sb, excludeIds);
            ConfigHelpers.AppendShowHeaderConfig(sb, surfaceData.showHeader);
            ConfigHelpers.AppendShowSubgridsConfig(sb, surfaceData.showSubgrids);
            ConfigHelpers.AppendSubgridUpdateFrequencyConfig(sb, surfaceData.subgridUpdateFrequency);
            ConfigHelpers.AppendShowDockedConfig(sb, surfaceData.showDocked);
            ConfigHelpers.AppendUseColorsConfig(sb, surfaceData.useColors);

            sb.AppendLine();
            sb.AppendLine("; [ PRODUCTION - SCROLLING OPTIONS ]");
            sb.AppendLine($"ToggleScroll={toggleScroll}");
            sb.AppendLine("; Enable scrolling to view items that don't fit on screen");
            sb.AppendLine("; Set to 'true' to activate. Scrolling only occurs when there's overflow data.");
            sb.AppendLine();
            sb.AppendLine($"ReverseDirection={reverseDirection}");
            sb.AppendLine("; Scroll direction: 'false' scrolls up (bottom items appear), 'true' scrolls down (top items appear)");
            sb.AppendLine("; The list wraps around, so you'll eventually see all items in a continuous loop");
            sb.AppendLine();
            sb.AppendLine($"ScrollSpeed={scrollSpeed}");
            sb.AppendLine("; Time between scroll steps in ticks (60 ticks ≈ 1 second at normal game speed)");
            sb.AppendLine("; Lower = faster scrolling, Higher = slower scrolling");
            sb.AppendLine();
            sb.AppendLine($"ScrollLines={scrollLines}");
            sb.AppendLine("; Number of lines to scroll per step");
            sb.AppendLine("; Set to 1 for smooth scrolling, higher values for faster navigation");
            sb.AppendLine();
            sb.AppendLine($"MaxListLines={maxListLines}");
            sb.AppendLine("; Maximum number of items to display per category (e.g., max refineries shown at once)");
            sb.AppendLine("; Limits list length even if more screen space is available. Set to 0 to use all available space.");
            sb.AppendLine("; Useful for grids with many production blocks - shows a portion and scrolls through all items");
            sb.AppendLine();

            sb.AppendLine("; [ PRODUCTION - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            sb.AppendLine();
            sb.AppendLine("; [ PRODUCTION - SCREEN OPTIONS ]");
            sb.AppendLine($"ShowRefineries={showRefineries}");
            sb.AppendLine($"ShowAssemblers={showAssemblers}");
            sb.AppendLine($"ShowGenerators={showGenerators}");
            sb.AppendLine($"ShowOxygenFarms={showOxygenFarms}");
            sb.AppendLine($"ShowFoodProcessors={showFoodProcessors}");
            sb.AppendLine($"ShowIrrigationSystems={showIrrigationSystems}");
            sb.AppendLine($"ShowAlgaeFarms={showAlgaeFarms}");
            sb.AppendLine();

            myTerminalBlock.CustomData = sb.ToString();
        }

        void LoadConfig()
        {
            try
            {
                configError = false;
                MyIniParseResult result;
                TryCreateSurfaceData();

                if (config.TryParse(myTerminalBlock.CustomData, CONFIG_SECTION_ID, out result))
                {

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowHeader", ref surfaceData.showHeader, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
                    else
                        configError = true;

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowSubgrids", ref surfaceData.showSubgrids, ref configError);
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SubgridUpdateFrequency")) surfaceData.subgridUpdateFrequency = config.Get(CONFIG_SECTION_ID, "SubgridUpdateFrequency").ToInt32();
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowDocked", ref surfaceData.showDocked, ref configError);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "TitleFieldWidth", ref surfaceData.titleOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "RatioFieldWidth", ref surfaceData.ratioOffset, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetX", ref surfaceData.viewPortOffsetX, ref configError);
                    MahUtillities.TryGetConfigFloat(config, CONFIG_SECTION_ID, "ViewPortOffsetY", ref surfaceData.viewPortOffsetY, ref configError);

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowRefineries", ref showRefineries, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowAssemblers", ref showAssemblers, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowGenerators", ref showGenerators, ref configError);
                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowOxygenFarms", ref showOxygenFarms, ref configError);

                    // New optional toggles: do not flag config error if absent (maintains backward compatibility)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowFoodProcessors"))
                        showFoodProcessors = config.Get(CONFIG_SECTION_ID, "ShowFoodProcessors").ToBoolean();
                    else
                        showFoodProcessors = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowIrrigationSystems"))
                        showIrrigationSystems = config.Get(CONFIG_SECTION_ID, "ShowIrrigationSystems").ToBoolean();
                    else
                        showIrrigationSystems = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowAlgaeFarms"))
                        showAlgaeFarms = config.Get(CONFIG_SECTION_ID, "ShowAlgaeFarms").ToBoolean();
                    else
                        showAlgaeFarms = true;

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "UseColors", ref surfaceData.useColors, ref configError);

                    // Load scrolling config (backward compatible - don't flag error if missing)
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ToggleScroll"))
                        toggleScroll = config.Get(CONFIG_SECTION_ID, "ToggleScroll").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ReverseDirection"))
                        reverseDirection = config.Get(CONFIG_SECTION_ID, "ReverseDirection").ToBoolean();
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollSpeed"))
                        scrollSpeed = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollSpeed").ToInt32(60));
                    if (config.ContainsKey(CONFIG_SECTION_ID, "ScrollLines"))
                        scrollLines = Math.Max(1, config.Get(CONFIG_SECTION_ID, "ScrollLines").ToInt32(1));
                    if (config.ContainsKey(CONFIG_SECTION_ID, "MaxListLines"))
                        maxListLines = Math.Max(0, config.Get(CONFIG_SECTION_ID, "MaxListLines").ToInt32(5));

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.showHeader = true;
                        surfaceData.showSummary = true;
                        surfaceData.textSize = 0.45f;
                        surfaceData.titleOffset = 220;
                        surfaceData.ratioOffset = 180;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while loading config: {e.ToString()}");
            }
        }

        void CreateExcludeIdsList()
        {
            if (!config.ContainsKey(CONFIG_SECTION_ID, "ExcludeIds")) return;

            string[] exclude = config.Get(CONFIG_SECTION_ID, "ExcludeIds").ToString().Split(',');
            excludeIds.Clear();

            foreach (string s in exclude)
            {
                string t = s.Trim();

                if (String.IsNullOrEmpty(t) || t == "*" || t == "" || t.Length < 3) continue;

                excludeIds.Add(t);
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<IMyOxygenFarm> oxygenFarms = new List<IMyOxygenFarm>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyGasGenerator> generators = new List<IMyGasGenerator>();

    List<IMyAssembler> foodProcessors = new List<IMyAssembler>();
    List<IMyGasGenerator> irrigationSystems = new List<IMyGasGenerator>();
    List<IMyTerminalBlock> algaeFarms = new List<IMyTerminalBlock>();

        // Cached subgrid collections
        List<IMyOxygenFarm> subgridOxygenFarms = new List<IMyOxygenFarm>();
        List<IMyRefinery> subgridRefineries = new List<IMyRefinery>();
        List<IMyAssembler> subgridAssemblers = new List<IMyAssembler>();
        List<IMyGasGenerator> subgridGenerators = new List<IMyGasGenerator>();
        List<IMyAssembler> subgridFoodProcessors = new List<IMyAssembler>();
        List<IMyGasGenerator> subgridIrrigationSystems = new List<IMyGasGenerator>();
        List<IMyTerminalBlock> subgridAlgaeFarms = new List<IMyTerminalBlock>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        int subgridScanTick = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDProductionSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Dispose()
        {

        }

        public override void Run()
        {
            // Prevent execution on a dedicated server to avoid server-side load
            if (Sandbox.ModAPI.MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;

            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            // Update scroll position if enabled
            if (toggleScroll)
            {
                ticksSinceLastScroll += 10;  // Update10 = 10 game ticks
                if (ticksSinceLastScroll >= scrollSpeed)
                {
                    ticksSinceLastScroll = 0;
                    if (reverseDirection)
                        scrollOffset -= scrollLines;
                    else
                        scrollOffset += scrollLines;
                }
            }
            else
            {
                // Reset scroll when disabled
                scrollOffset = 0;
            }

            UpdateBlocks();

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawProductionMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks()
        {
            try
            {
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                // Determine if we should scan subgrids/docked on this tick
                bool scanSubgrids = false;
                if (surfaceData.showSubgrids || surfaceData.showDocked)
                {
                    subgridScanTick++;
                    if (subgridScanTick >= surfaceData.subgridUpdateFrequency / 10)  // Divide by 10 for Update10 timing
                    {
                        subgridScanTick = 0;
                        scanSubgrids = true;
                    }
                }

                // Always scan main grid blocks (instant updates)
                var mainBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, false, false);

                // Periodically update subgrid cache
                if (scanSubgrids)
                {
                    var allBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);
                    
                    // Extract subgrid-only blocks
                    var subgridOnlyBlocks = new List<IMyCubeBlock>();
                    foreach (var block in allBlocks)
                        if (!mainBlocks.Contains(block))
                            subgridOnlyBlocks.Add(block);

                    // Categorize subgrid blocks
                    subgridOxygenFarms.Clear();
                    subgridRefineries.Clear();
                    subgridAssemblers.Clear();
                    subgridGenerators.Clear();
                    subgridFoodProcessors.Clear();
                    subgridIrrigationSystems.Clear();
                    subgridAlgaeFarms.Clear();

                    foreach (var myBlock in subgridOnlyBlocks)
                    {
                        if (myBlock == null) continue;

                        // Detect Algae Farms first so they don't fall into other categories
                        if (IsAlgaeFarm(myBlock as IMyTerminalBlock))
                        {
                            subgridAlgaeFarms.Add((IMyTerminalBlock)myBlock);
                        }
                        else if (myBlock is IMyRefinery)
                        {
                            subgridRefineries.Add((IMyRefinery)myBlock);
                        }
                        else if (myBlock is IMyAssembler)
                        {
                            var asm = (IMyAssembler)myBlock;
                            if (IsFoodProcessor(myBlock as IMyTerminalBlock))
                                subgridFoodProcessors.Add(asm);
                            else
                                subgridAssemblers.Add(asm);
                        }
                        else if (myBlock is IMyGasGenerator)
                        {
                            var gen = (IMyGasGenerator)myBlock;
                            if (IsIrrigationSystem(myBlock as IMyTerminalBlock))
                                subgridIrrigationSystems.Add(gen);
                            else
                                subgridGenerators.Add(gen);
                        }
                        else if (myBlock is IMyOxygenFarm)
                        {
                            subgridOxygenFarms.Add((IMyOxygenFarm)myBlock);
                        }
                    }
                }

                // Categorize main grid blocks
                var mainOxygenFarms = new List<IMyOxygenFarm>();
                var mainRefineries = new List<IMyRefinery>();
                var mainAssemblers = new List<IMyAssembler>();
                var mainGenerators = new List<IMyGasGenerator>();
                var mainFoodProcessors = new List<IMyAssembler>();
                var mainIrrigationSystems = new List<IMyGasGenerator>();
                var mainAlgaeFarms = new List<IMyTerminalBlock>();

                foreach (var myBlock in mainBlocks)
                {
                    if (myBlock == null) continue;

                    // Detect Algae Farms first so they don't fall into other categories
                    if (IsAlgaeFarm(myBlock as IMyTerminalBlock))
                    {
                        mainAlgaeFarms.Add((IMyTerminalBlock)myBlock);
                    }
                    else if (myBlock is IMyRefinery)
                    {
                        mainRefineries.Add((IMyRefinery)myBlock);
                    }
                    else if (myBlock is IMyAssembler)
                    {
                        var asm = (IMyAssembler)myBlock;
                        if (IsFoodProcessor(myBlock as IMyTerminalBlock))
                            mainFoodProcessors.Add(asm);
                        else
                            mainAssemblers.Add(asm);
                    }
                    else if (myBlock is IMyGasGenerator)
                    {
                        var gen = (IMyGasGenerator)myBlock;
                        if (IsIrrigationSystem(myBlock as IMyTerminalBlock))
                            mainIrrigationSystems.Add(gen);
                        else
                            mainGenerators.Add(gen);
                    }
                    else if (myBlock is IMyOxygenFarm)
                    {
                        mainOxygenFarms.Add((IMyOxygenFarm)myBlock);
                    }
                }

                // Merge main (fresh) and subgrid (cached) collections
                oxygenFarms.Clear();
                oxygenFarms.AddRange(mainOxygenFarms);
                oxygenFarms.AddRange(subgridOxygenFarms);

                refineries.Clear();
                refineries.AddRange(mainRefineries);
                refineries.AddRange(subgridRefineries);

                assemblers.Clear();
                assemblers.AddRange(mainAssemblers);
                assemblers.AddRange(subgridAssemblers);

                generators.Clear();
                generators.AddRange(mainGenerators);
                generators.AddRange(subgridGenerators);

                foodProcessors.Clear();
                foodProcessors.AddRange(mainFoodProcessors);
                foodProcessors.AddRange(subgridFoodProcessors);

                irrigationSystems.Clear();
                irrigationSystems.AddRange(mainIrrigationSystems);
                irrigationSystems.AddRange(subgridIrrigationSystems);

                algaeFarms.Clear();
                algaeFarms.AddRange(mainAlgaeFarms);
                algaeFarms.AddRange(subgridAlgaeFarms);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawProductionMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (compactMode)
                {
                    DrawCompactProductionSprite(ref frame, ref position);
                    return;
                }

                if (surfaceData.showHeader)
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Production Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");

                // Draw each category with scrolling support
                if (showRefineries)
                    DrawRefineriesWithScrolling(ref frame, ref position);
                if (showAssemblers)
                    DrawAssemblersWithScrolling(ref frame, ref position);
                if (showFoodProcessors)
                    DrawFoodProcessorsWithScrolling(ref frame, ref position);
                if (showGenerators)
                    DrawGeneratorsWithScrolling(ref frame, ref position);
                if (showIrrigationSystems)
                    DrawIrrigationSystemsWithScrolling(ref frame, ref position);
                if (showOxygenFarms)
                    DrawOxygenFarmsWithScrolling(ref frame, ref position);
                if (showAlgaeFarms)
                    DrawAlgaeFarmsWithScrolling(ref frame, ref position);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawProductionMainSprite: {e.ToString()}");
            }
        }

        void DrawCompactProductionSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Production Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                position -= surfaceData.newLine;

                // Refineries
                var working = 0;
                if (refineries.Count > 0 && showRefineries)
                {
                    foreach(IMyRefinery refinery in refineries)
                        working += refinery.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Refineries", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {refineries.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Assemblers
                if (assemblers.Count > 0 && showAssemblers)
                {
                    working = 0;
                    foreach (IMyAssembler assembler in assemblers)
                        working += assembler.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Assemblers", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {assemblers.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // H2/O2 Generators
                if (generators.Count > 0 && showGenerators)
                {
                    working = 0;
                    foreach (IMyGasGenerator generator in generators)
                        working += generator.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"H2/O2 Generators", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                      {generators.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Irrigation Systems
                if (irrigationSystems.Count > 0 && showIrrigationSystems)
                {
                    working = 0;
                    foreach (IMyGasGenerator gen in irrigationSystems)
                        working += gen.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Irrigation Systems", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                      {irrigationSystems.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // OxygenFarms
                if (oxygenFarms.Count > 0 && showOxygenFarms)
                {
                    working = 0;
                    foreach (IMyOxygenFarm oxygenFarm in oxygenFarms)
                        working += oxygenFarm.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Oxygen Farms", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {oxygenFarms.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Food Processors
                if (foodProcessors.Count > 0 && showFoodProcessors)
                {
                    working = 0;
                    foreach (IMyAssembler fp in foodProcessors)
                        working += fp.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Food Processors", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {foodProcessors.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                // Algae Farms
                if (algaeFarms.Count > 0 && showAlgaeFarms)
                {
                    working = 0;
                    foreach (var af in algaeFarms)
                        working += af.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Algae Farms", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                       {algaeFarms.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawCompactProductionSprite: {e.ToString()}");
            }
        }

        // Category drawers with scrolling support (Multi-Category Approach 2)

        void DrawRefineriesWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (refineries.Count <= 0) return;
            try
            {
                // Sort refineries alphabetically
                MahSorting.SortBlocksByName(refineries);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Refineries [{refineries.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active Task      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines from remaining screen space
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                // Apply MaxListLines limit
                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = refineries.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw refineries with wraparound
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int refIndex = (startIndex + i) % totalDataLines;
                    var refinery = refineries[refIndex];
                    if (refinery == null) continue;

                    var name = refinery.CustomName;
                    var inventory = refinery.GetInventory(0);
                    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> queuedItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    inventory.GetItems(queuedItems);

                    var subtypeId = queuedItems.Count > 0 ? queuedItems[0].Type.SubtypeId : "";
                    var amount = queuedItems.Count > 0 ? queuedItems[0].Amount.ToIntSafe() : 0;
                    var queue = subtypeId == "" ? "-" : $"{MahDefinitions.KiloFormat(amount)} {subtypeId}";
                    var outputBlocked = refinery.OutputInventory.CurrentVolume >= refinery.OutputInventory.MaxVolume * .9f;
                    var state = $"{(!refinery.IsWorking ? "    Off" : outputBlocked ? "   Full" : queuedItems.Count > 0 ? "  Work" : "   Halt")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : state.Contains("Full") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{queue}  +{(queuedItems.Count > 0 ? queuedItems.Count - 1 : 0).ToString("0").Replace("1", " 1")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawRefineriesWithScrolling: {e.ToString()}");
            }
        }

        void DrawAssemblersWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (assemblers.Count <= 0) return;
            try
            {
                // Sort assemblers alphabetically
                MahSorting.SortBlocksByName(assemblers);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Assemblers [{assemblers.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active Task      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = assemblers.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw assemblers with wraparound
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int asmIndex = (startIndex + i) % totalDataLines;
                    var assembler = assemblers[asmIndex];
                    if (assembler == null) continue;

                    var name = assembler.CustomName;
                    List<Sandbox.ModAPI.Ingame.MyProductionItem> queuedBlueprints = new List<Sandbox.ModAPI.Ingame.MyProductionItem>();
                    assembler.GetQueue(queuedBlueprints);
                    var blueprintId = queuedBlueprints.Count > 0 ? queuedBlueprints[0].BlueprintId.ToString().Split('/')[1] : "";
                    var blueprintAmount = queuedBlueprints.Count > 0 ? (int)queuedBlueprints[0].Amount : 0;

                    // Map blueprint display name
                    if (blueprintId != "")
                    {
                        CargoItemDefinition itemDef = MahDefinitions.GetDefinition("Component", blueprintId) ??
                                                      MahDefinitions.GetDefinition("AmmoMagazine", blueprintId) ??
                                                      MahDefinitions.GetDefinition("PhysicalGunObject", blueprintId);
                        if (itemDef != null)
                            blueprintId = itemDef.displayName;
                    }

                    var queue = blueprintId == "" ? "-" : $"{blueprintId}";
                    var outputBlocked = assembler.OutputInventory.CurrentVolume >= assembler.OutputInventory.MaxVolume * .9f;
                    var state = $"{(!assembler.IsWorking ? "    Off" : outputBlocked ? "   Full" : queuedBlueprints.Count > 0 && assembler.IsProducing ? "  Work" : "   Halt")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state} ", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : state.Contains("Full") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(blueprintAmount > 0 ? blueprintAmount.ToString("0") + " " : "")}{queue}  +{(queuedBlueprints.Count > 0 ? queuedBlueprints.Count - 1 : 0).ToString("0").Replace("1", " 1")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawAssemblersWithScrolling: {e.ToString()}");
            }
        }

        void DrawFoodProcessorsWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (foodProcessors.Count <= 0) return;
            try
            {
                // Sort food processors alphabetically
                MahSorting.SortBlocksByName(foodProcessors);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Food Processors [{foodProcessors.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active Task      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = foodProcessors.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw food processors with wraparound
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int fpIndex = (startIndex + i) % totalDataLines;
                    var assembler = foodProcessors[fpIndex];
                    if (assembler == null) continue;

                    var name = assembler.CustomName;
                    List<Sandbox.ModAPI.Ingame.MyProductionItem> queuedBlueprints = new List<Sandbox.ModAPI.Ingame.MyProductionItem>();
                    assembler.GetQueue(queuedBlueprints);
                    var blueprintId = queuedBlueprints.Count > 0 ? queuedBlueprints[0].BlueprintId.ToString().Split('/')[1] : "";
                    var blueprintAmount = queuedBlueprints.Count > 0 ? (int)queuedBlueprints[0].Amount : 0;

                    // Map blueprint display name
                    if (blueprintId != "")
                    {
                        CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("ConsumableItem", blueprintId) ??
                                                             MahDefinitions.GetDefinition("PhysicalObject", blueprintId) ??
                                                             MahDefinitions.GetDefinition("Package", blueprintId) ??
                                                             MahDefinitions.GetDefinition("Component", blueprintId);
                        if (itemDefinition != null)
                            blueprintId = itemDefinition.displayName;
                    }

                    var queue = blueprintId == "" ? "-" : $"{blueprintId}";
                    var outputBlocked = assembler.OutputInventory.CurrentVolume > assembler.OutputInventory.MaxVolume * .9f;
                    var state = $"{(!assembler.IsWorking ? "    Off" : outputBlocked ? "   Full" : queuedBlueprints.Count > 0 && assembler.IsProducing ? "  Work" : "   Halt")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state} ", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : state.Contains("Full") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(blueprintAmount > 0 ? blueprintAmount.ToString("0") : "")} {queue}  +{(queuedBlueprints.Count > 0 ? queuedBlueprints.Count - 1 : 0).ToString("0").Replace("1", " 1")}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawFoodProcessorsWithScrolling: {e.ToString()}");
            }
        }

        void DrawGeneratorsWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (generators.Count <= 0) return;
            try
            {
                // Sort generators alphabetically
                MahSorting.SortBlocksByName(generators);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"H2/O2 Generators [{generators.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Inventory      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = generators.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw generators with wraparound
                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int genIndex = (startIndex + i) % totalDataLines;
                    var gen = generators[genIndex];
                    if (gen == null) continue;

                    var name = gen.CustomName;
                    float currentVolume = 0.0f;
                    var inventory = gen.GetInventory(0);

                    if (iceDefinition != null)
                    {
                        inventory.GetItems(inventoryItems);
                        int amount = 0;
                        foreach (var item in inventoryItems.OrderBy(it => it.Type.SubtypeId))
                        {
                            if (item == null) continue;
                            var subtypeId = item.Type.SubtypeId;
                            if (subtypeId.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
                                amount += item.Amount.ToIntSafe();
                        }
                        currentVolume = amount * iceDefinition.volume;
                    }
                    else
                    {
                        currentVolume = (float)inventory.CurrentVolume;
                    }

                    float maximumVolume = (float)inventory.MaxVolume * 1000;
                    var state = $"{(!gen.IsWorking ? "    Off" : currentVolume <= 0 ? "   Halt" : "  Work")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, Color.Aquamarine);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawGeneratorsWithScrolling: {e.ToString()}");
            }
        }

        void DrawIrrigationSystemsWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (irrigationSystems.Count <= 0) return;
            try
            {
                // Sort irrigation systems alphabetically
                MahSorting.SortBlocksByName(irrigationSystems);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Irrigation Systems [{irrigationSystems.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Inventory      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = irrigationSystems.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw irrigation systems with wraparound
                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int isIndex = (startIndex + i) % totalDataLines;
                    var gen = irrigationSystems[isIndex];
                    if (gen == null) continue;

                    var name = gen.CustomName;
                    float currentVolume = 0.0f;
                    var inventory = gen.GetInventory(0);

                    if (iceDefinition != null)
                    {
                        inventory.GetItems(inventoryItems);
                        int amount = 0;
                        foreach (var item in inventoryItems.OrderBy(it => it.Type.SubtypeId))
                        {
                            if (item == null) continue;
                            var subtypeId = item.Type.SubtypeId;
                            if (subtypeId.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
                                amount += item.Amount.ToIntSafe();
                        }
                        currentVolume = amount * iceDefinition.volume;
                    }
                    else
                    {
                        currentVolume = (float)inventory.CurrentVolume;
                    }

                    float maximumVolume = (float)inventory.MaxVolume * 1000;
                    var state = $"{(!gen.IsWorking ? "    Off" : currentVolume <= 0 ? "   Halt" : "  Work")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Halt") ? Color.Yellow : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, Color.Aquamarine);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawIrrigationSystemsWithScrolling: {e.ToString()}");
            }
        }

        void DrawOxygenFarmsWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (oxygenFarms.Count <= 0) return;
            try
            {
                // Sort oxygen farms alphabetically
                MahSorting.SortBlocksByName(oxygenFarms);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Oxygen Farms [{oxygenFarms.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Current Progress      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = oxygenFarms.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw oxygen farms with wraparound
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int ofIndex = (startIndex + i) % totalDataLines;
                    var farm = oxygenFarms[ofIndex];
                    if (farm == null) continue;

                    var name = farm.CustomName;
                    float progress = ParseProgressPercent(farm.DetailedInfo);
                    var state = $"{(!farm.IsWorking ? "    Off" : progress > 0f ? "  Work" : "   Idle")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Idle") ? Color.Yellow : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, progress, 1f, Unit.Percent, Color.GreenYellow);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawOxygenFarmsWithScrolling: {e.ToString()}");
            }
        }

        void DrawAlgaeFarmsWithScrolling(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (algaeFarms.Count <= 0) return;
            try
            {
                // Sort algae farms alphabetically
                MahSorting.SortBlocksByName(algaeFarms);

                // Header
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Algae Farms [{algaeFarms.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Current Progress      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                // Calculate available lines
                float screenHeight = mySurface.SurfaceSize.Y;
                float lineHeight = 30 * surfaceData.textSize;
                float currentY = position.Y - surfaceData.viewPortOffsetY;
                float remainingHeight = screenHeight - currentY;
                int availableDataLines = Math.Max(1, (int)(remainingHeight / lineHeight));

                if (maxListLines > 0)
                    availableDataLines = Math.Min(availableDataLines, maxListLines);

                // Apply scrolling
                int totalDataLines = algaeFarms.Count;
                int startIndex = 0;
                if (toggleScroll && totalDataLines > 0)
                {
                    int normalizedOffset = ((scrollOffset % totalDataLines) + totalDataLines) % totalDataLines;
                    startIndex = normalizedOffset;
                }

                // Draw algae farms with wraparound
                int linesDrawn = 0;
                for (int i = 0; i < totalDataLines && linesDrawn < availableDataLines; i++)
                {
                    int afIndex = (startIndex + i) % totalDataLines;
                    var block = algaeFarms[afIndex];
                    if (block == null) continue;

                    var name = block.CustomName;
                    float progress = ParseProgressPercent(block.DetailedInfo);
                    var state = $"{(!block.IsWorking ? "    Off" : progress > 0f ? "  Work" : "   Idle")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("Idle") ? Color.Yellow : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[          ] {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, progress, 1f, Unit.Percent, Color.GreenYellow);

                    position += surfaceData.newLine;
                    linesDrawn++;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawAlgaeFarmsWithScrolling: {e.ToString()}");
            }
        }

        // Helpers to detect modded blocks by subtype or custom name
        bool IsFoodProcessor(IMyTerminalBlock tb)
        {
            if (tb == null) return false;
            var id = (tb.BlockDefinition.SubtypeName ?? "") + " " + (tb.CustomName ?? "");
            var norm = Normalize(id);
            return norm.Contains("foodprocessor") || (norm.Contains("food") && norm.Contains("processor"));
        }

        bool IsIrrigationSystem(IMyTerminalBlock tb)
        {
            if (tb == null) return false;
            var id = (tb.BlockDefinition.SubtypeName ?? "") + " " + (tb.CustomName ?? "");
            var norm = Normalize(id);
            return norm.Contains("irrigation");
        }

        bool IsAlgaeFarm(IMyTerminalBlock tb)
        {
            if (tb == null) return false;
            var id = (tb.BlockDefinition.SubtypeName ?? "") + " " + (tb.CustomName ?? "");
            var norm = Normalize(id);
            return norm.Contains("algaefarm") || (norm.Contains("algae") && norm.Contains("farm"));
        }

        string Normalize(string s)
        {
            return (s ?? "").Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        }

        float ParseProgressPercent(string detailedInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(detailedInfo)) return 0f;
                var lines = detailedInfo.Split('\n');
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.IndexOf("progress", StringComparison.OrdinalIgnoreCase) >= 0 && line.IndexOf('%') >= 0)
                    {
                        int idx = line.IndexOf('%');
                        int start = idx - 1;
                        while (start >= 0 && (char.IsDigit(line[start]) || line[start] == '.' || line[start] == ',')) start--;
                        var num = line.Substring(start + 1, idx - (start + 1));
                        num = num.Replace(',', '.');
                        float val = 0f;
                        if (float.TryParse(num, out val))
                            return Math.Max(0f, Math.Min(1f, val / 100f));
                    }
                }
            }
            catch { }
            return 0f;
        }
    }
}
