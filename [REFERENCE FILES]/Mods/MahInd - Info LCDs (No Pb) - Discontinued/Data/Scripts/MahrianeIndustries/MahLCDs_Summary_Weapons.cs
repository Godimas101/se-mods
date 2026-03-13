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
    [MyTextSurfaceScript("LCDInfoScreenWeaponsSummary", "$IOS LCD - Weapons")]
    public class LCDWeaponsSummaryInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsWeaponsSummary";

        // The IMyInventoryItem.Type.TypeIds this Script is looking for.
        List<string> item_types = new List<string>
        {
            "AmmoMagazine",
        };

        // Initialize settings
        string searchId = "*";
        bool detailedInfo = true;
        bool showTurrets = true;
        bool showInteriors = true;
        bool showCannons = true;
        bool showCustom = true;

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
                titleOffset = 128,
                ratioOffset = mySurface.SurfaceSize.X > 300 || detailedInfo ? 128 : 64,
                viewPortOffsetX = 20,
                viewPortOffsetY = 10,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSummary = false,
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
            config.Set(CONFIG_SECTION_ID, "ShowSubgrids",       $"{surfaceData.showSubgrids}");
            config.Set(CONFIG_SECTION_ID, "ShowTurrets",        $"{showTurrets}");
            config.Set(CONFIG_SECTION_ID, "ShowInteriors",      $"{showInteriors}");
            config.Set(CONFIG_SECTION_ID, "ShowCannons",        $"{showCannons}");
            config.Set(CONFIG_SECTION_ID, "ShowCustom",         $"{showCustom}");
            config.Set(CONFIG_SECTION_ID, "DetailedInfo",       $"{detailedInfo}");
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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowSubgrids"))
                        surfaceData.showSubgrids = config.Get(CONFIG_SECTION_ID, "ShowSubgrids").ToBoolean();
                    else
                        configError = true;

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

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowTurrets"))
                        showTurrets = config.Get(CONFIG_SECTION_ID, "ShowTurrets").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowInteriors"))
                        showInteriors = config.Get(CONFIG_SECTION_ID, "ShowInteriors").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowCannons"))
                        showCannons = config.Get(CONFIG_SECTION_ID, "ShowCannons").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "ShowCustom"))
                        showCustom = config.Get(CONFIG_SECTION_ID, "ShowCustom").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "DetailedInfo"))
                        detailedInfo = config.Get(CONFIG_SECTION_ID, "DetailedInfo").ToBoolean();
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "UseColors"))
                        surfaceData.useColors = config.Get(CONFIG_SECTION_ID, "UseColors").ToBoolean();
                    else
                        configError = true;

                    CreateExcludeIdsList();
                }
                else
                {
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Config Syntax error at Line {result}");
                }
                
                CreateCargoItemDefinitionList();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while loading config: {e.ToString()}");
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

        void CreateCargoItemDefinitionList()
        {
            itemDefinitions.Clear();

            foreach (CargoItemDefinition definition in MahDefinitions.cargoItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    itemDefinitions.Add(new CargoItemDefinition { subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0 });
                }
            }

            foreach (CargoItemDefinition definition in unknownItemDefinitions)
            {
                if (item_types.Contains(definition.typeId))
                {
                    itemDefinitions.Add(new CargoItemDefinition { subtypeId = definition.subtypeId, displayName = definition.displayName, volume = definition.volume, minAmount = 0 });
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
        List<IMyLargeTurretBase> turrets = new List<IMyLargeTurretBase>();
        List<IMyLargeInteriorTurret> interiorTurrets = new List<IMyLargeInteriorTurret>();
        List<IMyUserControllableGun> cannons = new List<IMyUserControllableGun>();
        List<IMyTurretControlBlock> customTurretControllers = new List<IMyTurretControlBlock>();

        Dictionary<string, CargoItemType> cargo = new Dictionary<string, CargoItemType>();
        float pixelPerChar;

        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        SurfaceDrawer.SurfaceData surfaceData;
        string gridId = "Unknown grid";
        bool configError = false;
        bool compactMode = false;
        bool isStation = false;
        Sandbox.ModAPI.Ingame.MyShipMass gridMass;

        public LCDWeaponsSummaryInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            UpdateBlocksAndInventories();
            UpdateContents();

            pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY) + myViewport.Position;
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (configError)
                SurfaceDrawer.DrawErrorSprite(ref myFrame, surfaceData, $"<< Config error. Please Delete CustomData >>", Color.Orange);
            else
            {
                DrawWeaponsMainSprite(ref myFrame, ref myPosition);
            }

            myFrame.Dispose();
        }

        void UpdateBlocksAndInventories()
        {
            try
            {
                cannons.Clear();
                turrets.Clear();
                inventories.Clear();
                interiorTurrets.Clear();
                customTurretControllers.Clear();

                var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;

                if (myCubeGrid == null) return;

                IMyCubeGrid cubeGrid = myCubeGrid as IMyCubeGrid;
                isStation = cubeGrid.IsStatic;
                gridId = cubeGrid.CustomName;

                var myFatBlocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, surfaceData.showSubgrids);

                foreach (var myBlock in myFatBlocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyTurretControlBlock)
                    {
                        customTurretControllers.Add((IMyTurretControlBlock)myBlock);
                    }
                    else if (myBlock is IMyUserControllableGun)
                    {
                        if (myBlock is IMyLargeInteriorTurret)
                        {
                            interiorTurrets.Add((IMyLargeInteriorTurret)myBlock);

                            if (myBlock.HasInventory)
                            {
                                for (int i = 0; i < myBlock.InventoryCount; i++)
                                {
                                    inventories.Add(myBlock.GetInventory(i));
                                }
                            }
                        }
                        else if (myBlock is IMyLargeTurretBase)
                        {
                            turrets.Add((IMyLargeTurretBase)myBlock);

                            if (myBlock.HasInventory)
                            {
                                for (int i = 0; i < myBlock.InventoryCount; i++)
                                {
                                    inventories.Add(myBlock.GetInventory(i));
                                }
                            }
                        }
                        else
                        {
                            cannons.Add((IMyUserControllableGun)myBlock);
                    
                            if (myBlock.HasInventory)
                            {
                                for (int i = 0; i < myBlock.InventoryCount; i++)
                                {
                                    inventories.Add(myBlock.GetInventory(i));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while updating blocks and inventories: {e.ToString()}");
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
                    if (inventory.ItemCount == 0) continue;

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
                                    itemDefinition.displayName = subtypeId.Length >= 18 ? subtypeId.Substring(0, 18) : subtypeId;
                                    itemDefinition.volume = 1f;
                                    itemDefinition.minAmount = currentAmount;

                                    itemDefinitions.Add(itemDefinition);
                                }

                                cargo[subtypeId].definition = itemDefinition;
                            }
                            else
                            {
                                cargo[subtypeId].definition.minAmount = (int)config.Get("SettingsWeaponsSummary", $"{item.Type.SubtypeId}").ToInt64();
                                cargo[subtypeId].amount += currentAmount;
                            }

                            cargo[subtypeId].item = item;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while updating contents: {e.ToString()}");
            }
        }

        void DrawWeaponsMainSprite (ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            try
            {
                if (surfaceData.showHeader)
                    SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Weapons Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}]");

                if (detailedInfo)
                {
                    if (showCustom)
                        DrawCustomTurretsControllerDetailedSprite(ref frame, ref position);
                    if (showTurrets)
                        DrawTurretsDetailedSprite(ref frame, ref position);
                    if (showInteriors)
                        DrawInteriorTurretsDetailedSprite(ref frame, ref position);
                    if (showCannons)
                        DrawCannonsDetailedSprite(ref frame, ref position);
                }
                else
                {
                    if (showCustom)
                        DrawCustomTurretControllerCompactSprite(ref frame, ref position);
                    if (showTurrets)
                        DrawTurretsCompactSprite(ref frame, ref position);
                    if (showInteriors)
                        DrawInteriorTurretsCompactSprite(ref frame, ref position);
                    if (showCannons)
                        DrawCannonsCompactSprite(ref frame, ref position);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawWeaponsMainSprite: {e.ToString()}");
            }
        }

        void DrawInteriorTurretsDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (interiorTurrets.Count <= 0) return;
            
            try
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Interior Turrets [{interiorTurrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyLargeInteriorTurret turret in interiorTurrets)
                {
                    if (turret == null) continue;

                    DrawDetailedTurretSprite(ref frame, ref position, turret);
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawInteriorTurretsDetailedSprite: {e.ToString()}");
            }
        }

        void DrawInteriorTurretsCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (interiorTurrets.Count <= 0) return;

            try
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Interior Turrets [{interiorTurrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Ammo", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyLargeInteriorTurret turret in interiorTurrets)
                {
                    if (turret == null) continue;

                    DrawCompactTurretSprite(ref frame, ref position, turret);
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawInteriorTurretsCompactSprite: {e.ToString()}");
            }
        }

        void DrawTurretsDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (turrets.Count <= 0) return;

            try
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Turrets [{turrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyLargeTurretBase turret in turrets)
                {
                    if (turret == null) continue;

                    DrawDetailedTurretSprite(ref frame, ref position, turret);
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawTurretsDetailedSprite: {e.ToString()}");
            }
        }

        void DrawTurretsCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (turrets.Count <= 0) return;

            try
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Turrets [{turrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Ammo", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyLargeTurretBase turret in turrets)
                {
                    if (turret == null) continue;

                    DrawCompactTurretSprite(ref frame, ref position, turret);
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawTurretsCompactSprite: {e.ToString()}");
            }
        }

        void DrawCustomTurretsControllerDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (customTurretControllers.Count <= 0) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Custom Turrets [{customTurretControllers.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyTurretControlBlock controller in customTurretControllers)
                {
                    if (controller == null) continue;

                    var controllerName = controller.CustomName.Length > maxNameLength ? controller.CustomName.Substring(0, maxNameLength) : controller.CustomName;

                    List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock> tools = new List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock>();
                    controller.GetTools(tools);
                    var hasGuns = false;
                    var isShooting = false;
                    var isRecharging = false;
                    var secondsLeftToRecharge = 0;
                    var secondsToRecharge = 1;
                    var gunCount = 0;
                    var currentVolume = 0.0f;
                    var maximumVolume = 0.0f;

                    foreach (var myTool in tools)
                    {
                        if (myTool == null) continue;

                        if (myTool is IMyUserControllableGun)
                        {
                            isShooting = ((IMyUserControllableGun)myTool).IsShooting;
                            hasGuns = true;
                            gunCount++;

                            string detailedInfo = ((IMyUserControllableGun)myTool).DetailedInfo;
                            string rechargeInfo = "";
                            isRecharging = detailedInfo.Contains("Fully recharged in:") && !detailedInfo.Contains("Fully recharged in: 0 sec");

                            if (isRecharging) // Might be a railgun
                            {
                                secondsToRecharge = 60;

                                string[] s = detailedInfo.Split('\n');

                                if (s.Length > 2)
                                {
                                    bool isMinutes = s.Contains("min");
                                    rechargeInfo = (isMinutes ? s[2].Replace(" min", "") : s[2].Replace(" sec", "")).Replace("Fully recharged in: ", "");
                                    int.TryParse(rechargeInfo, out secondsLeftToRecharge);
                                    secondsLeftToRecharge *= isMinutes ? 60 : 1;
                                    secondsLeftToRecharge = secondsLeftToRecharge <= 0 ? 60 : secondsLeftToRecharge;
                                }
                            }

                            if (myTool.HasInventory)
                            {
                                currentVolume += (float)myTool.GetInventory(0).CurrentVolume;
                                maximumVolume += (float)myTool.GetInventory(0).MaxVolume;
                            }
                        }
                    }
                
                    var rechargeBar = $"  {secondsLeftToRecharge} sec";
                    var functional = controller.AzimuthRotor != null && controller.ElevationRotor != null && controller.Camera != null;
                    var state = $"{(!functional ? " Missing" : !controller.IsWorking ? "     Off     " : controller.IsSunTrackerEnabled ? "Tracking" : hasGuns && isRecharging ? $"{rechargeBar}" : hasGuns && currentVolume <= 0 ? "NoAmmo" : isShooting ? "  Firing" : controller.IsUnderControl ? " Manned" : controller.AIEnabled ? controller.HasTarget && !controller.IsAimed ? "Follow" : controller.HasTarget && controller.IsAimed ? "Locked" : "    Idle" : " Manual")}";
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : isRecharging || state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") || state.Contains("Missing") ? Color.Red : state.Contains("Manned") || state.Contains("Tracking") ? Color.Magenta : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {controllerName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);

                    if (functional)
                    {
                        var targetCharacters = $"[{(controller.TargetCharacters ? "X" : "  ")}]";
                        var targetFriends = $"[{(controller.TargetFriends ? "X" : "  ")}]";
                        var targetLargeGrids = $"[{(controller.TargetLargeGrids ? "X" : "  ")}]";
                        var targetMeteors = $"[{(controller.TargetMeteors ? "X" : "  ")}]";
                        var targetMissiles = $"[{(controller.TargetMissiles ? "X" : "  ")}]";
                        var targetNeutrals = $"[{(controller.TargetNeutrals ? "X" : "  ")}]";
                        var targetSmallGrids = $"[{(controller.TargetSmallGrids ? "X" : "  ")}]";
                        var targetStations = $"[{(controller.TargetStations ? "X" : "  ")}]";
                        var targetSet = $"{(controller.IsSunTrackerEnabled ? "Tracking Sun" : "Ch Fr LG Me Mi Ne SG St ")}";
                        var targetting = controller.IsSunTrackerEnabled ? "" : $"{targetCharacters} {targetFriends} {targetLargeGrids} {targetMeteors} {targetMissiles} {targetNeutrals} {targetSmallGrids} {targetStations}";

                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetSet}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{(hasGuns ? $"Armed ({gunCount})" : "Unarmed")}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetting}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                        if (hasGuns)
                        {
                            SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, Color.Orange);
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[Range: {controller.Range.ToString("#,0")} m]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        }
                    }
                    else
                    {
                        position += surfaceData.newLine;
                        var builtState = "";
                        if (controller.AzimuthRotor == null)
                            builtState = "No Azimuth Rotor found";
                        else if (controller.ElevationRotor == null)
                            builtState = "No Elevation Rotor found";
                        else if (controller.Camera == null)
                            builtState = "No Camera found";
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{builtState}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    position += surfaceData.newLine;
                }
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCustomTurretsControllerDetailedSprite: {e.ToString()}");
            }
        }

        void DrawCustomTurretControllerCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (customTurretControllers.Count <= 0) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Custom Turrets [{customTurretControllers.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Ammo", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyTurretControlBlock controller in customTurretControllers)
                {
                    if (controller == null) continue;

                    var controllerName = controller.CustomName.Length > maxNameLength ? controller.CustomName.Substring(0, maxNameLength) : controller.CustomName;

                    List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock> tools = new List<Sandbox.ModAPI.Ingame.IMyFunctionalBlock>();
                    controller.GetTools(tools);
                    var hasGuns = false;
                    var isShooting = false;
                    var isRecharging = false;
                    var secondsLeftToRecharge = 0;
                    var secondsToRecharge = 1;
                    var gunCount = 0;
                    var currentVolume = 0.0f;
                    var maximumVolume = 0.0f;

                    foreach (var myTool in tools)
                    {
                        if (myTool == null) continue;

                        if (myTool is IMyUserControllableGun)
                        {
                            isShooting = ((IMyUserControllableGun)myTool).IsShooting;
                            hasGuns = true;
                            gunCount++;

                            string detailedInfo = ((IMyUserControllableGun)myTool).DetailedInfo;
                            string rechargeInfo = "";
                            isRecharging = detailedInfo.Contains("Fully recharged in:") && !detailedInfo.Contains("Fully recharged in: 0 sec");

                            if (isRecharging) // Might be a railgun
                            {
                                secondsToRecharge = 60;

                                string[] s = detailedInfo.Split('\n');

                                if (s.Length > 2)
                                {
                                    bool isMinutes = s.Contains("min");
                                    rechargeInfo = (isMinutes ? s[2].Replace(" min", "") : s[2].Replace(" sec", "")).Replace("Fully recharged in: ", "");
                                    int.TryParse(rechargeInfo, out secondsLeftToRecharge);
                                    secondsLeftToRecharge *= isMinutes ? 60 : 1;
                                    secondsLeftToRecharge = secondsLeftToRecharge <= 0 ? 60 : secondsLeftToRecharge;
                                }
                            }

                            if (myTool.HasInventory)
                            {
                                currentVolume += (float)myTool.GetInventory(0).CurrentVolume;
                                maximumVolume += (float)myTool.GetInventory(0).MaxVolume;
                            }
                        }
                    }

                    var rechargeBar = $"  {secondsLeftToRecharge} sec";
                    var functional = controller.AzimuthRotor != null && controller.ElevationRotor != null && controller.Camera != null;
                    var state = $"{(!functional ? " Missing" : !controller.IsWorking ? "     Off     " : controller.IsSunTrackerEnabled ? "Tracking" : hasGuns && isRecharging ? $"{rechargeBar}" : hasGuns && currentVolume <= 0 ? "NoAmmo" : isShooting ? "  Firing" : controller.IsUnderControl ? " Manned" : controller.AIEnabled ? controller.HasTarget && !controller.IsAimed ? "Follow" : controller.HasTarget && controller.IsAimed ? "Locked" : "    Idle" : " Manual")}";
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : isRecharging || state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") || state.Contains("Missing") ? Color.Red : state.Contains("Manned") || state.Contains("Tracking") ? Color.Magenta : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {controllerName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                
                    if (functional)
                    {
                        if (hasGuns)
                        {
                            SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, Color.Orange);
                        }
                        else
                        {
                            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{("Unarmed")}]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                        }
                        position += surfaceData.newLine;
                    }
                    else
                    {
                        position += surfaceData.newLine;
                        var builtState = "";
                        if (controller.AzimuthRotor == null)
                            builtState = "No Azimuth Rotor found";
                        else if (controller.ElevationRotor == null)
                            builtState = "No Elevation Rotor found";
                        else if (controller.Camera == null)
                            builtState = "No Camera found";
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{builtState}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCustomTurretControllerCompactSprite: {e.ToString()}");
            }
        }

        void DrawDetailedTurretSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyLargeTurretBase turret)
        {
            if (turret == null) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                var turrentName = turret.CustomName.Length > maxNameLength ? turret.CustomName.Substring(0, maxNameLength) : turret.CustomName;
                var currentVolume = (float)turret.GetInventory(0).CurrentVolume;
                var maximumVolume = (float)turret.GetInventory(0).MaxVolume;
                var ammoType = "No Ammo";
                var ammoCount = 0;
                var percentValue = (currentVolume / maximumVolume) * 100;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);

                if (turret.GetInventory(0).CurrentVolume > 0)
                {
                    List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    turret.GetInventory(0).GetItems(inventoryItems);

                    foreach (var item in inventoryItems)
                    {
                        if (item == null) continue;

                        if (ammoType == "No Ammo")
                        {
                            ammoType = item.Type.SubtypeId;
                            CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("AmmoMagazine", ammoType);

                            if (itemDefinition != null)
                            {
                                ammoType = itemDefinition.displayName;
                            }
                        }

                        ammoCount += (int)item.Amount;
                    }
                }

                var targetCharacters = $"[{(turret.TargetCharacters ? "X" : "  ")}]";
                var targetEnemies = $"[{(turret.TargetEnemies ? "X" : "  ")}]";
                var targetLargeGrids = $"[{(turret.TargetLargeGrids ? "X" : "  ")}]";
                var targetMeteors = $"[{(turret.TargetMeteors ? "X" : "  ")}]";
                var targetMissiles = $"[{(turret.TargetMissiles ? "X" : "  ")}]";
                var targetNeutrals = $"[{(turret.TargetNeutrals ? "X" : "  ")}]";
                var targetSmallGrids = $"[{(turret.TargetSmallGrids ? "X" : "  ")}]";
                var targetStations = $"[{(turret.TargetStations ? "X" : "  ")}]";
                var targetSet = $"Ch En LG Me Mi Ne SG St ";
                var targetting = $"{targetCharacters} {targetEnemies} {targetLargeGrids} {targetMeteors} {targetMissiles} {targetNeutrals} {targetSmallGrids} {targetStations}";

                ammoType = ammoType.Length >= maxNameLength ? ammoType.Substring(0, maxNameLength) : ammoType;
                var state = $"{(!turret.IsWorking ? "     Off     " : currentVolume <= 0 ? "NoAmmo" : turret.IsShooting ? "  Firing" : turret.IsUnderControl ? " Manned" : turret.HasTarget && !turret.IsAimed ? "Follow" : turret.HasTarget && turret.IsAimed ? "Locked" : "    Idle")}";

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : state.Contains("Manned") ? Color.Magenta : state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {turrentName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetSet}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.KiloFormat(ammoCount)}x <{ammoType}> ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetting}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
                SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[Range: {turret.Range.ToString("#,0")} m]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawDetailedTurretSprite: {e.ToString()}");
            }
        }

        void DrawCompactTurretSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyLargeTurretBase turret)
        {
            if (turret == null) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                var turrentName = turret.CustomName.Length > maxNameLength ? turret.CustomName.Substring(0, maxNameLength) : turret.CustomName;
                var currentVolume = (float)turret.GetInventory(0).CurrentVolume;
                var maximumVolume = (float)turret.GetInventory(0).MaxVolume;
                var ammoType = "No Ammo";
                var percentValue = (currentVolume / maximumVolume) * 100;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);

                var state = $"{(!turret.IsWorking ? "     Off     " : currentVolume <= 0 ? "NoAmmo" : turret.IsShooting ? "  Firing" : turret.IsUnderControl ? " Manned" : turret.HasTarget && !turret.IsAimed ? "Follow" : turret.HasTarget && turret.IsAimed ? "Locked" : "    Idle")}";

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : state.Contains("Manned") ? Color.Magenta : state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {turrentName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCompactTurretSprite: {e.ToString()}");
            }
        }

        void DrawCannonsDetailedSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (cannons.Count <= 0) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Fixed Weapons [{cannons.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyUserControllableGun cannon in cannons)
                {
                    if (cannon == null) continue;

                    var cannonName = cannon.CustomName.Length > maxNameLength ? cannon.CustomName.Substring(0, maxNameLength) : cannon.CustomName;
                    var currentVolume = (float)cannon.GetInventory(0).CurrentVolume;
                    var maximumVolume = (float)cannon.GetInventory(0).MaxVolume;
                    var ammoType = "No Ammo";
                    var ammoCount = 0;
                    var isShooting = cannon.IsShooting;
                    var isRecharging = false;
                    var secondsLeftToRecharge = 0;
                    var secondsToRecharge = 1;
                    var percentValue = (currentVolume / maximumVolume) * 100;

                    string detailedInfo = ((IMyUserControllableGun)cannon).DetailedInfo;
                    string rechargeInfo = "";
                    isRecharging = detailedInfo.Contains("Fully recharged in:") && !detailedInfo.Contains("Fully recharged in: 0 sec");

                    if (isRecharging) // Might be a railgun
                    {
                        secondsToRecharge = 60;

                        string[] s = detailedInfo.Split('\n');

                        if (s.Length > 2)
                        {
                            bool isMinutes = s.Contains("min");
                            rechargeInfo = (isMinutes ? s[2].Replace(" min", "") : s[2].Replace(" sec", "")).Replace("Fully recharged in: ", "");
                            int.TryParse(rechargeInfo, out secondsLeftToRecharge);
                            secondsLeftToRecharge *= isMinutes ? 60 : 1;
                            secondsLeftToRecharge = secondsLeftToRecharge <= 0 ? 60 : secondsLeftToRecharge;
                        }
                    }

                    if (cannon.GetInventory(0).CurrentVolume > 0)
                    {
                        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        cannon.GetInventory(0).GetItems(inventoryItems);

                        foreach (var item in inventoryItems)
                        {
                            if (item == null) continue;

                            if (ammoType == "No Ammo")
                            {
                                ammoType = item.Type.SubtypeId;
                                CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("AmmoMagazine", ammoType);

                                if (itemDefinition != null)
                                {
                                    ammoType = itemDefinition.displayName;
                                }
                            }

                            ammoCount += (int)item.Amount;
                        }
                    }

                    ammoType = ammoType.Length >= 18 ? ammoType.Substring(0, 18) : ammoType;
                    var rechargeBar = $"  {secondsLeftToRecharge} sec";
                    var state = $"{(!cannon.IsWorking ? "     Off     " : isRecharging ? $"{rechargeBar}" : currentVolume <= 0 ? "NoAmmo" : isShooting ? "  Firing" : "  Ready    ")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : isRecharging ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {cannonName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{MahDefinitions.KiloFormat(ammoCount)}] <{ammoType}> ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCannonsDetailedSprite: {e.ToString()}");
            }
        }
        
        void DrawCannonsCompactSprite(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            if (cannons.Count <= 0) return;

            try
            {
                float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
                Vector2 stateOffset = new Vector2(pixelPerChar * 19, 0);
                var maxNameLength = (int)(mySurface.SurfaceSize.X > 300 ? 35 : 20);

                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"Fixed Weapons [{cannons.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Ammo", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine;

                foreach (IMyUserControllableGun cannon in cannons)
                {
                    if (cannon == null) continue;

                    var cannonName = cannon.CustomName.Length > maxNameLength ? cannon.CustomName.Substring(0, maxNameLength) : cannon.CustomName;
                    var currentVolume = (float)cannon.GetInventory(0).CurrentVolume;
                    var maximumVolume = (float)cannon.GetInventory(0).MaxVolume;
                    var ammoType = "No Ammo";
                    var isShooting = cannon.IsShooting;
                    var isRecharging = false;
                    var secondsLeftToRecharge = 0;
                    var secondsToRecharge = 1;
                    var percentValue = (currentVolume / maximumVolume) * 100;

                    string detailedInfo = ((IMyUserControllableGun)cannon).DetailedInfo;
                    string rechargeInfo = "";
                    isRecharging = detailedInfo.Contains("Fully recharged in:") && !detailedInfo.Contains("Fully recharged in: 0 sec");

                    if (isRecharging) // Might be a railgun
                    {
                        secondsToRecharge = 60;

                        string[] s = detailedInfo.Split('\n');

                        if (s.Length > 2)
                        {
                            bool isMinutes = s.Contains("min");
                            rechargeInfo = (isMinutes ? s[2].Replace(" min", "") : s[2].Replace(" sec", "")).Replace("Fully recharged in: ", "");
                            int.TryParse(rechargeInfo, out secondsLeftToRecharge);
                            secondsLeftToRecharge *= isMinutes ? 60 : 1;
                            secondsLeftToRecharge = secondsLeftToRecharge <= 0 ? 60 : secondsLeftToRecharge;
                        }
                    }

                    var rechargeBar = $"  {secondsLeftToRecharge} sec";
                    var state = $"{(!cannon.IsWorking ? "     Off     " : isRecharging ? $"{rechargeBar}" : currentVolume <= 0 ? "NoAmmo" : isShooting ? "  Firing" : "  Ready    ")}";

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {state}", TextAlignment.LEFT, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : isRecharging ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {cannonName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.RIGHT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
                    position += surfaceData.newLine;
                }

                position += surfaceData.newLine;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenWeaponsSummary: Caught Exception while DrawCannonsCompactSprite: {e.ToString()}");
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
