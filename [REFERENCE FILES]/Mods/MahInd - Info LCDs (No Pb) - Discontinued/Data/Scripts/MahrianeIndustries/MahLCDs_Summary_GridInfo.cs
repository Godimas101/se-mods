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
    [MyTextSurfaceScript("LCDInfoScreenGridInfoSummary", "$IOS LCD - Grid & Flight Info")]
    public class LCDGridInfoSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsGridInfoStatus";

        // Initialize settings
        string searchId = "*";

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
                ratioOffset = compactMode ? 300 : 104,
                viewPortOffsetX = 10,
                viewPortOffsetY = compactMode ? 5 : 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = true,
                showMissing = false,
                showRatio = false,
                showBars = true,
                showSubgrids = true,
                showDocked = true,
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
                        surfaceData.showDocked = true;
                        surfaceData.textSize = 0.4f;
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 300;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGridInfoSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGridInfoSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyJumpDrive> jumpdrives = new List<IMyJumpDrive>();
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();

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
        
        public LCDGridInfoSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                DrawGridInfoSummaryMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                jumpdrives.Clear();
                connectors.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, false);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyJumpDrive)
                    {
                        jumpdrives.Add((IMyJumpDrive)myBlock);
                    }
                    else if (myBlock is IMyShipConnector)
                    {
                        connectors.Add(myBlock as IMyShipConnector);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenGridInfoSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawGridInfoSummaryMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (surfaceData.showHeader)
            {
                SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"{(isStation ? "Grid" : "Flight")} Info '{gridId}' {(isStation ? "<Station>" : "<Vessel>")}");
                if (compactMode) position -= surfaceData.newLine;
            }

            if (surfaceData.showSummary)
            {
                // Stationary objects have, for whatever reason, no mass calculation.
                if (!isStation)
                {
                    SurfaceDrawer.DrawShipMassSprite(ref frame, ref position, surfaceData, gridMass, compactMode);
                    position += surfaceData.newLine;
                }

                if (surfaceData.showDocked)
                {
                    if (!compactMode)
                    {
                        foreach (IMyShipConnector connector in connectors)
                        {
                            if (connector == null) continue;
                            Sandbox.ModAPI.Ingame.MyShipConnectorStatus status = connector.Status;

                            string state = $"{(status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connectable ? "Ready    " : status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected ? "Locked    " : "Unlocked  ")}";
                            string connectedGridId = $"{(connector.IsConnected ? $"<{(connector.OtherConnector.CubeGrid as IMyCubeGrid).CustomName}>" : "")}";
                            Color stateColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connectable ? Color.Orange : status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected ? Color.GreenYellow : Color.Yellow;

                            // Only show all connectors on stations. On Vessels only show actually connected.
                            if (isStation || status != Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Unconnected)
                            {
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(" " + connector.CustomName + ":")}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{connectedGridId}   ", TextAlignment.CENTER, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Yellow);
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                 ]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" {state}", TextAlignment.RIGHT, stateColor);
                                position += surfaceData.newLine;
                            }
                        }
                        position += surfaceData.newLine;
                    }
                    else
                    {
                        if (!isStation) position -= surfaceData.newLine;
                        var dockedVesselCount = 0;

                        foreach (IMyShipConnector connector in connectors)
                        {
                            if (connector == null) continue;
                            if (connector.IsConnected) dockedVesselCount++;
                        }

                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Docked Ships: {dockedVesselCount}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                }
            }

            SurfaceDrawer.DrawJumpDriveSprite(ref frame, ref position, surfaceData, jumpdrives, isStation, compactMode);
        }
    }
}
