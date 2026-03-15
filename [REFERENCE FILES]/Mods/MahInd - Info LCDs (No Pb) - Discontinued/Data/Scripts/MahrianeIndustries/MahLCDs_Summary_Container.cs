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
    [MyTextSurfaceScript("LCDInfoScreenContainerSummary", "$IOS LCD - Container Summary")]
    public class LCDContainerSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsContainerSummary";

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
                titleOffset = 300,
                ratioOffset = 82,
                viewPortOffsetX = 20,
                viewPortOffsetY = 10,
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

            config.Set(CONFIG_SECTION_ID, "SearchId", "*");
            config.Set(CONFIG_SECTION_ID, "ExcludeIds", "");
            config.Set(CONFIG_SECTION_ID, "TextSize",           $"{surfaceData.textSize}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetX",    $"{surfaceData.viewPortOffsetX}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetY",    $"{surfaceData.viewPortOffsetY}");
            config.Set(CONFIG_SECTION_ID, "TitleFieldWidth",    $"{surfaceData.titleOffset}");
            config.Set(CONFIG_SECTION_ID, "RatioFieldWidth",    $"{surfaceData.ratioOffset}");
            config.Set(CONFIG_SECTION_ID, "ShowHeader",         $"{surfaceData.showHeader}");
            config.Set(CONFIG_SECTION_ID, "ShowMissing",        $"{surfaceData.showMissing}");
            config.Set(CONFIG_SECTION_ID, "ShowBars",           $"{surfaceData.showBars}");
            config.Set(CONFIG_SECTION_ID, "ShowSummary",        $"{surfaceData.showSummary}");
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids",       $"{surfaceData.showSubgrids}");
            config.Set(CONFIG_SECTION_ID, "ShowDocked",         $"{surfaceData.showDocked}");
            config.Set(CONFIG_SECTION_ID, "UseColors",          $"{surfaceData.useColors}");
            config.Set(CONFIG_SECTION_ID, "HideEmpty",          $"{hideEmpty}");

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSubgrids"))
                        surfaceData.showSubgrids = config.Get(CONFIG_SECTION_ID, "ShowSubgrids").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowDocked"))
                        surfaceData.showDocked = config.Get(CONFIG_SECTION_ID, "ShowDocked").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowMissing"))
                        surfaceData.showMissing = config.Get(CONFIG_SECTION_ID, "ShowMissing").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowBars"))
                        surfaceData.showBars = config.Get(CONFIG_SECTION_ID, "ShowBars").ToBoolean();
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "UseColors"))
                        surfaceData.useColors = config.Get(CONFIG_SECTION_ID, "UseColors").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "HideEmpty"))
                        hideEmpty = config.Get(CONFIG_SECTION_ID, "HideEmpty").ToBoolean();
                    else
                        configError = true;

                    surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);

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
                        surfaceData.textSize = 0.6f;
                        surfaceData.titleOffset = 200;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = 5;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Config Syntax error at Line {result}");
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Caught Exception while loading config: {e.ToString()}");
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

                if (t.Contains("<")) continue;
                if (String.IsNullOrEmpty(t) || t == "*" || t == "" || t.Length < 3) continue;

                excludeIds.Add(t);
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<string> searchIds = new List<string>();
        List<BlockInventoryData> blockInventoriDataList = new List<BlockInventoryData>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "";
        string searchId = "";
        bool configError = false;
        bool compactMode = false;
        bool hideEmpty = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDContainerSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

            UpdateInventories();

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
                DrawMainSprite(ref myFrame, ref myPosition);

            myFrame.Dispose();
        }

        void UpdateInventories()
        {
            try
            {
                blockInventoriDataList.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return;
                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;
                    if (!myBlock.HasInventory) continue;
                    if (myBlock is IMyTerminalBlock)
                    {
                        IMyTerminalBlock block = myBlock as IMyTerminalBlock;

                        if (block == null) continue;

                        BlockInventoryData blockInventoryData = new BlockInventoryData();
                        blockInventoryData.block = block;
                        blockInventoryData.inventories = new IMyInventory[block.InventoryCount];

                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            blockInventoryData.inventories [i] = block.GetInventory(i);
                        }

                        blockInventoriDataList.Add(blockInventoryData);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Caught Exception while updating inventories: {e.ToString()}");
            }
        }

        void DrawMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Containers [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}][{blockInventoriDataList.Count}]:");
                }

                foreach (BlockInventoryData blockInventoryData in blockInventoriDataList)
                {
                    if (blockInventoryData.block == null) continue;
                    if (blockInventoryData.inventories.Length <= 0) continue;
                    if (hideEmpty && blockInventoryData.CurrentVolume <= 0) continue;

                    // If there is only 1 inventory
                    if (blockInventoryData.inventories.Length == 1)
                    {
                        double current = (double)blockInventoryData.inventories[0].CurrentVolume;
                        double total = (double)blockInventoryData.inventories[0].MaxVolume;

                        string blockId = blockInventoryData.block.CustomName;
                        
                        if (surfaceData.showBars)
                        {
                            SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, $"{blockId}", current, total, Unit.Percent, false, !surfaceData.useColors);
                        }
                        else
                        {
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{blockId}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(current / total * 100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                            position += surfaceData.newLine;
                        }
                    }
                    // If this is a production block with multiple inventories
                    else
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{blockInventoryData.block.CustomName} [{blockInventoryData.inventories.Length.ToString("0")}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;

                        for (int i = 0; i < blockInventoryData.inventories.Length; i++)
                        {
                            double current = (double)blockInventoryData.inventories[i].CurrentVolume;
                            double total = (double)blockInventoryData.inventories[i].MaxVolume;

                            if (surfaceData.showBars)
                            {
                                string inventoryId = i == 0 ? "Input" : "Output";

                                SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, $"- {inventoryId}:", current, total, Unit.Percent, false, !surfaceData.useColors);
                            }
                            else
                            {
                                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(current / total * 100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                                position += surfaceData.newLine;
                            }
                        }
                    }
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDContainerSummaryInfo: Caught Exception while DrawMainSprite: {e.ToString()}");
            }
        }
    }
}
