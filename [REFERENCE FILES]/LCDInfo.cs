﻿using Sandbox.Definitions;
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
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace EconomySurvival.LCDInfo.Enhanced
{
    class cargoItemType
    {
        public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        public int amount;
    }

    [MyTextSurfaceScript("LCDInfoScreen", "ES Info LCD Enhanced")]
    public class LCDInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

// ---------- BLOCK DICTIONAIRY. USED TO DEFINE CARGO CATEGORIES, AND POWER BLOCK TYPES----------
        List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        List<IMyPowerProducer> hydroenEngines = new List<IMyPowerProducer>();
		//List<IMyPowerProducer> fuelCells = new List<IMyPowerProducer>();
        //List<IMyPowerProducer> fusionReactors = new List<IMyPowerProducer>();
        List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();

        List<IMyInventory> inventorys = new List<IMyInventory>();
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        Dictionary<string, cargoItemType> cargoOres = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoIngots = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoComponents = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoAmmos = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoHandWeaponAmmos = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoBottles = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoWeapons = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoConsumables = new Dictionary<string, cargoItemType>();
		//Dictionary<string, cargoItemType> cargoFoods = new Dictionary<string, cargoItemType>();
		Dictionary<string, cargoItemType> cargoTools = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoItems = new Dictionary<string, cargoItemType>();

/*
// ---------- HAND WRITTEN ITEM LISTS, IMPORTANT FOR COMPATIBILITY WITH OTHER MODS ----------
		List<string> foodItems = new List<string> {
    		"SparklingWater",
    		"ClangCola",
			"Kosmit_Kola",
			"CosmicCoffee",
			"EuropaTea",
			"Rembrau",
			"Rabenswild",
			"Sektans_Jednosladová",
			"InterBeer",
			"Medik_Vodka",
            "Emergency_Ration",
			"N1roos",
            "Fendom_Fries",
            "Pickled_FatFlies",
			"Feines_Essen",
            "Bits's",
            "Sixdiced_Stew",
            "Burger",
			"ShroomSteak",
            "Bread",
            "Potato",
            "Cabbage",
            "Herbs",
            "Mushrooms",
            "Pumpkin",
            "Soya",
			"Tofu",
			"Wheat"
		};

		List<string> toolItems = new List<string> {
    		"BinocularsItem",
			"PhysicalPaintGun",
            "PaintGunMag"
		};
	
		List<string> handWeaponAmmoItems = new List<string> {
			"NATO_5p56x45mm",
			"Deagle-Mag",
			"Ven_Flamer_HTP_Tank",
			"Ven_40MM_Grenade_Box",
			"Standardized7p62x51mm",
			"Ven_5MM_Jacketed_Box",
			"Ven_357_Jacketed_Box",
			"M1014_Buckshots"
		};
*/

// ---------- FIND THE PARENT GRID ----------
        IMyCubeGrid FindParentGrid(IMyTerminalBlock block)
        {
            var grid = myTerminalBlock.CubeGrid;
            if (grid != null)
            {
                return grid;
            }
            // If no grid found, you can handle the error here or return null
            return null;
        }      

// ---------- FIND GRID MASS----------
float CalculateGridMass(IMyCubeGrid grid)
{
    var gridPhysics = grid.Physics;
    if (gridPhysics != null)
    {
        return gridPhysics.Mass;
    }

    return 0f;
}

// ---------- FIND BLOCK NAMES FOR THE DETAILS SPRITE ----------
        IMyTerminalBlock FindBlockWithName(IMyCubeGrid grid, string blockName)
        {
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);

            foreach (var slimBlock in blocks)
            {
                var block = slimBlock.FatBlock as IMyTerminalBlock;
                if (block != null && block.CustomName == blockName)
                {
                    return block;
                }
            }

            return null;
        }

// ---------- GETTING THE GRID OUR LCD PANEL IS ATTACHED TO ----------
        Vector2 right;
        Vector2 newLine;
        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;
        float textSize = 1.0f;

        bool ConfigCheck = false;

        public LCDInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

// ---------- GETTING POWER BLOCKS AND GROUPING THEM IN CARAGORIES ----------
        public override void Run()
        {
            if (myTerminalBlock.CustomData.Length <= 0)
                CreateConfig();

            LoadConfig();

            if (!ConfigCheck)
                return;

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);

            batteryBlocks.Clear();
            windTurbines.Clear();
            hydroenEngines.Clear();
			//fuelCells.Clear();
            //fusionReactors.Clear();
            solarPanels.Clear();
            reactors.Clear();
            inventorys.Clear();
            tanks.Clear();

            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock is IMyBatteryBlock)
                {
                    batteryBlocks.Add((IMyBatteryBlock)myBlock);
                }
                else if (myBlock is IMyPowerProducer)
                {
                    if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                    {
                        windTurbines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                    {
                        hydroenEngines.Add((IMyPowerProducer)myBlock);
                    }
                    /*
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Fuelcell"))
                    {
                        fuelCells.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Fusion"))
                    {
                        fusionReactors.Add((IMyPowerProducer)myBlock);
                    }
                    */
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

                if (myBlock.HasInventory)
                {
                    for (int i = 0; i < myBlock.InventoryCount; i++)
                    {
                        inventorys.Add(myBlock.GetInventory(i));
                    }
                }
            }

// ---------- GEWTTING CARGO ITEMS AND ASSIGNING THEM TO DIFFERENT CARGO ITEM LISTS ----------
            cargoOres.Clear();
            cargoIngots.Clear();
            cargoComponents.Clear();
            cargoAmmos.Clear();
			cargoHandWeaponAmmos.Clear();
            cargoBottles.Clear();
            cargoWeapons.Clear();
            cargoConsumables.Clear();
            //cargoFoods.Clear();
            cargoTools.Clear();
            cargoItems.Clear();

            foreach (var inventory in inventorys)
            {
                if (inventory.ItemCount == 0)
                    continue;

                inventoryItems.Clear();
                inventory.GetItems(inventoryItems);

                foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                {
                    var type = item.Type.TypeId.Split('_')[1];
					var subtypename = item.Type.SubtypeId;
                    var name = item.Type.SubtypeId;
                    var amount = item.Amount.ToIntSafe();
                    var myType = new cargoItemType { item=item, amount=0 };

                    /*if (subtypename.Contains("Meat") || subtypename.Contains("Apple") || subtypename.Contains("Soup") || subtypename.Contains("Chips") || foodItems.Contains(subtypename)) 
                    {
                        if (!cargoFoods.ContainsKey(name))
                            cargoFoods.Add(name, myType);

                        cargoFoods[name].amount += amount;
                    }
                    else*/ if (subtypename.Contains("HandDrill") || subtypename.Contains("Welder") || subtypename.Contains("Grinder") /*|| subtypename.Contains("FlareGrenade") || subtypename.Contains("DemoCharge") || toolItems.Contains(subtypename)*/) 
                    {
                        if (!cargoTools.ContainsKey(name))
                            cargoTools.Add(name, myType);

                        cargoTools[name].amount += amount;
                    }
                    else if (subtypename.Contains("AutomaticRifleGun") || subtypename.Contains("PistolMagazine") /*|| handWeaponAmmoItems.Contains(subtypename)*/)
                    {
                        if (!cargoHandWeaponAmmos.ContainsKey(name))
                            cargoHandWeaponAmmos.Add(name, myType);

                        cargoHandWeaponAmmos[name].amount += amount;
                    }
					else if (type == "Ore")
                    {
                        if (!cargoOres.ContainsKey(name))
                            cargoOres.Add(name, myType);

                        cargoOres[name].amount += amount;
                    }
                    else if (type == "Ingot")
                    {
                        if (!cargoIngots.ContainsKey(name))
                            cargoIngots.Add(name, myType);

                        cargoIngots[name].amount += amount;
                    }
                    else if (type == "AmmoMagazine")
                    {
                        if (!cargoAmmos.ContainsKey(name))
                            cargoAmmos.Add(name, myType);

                        cargoAmmos[name].amount += amount;
                    }
                    else if (type == "GasContainerObject" ^ type == "OxygenContainerObject")
                    {
                        if (!cargoBottles.ContainsKey(name))
                            cargoBottles.Add(name, myType);

                        cargoBottles[name].amount += amount;
                    }
                    else if (type == "PhysicalGunObject" /*|| subtypename.Contains("Grenade")*/)
                    {
                        if (!cargoWeapons.ContainsKey(name))
                            cargoWeapons.Add(name, myType);

                        cargoWeapons[name].amount += amount;
                    }
                    else if (type == "ConsumableItem")
                    {
                        if (!cargoConsumables.ContainsKey(name))
                            cargoConsumables.Add(name, myType);

                        cargoConsumables[name].amount += amount;
                    }
                    else if (type == "Component")
                    {
                        if (!cargoComponents.ContainsKey(name))
                            cargoComponents.Add(name, myType);

                        cargoComponents[name].amount += amount;
                    }
                    else
                    {
                        if (!cargoItems.ContainsKey(name))
                            cargoItems.Add(name, myType);

                        cargoItems[name].amount += amount;
                    }
                }
            }

// ---------- SPRITE SETUP ----------
// ---------- ARGUEMENTS MUST MATCH THOSE IN "CUSTOM DATA SETTINGS OPTIONS" ----------
            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(5, 5) + myViewport.Position;

            textSize = config.Get(" SETTINGS ", "TextSize").ToSingle(defaultValue: 1.0f);
            right = new Vector2(mySurface.SurfaceSize.X - 10, 0);
            newLine = new Vector2(0, 30 * textSize);
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (config.Get(" POWER ", "Battery").ToBoolean())
                DrawBatterySprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" POWER ", "Wind Turbine").ToBoolean())
                DrawWindTurbineSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" POWER ", "Hydrogen Engine").ToBoolean())
                DrawHydrogenEngineSprite(ref myFrame, ref myPosition, mySurface);
				
            /*    
            if (config.Get(" POWER ", "Fuel Cell").ToBoolean())
                DrawFuelCellSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get(" POWER ", "Fusion Reactor").ToBoolean())
                DrawFusionReactorSprite(ref myFrame, ref myPosition, mySurface);
            */ 

            if (config.Get(" POWER ", "Solar").ToBoolean())
                DrawSolarPanelSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" POWER ", "Nuclear Reactor").ToBoolean())
                DrawReactorSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" TANKS ", "All Tanks").ToBoolean())
                DrawTanksSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get(" TANKS ", "Hydrogen Tanks").ToBoolean())
                DrawHydrogenTanksSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get(" TANKS ", "Oxygen Tanks").ToBoolean())
                DrawOxygenTanksSprite(ref myFrame, ref myPosition, mySurface);
			
            /*
            if (config.Get(" TANKS ", "Deuterium Tanks").ToBoolean())
                DrawDeuteriumTanksSprite(ref myFrame, ref myPosition, mySurface);
            */

            if (config.Get(" MATERIALS ", "Ore").ToBoolean())
                DrawOreSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" MATERIALS ", "Ingot").ToBoolean())
                DrawIngotSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" MATERIALS ", "Component").ToBoolean())
                DrawComponentSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" ITEMS ", "Bottles").ToBoolean())
                DrawBottleSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" ITEMS ", "Tools").ToBoolean())
                DrawToolSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" ITEMS ", "Vehicle Ammo").ToBoolean())
                DrawAmmoSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get(" ITEMS ", "Hand Weapons").ToBoolean())
                DrawWeaponSprite(ref myFrame, ref myPosition, mySurface);
		
			if (config.Get(" ITEMS ", "Hand Weapon Ammo").ToBoolean())
                DrawHandWeaponAmmoSprite(ref myFrame, ref myPosition, mySurface);
				
            if (config.Get(" ITEMS ", "Consumables").ToBoolean())
                DrawConsumableSprite(ref myFrame, ref myPosition, mySurface);

            /*	
            if (config.Get(" ITEMS ", "Food").ToBoolean())
                DrawFoodSprite(ref myFrame, ref myPosition, mySurface);
            */

            if (config.Get(" ITEMS ", "Miscellaneous Items").ToBoolean())
                DrawItemsSprite(ref myFrame, ref myPosition, mySurface);
			
            if (config.Get(" SYSTEMS ", "Damage Report").ToBoolean())
                DrawDamageSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get(" SYSTEMS ", "Grid Info").ToBoolean())
                DrawGridInfoSprite(ref myFrame, ref myPosition, mySurface);

            string blockDetailsValue = config.Get(" SYSTEMS ", "Block Details").ToString();
            if (blockDetailsValue != "false")
                DrawDetailsSprite(ref myFrame, ref myPosition, mySurface, blockDetailsValue);

                        myFrame.Dispose();
                    }

// ---------- BATTERIES SPRITE ----------
        void DrawBatterySprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = batteryBlocks.Sum(block => block.CurrentStoredPower);
            var total = batteryBlocks.Sum(block => block.MaxStoredPower);
            var input = batteryBlocks.Sum(block => block.CurrentInput);
            var output = batteryBlocks.Sum(block => block.CurrentOutput);

            WriteTextSprite(ref frame, "[ BATTERIES ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Stored Power:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MWh", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Stored Power:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MWh", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Input:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, input.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, output.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

// ---------- WIND TURBINE SPRITE ----------
        void DrawWindTurbineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = windTurbines.Sum(block => block.CurrentOutput);
            var currentMax = windTurbines.Sum(block => block.MaxOutput);
            var total = windTurbines.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

            WriteTextSprite(ref frame, "[ WIND TURBINES ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

// ---------- HYDROGEN ENGINE SPRITE ----------
        void DrawHydrogenEngineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = hydroenEngines.Sum(block => block.CurrentOutput);
            var total = hydroenEngines.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ HYDROGEN ENGINES ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

/*
// ---------- HYDROGEN FUEL CELL SPRITE ----------		
        void DrawFuelCellSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = fuelCells.Sum(block => block.CurrentOutput);
            var total = fuelCells.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ HYDROGEN FUEL CELLS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

// ---------- FUSION REACTOR SPRITE ----------		
        void DrawFusionReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = fusionReactors.Sum(block => block.CurrentOutput);
            var total = fusionReactors.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ FUSION REACTORS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }
*/

// ---------- SOLAR PANEL SPRITE ----------
        void DrawSolarPanelSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = solarPanels.Sum(block => block.CurrentOutput);
            var currentMax = solarPanels.Sum(block => block.MaxOutput);
            var total = solarPanels.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

            WriteTextSprite(ref frame, "[ SOLAR PANELS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

// ---------- NUCLEAR REACTOR SPRITE ----------
        void DrawReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = reactors.Sum(block => block.CurrentOutput);
            var total = reactors.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "[ NUCLEAR REACTORS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

// ---------- ALL TANKS SPRITE ----------
        void DrawTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var hydrogenTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
            var oxygenTanks = tanks.Where(block => ((!block.BlockDefinition.SubtypeName.Contains("Hydrogen")) /*&& (!block.BlockDefinition.SubtypeName.Contains("Deuterium"))*/));
            //var deuteriumTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Deuterium"));

            var currentHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Average(block => block.FilledRatio * 100);
            var totalHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Sum(block => block.Capacity);

            var currentOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Average(block => block.FilledRatio * 100);
            var totalOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Sum(block => block.Capacity);
            
            //var currentDeuterium = deuteriumTanks.Count() == 0 ? 0 : deuteriumTanks.Average(block => block.FilledRatio * 100);
            //var totalDeuterium = deuteriumTanks.Count() == 0 ? 0 : deuteriumTanks.Sum(block => block.Capacity);

            WriteTextSprite(ref frame, "[ HYDROGEN TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentHydrogen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalHydrogen), position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "[ OXYGEN TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentOxygen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalOxygen), position + right, TextAlignment.RIGHT);

            /*
            position += newLine;
			
            WriteTextSprite(ref frame, "[ DEUTERIUM TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentDeuterium.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalDeuterium), position + right, TextAlignment.RIGHT);
            */

            position += newLine + newLine;;
        }

// ---------- HYDROGEN TANK ONLY SPRITE ----------		
		void DrawHydrogenTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var hydrogenTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Hydrogen"));

            var currentHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Average(block => block.FilledRatio * 100);
            var totalHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Sum(block => block.Capacity);

            WriteTextSprite(ref frame, "[ HYDROGEN TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentHydrogen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalHydrogen), position + right, TextAlignment.RIGHT);

            position += newLine + newLine;;
        }

// ---------- OXYGEN TANK ONLY SPRITE ----------		
		void DrawOxygenTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var oxygenTanks = tanks.Where(block => ((!block.BlockDefinition.SubtypeName.Contains("Hydrogen")) && (!block.BlockDefinition.SubtypeName.Contains("Deuterium"))));

            var currentOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Average(block => block.FilledRatio * 100);
            var totalOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Sum(block => block.Capacity);

            WriteTextSprite(ref frame, "[ OXYGEN TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentOxygen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalOxygen), position + right, TextAlignment.RIGHT);

            position += newLine + newLine;;
        }

/*
// ---------- DEUTERIUM TANK ONLY SPRITE ----------		
		void DrawDeuteriumTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var deuteriumTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Deuterium"));

            var currentDeuterium = deuteriumTanks.Count() == 0 ? 0 : deuteriumTanks.Average(block => block.FilledRatio * 100);
            var totalDeuterium = deuteriumTanks.Count() == 0 ? 0 : deuteriumTanks.Sum(block => block.Capacity);

            WriteTextSprite(ref frame, "[ DEUTERIUM TANKS ]", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentDeuterium.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalDeuterium), position + right, TextAlignment.RIGHT);

            position += newLine + newLine;;
        }
*/

// ---------- ORE SPRITE ----------
        void DrawOreSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ ORES ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoOres.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoOres[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }

// ---------- INGOTS SPRITE ----------
        void DrawIngotSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ INGOTS ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoIngots.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoIngots[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }

// ---------- COMPONENTS SPRITE ----------
        void DrawComponentSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ COMPONENTS ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoComponents.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoComponents[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }

// ---------- BOTTLES SPRITE ----------
        void DrawBottleSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ BOTTLES ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoBottles.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoBottles[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }

// ---------- TOOLS SPRITE ----------
        void DrawToolSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ TOOLS ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoTools.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoTools[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }

// ---------- VEHICLE AMMUNITION SPRITE ----------
        void DrawAmmoSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ VEHICLE AMMUNITION ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoAmmos.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoAmmos[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }
		
// ---------- HAND WEAPPON SPRITE ----------
        void DrawWeaponSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ HAND WEAPONS ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoWeapons.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoWeapons[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }
		
// ---------- HAND WEAPPON AMMUNITION SPRITE ----------
        void DrawHandWeaponAmmoSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ HAND WEAPON AMMUNITION ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoHandWeaponAmmos.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoHandWeaponAmmos[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }
		
// ---------- CONSUMABLES SPRITE ----------
        void DrawConsumableSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ CONSUMABLES ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoConsumables.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoConsumables[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }
		
/*        
// ---------- FOOD AND DRINK SPRITE ----------
        void DrawFoodSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ FOOD AND DRINK ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoFoods.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoFoods[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }
*/

// ---------- MISCELLANEOUS ITEMS SPRITE ----------
        void DrawItemsSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ MISCELLANEOUS ITEMS ]", position, TextAlignment.LEFT);

            position += newLine;

            
	    var sortedKeys = cargoItems.Keys.ToList();
		sortedKeys.Sort();

		foreach (var name in sortedKeys) {
				var item = cargoItems[name];

                MyDefinitionId.TryParse(item.item.Type.TypeId, name, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
			position += newLine;
        }

// ---------- DAMAGE REPORT SPRITE ----------		
        void DrawDamageSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "[ DAMAGE REPORT ]", position, TextAlignment.LEFT);

            position += newLine;

            var damagedBlocks = new List<IMyTerminalBlock>();

            var grid = FindParentGrid(myTerminalBlock);

            var slimBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(slimBlocks, b => b.CurrentDamage > 0f);

            foreach (var slimBlock in slimBlocks)
            {
                var damagedBlock = slimBlock.FatBlock as IMyTerminalBlock;
                if (damagedBlock != null)
                {
                    var currentDamage = slimBlock.CurrentDamage;
                    var maxIntegrity = slimBlock.MaxIntegrity;
                    var remainingHealth = maxIntegrity - currentDamage;
                    var healthPercentage = (remainingHealth / maxIntegrity) * 100f;

                    WriteTextSprite(ref frame, damagedBlock.CustomName.ToString(), position, TextAlignment.LEFT);
                    WriteTextSprite(ref frame, $"{healthPercentage:0.00}%", position + right, TextAlignment.RIGHT);

                    position += newLine;

                    damagedBlocks.Add(damagedBlock);
                }
            }

            if (damagedBlocks.Count == 0)
            {
                WriteTextSprite(ref frame, "No damage detected", position, TextAlignment.LEFT);
                position += newLine;
            }

            position += newLine;
        }

// ---------- GRID INFO SPRITE ----------		
        void DrawGridInfoSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var grid = FindParentGrid(myTerminalBlock);
            string gridName = grid.DisplayName;
            int nonArmorBlockCount = 0;
            float gridMass = CalculateGridMass(grid);

            WriteTextSprite(ref frame, "[ GRID INFO ]", position, TextAlignment.LEFT);
            position += newLine;
            WriteTextSprite(ref frame, $"Grid Name: {gridName}", position, TextAlignment.LEFT);
            position += newLine;
            WriteTextSprite(ref frame, $"Grid Mass: {gridMass:0.00} kg", position, TextAlignment.LEFT);
            position += newLine;

            // Retrieve all blocks on the grid 
            var allBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(allBlocks);

            int blockCount = allBlocks.Count;
            WriteTextSprite(ref frame, $"Number of Blocks: {blockCount}", position, TextAlignment.LEFT);
            position += newLine;

            // Count up the non-armor blocks
            foreach (var gridBlock in allBlocks)
            {
                if (gridBlock.FatBlock != null && gridBlock.FatBlock.BlockDefinition.TypeId.ToString() != "MyObjectBuilder_CubeBlock/ArmorBlock")
                {
                    nonArmorBlockCount++;
                }
            }

            int armorBlockCount = blockCount - nonArmorBlockCount;

            WriteTextSprite(ref frame, $"Number of Non-Armor Blocks: {nonArmorBlockCount}", position, TextAlignment.LEFT);
            position += newLine;
            WriteTextSprite(ref frame, $"Number of Armor Blocks: {armorBlockCount}", position, TextAlignment.LEFT);
            position += newLine;

            position += newLine;
        }

// ---------- BLOCK DETAILS SPRITE ----------	
        void DrawDetailsSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface, string blockName)
        {
            var grid = FindParentGrid(myTerminalBlock);

            WriteTextSprite(ref frame, "[ BLOCK DETAILS ]", position, TextAlignment.LEFT);
            position += newLine;

            // Find the block with the provided name on the same grid
            var block = FindBlockWithName(grid, blockName) as IMyTerminalBlock;
            if (block != null)
            {
                WriteTextSprite(ref frame, $"Block Name: {block.CustomName}", position, TextAlignment.LEFT);
                position += newLine;

                // Display DetailedInfo of the matching block
                var detailedInfo = block.DetailedInfo;
                WriteTextSprite(ref frame, $"Block {detailedInfo}", position, TextAlignment.LEFT);
                position += newLine;
            }
            else
            {
                WriteTextSprite(ref frame, "Unable to find block", position, TextAlignment.LEFT);
                position += newLine;
                WriteTextSprite(ref frame, "Verify block name in Custom Data", position, TextAlignment.LEFT);
                position += newLine;
            }

            position += newLine;
        }

// ---------- UNIT FORMAT ----------
        static string KiloFormat(int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + " M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0 K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + " K";

            return num.ToString("#,0");

        }
// ---------- UTILITY METHOD TO WTIRE TEXT SPRITES ----------
        void WriteTextSprite(ref MySpriteDrawFrame frame, string text, Vector2 position, TextAlignment alignment)
        {
            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = textSize,
                Color = mySurface.ScriptForegroundColor,
                Alignment = alignment,
                FontId = "White"
            };

            frame.Add(sprite);
        }

// ---------- CUSTOM DATA SETTINGS OPTIONS ----------
        private void CreateConfig()
        {
            config.AddSection(" SETTINGS ");
            config.Set(" SETTINGS ", "TextSize", "1.0");
            config.AddSection(" POWER ");
            config.Set(" POWER ", "Battery", "false");
            config.Set(" POWER ", "Wind Turbine", "false");
            config.Set(" POWER ", "Hydrogen Engine", "false");
            //config.Set(" POWER ", "Fuel Cell", "false");
            //config.Set(" POWER ", "Fusion Reactor", "false");
            config.Set(" POWER ", "Solar", "false");
            config.Set(" POWER ", "Nuclear Reactor", "false");
            config.AddSection(" TANKS ");
            config.Set(" TANKS ", "All Tanks", "false");
            config.Set(" TANKS ", "Hydrogen Tanks", "false");
            config.Set(" TANKS ", "Oxygen Tanks", "false");
            //config.Set(" TANKS ", "Deuterium Tanks", "false");
            config.AddSection(" MATERIALS ");
            config.Set(" MATERIALS ", "Ore", "false");
            config.Set(" MATERIALS ", "Ingot", "false");
            config.Set(" MATERIALS ", "Component", "false");
            config.AddSection(" ITEMS ");
            config.Set(" ITEMS ", "Bottles", "false");
            config.Set(" ITEMS ", "Tools", "false");
            config.Set(" ITEMS ", "Vehicle Ammo", "false");
			config.Set(" ITEMS ", "Hand Weapons", "false");
			config.Set(" ITEMS ", "Hand Weapon Ammo", "false");
			config.Set(" ITEMS ", "Consumables", "false");
			//config.Set(" ITEMS ", "Food", "false");
            config.Set(" ITEMS ", "Miscellaneous Items", "false");
			config.AddSection(" SYSTEMS ");
            config.Set(" SYSTEMS ", "Damage Report", "false");
            config.Set(" SYSTEMS ", "Grid Info", "false");
            config.Set(" SYSTEMS ", "Block Details", "false");

    config.Invalidate();
    myTerminalBlock.CustomData = config.ToString();
}

        private void LoadConfig()
        {
            ConfigCheck = false;

            if (config.TryParse(myTerminalBlock.CustomData))
            {
                if (config.ContainsSection(" SETTINGS "))
                {
                    ConfigCheck = true;
                }
                else if (config.ContainsSection(" POWER "))
                {
                    ConfigCheck = true;
                }
                else if (config.ContainsSection(" TANKS "))
                {
                    ConfigCheck = true;
                }
                else if (config.ContainsSection(" ITEMS "))
                {
                    ConfigCheck = true;
                }
				else if (config.ContainsSection(" SYSTEMS "))
                {
                    ConfigCheck = true;
                }
                else
                {
                    MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Value error");
                }
            }
            else
            {
                MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Syntax error");
            }
        }
    }
}