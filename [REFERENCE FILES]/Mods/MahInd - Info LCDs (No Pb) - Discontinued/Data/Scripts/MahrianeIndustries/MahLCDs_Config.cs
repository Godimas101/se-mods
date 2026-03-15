using MahrianeIndustries.LCDInfo;
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
    public static class MahDefinitions
    {
        public static float pixelPerChar = 6.0f;

        // All Vanilla items. If you want to add modded items, simply duplicate the last line, of the desired typeId...
        // ...enter the mod items IMyInventoryItem.Type.SubtypeId into the subtypeId = "id here"
        // ...enter the mod items displayed name into the displayName = "displayed name here"
        // ...enter the mod items volume from the ingame UI into the volume = xf
        // ...enter a desired minAmount into the minAmount = xf field.
        // If you don't want to have a minAmount (will hide the status bar on LCD screen) just set minAmount = 0
        public static List<CargoItemDefinition> cargoItemDefinitions = new List<CargoItemDefinition>
        {
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Cobalt",                               displayName = "Cobalt",                 volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Gold",                                 displayName = "Gold",                   volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Ice",                                  displayName = "Ice",                    volume = 0.37f,     minAmount = 20000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Iron",                                 displayName = "Iron",                   volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Magnesium",                            displayName = "Magnesium",              volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Nickel",                               displayName = "Nickel",                 volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Platinum",                             displayName = "Platinum",               volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Silicon",                              displayName = "Silicon",                volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Silver",                               displayName = "Silver",                 volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Stone",                                displayName = "Stone",                  volume = 0.37f,     minAmount = 10000   },
            new CargoItemDefinition { typeId = "Ore",           subtypeId = "Uranium",                              displayName = "Uranium",                volume = 0.37f,     minAmount = 10000   },

            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Cobalt",                                displayName = "Cobalt",                 volume = 0.112f,    minAmount =  25000  },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Gold",                                  displayName = "Gold",                   volume = 0.052f,    minAmount =   5000  },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Iron",                                  displayName = "Iron",                   volume = 0.127f,    minAmount = 100000  },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Magnesium",                             displayName = "Magnesium Pow.",         volume = 0.575f,    minAmount = 15000   },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Nickel",                                displayName = "Nickel",                 volume = 0.112f,    minAmount = 25000   },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Platinum",                              displayName = "Platinum",               volume = 0.047f,    minAmount = 2000    },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Silicon",                               displayName = "Silicon Waf.",           volume = 0.429f,    minAmount = 15000   },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Silver",                                displayName = "Silver",                 volume = 0.095f,    minAmount = 5000    },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Stone",                                 displayName = "Gravel",                 volume = 0.37f,     minAmount = 5000    },
            new CargoItemDefinition { typeId = "Ingot",        subtypeId = "Uranium",                               displayName = "Uranium",                volume = 0.052f,    minAmount = 2000    },

            new CargoItemDefinition { typeId = "Component",    subtypeId = "BulletproofGlass",                      displayName = "Bulletproof Glass",      volume = 8.0f,      minAmount = 12000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Canvas",                                displayName = "Canvas",                 volume = 8.0f,      minAmount = 300     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Computer",                              displayName = "Computer",               volume = 1.0f,      minAmount = 6500    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Construction",                          displayName = "Construction",           volume = 2.0f,      minAmount = 50000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Detector",                              displayName = "Detector Compnts",       volume = 6.0f,      minAmount = 400     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Display",                               displayName = "Displays",               volume = 6.0f,      minAmount = 500     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "EngineerPlushie",                       displayName = "Engineer Plushie",       volume = 3.0f,      minAmount = 1       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Explosives",                            displayName = "Explosives",             volume = 2.0f,      minAmount = 500     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Girder",                                displayName = "Girder",                 volume = 2.0f,      minAmount = 3500    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "GravityGenerator",                      displayName = "Gravity Generators",     volume = 200.0f,    minAmount = 250     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "InteriorPlate",                         displayName = "Interior Plates",        volume = 5.0f,      minAmount = 55000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "LargeTube",                             displayName = "Large Steeltubes",       volume = 38.0f,     minAmount = 6000    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Medical",                               displayName = "Medical Compnts",        volume = 160.0f,    minAmount = 120     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "MetalGrid",                             displayName = "Metalgrids",             volume = 15.0f,     minAmount = 15500   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Motor",                                 displayName = "Motors",                 volume = 8.0f,      minAmount = 16000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "PowerCell",                             displayName = "Powercells",             volume = 40.0f,     minAmount = 2800    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "RadioCommunication",                    displayName = "Radio Comms.",           volume = 70.0f,     minAmount = 250     },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Reactor",                               displayName = "Reactor Compnts",        volume = 8.0f,      minAmount = 10000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SabiroidPlushie",                       displayName = "Sabiroid Plushie",       volume = 3.0f,      minAmount = 1       },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SolarCell",                             displayName = "Solar Cells",            volume = 12.0f,     minAmount = 2800    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SmallTube",                             displayName = "Small Steel Tubes",      volume = 2.0f,      minAmount = 26000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "SteelPlate",                            displayName = "Steelplates",            volume = 3.0f,      minAmount = 300000  },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Superconductor",                        displayName = "Super Conductors",       volume = 8.0f,      minAmount = 3000    },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "Thrust",                                displayName = "Thruster Compnts",       volume = 10.0f,     minAmount = 16000   },
            new CargoItemDefinition { typeId = "Component",    subtypeId = "ZoneChip",                              displayName = "Zone Chips",             volume = 0.2f,      minAmount = 100     },

            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "NATO_25x184mm",                         displayName = "Gatling Ammo Box",       volume = 16.0f,     minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "NATO_5p56x45mm",                        displayName = "5.56x45mm Mag",          volume = 0.2f,      minAmount = 100     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "Missile200mm",                          displayName = "Rocket",                 volume = 60.0f,     minAmount = 200     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "AutocannonClip",                        displayName = "Autocannon Mag",         volume = 24.0f,     minAmount = 500     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FlareClip",                             displayName = "Flare Gun Clip",         volume = 0.05f,     minAmount = 10      },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "LargeCalibreAmmo",                      displayName = "Artillery Shell",        volume = 100.0f,    minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "LargeRailgunAmmo",                      displayName = "Large Railgun Sabot",    volume = 40.0f,     minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "MediumCalibreAmmo",                     displayName = "Assault Cannon Shell",   volume = 30.0f,     minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "SmallRailgunAmmo",                      displayName = "Small Railgun Sabot",    volume = 8.0f,      minAmount = 250     },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "SemiAutoPistolMagazine",                displayName = "S-10 Pistol Mag",        volume = 0.1f,      minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "ElitePistolMagazine",                   displayName = "S-10E Pistol Mag",       volume = 0.1f,      minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "FullAutoPistolMagazine",                displayName = "S-20A Pistol Mag",       volume = 0.15f,     minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "AutomaticRifleGun_Mag_20rd",            displayName = "MR-20 Rifle Mag",        volume = 0.2f,      minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "PreciseAutomaticRifleGun_Mag_5rd",      displayName = "MR-8P Rifle Mag",        volume = 0.15f,     minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "RapidFireAutomaticRifleGun_Mag_50rd",   displayName = "MR-50A Rifle Mag",       volume = 0.5f,      minAmount = 1000    },
            new CargoItemDefinition { typeId = "AmmoMagazine", subtypeId = "UltimateAutomaticRifleGun_Mag_30rd",    displayName = "MR-30E Rifle Mag",       volume = 0.3f,      minAmount = 1000    },
        
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinderItem",                 displayName = "Grinder",                volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrillItem",                    displayName = "Hand Drill",             volume = 25.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "WelderItem",                       displayName = "Welder",                 volume = 8.0f,      minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinder2Item",                displayName = "Enhanced Grinder",       volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrill2Item",                   displayName = "Enhanced Hand Drill",    volume = 25.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "Welder2Item",                      displayName = "Enhanced Welder",        volume = 8.0f,      minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinder3Item",                displayName = "Proficient Grinder",     volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrill3Item",                   displayName = "Proficient Hand Drill",  volume = 25.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "Welder3Item",                      displayName = "Proficient Welder",      volume = 8.0f,      minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AngleGrinder4Item",                displayName = "Elite Grinder",          volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "HandDrill4Item",                   displayName = "Elite Hand Drill",       volume = 25.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "Welder4Item",                      displayName = "Elite Welder",           volume = 8.0f,      minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "AutomaticRifleItem",               displayName = "MR-20 Rifle",            volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "PreciseAutomaticRifleItem",        displayName = "MR-8P Rifle",            volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "RapidFireAutomaticRifleItem",      displayName = "MR-50A Rifle",           volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "UltimateAutomaticRifleItem",       displayName = "MR-30E Rifle",           volume = 20.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "SemiAutoPistolItem",               displayName = "S-10 Pistol",            volume =  6.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "ElitePistolItem",                  displayName = "S-10E Pistol",           volume =  6.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "FullAutoPistolItem",               displayName = "S-20A Pistol",           volume =  8.0f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "BasicHandHeldLauncherItem",        displayName = "R-01 Rocket Launcher",   volume = 125.0f,    minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalGunObject", subtypeId = "FlareGunItem",                     displayName = "Flare Gun",              volume = 0.37f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "PhysicalObject",    subtypeId = "SpaceCredit",                      displayName = "Space Credits",          volume = 0.37f,     minAmount =   0     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Medkit",                           displayName = "Medkits",                volume = 0.37f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "Powerkit",                         displayName = "Powerkits",              volume = 0.37f,     minAmount =  10     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "ClangCola",                        displayName = "Clang Cola",             volume = 1.0f,      minAmount =   0     },
            new CargoItemDefinition { typeId = "ConsumableItem",    subtypeId = "CosmicCoffee",                     displayName = "Cosmic Coffee",          volume = 1.0f,      minAmount =   0     },
            new CargoItemDefinition { typeId = "Datapad",           subtypeId = "Datapad",                          displayName = "Datapads",               volume = 0.4f,      minAmount =   0     },
            new CargoItemDefinition { typeId = "Package",           subtypeId = "Package",                          displayName = "Packages",               volume = 125.0f,    minAmount =   0     },
            
            new CargoItemDefinition { typeId = "OxygenContainerObject", subtypeId = "OxygenBottle",                 displayName = "Oxygen Bottle",          volume = 120.0f,    minAmount =  10     },
            new CargoItemDefinition { typeId = "GasContainerObject",    subtypeId = "HydrogenBottle",               displayName = "Hydrogen Bottle",        volume = 120.0f,    minAmount =  10     },
        };

        public static CargoItemDefinition GetDefinition(string typeId, string subtypeId)
        {
            foreach(CargoItemDefinition definition in MahDefinitions.cargoItemDefinitions)
            {
                if (definition.typeId != typeId) continue;
                if (subtypeId != definition.subtypeId && !subtypeId.Contains(definition.subtypeId)) continue;

                return definition;
            }

            return null;
        }

        public static string WattFormat(double num)
        {
            // Values from power production blocks come in MW / MWh so we need to first get W
            num *= 1000000;

            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 MW");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.# MW");

            if (num >= 100000)
                return (num / 1000).ToString("#,0 kW");

            if (num >= 10000)
                return (num / 1000).ToString("0.# kW");

            return num.ToString("#,0 W");
        }

        public static string KiloFormat(double num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.# M");

            if (num >= 100000)
                return (num / 1000).ToString("#,0 K");

            if (num >= 10000)
                return (num / 1000).ToString("0.# K");

            return num.ToString("#,0");
        }

        public static string LiterFormat(double num)
        {
            if (num >= 100000)
                return (num / 1000).ToString("0.0 hL");

            if (num >= 10000)
                return (num / 1000).ToString("0.00 L");

            return num.ToString("0.00 L");
        }

        public static string TimeFormat (int num)
        {
            TimeSpan t = TimeSpan.FromSeconds(num);

            return string.Format("{0:D1}h {1:D2}m",
                            t.Hours,
                            t.Minutes);

            /*
            return string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                            t.Hours,
                            t.Minutes,
                            t.Seconds,
                            t.Milliseconds);
            */
        }

        public static string CurrentTimeStamp
        {
            get
            {
                System.DateTime moment = DateTime.Now;
                int hours = moment.Hour;
                int minutes = moment.Minute;
                int seconds = moment.Second;

                return $"{hours.ToString("#00").Replace("1", " 1")}:{minutes.ToString("#00").Replace("1", " 1")}:{seconds.ToString("#00").Replace("1", " 1")}";
            }
        }
    }

    public static class MahUtillities
    {
        public static IMySlimBlock GetSlimblock(IMyTerminalBlock block) => (block.CubeGrid as MyCubeGrid).GetCubeBlock(block.Position);

        public static float GetMaxOutput(List<IMyPowerProducer> powerProducers)
        {
            List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
            List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
            List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
            List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
            List<IMyReactor> reactors = new List<IMyReactor>();

            foreach(IMyPowerProducer block in powerProducers)
            {
                if (block is IMyBatteryBlock)
                {
                    batteries.Add((IMyBatteryBlock)block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Wind"))
                {
                    windTurbines.Add(block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                {
                    hydrogenEngines.Add(block);
                }
                else if (block is IMyReactor)
                {
                    reactors.Add((IMyReactor)block);
                }
                else if (block is IMySolarPanel)
                {
                    solarPanels.Add((IMySolarPanel)block);
                }
            }

            float value = windTurbines.Sum(block => block.MaxOutput);
            value += solarPanels.Sum(block => block.MaxOutput);
            value += hydrogenEngines.Sum(block => block.MaxOutput);
            value += batteries.Sum(block => block.MaxOutput);
            value += reactors.Sum(block => block.MaxOutput);

            return value;
        }

        public static float GetCurrentOutput(List<IMyPowerProducer> powerProducers)
        {
            List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
            List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
            List<IMyPowerProducer> hydrogenEngines = new List<IMyPowerProducer>();
            List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
            List<IMyReactor> reactors = new List<IMyReactor>();

            foreach (IMyPowerProducer block in powerProducers)
            {
                if (block is IMyBatteryBlock)
                {
                    batteries.Add((IMyBatteryBlock)block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Wind"))
                {
                    windTurbines.Add(block);
                }
                else if (((MyCubeBlock)block).BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                {
                    hydrogenEngines.Add(block);
                }
                else if (block is IMyReactor)
                {
                    reactors.Add((IMyReactor)block);
                }
                else if (block is IMySolarPanel)
                {
                    solarPanels.Add((IMySolarPanel)block);
                }
            }

            float value = windTurbines.Sum(block => block.CurrentOutput);
            value += solarPanels.Sum(block => block.CurrentOutput);
            value += hydrogenEngines.Sum(block => block.CurrentOutput);
            value += batteries.Sum(block => block.CurrentOutput);
            value += reactors.Sum(block => block.CurrentOutput);
            value -= batteries.Sum(block => block.CurrentInput);

            return value;
        }

        public static float GetPowerTimeLeft(List<IMyPowerProducer> powerProducers)
        {
            var timeRemaining = 0.0f;

            try
            {
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                List<IMyReactor> reactors = new List<IMyReactor>();

                foreach (var block in powerProducers)
                {
                    if (block is IMyBatteryBlock)
                        batteries.Add((IMyBatteryBlock)block);
                    else if (block is IMyReactor)
                        reactors.Add((IMyReactor)block);
                }

                // Calculate time left depending on stored Power in batteries
                if (batteries.Count > 0)
                {
                    var currentBatteryInput = batteries.Sum(block => block.CurrentInput);
                    var currentBatteryOutput = batteries.Sum(block => block.CurrentOutput);
                    var currentStoredPower = batteries.Sum(block => block.CurrentStoredPower);
                    var maximumStoredPower = batteries.Sum(block => block.MaxStoredPower);
                    // Only take battery input into account, when actually loading, not when hopping forward and back <2% close to maxStorage to minimize output stutter.
                    var absoluteBatteryOutput = currentBatteryOutput - (currentStoredPower / maximumStoredPower > 0.98 ? 0 : currentBatteryInput);

                    if (absoluteBatteryOutput > 0)
                    {
                        timeRemaining += (currentStoredPower / absoluteBatteryOutput) * 3600;
                    }
                }
                // If there are no batteries on this grid, try to get a rough estimation from the installed reactors or engines
                else
                {
                    // Reactors & Uranium are only used, if there are no batteries
                    if (reactors.Count > 0)
                    {
                        var currentReactorOutput = reactors.Sum(block => block.CurrentOutput);
                        var currentReactorMaxOutput = reactors.Sum(block => block.MaxOutput);

                        if (currentReactorOutput > 0)
                        {
                            timeRemaining = (MahUtillities.GetItemAmountFromBlockList(reactors.Select(x => x as IMyCubeBlock).ToList(), "Ingot", "Uranium") / currentReactorOutput) * 3600;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while calculating PowerTimeLeft: {e.ToString()}");
            }

            return timeRemaining;
        }

        public static float GetItemAmountFromBlockList(List<IMyCubeBlock> blocks, string targetTypeId, string targetSubtypeId)
        {
            var amount = 0.0f;

            try
            {
                List<IMyInventory> inventorys = new List<IMyInventory>();
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

                foreach (var block in blocks)
                {
                    if (block == null) continue;
                    if (!block.HasInventory) continue;

                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        inventorys.Add(block.GetInventory(i));
                    }
                }

                foreach (var inventory in inventorys)
                {
                    if (inventory == null) continue;
                    if (inventory.ItemCount == 0) continue;

                    inventory.GetItems(inventoryItems);

                    foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                    {
                        if (item == null) continue;

                        var typeId = item.Type.TypeId.Split('_')[1];
                        var subtypeId = item.Type.SubtypeId;
                        var currentAmount = item.Amount.ToIntSafe();

                        if (typeId == targetTypeId || typeId.Contains(targetTypeId))
                        {
                            if (subtypeId == targetSubtypeId || subtypeId.Contains(targetSubtypeId))
                            {
                                amount += currentAmount;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while GetItemAmountFromBlockList: {e.ToString()}");
            }

            return amount;
        }

        public static int GetIceAmountFromBlockList(List<IMyGasGenerator> generators)
        {
            var amount = 0;

            try
            {
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                foreach (var generator in generators)
                {
                    if (generator == null) continue;

                    generator.GetInventory(0).GetItems(inventoryItems);

                    foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                    {
                        if (item == null) continue;

                        var subtypeId = item.Type.SubtypeId;

                        if (subtypeId.Contains("Ice"))
                        {
                            amount += item.Amount.ToIntSafe();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while GetItemAmountFromBlockList: {e.ToString()}");
            }

            return amount;
        }

        public static string GetSubstring(string s, SurfaceDrawer.SurfaceData surfaceData, bool cutLeft = false)
        {
            int maxLength = (int)(surfaceData.titleOffset / MahDefinitions.pixelPerChar * surfaceData.textSize);
            s = s.Length <= maxLength ? s : s.Substring(cutLeft ? 0 : (s.Length - maxLength - 1), maxLength);

            return s;
        }

        public static GasTankVolumes GetGasTankVolumeFromTanks (List<IMyGasTank> allTanks, string gasSubtypeId)
        {
            GasTankVolumes volumes = new GasTankVolumes();

            try
            {
                if (allTanks.Count > 0)
                {
                    var tanks = allTanks.Where(block => gasSubtypeId == "Hydrogen" ? block.BlockDefinition.SubtypeName.Contains(gasSubtypeId) : !block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
                    volumes.totalVolume = (float)(tanks.Count() == 0 ? 0 : tanks.Sum(block => block.Capacity));
                    volumes.currentVolume = (float)(tanks.Count() == 0 ? 0 : tanks.Average(block => block.FilledRatio) * volumes.totalVolume);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenPowerSummary: Caught Exception while GetGasTankVolumeFromTanks: {e.ToString()}");
            }

            return volumes;
        }

        public static List<MyCubeBlock> GetBlocks (MyCubeGrid cubeGrid, string searchId, List<string> excludeIds, ref Sandbox.ModAPI.Ingame.MyShipMass gridMass, bool includeSubGrids = false, bool includeDocked = false)
        {
            if (cubeGrid == null) return null;

            var myFatBlocks = cubeGrid.GetFatBlocks().Where(block => block is IMyTerminalBlock);
            List<MyCubeBlock> allBlocks = new List<MyCubeBlock>();
            List<MyCubeGrid> scannedGrids = new List<MyCubeGrid>();

            scannedGrids.Add(cubeGrid);
            
            try
            {
                foreach (var block in myFatBlocks)
                {
                    if (block == null) continue;

                    if (block is IMyShipController)
                    {
                        gridMass = (block as IMyShipController).CalculateShipMass();
                        continue;
                    }

                    // If docked grids should be included.
                    if (includeDocked)
                    {
                        // Try get a ship connector
                        if (block is IMyShipConnector)
                        {
                            IMyShipConnector connector = block as IMyShipConnector;

                            // Check if connector is connected to something.
                            if (connector.IsConnected)
                            {
                                // Get the connected IMyShipConnector.
                                IMyShipConnector otherConnector = connector.OtherConnector;

                                if (otherConnector != null)
                                {
                                    // Get the grid connected to the IMyShipConnector.
                                    MyCubeGrid connectedGrid = otherConnector.CubeGrid as MyCubeGrid;

                                    // If there is a grid connected, try scanning that.
                                    if (connectedGrid != null)
                                    {
                                        // Abort if the grid of the base has been scanned before.
                                        if (!scannedGrids.Contains(connectedGrid))
                                        {
                                            // Scan all blocks from the connectedGrid, but disable showDocked for this scan...otherwise an endless loop is produced crashing the game.
                                            allBlocks.AddRange(GetBlocks(connectedGrid, searchId, excludeIds, ref gridMass, includeSubGrids, false));
                                        }

                                        // Add the other grid to the allready scanned grids.
                                        scannedGrids.Add(connectedGrid);
                                    }
                                }
                            }
                        }
                    }

                    // Scan SubGrids if this is either a baseBlock or topBlock of some mechanical connection.
                    if (includeSubGrids)
                    {
                        // Block is rotor, piston or hinge base
                        if (block is IMyMechanicalConnectionBlock)
                        {
                            // Get block as base.
                            IMyMechanicalConnectionBlock baseBlock = block as IMyMechanicalConnectionBlock;

                            // Get the top of that base.
                            var topBlock = baseBlock.Top;

                            // Get the grid of that top.
                            MyCubeGrid subGrid = topBlock != null ? topBlock.CubeGrid as MyCubeGrid : null;

                            // Scan all blocks of that top/subGrid.
                            allBlocks.AddRange(GetBlocks(subGrid, searchId, excludeIds, ref gridMass, true, includeDocked));
                        }
                    }

                    // Check if block is valid or should be ignored.
                    if (!HasValidId(block as IMyTerminalBlock, searchId, excludeIds)) continue;

                    allBlocks.Add(block as MyCubeBlock);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.MahUtillities: Caught Exception while GetBlocks: {e.ToString()}");
            }

            return allBlocks;
        }

        public static List<IMyInventory> GetInventories(MyCubeGrid cubeGrid, string searchId, List<string> excludeIds, ref Sandbox.ModAPI.Ingame.MyShipMass gridMass, bool includeSubGrids = false, bool includeDocked = false)
        {
            List<IMyInventory> inventories = new List<IMyInventory>();

            if (cubeGrid == null) return inventories;

            // Only grab those terminal blocks that actually have an inventory.
            var myFatBlocks = GetBlocks(cubeGrid, searchId, excludeIds, ref gridMass, includeSubGrids, includeDocked).Where(block => block.HasInventory).ToList();

            foreach (var block in myFatBlocks)
            {
                if (block == null) continue;
                if (!HasValidId(block as IMyTerminalBlock, searchId, excludeIds)) continue;

                for (int i = 0; i < block.InventoryCount; i++)
                {
                    inventories.Add(block.GetInventory(i));
                }
            }

            return inventories;
        }

        public static bool HasValidId (IMyTerminalBlock block, string searchId, List<string> excludeIds)
        {
            if (block == null) return false;

            // If this is a multi-layer searchId, scan every one of them
            if (searchId.Contains(",")) return HasValidId(block, searchId.Split(',').ToList(), excludeIds);

            string blockId = block.CustomName.ToLower();
            searchId = searchId.ToLower();
            
            if (searchId != "*" && searchId.Trim() != "")
            {
                if (!blockId.Contains(searchId))
                {
                    return false;
                }
            }
            
            foreach (string s in excludeIds)
            {
                string ex = s.ToLower();

                if (ex == "*") continue;
                if (ex == searchId) continue;
                if (ex.Trim().Length <= 0) continue;
                if (ex.Contains("<")) continue;
                if (blockId.Contains(ex))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasValidId (IMyTerminalBlock block, List<string> searchIds, List<string> excludeIds)
        {
            if (block == null) return false;

            string blockId = block.CustomName.ToLower();

            foreach (string s in excludeIds)
            {
                string excludeId = s.ToLower();

                if (excludeId == "*") continue;
                if (excludeId.Trim().Length <= 0) continue;
                if (excludeId.Contains("<")) continue;
                if (searchIds.Contains(s)) continue;
                if (searchIds.Contains(s.ToLower())) continue;
                if (blockId.Contains(excludeId))
                {
                    return false;
                }
            }

            foreach (string s in searchIds)
            {
                string searchId = s.Trim().ToLower();
                if (searchId == "*") return true;
                if (String.IsNullOrEmpty(searchId) || searchId == "" || searchId.Length < 3) continue;

                if (blockId.Contains(searchId))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class CargoItemType
    {
        public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        public CargoItemDefinition definition;
        public int amount;
    }

    public class CargoItemDefinition
    {
        public int minAmount;
        public float volume;
        public string typeId;
        public string subtypeId;
        public string displayName;
    }

    public enum Unit
    {
        None,
        Count,
        Percent,
        Kilograms,
        Liters,
        Watt,
        WattHours,
    }

    public struct GasTankVolumes
    {
        public float currentVolume;
        public float totalVolume;
        public float ratio => (currentVolume / totalVolume) * 100;
    }

    public struct BlockStateData
    {
        public IMyTerminalBlock block;
        public IMySlimBlock slimBlock;
        public int priority;

        public BlockStateData (IMyTerminalBlock block)
        {
            this.block = block;
            slimBlock = MahUtillities.GetSlimblock(block);

            if (block is IMyUserControllableGun)
                priority = 0;
            else if (block is IMyPowerProducer)
                priority = 0;
            else if (block is IMyShipToolBase)
                priority = 1;
            else if (block is IMyCockpit)
                priority = 2;
            else if (block is IMyCryoChamber)
                priority = 2;
            else if (block is IMyMedicalRoom)
                priority = 2;
            else if (block is IMyGasGenerator)
                priority = 3;
            else if (block is IMyGasTank)
                priority = 3;
            else if (block is IMyAirVent)
                priority = 3;
            else if (block is IMyOxygenFarm)
                priority = 3;
            else if (block is IMyDoor)
                priority = 3;
            else if (block is IMyCargoContainer)
                priority = 4;
            else if (block is IMyProductionBlock)
                priority = 4;
            else
                priority = 10;
        }

        public bool IsNull => block == null || slimBlock == null;
        public bool IsWorking => block != null ? block.IsWorking : false;
        public bool IsFunctional => block != null ? block.IsFunctional : false;
        public bool IsBeingHacked => block != null ? block.IsBeingHacked : false;
        public bool IsFullIntegrity => slimBlock != null ? slimBlock.IsFullIntegrity : true;
        public bool IsWeapon => block != null ? block is IMyUserControllableGun : false;
        public bool IsRecharging => block != null ? block.DetailedInfo.Contains("Fully recharged in:") && !block.DetailedInfo.Contains("Fully recharged in: 0 sec") : false;
        public float MaxIntegrity => slimBlock != null ? slimBlock.MaxIntegrity : 1;
        public float CurrentIntegrity => slimBlock != null ? slimBlock.CurrentDamage <= 0 ? slimBlock.BuildIntegrity : slimBlock.MaxIntegrity - slimBlock.CurrentDamage : 0;
        public string CustomName => block != null ? block.CustomName : "Unknown";
    }

    public struct BlockInventoryData
    {
        public IMyTerminalBlock block;
        public IMyInventory[] inventories;

        public double CurrentVolume => inventories.Sum(x => (double)x.CurrentVolume);
        public double MaxVolume => inventories.Sum(x => (double)x.MaxVolume);
    }
}
