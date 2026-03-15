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

        // Initialize settings
        string searchId = "*";
        bool showInactive = false;
        bool showRefineries = true;
        bool showAssemblers = true;
        bool showGenerators = true;
        bool showOxygenFarms = true;

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
                showSubgrids = true,
                showDocked = false,
                useColors = true
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            config.Clear();
            config.AddSection(CONFIG_SECTION_ID);

            config.Set(CONFIG_SECTION_ID, "SearchId", $"{searchId}");
            config.Set(CONFIG_SECTION_ID, "ExcludeIds", $"{(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "")}");
            config.Set(CONFIG_SECTION_ID, "TextSize", $"{surfaceData.textSize}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetX", $"{surfaceData.viewPortOffsetX}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetY", $"{surfaceData.viewPortOffsetY}");
            config.Set(CONFIG_SECTION_ID, "TitleFieldWidth", $"{surfaceData.titleOffset}");
            config.Set(CONFIG_SECTION_ID, "RatioFieldWidth", $"{surfaceData.ratioOffset}");
            config.Set(CONFIG_SECTION_ID, "ShowHeader", $"{surfaceData.showHeader}");
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids", $"{surfaceData.showSubgrids}");
            config.Set(CONFIG_SECTION_ID, "ShowDocked", $"{surfaceData.showDocked}");
            config.Set(CONFIG_SECTION_ID, "ShowRefineries", $"{showRefineries}");
            config.Set(CONFIG_SECTION_ID, "ShowAssemblers", $"{showAssemblers}");
            config.Set(CONFIG_SECTION_ID, "ShowGenerators", $"{showGenerators}");
            config.Set(CONFIG_SECTION_ID, "ShowOxygenFarms", $"{showOxygenFarms}");
            config.Set(CONFIG_SECTION_ID, "UseColors", $"{surfaceData.useColors}");

            config.Invalidate();
            myTerminalBlock.CustomData += "\n" + config.ToString() + "\n";
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowHeader"))
                        surfaceData.showHeader = config.Get(CONFIG_SECTION_ID, "ShowHeader").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSubgrids"))
                        surfaceData.showSubgrids = config.Get(CONFIG_SECTION_ID, "ShowSubgrids").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowDocked"))
                        surfaceData.showDocked = config.Get(CONFIG_SECTION_ID, "ShowDocked").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TextSize"))
                        surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(defaultValue: 1.0f);
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "TitleFieldWidth"))
                        surfaceData.titleOffset = config.Get(CONFIG_SECTION_ID, "TitleFieldWidth").ToInt32();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "RatioFieldWidth"))
                        surfaceData.ratioOffset = config.Get(CONFIG_SECTION_ID, "RatioFieldWidth").ToInt32();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ViewPortOffsetX"))
                        surfaceData.viewPortOffsetX = config.Get(CONFIG_SECTION_ID, "ViewPortOffsetX").ToInt32();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ViewPortOffsetY"))
                        surfaceData.viewPortOffsetY = config.Get(CONFIG_SECTION_ID, "ViewPortOffsetY").ToInt32();
                    else
                        configError = true;

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowRefineries"))
                        showRefineries = config.Get(CONFIG_SECTION_ID, "ShowRefineries").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowAssemblers"))
                        showAssemblers = config.Get(CONFIG_SECTION_ID, "ShowAssemblers").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowGenerators"))
                        showGenerators = config.Get(CONFIG_SECTION_ID, "ShowGenerators").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowOxygenFarms"))
                        showOxygenFarms = config.Get(CONFIG_SECTION_ID, "ShowOxygenFarms").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "UseColors"))
                        surfaceData.useColors = config.Get(CONFIG_SECTION_ID, "UseColors").ToBoolean();
                    else
                        configError = true;

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

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDProductionSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        public override void Dispose()
        {

        }

        public override void Run()
        {
            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

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
                oxygenFarms.Clear();
                assemblers.Clear();
                refineries.Clear();
                generators.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyRefinery)
                    {
                        refineries.Add((IMyRefinery)myBlock);
                    }
                    else if (myBlock is IMyAssembler)
                    {
                        assemblers.Add((IMyAssembler)myBlock);
                    }
                    else if (myBlock is IMyGasGenerator)
                    {
                        generators.Add((IMyGasGenerator)myBlock);
                    }
                    else if (myBlock is IMyOxygenFarm)
                    {
                        oxygenFarms.Add((IMyOxygenFarm)myBlock);
                    }
                }
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

                if (showRefineries)
                    SurfaceDrawer.DrawRefinerySummarySprite(ref frame, ref position, surfaceData, refineries);
                if (showAssemblers)
                    SurfaceDrawer.DrawAssemblerSummarySprite(ref frame, ref position, surfaceData, assemblers);
                if (showGenerators)
                    SurfaceDrawer.DrawGasGeneratorSummarySprite(ref frame, ref position, surfaceData, generators);
                if (showOxygenFarms)
                    SurfaceDrawer.DrawOxygenFarmSummarySprite(ref frame, ref position, surfaceData, oxygenFarms);
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
                }
                position += surfaceData.newLine;

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
                }
                position += surfaceData.newLine;

                // Generators
                if (generators.Count > 0 && showGenerators)
                {
                    working = 0;
                    foreach (IMyGasGenerator generator in generators)
                        working += generator.IsWorking ? 1 : 0;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Generators", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{working}          ", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Active:              /      ", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"                      {generators.Count}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                }
                position += surfaceData.newLine;

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
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenProductionSummary: Caught Exception while DrawCompactProductionSprite: {e.ToString()}");
            }
        }
    }
}
