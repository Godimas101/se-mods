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
    [MyTextSurfaceScript("LCDInfoScreenDamageMonitorSummary", "$IOS LCD - Damage Control")]
    public class LCDDamageMonitorSummary : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsDamageMonitorStatus";

        // Initialize settings
        string searchId = "*";

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .6f : .4f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 128,
                ratioOffset = 64,
                viewPortOffsetX = 10,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
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

            config.Set(CONFIG_SECTION_ID, "SearchId",           $"{searchId}");
            config.Set(CONFIG_SECTION_ID, "ExcludeIds",         $"{(excludeIds != null && excludeIds.Count > 0 ? String.Join(", ", excludeIds.ToArray()) : "")}");
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "*" : searchId;
                    }
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
                        surfaceData.textSize = 0.6f;
                        surfaceData.titleOffset = 200;
                        surfaceData.ratioOffset = 104;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Caught Exception while loading config: {e.ToString()}");
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
        List<BlockStateData> blocks = new List<BlockStateData>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        int damagedBlocks = 0;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDDamageMonitorSummary(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                if (surfaceData.showHeader)
                    SurfaceDrawer.DrawHeader(ref myFrame, ref myPosition, surfaceData, $"Damage Control [{(searchId.Trim().Length <= 0 || searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");

                DrawDamageMonitorSprite(ref myFrame, ref myPosition);
            }
            myFrame.Dispose();
        }

        void UpdateBlocks ()
        {
            try
            {
                blocks.Clear();
                damagedBlocks = 0;

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    IMyTerminalBlock block = (IMyTerminalBlock)myBlock;
                    BlockStateData blockData = new BlockStateData(block);
                    blocks.Add(blockData);

                    if (!blockData.IsFullIntegrity) damagedBlocks++;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Caught Exception while updating blocks: {e.ToString()}");
            }
        }

        void DrawDamageMonitorSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (blocks.Count <= 0) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 15, 0);

                if (damagedBlocks <= 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"No damaged blocks found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    return;
                }

                if (compactMode) position -= surfaceData.newLine;
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Damaged Blocks [{damagedBlocks}/{blocks.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Integrity", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var block in blocks.OrderBy(i => i.priority))
                {
                    if (block.IsNull) continue;
                    if (block.IsFullIntegrity) continue;

                    var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                    var state = block.IsFunctional ? block.IsWorking ? "   On" : "   Off" : "Broken";
                    var blockId = block.CustomName.Length > maxNameLength ? block.CustomName.Substring(0, maxNameLength) : block.CustomName;
                    var maxIntegrity = block.MaxIntegrity;
                    var curIntegrity = block.CurrentIntegrity;
                    var ratio = curIntegrity / maxIntegrity;

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") ? Color.Orange : state.Contains("On") ? Color.GreenYellow : Color.Red);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[             ] {blockId}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                
                    if (surfaceData.showBars)
                    {
                        SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, curIntegrity, maxIntegrity, Unit.Percent, ratio > .9f ? Color.GreenYellow : ratio > .6f ? Color.Yellow : ratio > .2f ? Color.Orange : Color.Red);
                    }
                    else
                    {
                        ratio *= 100;
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{ratio.ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    }

                    position += surfaceData.newLine;
                    
                    // If compactMode, only show the first damaged block, then quit.
                    if (compactMode) return;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenDamageMonitorSummary: Caught Exception while DrawDamageMonitorSprite: {e.ToString()}");
            }
        }
    }
}