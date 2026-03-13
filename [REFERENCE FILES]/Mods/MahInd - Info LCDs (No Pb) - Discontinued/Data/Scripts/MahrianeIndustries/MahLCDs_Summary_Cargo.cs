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
    [MyTextSurfaceScript("LCDInfoScreenCargoSummary", "$IOS LCD - Cargo Summary")]
    public class LCDCargoSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsCargoSummary";

        // Initialize settings
        bool showAmmo = true;
        bool showComponents = true;
        bool showIngots = true;
        bool showItems = true;
        bool showOres = true;

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            compactMode = mySurface.SurfaceSize.X / mySurface.SurfaceSize.Y > 4;
            var textSize = compactMode ? .6f : .45f;

            // Initialize surface settings
            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 140,
                ratioOffset = 82,
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
            config.Set(CONFIG_SECTION_ID, "ShowAmmo",           $"{showAmmo}");
            config.Set(CONFIG_SECTION_ID, "ShowComponents",     $"{showComponents}");
            config.Set(CONFIG_SECTION_ID, "ShowIngots",         $"{showIngots}");
            config.Set(CONFIG_SECTION_ID, "ShowItems",          $"{showItems}");
            config.Set(CONFIG_SECTION_ID, "ShowOres",           $"{showOres}");
            config.Set(CONFIG_SECTION_ID, "UseColors",          $"{surfaceData.useColors}");

            CreateCargoItemDefinitionList();

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowAmmo"))
                        showAmmo = config.Get(CONFIG_SECTION_ID, "ShowAmmo").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowComponents"))
                        showComponents = config.Get(CONFIG_SECTION_ID, "ShowComponents").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowIngots"))
                        showIngots = config.Get(CONFIG_SECTION_ID, "ShowIngots").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowItems"))
                        showItems = config.Get(CONFIG_SECTION_ID, "ShowItems").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowOres"))
                        showOres = config.Get(CONFIG_SECTION_ID, "ShowOres").ToBoolean();
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
                        surfaceData.textSize = surfaceData.showHeader ? 0.8f : 1.2f;
                        surfaceData.titleOffset = 82;
                        surfaceData.ratioOffset = 82;
                        surfaceData.viewPortOffsetX = 10;
                        surfaceData.viewPortOffsetY = surfaceData.showHeader ? 10 : 15;
                        surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Config Syntax error at Line {result}");
                }

                CreateCargoItemDefinitionList();                    

                // Check Cargo config
                foreach (CargoItemDefinition definition in itemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }

                // Check unknownCargo config
                foreach (CargoItemDefinition definition in unknownItemDefinitions)
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, definition.subtypeId))
                        definition.minAmount = config.Get(CONFIG_SECTION_ID, definition.subtypeId).ToInt32();
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while loading config: {e.ToString()}");
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
                itemDefinitions.Add(new CargoItemDefinition { subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0 });

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
                itemDefinitions.Add(new CargoItemDefinition { subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0 });
        }

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<string> excludeIds = new List<string>();
        List<CargoItemDefinition> itemDefinitions = new List<CargoItemDefinition>();
        List<CargoItemDefinition> unknownItemDefinitions = new List<CargoItemDefinition>();
        List<IMyInventory> inventories = new List<IMyInventory>();

        Dictionary<string, CargoItemType> ammo = new Dictionary<string, CargoItemType>();
        Dictionary<string, CargoItemType> components = new Dictionary<string, CargoItemType>();
        Dictionary<string, CargoItemType> ingots = new Dictionary<string, CargoItemType>();
        Dictionary<string, CargoItemType> items = new Dictionary<string, CargoItemType>();
        Dictionary<string, CargoItemType> ores = new Dictionary<string, CargoItemType>();

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string searchId = "";
        int minVisibleAmount = 0;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDCargoSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while updating inventories: {e.ToString()}");
            }
        }

        void UpdateContents()
        {
            try
            {
                ammo.Clear();
                components.Clear();
                ingots.Clear();
                items.Clear();
                ores.Clear();

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0)
                        continue;

                    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems)
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;
                        var currentAmount = item.Amount.ToIntSafe();

                        if (typeId.Contains("Ammo"))
                            AddCargoItemDefinition(item, ammo);
                        else if (typeId.Contains("Component"))
                            AddCargoItemDefinition(item, components);
                        else if (typeId.Contains("Ingot"))
                            AddCargoItemDefinition(item, ingots);
                        else if (typeId.Contains("Ore"))
                            AddCargoItemDefinition(item, ores);
                        else
                            AddCargoItemDefinition(item, items);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while updating contents: {e.ToString()}");
            }
        }

        void DrawMainSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                float total = inventories.Sum(inventory => (float)inventory.MaxVolume);
                float current = inventories.Sum(inventory => (float)inventory.CurrentVolume);

                if (surfaceData.showHeader)
                {
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Cargo [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");
                    if (compactMode) position -= surfaceData.newLine;
                }

                // Stationary objects have, for whatever reason, no mass calculation.
                if (!isStation && !compactMode)
                {
                    SurfaceDrawer.DrawShipMassSprite(ref frame, ref position, surfaceData, gridMass, compactMode);
                }
                
                if (surfaceData.showBars)
                {
                    SurfaceDrawer.DrawBar(ref frame, ref position, surfaceData, "Total", current, total, Unit.Percent, false, false);
                }
                else
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Total", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{(current / total * 100).ToString("0.0")}%", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
                position += surfaceData.newLine;

                // If this is a corner LCD, no more data will be visible.
                if (compactMode) return;

                if (surfaceData.showSummary)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Summary", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                
                    if (showAmmo)
                        SurfaceDrawer.DrawCargoItemBar(ref frame, ref position, surfaceData, ammo, "Ammo", total);
                    if (showComponents)
                        SurfaceDrawer.DrawCargoItemBar(ref frame, ref position, surfaceData, components, "Cmpnts", total);
                    if (showIngots)
                        SurfaceDrawer.DrawCargoItemBar(ref frame, ref position, surfaceData, ingots, "Ingots", total);
                    if (showItems)
                        SurfaceDrawer.DrawCargoItemBar(ref frame, ref position, surfaceData, items, "Items", total);
                    if (showOres)
                        SurfaceDrawer.DrawCargoItemBar(ref frame, ref position, surfaceData, ores, "Ores", total);
                    if (current <= 0)
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $" - Cargo hold is empty.", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while DrawMainSprite: {e.ToString()}");
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

        void AddCargoItemDefinition (VRage.Game.ModAPI.Ingame.MyInventoryItem item, Dictionary<string, CargoItemType> dict)
        {
            try
            {
                if (item == null) return;

                var subtypeId = item.Type.SubtypeId;
                var currentAmount = item.Amount.ToIntSafe();

                if (!dict.ContainsKey(subtypeId))
                {
                    dict.Add(subtypeId, new CargoItemType { item = item, amount = currentAmount });
                    CargoItemDefinition itemDefinition = FindCargoItemDefinition(subtypeId);

                    if (itemDefinition == null)
                    {
                        itemDefinition = new CargoItemDefinition();
                        itemDefinition.subtypeId = subtypeId;
                        itemDefinition.displayName = subtypeId.Length >= 15 ? subtypeId.Substring(0, 15) : subtypeId;
                        itemDefinition.volume = .1f;
                        itemDefinition.minAmount = 0;

                        itemDefinitions.Add(itemDefinition);
                        unknownItemDefinitions.Add(itemDefinition);
                    }

                    dict[subtypeId].definition = itemDefinition;
                }
                else
                {
                    dict[subtypeId].amount += currentAmount;
                }

                dict[subtypeId].item = item;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDCargoSummaryInfo: Caught Exception while AddCargoItemDefinition: {e.ToString()}");
            }
        }
        
        CargoItemDefinition FindCargoItemDefinition(string subtypeId)
        {
            foreach (CargoItemDefinition definition in itemDefinitions)
            {
                if (definition.subtypeId == subtypeId) return definition;
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (definition.subtypeId == subtypeId) return definition;
            }

            return null;
        }
    }
}
