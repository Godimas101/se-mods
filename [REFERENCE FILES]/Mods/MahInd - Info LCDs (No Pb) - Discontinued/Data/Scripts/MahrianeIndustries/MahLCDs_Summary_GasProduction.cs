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
    [MyTextSurfaceScript("LCDInfoScreenGasGenerationSummary", "$IOS LCD - Gas Production")]
    public class LCDGasGenerationSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsGasGenerationStatus";

        // Initialize settings
        string searchId = "*";
        bool showHydrogen = true;
        bool showOxygen = true;
        bool showIce = true;
        bool showGenerators = true;
        bool showOxygenFarms = true;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .4f : .35f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = compactMode ? 220 : 128,
                ratioOffset = compactMode ? 180 : 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
                showMissing = false,
                showRatio = false,
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

            config.Set(CONFIG_SECTION_ID, "SearchId",           $"{searchId}");
            config.Set(CONFIG_SECTION_ID, "ExcludeIds",         $"{(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "Airlock,")}");
            config.Set(CONFIG_SECTION_ID, "TextSize",           $"{surfaceData.textSize}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetX",    $"{surfaceData.viewPortOffsetX}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetY",    $"{surfaceData.viewPortOffsetY}");
            config.Set(CONFIG_SECTION_ID, "TitleFieldWidth",    $"{surfaceData.titleOffset}");
            config.Set(CONFIG_SECTION_ID, "RatioFieldWidth",    $"{surfaceData.ratioOffset}");
            config.Set(CONFIG_SECTION_ID, "ShowHeader",         $"{surfaceData.showHeader}");
            config.Set(CONFIG_SECTION_ID, "ShowRatio",          $"{surfaceData.showRatio}");
            config.Set(CONFIG_SECTION_ID, "ShowBars",           $"{surfaceData.showBars}");
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids",       $"{surfaceData.showSubgrids}");
            config.Set(CONFIG_SECTION_ID, "ShowDocked",         $"{surfaceData.showDocked}");
            config.Set(CONFIG_SECTION_ID, "ShowHydrogen",       $"{showHydrogen}");
            config.Set(CONFIG_SECTION_ID, "ShowOxygen",         $"{showOxygen}");
            config.Set(CONFIG_SECTION_ID, "ShowIce",            $"{showIce}");
            config.Set(CONFIG_SECTION_ID, "ShowGenerators",     $"{showGenerators}");
            config.Set(CONFIG_SECTION_ID, "ShowOxygenFarms",    $"{showOxygenFarms}");
            config.Set(CONFIG_SECTION_ID, "UseColors",          $"{surfaceData.useColors}");

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowRatio"))
                        surfaceData.showRatio = config.Get(CONFIG_SECTION_ID, "ShowRatio").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowBars"))
                        surfaceData.showBars = config.Get(CONFIG_SECTION_ID, "ShowBars").ToBoolean();
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

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowHydrogen"))
                        showHydrogen = config.Get(CONFIG_SECTION_ID, "ShowHydrogen").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowOxygen"))
                        showOxygen = config.Get(CONFIG_SECTION_ID, "ShowOxygen").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowIce"))
                        showIce = config.Get(CONFIG_SECTION_ID, "ShowIce").ToBoolean();
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
                    else
                        configError = true;

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.showHeader = true;
                        surfaceData.showSummary = true;
                        surfaceData.textSize = 0.4f;
                        surfaceData.titleOffset = 220;
                        surfaceData.ratioOffset = 180;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyGasGenerator> generators = new List<IMyGasGenerator>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        float textSize = 1.0f;

        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
        float currentIceVolume = 0.0f;
        float maximumIceVolume = 0.0f;

        int hydrogenTanks = 0;
        int oxygenTanks = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDGasGenerationSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            UpdateIceContents();

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();
            
            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawGasProductionMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);

                oxygenFarms.Clear();
                generators.Clear();
                tanks.Clear();

                oxygenTanks = 0;
                hydrogenTanks = 0;

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyGasGenerator)
                    {
                        generators.Add((IMyGasGenerator)myBlock);
                    }
                    else if (myBlock is IMyGasTank)
                    {
                        tanks.Add((IMyGasTank)myBlock);

                        if (((IMyGasTank)myBlock).BlockDefinition.SubtypeName.Contains("Hydrogen"))
                        {
                            hydrogenTanks++;
                        }
                        else
                        {
                            oxygenTanks++;
                        }
                    }
                    else if (myBlock is IMyOxygenFarm)
                    {
                        oxygenFarms.Add((IMyOxygenFarm)myBlock);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void UpdateIceContents ()
        {
            try
            {
                currentIceVolume = 0.0f;
                maximumIceVolume = 0.0f;

                CargoItemDefinition iceDefinition = MahDefinitions.GetDefinition("Ore", "Ice");
                if (iceDefinition != null)
                    currentIceVolume = (float)MahUtillities.GetIceAmountFromBlockList(generators) * iceDefinition.volume;

                foreach (var generator in generators)
                {
                    if (generator == null) continue;

                    if (iceDefinition == null)
                        currentIceVolume = (float)generator.GetInventory(0).CurrentVolume;

                    maximumIceVolume += (float)generator.GetInventory(0).MaxVolume * 1000;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while updating ice contents: {e.ToString()}");
            }
        }

        void DrawGasProductionMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Gas Production Summary: [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                    if (compactMode) DrawGasProductionCompactSprite(ref frame, ref position);
                }

                // Print hydrogen fuel bar (if tanks AND engines are available. Otherwise H2 will have no effect on GasGeneration.
                if (tanks.Count > 0)
                {
                    tanks.RemoveAll(t => t == null);

                    if (showHydrogen)
                        SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Hydrogen", $"HYD ({hydrogenTanks.ToString()})", tanks, true);
                    if (showOxygen)
                        SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Oxygen", $"OXY ({oxygenTanks.ToString()})", tanks, true);
                }

                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                // Print ice load bar
                if (generators.Count > 0)
                {
                    if (showIce)
                    {

                        if (surfaceData.showBars)
                        {
                            SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "Ice", currentIceVolume, maximumIceVolume, Color.Aquamarine, (surfaceData.showRatio ? Unit.Percent : Unit.Liters));
                        }
                        else
                        {
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Ice", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(surfaceData.showRatio ? (currentIceVolume / maximumIceVolume * 100) + " %" : (MahDefinitions.LiterFormat(currentIceVolume)))}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                            position += surfaceData.newLine;
                        }
                        position += surfaceData.newLine;
                    }

                    if (showGenerators)
                        SurfaceDrawer.DrawGasGeneratorSummarySprite(ref frame, ref position, surfaceData, generators);
                }

                if (showOxygenFarms)
                    SurfaceDrawer.DrawOxygenFarmSummarySprite(ref frame, ref position, surfaceData, oxygenFarms);

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGasGenerationSummary: Caught Exception while DrawGasGenerationMainSprite: {e.ToString()}");
            }
        }

        void DrawGasProductionCompactSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            position -= surfaceData.newLine;

            if (generators.Count > 0 && showGenerators)
            {
                // Generators
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Generators [{generators.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                if (surfaceData.showBars)
                {
                    // Ice Bar
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentIceVolume, maximumIceVolume, surfaceData.showRatio ? Unit.Percent : Unit.Liters, Color.Aquamarine);
                }
                else
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(surfaceData.showRatio ? (currentIceVolume / maximumIceVolume * 100) + " %" : (MahDefinitions.LiterFormat(currentIceVolume)))}", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
                }
            }
            position += surfaceData.newLine;

            if (oxygenFarms.Count > 0 && showOxygenFarms)
            {
                // Oxygen Farms
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Oxygen Farms [{oxygenFarms.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                var outputOverall = 0.0f;
                foreach (var farm in oxygenFarms)
                {
                    if (farm == null) continue;

                    var name = farm.CustomName;
                    var currentOutputString = farm.DetailedInfo.Split('\n')[2].Replace("Oxygen Output:", "").Replace("L/min", "").Trim();

                    var currentOutput = 0.0f;
                    float.TryParse(currentOutputString, out currentOutput);
                    outputOverall += currentOutput;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.LiterFormat(outputOverall)}/min", TextAlignment.RIGHT, surfaceData.useColors ? Color.GreenYellow : surfaceData.surface.ScriptForegroundColor);
            }

            position += surfaceData.newLine;
        }
    }
}
