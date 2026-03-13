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
    [MyTextSurfaceScript("LCDInfoScreenIngotsSummary", "$IOS LCD - Ingots")]
    public class LCDIngotsSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsIngotsSummary";

        // The IMyInventoryItem.Type.TypeIds this Script is looking for.
        List<string> item_types = new List<string>
        {
            "Ingot"
        };

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
                titleOffset = 280,
                ratioOffset = 82,
                viewPortOffsetX = compactMode ? 10 : 20,
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

            config.Set(CONFIG_SECTION_ID, "SearchId", "*");
            config.Set(CONFIG_SECTION_ID, "ExcludeIds", "");
            config.Set(CONFIG_SECTION_ID, "TextSize", $"{surfaceData.textSize}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetX", $"{surfaceData.viewPortOffsetX}");
            config.Set(CONFIG_SECTION_ID, "ViewPortOffsetY", $"{surfaceData.viewPortOffsetY}");
            config.Set(CONFIG_SECTION_ID, "TitleFieldWidth", $"{surfaceData.titleOffset}");
            config.Set(CONFIG_SECTION_ID, "RatioFieldWidth", $"{surfaceData.ratioOffset}");
            config.Set(CONFIG_SECTION_ID, "ShowHeader", $"{surfaceData.showHeader}");
            config.Set(CONFIG_SECTION_ID, "ShowMissing", $"{surfaceData.showMissing}");
            config.Set(CONFIG_SECTION_ID, "ShowRatio", $"{surfaceData.showRatio}");
            config.Set(CONFIG_SECTION_ID, "ShowBars", $"{surfaceData.showBars}");
            config.Set(CONFIG_SECTION_ID, "ShowSummary", $"{surfaceData.showSummary}");
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids", $"{surfaceData.showSubgrids}");
            config.Set(CONFIG_SECTION_ID, "ShowDocked", $"{surfaceData.showDocked}");
            config.Set(CONFIG_SECTION_ID, "UseColors", $"{surfaceData.useColors}");

            CreateCargoItemDefinitionList();

            foreach (CargoItemDefinition itemDefinition in itemDefinitions)
            {
                config.Set(CONFIG_SECTION_ID, itemDefinition.subtypeId, itemDefinition.minAmount);
            }

            foreach (CargoItemDefinition itemDefinition in unknownItemDefinitions)
            {
                config.Set(CONFIG_SECTION_ID, itemDefinition.subtypeId, itemDefinition.minAmount);
            }

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowMissing"))
                        surfaceData.showMissing = config.Get(CONFIG_SECTION_ID, "ShowMissing").ToBoolean();
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "UseColors"))
                        surfaceData.useColors = config.Get(CONFIG_SECTION_ID, "UseColors").ToBoolean();
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
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Config Syntax error at Line {result}");
                }

                CreateCargoItemDefinitionList();

                // Check Ingots config
                foreach (CargoItemDefinition definition in itemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }

                // Check unknownIngots config
                foreach (CargoItemDefinition definition in unknownItemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Caught Exception while loading config: {e.ToString()}");
            }
        }

        void CreateExcludeIdsList()
        {
            if (!config.ContainsKey(CONFIG_SECTION_ID, "ExcludeIds")) return;

            string[] exclude = config.Get(CONFIG_SECTION_ID, "ExcludeIds").ToString().Split(',');
            excludeIds.Clear();
            minVisibleAmount = 0;

            foreach (string s in exclude)
            {
                string t = s.Trim();

                if (t.Contains("<"))
                {
                    t = t.Replace("<", "").Trim();
                    int.TryParse(t, out minVisibleAmount);
                    continue;
                }

                if (String.IsNullOrEmpty(t) || t == "*" || t == "" || t.Length < 3) continue;

                excludeIds.Add(t);
            }
        }

        void CreateCargoItemDefinitionList()
        {
            itemDefinitions.Clear();

            foreach (CargoItemDefinition definition in MahDefinitions.cargoItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    int minAmount = config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId) ? (int)config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt64() : definition.minAmount;
                    itemDefinitions.Add(new CargoItemDefinition { subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = minAmount });
                }
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    int minAmount = config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId) ? (int)config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt64() : definition.minAmount;
                    itemDefinitions.Add(new CargoItemDefinition { subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = minAmount });
                }
            }
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<CargoItemDefinition> itemDefinitions = new List<CargoItemDefinition>();
        List<CargoItemDefinition> unknownItemDefinitions = new List<CargoItemDefinition>();
        List<IMyInventory> inventories = new List<IMyInventory>();
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        Dictionary<string, CargoItemType> cargo = new Dictionary<string, CargoItemType>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string searchId = "";
        string gridId = "Unknown grid";
        int minVisibleAmount = 0;
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDIngotsSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            UpdateContents();

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
                inventories.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                inventories = MahUtillities.GetInventories(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids, surfaceData.showDocked);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Caught Exception while updating inventories: {e.ToString()}");
            }
        }

        void UpdateContents()
        {
            try
            {
                cargo.Clear();

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0)
                        continue;

                    inventoryItems.Clear();
                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;
                        var currentAmount = item.Amount.ToIntSafe();

                        if (item_types.Contains(typeId))
                        {
                            if (!cargo.ContainsKey(subtypeId))
                            {
                                cargo.Add(subtypeId, new CargoItemType { item = item, amount = currentAmount });
                                CargoItemDefinition itemDefinition = FindCargoItemDefinition(subtypeId);

                                if (itemDefinition == null)
                                {
                                    itemDefinition = new CargoItemDefinition();
                                    itemDefinition.subtypeId = subtypeId;
                                    itemDefinition.displayName = subtypeId.Length >= 15 ? subtypeId.Substring(0, 15) : subtypeId;
                                    itemDefinition.volume = .1f;
                                    itemDefinition.minAmount = 1000;

                                    itemDefinitions.Add(itemDefinition);
                                    unknownItemDefinitions.Add(itemDefinition);
                                }

                                cargo[subtypeId].definition = itemDefinition;
                            }
                            else
                            {
                                cargo[subtypeId].amount += currentAmount;
                            }

                            cargo[subtypeId].item = item;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Caught Exception while updating contents: {e.ToString()}");
            }
        }

        void DrawMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                double total = 0;
                double current = 0;

                // Total Cargo
                foreach (IMyInventory inventory in inventories)
                {
                    if (inventory == null) continue;

                    total += (double)inventory.MaxVolume;
                    current += (double)inventory.CurrentVolume;
                }

                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Ingots [{cargo.Count}/{itemDefinitions.Count}/{unknownItemDefinitions.Count}/{inventories.Count}]:");
                    if (compactMode) position -= 2 * surfaceData.newLine;
                }

                if (surfaceData.showSummary)
                {
                    if (!compactMode)
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}] >> ({inventories.Count})", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    position += surfaceData.newLine;
                    SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Total", current, total, Unit.Percent, false, false);

                    // Total Volume
                    double volume = 0;

                    foreach (var item in cargo)
                    {
                        volume += item.Value.amount * item.Value.definition.volume;
                    }

                    volume /= 1000;
                    SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Ingots", volume, current, Unit.Percent, false, true);
                    position += surfaceData.newLine;
                }

                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                if (surfaceData.showMissing)
                {
                    DrawAllKnownSprite(ref frame, ref position);
                }
                else
                {
                    DrawAllAvailableSprite(ref frame, ref position);
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Caught Exception while DrawMainSprite: {e.ToString()}");
            }
        }

        void DrawAllKnownSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Id [Ingots]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Available", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);

                position += surfaceData.newLine;

                foreach (var itemDefinition in itemDefinitions)
                {
                    if (itemDefinition == null) continue;
                    if (IgnoreDefinition(itemDefinition)) continue;

                    SurfaceDrawer.DrawItemSprite(ref frame, ref position, surfaceData,
                        itemDefinition.subtypeId,
                        itemDefinition.displayName,
                        cargo.ContainsKey(itemDefinition.subtypeId) ? cargo[itemDefinition.subtypeId].amount : 0,
                        itemDefinition.minAmount,
                        true);
                }

                foreach (var itemDefinition in unknownItemDefinitions)
                {
                    if (itemDefinition == null) continue;
                    if (IgnoreDefinition(itemDefinition)) continue;

                    SurfaceDrawer.DrawItemSprite(ref frame, ref position, surfaceData,
                        itemDefinition.subtypeId,
                        itemDefinition.displayName,
                        cargo.ContainsKey(itemDefinition.subtypeId) ? cargo[itemDefinition.subtypeId].amount : 0,
                        itemDefinition.minAmount,
                        true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Caught Exception while DrawAllKnownSprite: {e.ToString()}");
            }
        }

        void DrawAllAvailableSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (cargo.Count <= 0)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "No Ingots found.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    return;
                }

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Id [Ingots]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Available", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (var item in cargo)
                {
                    if (item.Value.item == null) continue;

                    MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                    CargoItemDefinition itemDefinition = FindCargoItemDefinition(item.Value.item.Type.SubtypeId);

                    if (IgnoreDefinition(itemDefinition)) continue;
                    if (cargo[itemDefinition.subtypeId].amount < minVisibleAmount) continue;

                    SurfaceDrawer.DrawItemSprite(ref frame, ref position, surfaceData,
                        itemDefinition.subtypeId,
                        itemDefinition.displayName,
                        cargo.ContainsKey(itemDefinition.subtypeId) ? cargo[itemDefinition.subtypeId].amount : 0,
                        itemDefinition.minAmount,
                        true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDIngotsSummaryInfo: Caught Exception while DrawAllAvailableSprite: {e.ToString()}");
            }
        }

        bool IgnoreDefinition(CargoItemDefinition itemDefinition)
        {
            string sType = itemDefinition.subtypeId.ToLower();
            string dName = itemDefinition.displayName.ToLower();

            foreach (string s in excludeIds)
                if (sType.Contains(s.ToLower()) || dName.Contains(s.ToLower())) return true;

            return false;
        }

        CargoItemDefinition FindCargoItemDefinition(string subtypeId)
        {
            foreach (CargoItemDefinition definition in itemDefinitions)
            {
                if (definition == null) continue;
                if (definition.subtypeId == subtypeId) return definition;
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (definition == null) continue;
                if (definition.subtypeId == subtypeId) return definition;
            }

            return null;
        }
    }
}
