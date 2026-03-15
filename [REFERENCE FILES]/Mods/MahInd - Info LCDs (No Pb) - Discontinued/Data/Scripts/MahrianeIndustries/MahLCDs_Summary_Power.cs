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
    [MyTextSurfaceScript("LCDInfoScreenPowerSummary", "$IOS LCD - Power")]
    public class LCDPowerSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsPowerStatus";

        // Initialize settings
        string searchId = "*";
        bool showBatteries = true;
        bool showSolar = true;
        bool showWind = true;
        bool showReactors = true;
        bool showEngines = true;
        bool showInactive = true;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .4f : .4f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 140,
                ratioOffset = 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
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
            config.Set(CONFIG_SECTION_ID, "ExcludeIds",         $"{(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "")}");
            config.Set(CONFIG_SECTION_ID, "TextSize",           $"{surfaceData.textSize}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetX",    $"{surfaceData.viewPortOffsetX}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetY",    $"{surfaceData.viewPortOffsetY}");
            config.Set(CONFIG_SECTION_ID, "TitleFieldWidth",    $"{surfaceData.titleOffset}");
            config.Set(CONFIG_SECTION_ID, "RatioFieldWidth",    $"{surfaceData.ratioOffset}");
            config.Set(CONFIG_SECTION_ID, "ShowHeader",         $"{surfaceData.showHeader}");
            config.Set(CONFIG_SECTION_ID, "ShowRatio",          $"{surfaceData.showRatio}");
            config.Set(CONFIG_SECTION_ID, "ShowSummary",        $"{surfaceData.showSummary}");
            config.Set(CONFIG_SECTION_ID, "ShowBars",           $"{surfaceData.showBars}");
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids",       $"{surfaceData.showSubgrids}");
            config.Set(CONFIG_SECTION_ID, "ShowDocked",         $"{surfaceData.showDocked}");
            config.Set(CONFIG_SECTION_ID, "ShowSolar",          $"{showSolar}");
            config.Set(CONFIG_SECTION_ID, "ShowWind",           $"{showWind}");
            config.Set(CONFIG_SECTION_ID, "ShowBatteries",      $"{showBatteries}");
            config.Set(CONFIG_SECTION_ID, "ShowReactors",       $"{showReactors}");
            config.Set(CONFIG_SECTION_ID, "ShowEngines",        $"{showEngines}");
            config.Set(CONFIG_SECTION_ID, "ShowInactive",       $"{showInactive}");
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSummary"))
                        surfaceData.showSummary = config.Get(CONFIG_SECTION_ID, "ShowSummary").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowInactive"))
                        showInactive = config.Get(CONFIG_SECTION_ID, "ShowInactive").ToBoolean();
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowBatteries"))
                        showBatteries = config.Get(CONFIG_SECTION_ID, "ShowBatteries").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSolar"))
                        showSolar = config.Get(CONFIG_SECTION_ID, "ShowSolar").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowWind"))
                        showWind = config.Get(CONFIG_SECTION_ID, "ShowWind").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowReactors"))
                        showReactors = config.Get(CONFIG_SECTION_ID, "ShowReactors").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowEngines"))
                        showEngines = config.Get(CONFIG_SECTION_ID, "ShowEngines").ToBoolean();
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
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 104;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
        List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();
        List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        float timeRemaining = 0.0f;
        
        float reactorsCurrentVolume = 0.0f;
        float reactorsMaximumVolume = 0.0f;
        float reactorsCurrentLoad = 0.0f;
        float reactorsMaximumLoad = 0.0f;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDPowerSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                DrawPowerSummaryMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                batteries.Clear();
                windTurbines.Clear();
                hydrogenEngines.Clear();
                solarPanels.Clear();
                reactors.Clear();
                powerProducers.Clear();
                tanks.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyPowerProducer)
                    {
                        powerProducers.Add((IMyPowerProducer)myBlock);

                        if (myBlock is IMyBatteryBlock)
                        {
                            batteries.Add((IMyBatteryBlock)myBlock);
                        }
                        else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                        {
                            windTurbines.Add((IMyPowerProducer)myBlock);
                        }
                        else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                        {
                            hydrogenEngines.Add((IMyPowerProducer)myBlock);
                        }
                        else if (myBlock is IMyReactor)
                        {
                            reactors.Add((IMyReactor)myBlock);
                        }
                        else if (myBlock is IMySolarPanel)
                        {
                            solarPanels.Add((IMySolarPanel)myBlock);
                        }
                    }
                    else if (myBlock is IMyGasTank)
                    {
                        tanks.Add((IMyGasTank)myBlock);
                    }
                }

                // Calculate reactor load
                reactorsCurrentVolume = 0.0f;
                reactorsMaximumVolume = 0.0f;

                if (reactors.Count > 0)
                {
                    foreach (IMyReactor reactor in reactors)
                    {
                        reactorsCurrentVolume += (float)reactor.GetInventory(0).CurrentVolume;
                        reactorsMaximumVolume += (float)reactor.GetInventory(0).MaxVolume;
                    }

                    reactorsCurrentLoad = (reactorsCurrentVolume / 0.052f) * 1000;
                    reactorsMaximumLoad = (reactorsMaximumVolume / 0.052f) * 1000;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawPowerSummaryMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (surfaceData.showHeader)
            {
                SurfaceDrawer.DrawPowerTimeHeaderSprite(ref frame, ref position, surfaceData, $"Power [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]", powerProducers);
                if (compactMode) position -= surfaceData.newLine;
            }
            if (surfaceData.showSummary)
            {
                var currentOutput = MahUtillities.GetCurrentOutput(powerProducers);
                var maxOutput = MahUtillities.GetMaxOutput(powerProducers);

                // Power overall
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "PWR", currentOutput, maxOutput, showInactive, Unit.Watt, false);
                
                if (batteries.Count > 0 && showBatteries)
                    DrawBatterySprite(ref frame, ref position);
                // Print reactor fuel bar
                if (reactors.Count > 0 && showReactors)
                    SurfaceDrawer.DrawBarFixedColor(ref frame, ref position, surfaceData, "U", reactorsCurrentLoad, reactorsMaximumLoad, Color.Gold, Unit.Count);
                // Print hydrogen fuel bar (if tanks AND engines are available. Otherwise H2 will have no effect on power.
                if (tanks.Count > 0 && hydrogenEngines.Count > 0 && showEngines)
                    SurfaceDrawer.DrawGasTankSprite(ref frame, ref position, surfaceData, "Hydrogen", "HYD", tanks, true);
                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                if (solarPanels.Count > 0 && showSolar)
                    DrawSolarSprite(ref frame, ref position);
                if (windTurbines.Count > 0 && showWind)
                    DrawWindTurbinesSprite(ref frame, ref position);
                if (reactors.Count > 0 && showReactors)
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "REA", reactors.Sum(block => block.CurrentOutput), reactors.Sum(block => block.MaxOutput), showInactive, Unit.Watt, false);
                if (hydrogenEngines.Count > 0 && showEngines)
                    DrawHydrogenEnginesSprite(ref frame, ref position);
            }

            position += surfaceData.newLine;
        }

        void DrawBatterySprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "BAT", batteries.Sum(block => block.CurrentStoredPower), batteries.Sum(block => block.MaxStoredPower), showInactive, Unit.WattHours, true);

                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, " <<", batteries.Sum(block => block.CurrentOutput), batteries.Sum(block => block.MaxOutput), showInactive, Unit.Watt, false);
                SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, " >>", batteries.Sum(block => block.CurrentInput), batteries.Sum(block => block.MaxInput), showInactive, Unit.Watt, false);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawBatterySprite: {e.ToString()}");
            }
        }

        void DrawSolarSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                var currentOuput = solarPanels.Sum(block => block.CurrentOutput);

                if (currentOuput > 0 || showInactive)
                {
                    // Current Output
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "SOL", currentOuput, solarPanels.Sum(block => block.MaxOutput), showInactive, Unit.Watt, true);
                    // Exposure to sunlight (current max to absolute max)
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "EXP", solarPanels.Sum(block => block.MaxOutput), solarPanels.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput), showInactive, Unit.Percent, true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawSolarSprite: {e.ToString()}");
            }
        }

        void DrawWindTurbinesSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                var currentOuput = windTurbines.Sum(block => block.CurrentOutput);
                var outputMax = windTurbines.Sum(block => block.MaxOutput);
                var definedMax = windTurbines.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

                if ((currentOuput > 0 || showInactive) && outputMax > 0)
                {
                    // Current Output
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "WND", currentOuput, outputMax, showInactive, Unit.Watt, false);
                    // Current max (depending on wind) to absolute max
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "EFF", outputMax, definedMax, showInactive, Unit.Percent, false);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawWindTurbinesSprite: {e.ToString()}");
            }
        }

        void DrawHydrogenEnginesSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                var currentOuput = hydrogenEngines.Sum(block => block.CurrentOutput);

                if (currentOuput > 0 || showInactive)
                {
                    SurfaceDrawer.DrawOutputSprite(ref frame, ref position, surfaceData, "ENG", hydrogenEngines.Sum(block => block.CurrentOutput), hydrogenEngines.Sum(block => block.MaxOutput), showInactive, Unit.Watt, false);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while DrawHydrogenEnginesSprite: {e.ToString()}");
            }
        }
    }
}
