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
    [MyTextSurfaceScript("LCDInfoScreenAirlockMonitorSummary", "$IOS LCD - Airlock Monitor")]
    public class LCDAirlockMonitorSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsAirlockMonitorStatus";

        // Initialize settings
        string searchId = "Airlock";
        bool compactMode = false;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .8f : mySurface.SurfaceSize.Y > 300 ? .6f : .4f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 200,
                ratioOffset = 82,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
                showMissing = false,
                showRatio = false,
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
            config.AddSection(CONFIG_SECTION_ID);

            config.Set(CONFIG_SECTION_ID, "SearchId",           $"{searchId}");
            config.Set(CONFIG_SECTION_ID, "ExcludeIds",         $"{(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "")}");
            config.Set(CONFIG_SECTION_ID, "TextSize",           $"{surfaceData.textSize}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetX",    $"{surfaceData.viewPortOffsetX}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetY",    $"{surfaceData.viewPortOffsetY}");
            config.Set(CONFIG_SECTION_ID, "TitleFieldWidth",    $"{surfaceData.titleOffset}");
            config.Set(CONFIG_SECTION_ID, "RatioFieldWidth",    $"{surfaceData.ratioOffset}");
            config.Set(CONFIG_SECTION_ID, "ShowHeader",         $"{surfaceData.showHeader}");
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids",       $"{surfaceData.showSubgrids}");
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "Airlock" : searchId;
                    }
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSubgrids"))
                        surfaceData.showSubgrids = config.Get(CONFIG_SECTION_ID, "ShowSubgrids").ToBoolean();
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

                    CreateExcludeIdsList();

                    // Is Corner LCD?
                    if (compactMode)
                    {
                        surfaceData.textSize = surfaceData.showHeader ? 0.8f : 1.2f;
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 82;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = surfaceData.showHeader ? 10 : 15;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenAirlockMonitorSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenAirlockMonitorSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<IMyDoor> doors = new List<IMyDoor>();
        List<IMyAirVent> airVents = new List<IMyAirVent>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        bool configError = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDAirlockMonitorSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains("SettingsAirlockMonitorStatus"))
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
                DrawAirlockMainSprite(ref myFrame, ref myPosition);

            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            doors.Clear();
            airVents.Clear();

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

            if (myCubeGrid == null) return;

            IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
            isStation = cubeGrid.IsStatic;
            gridId = cubeGrid.CustomName;

            var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, false);

            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock == null) continue;

                if (myBlock is IMyAirVent)
                {
                    airVents.Add((IMyAirVent)myBlock);
                }
                if (myBlock is IMyDoor)
                {
                    doors.Add((IMyDoor)myBlock);
                }
            }
        }

        void DrawAirlockMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (surfaceData.showHeader)
            {
                SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"[{searchId} Monitor]");
                if (compactMode) position -= surfaceData.newLine;
            }

            DrawAirlockMonitorAirVentSprite(ref frame, ref position);
            DrawAirlockMonitorDoorSprite(ref frame, ref position);
        }

        void DrawAirlockMonitorAirVentSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (searchId == "*" || string.IsNullOrWhiteSpace(searchId))
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" No [SearchId] has been provided.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                return;
            }

            // Remove all non existent or missing airVents.
            airVents.RemoveAll(block => block == null);

            if (airVents.Count <= 0)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" No airVents with id [{searchId}] could be found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                return;
            }

            // We only need to check 1 single airlock to get the pressure info.
            SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, $"{(airVents[0].GetOxygenLevel() <= 0 ? "Depressurized" : airVents[0].Status.ToString())}", (airVents[0].GetOxygenLevel() * 100), 100, Unit.Percent, true);
            position += surfaceData.newLine;
        }

        void DrawAirlockMonitorDoorSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (compactMode) return;

            if (doors.Count <= 0) return;

            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Airlock Doors [{doors.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"State", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
            position += surfaceData.newLine;

            foreach (var door in doors)
            {
                if (door == null) continue;

                var state = door.IsWorking ? "  On " : "  Off ";
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Red : Color.GreenYellow);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[      ] {(door.CustomName.Length > 25 ? door.CustomName.Substring(0, 25) : door.CustomName)}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                var status = door.Status.ToString();
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{status}", TextAlignment.RIGHT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : status.Contains("Closed") ? Color.GreenYellow : Color.Orange);
                position += surfaceData.newLine;
            }
        }
    }
}