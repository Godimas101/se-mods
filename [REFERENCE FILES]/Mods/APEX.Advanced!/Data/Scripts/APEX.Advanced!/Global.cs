using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;


namespace APEX.Advanced
{

    public static class Network
    {
        public const ushort NOTIFICATION_CHANNEL = 57190;
        public const ushort STORAGE_CHANNEL = 57191;
        public const ushort CONFIG_CHANNEL = 57192;
        public const ushort ADMIN_COMMAND_CHANNEL = 57193;
        public const ushort RADIATION_CHANNEL = 57194;
    };

    public class ConsumableStatsPatcher
    {
        public const float DIVISOR = 1000000f;

        public ConsumableStatsPatcher()
        {
            // Log that our mod is starting its work.
            Debug.LogInfo($"Initializing. Patching consumable item definitions...");

            try
            {
                PatchConsumableDefinitions();
            }
            catch (Exception e)
            {
                // If anything goes wrong, log the error to prevent the game from crashing.
                Debug.LogError($"Error during definition patching: {e.Message}\n{e.StackTrace}");
            }

            Debug.LogInfo($"Consumable patching complete.");
        }

        private void PatchConsumableDefinitions()
        {
            // --- Enhanced Debugging Counters ---
            int totalDefinitionsChecked;
            int consumableItemsFound = 0;
            int itemsWithNoStats = 0;
            int statsChecked = 0;
            int statsSkipped = 0;
            int patchedItems = 0;
            int patchedStats = 0;

            var allDefinitions = MyDefinitionManager.Static.GetAllDefinitions();
            totalDefinitionsChecked = allDefinitions.Count;
            Debug.LogInfo($"Starting patch process. Checking {totalDefinitionsChecked} total definitions...");

            foreach (var definition in allDefinitions)
            {
                // Use 'as' and a null-check, which is compatible with C# 6.
                var consumableDef = definition as MyConsumableItemDefinition;
                if (consumableDef != null)
                {
                    consumableItemsFound++;
                    Debug.LogInfo($"Found consumable: {consumableDef.Id.SubtypeId}. Checking stats...");

                    // --- SOLUTION ---
                    // Access the 'Stats' property directly from the live definition ('consumableDef').
                    // This avoids timing issues where the underlying ObjectBuilder might not be fully populated yet.
                    if (consumableDef.Stats == null || consumableDef.Stats.Count == 0)
                    {
                        itemsWithNoStats++;
                        Debug.LogInfo($"-- Skipping '{consumableDef.Id.SubtypeId}', it has no stats to process.");
                        continue;
                    }

                    bool itemWasPatched = false;

                    // The live 'Stats' property is a List<T>, so we use .Count.
                    for (int i = 0; i < consumableDef.Stats.Count; i++)
                    {
                        // Get a copy of the stat from the list.
                        var stat = consumableDef.Stats[i];
                        statsChecked++;

                        if (Enum.IsDefined(typeof(PrecalculateEffectTypes), stat.Name))
                        {
                            Debug.LogInfo($"-- Found matching stat '{stat.Name}' on '{consumableDef.Id.SubtypeId}'. Original Value: {stat.Value}, Time: {stat.Time}");

                            float newValue = stat.Value / DIVISOR;
                            float newTime = stat.Time / DIVISOR;

                            var newStat = new MyConsumableItemDefinition.StatValue(stat.Name, newValue, newTime);
                            
                            patchedStats++;
                            itemWasPatched = true;

                            // Write the modified copy back into the list at the same position.
                            consumableDef.Stats[i] = newStat;

                            Debug.LogInfo($"   -> Patched! New Value: {stat.Value}, new Time: {stat.Time}");
                        }
                        else
                        {
                            statsSkipped++;
                            Debug.LogInfo($"-- Skipping stat '{stat.Name}' on item '{consumableDef.Id.SubtypeId}', name not in patch list.");
                        }
                    }

                    if (itemWasPatched)
                    {
                        patchedItems++;
                    }
                }
            }

            // --- Final Summary Log ---
            Debug.LogInfo("--- Patching Summary ---");
            Debug.LogInfo($"Total definitions checked: {totalDefinitionsChecked}");
            Debug.LogInfo($"Consumable items found: {consumableItemsFound}");
            Debug.LogInfo($"Consumables skipped (no stats): {itemsWithNoStats}");
            Debug.LogInfo("---");
            Debug.LogInfo($"Total stats checked: {statsChecked}");
            Debug.LogInfo($"Stats skipped (name mismatch): {statsSkipped}");
            Debug.LogInfo($"Successfully patched {patchedStats} stats across {patchedItems} items.");
            Debug.LogInfo("--- Patching complete ---");
        }
    };

    /// <summary>
    /// Single point of contact to integrate other mods
    /// </summary>
    public static class CompatibilityManager
    {
        public static void CreateCompatibilityLayerIfNeeded()
        {
            #region Vertical (wall) Farm Plot
            if (Util.IsModActive(3562249355))
            {
                Debug.LogInfo($"Mod with ID {3562249355} found! Applying block visibility patch...");
                string variantGroupId = "FoodProductionGroup";

                // --- Find the group definition once ---
                var groupDefinition = MyDefinitionManager.Static.GetBlockVariantGroupDefinitions()
                    .Values
                    .FirstOrDefault(g => g.Id.SubtypeId.String == variantGroupId);

                // If the group doesn't exist for some reason, we can't do anything.
                if (groupDefinition == null)
                {
                    Debug.LogWarning($"Could not find BlockVariantGroup '{variantGroupId}' to patch. Aborting dynamic changes.");
                    return;
                }

                // Overwrite other mods group
                groupDefinition.DisplayNameString = "DisplayName_Block_AlgaeFarm";
                groupDefinition.DescriptionString = "Description_AlgaeFarm";
                groupDefinition.Icons = new string[] { @"Textures\GUI\Icons\Cubes\AlgaeFarm.dds" };

                // enable own vertical farm blocks
                Util.SetBlockPublicStatus("MyObjectBuilder_FunctionalBlock", "VerticalFarmPlotAdvanced", true);
                Util.SetBlockPublicStatus("MyObjectBuilder_FunctionalBlock", "InsetFarmPlotAdvanced", true);

                var addBlocks = new List<string> { "InsetFarmPlotAdvanced", "VerticalFarmPlotAdvanced" };
                Util.AddBlocksToVariantGroup(variantGroupId, "MyObjectBuilder_FunctionalBlock", addBlocks);

                var removeBlocks = new List<string> { "InsetFarmPlot", "VerticalFarmPlot" };
                Util.RemoveBlocksFromVariantGroup(variantGroupId, "MyObjectBuilder_FunctionalBlock", removeBlocks);

                Util.SetBlockPublicStatus("MyObjectBuilder_FunctionalBlock", "InsetFarmPlot", false);
                Util.SetBlockPublicStatus("MyObjectBuilder_FunctionalBlock", "VerticalFarmPlot", false);
            }
            #endregion
        }
    }

    public static class Util
    {
        public static readonly Guid CHARACTER_STORAGE_GUID = new Guid("6D454DF7-B016-47D2-8A9D-7B2B77C402B6");
        public const char STRING_SEPERATOR = ';';
        public const string DEFAULT_KEYBIND_FOR_GUI = "N";

        // TODO delete this
        public static readonly HashSet<string> IGNORED_ITEM_SUBTYPES = new HashSet<string>()
        {
            "BioPaste",
        };

        /// <summary>
        /// Eine Hilfsfunktion, um die 'Public'-Eigenschaft einer Block-Definition zu ändern.
        /// </summary>
        /// <param name="typeId">Der Typ des Blocks, z.B. "MyObjectBuilder_FunctionalBlock".</param>
        /// <param name="subtypeId">Die SubtypeId des Blocks.</param>
        /// <param name="isPublic">Der neue Wert für die Public-Eigenschaft.</param>
        public static void SetBlockPublicStatus(string typeId, string subtypeId, bool isPublic)
        {
            try
            {
                // Erstelle die DefinitionId mit dem variablen Typ.
                var definitionId = new MyDefinitionId(MyObjectBuilderType.Parse(typeId), subtypeId);
                var definition = MyDefinitionManager.Static.GetDefinition(definitionId) as MyCubeBlockDefinition;

                if (definition != null)
                {
                    definition.Public = isPublic;
                    Debug.LogInfo($"Patched block '{subtypeId}': Set Public to {isPublic}.");
                }
                else
                {
                    Debug.LogWarning($"Could not find block definition for '{typeId}/{subtypeId}' to patch.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while patching block '{subtypeId}': {e.Message}");
            }
        }

        /// <summary>
        /// Dynamically removes blocks from a BlockVariantGroup.
        /// </summary>
        /// <param name="groupSubtypeId">The SubtypeId of the group to modify.</param>
        /// <param name="typeId">The TypeId of the blocks to remove, e.g., "MyObjectBuilder_FunctionalBlock".</param>
        /// <param name="subtypeIdsToRemove">A list of SubtypeIds of the blocks to remove.</param>
        public static void RemoveBlocksFromVariantGroup(string groupSubtypeId, string typeId, List<string> subtypeIdsToRemove)
        {
            Debug.LogInfo($"Attempting to remove {subtypeIdsToRemove.Count} blocks of type '{typeId}' from group '{groupSubtypeId}'.");

            try
            {
                var groupDefinition = MyDefinitionManager.Static.GetBlockVariantGroupDefinitions()
                    .Values
                    .FirstOrDefault(g => g.Id.SubtypeId.String == groupSubtypeId);

                if (groupDefinition == null)
                {
                    Debug.LogWarning($"Could not find BlockVariantGroup with SubtypeId '{groupSubtypeId}'. Aborting removal.");
                    return;
                }

                // Create a HashSet of the full MyDefinitionIds for efficient lookup.
                var idsToRemoveSet = new HashSet<MyDefinitionId>(
                    subtypeIdsToRemove.Select(subtype => new MyDefinitionId(MyObjectBuilderType.Parse(typeId), subtype))
                );

                int originalCount = groupDefinition.Blocks.Length;
                Debug.LogDebug($"Group '{groupSubtypeId}' currently has {originalCount} blocks. Filtering...");

                // Filter the array in a single pass using LINQ, creating a new array only once.
                var newBlockList = groupDefinition.Blocks
                    .Where(blockDef => !idsToRemoveSet.Contains(blockDef.Id))
                    .ToArray();

                int newCount = newBlockList.Length;
                Debug.LogDebug($"After filtering, group '{groupSubtypeId}' will have {newCount} blocks.");

                // Only assign the new array if something has actually changed.
                if (newCount < originalCount)
                {
                    groupDefinition.Blocks = newBlockList;
                    groupDefinition.Postprocess();
                    Debug.LogInfo($"Successfully removed {originalCount - newCount} blocks from group '{groupSubtypeId}'.");
                }
                else
                {
                    Debug.LogInfo($"No blocks were removed from group '{groupSubtypeId}' as no matches were found.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while modifying BlockVariantGroup '{groupSubtypeId}': {e.Message}");
            }
        }

        /// <summary>
        /// Dynamically adds blocks to a BlockVariantGroup, ensuring no duplicates are added.
        /// </summary>
        /// <param name="groupSubtypeId">The SubtypeId of the group to modify.</param>
        /// <param name="typeId">The TypeId of the blocks to add.</param>
        /// <param name="subtypeIdsToAdd">A list of SubtypeIds of the blocks to add.</param>
        public static void AddBlocksToVariantGroup(string groupSubtypeId, string typeId, List<string> subtypeIdsToAdd)
        {
            Debug.LogInfo($"Attempting to add {subtypeIdsToAdd.Count} blocks of type '{typeId}' to group '{groupSubtypeId}'.");
            try
            {
                var groupDefinition = MyDefinitionManager.Static.GetBlockVariantGroupDefinitions()
                    .Values
                    .FirstOrDefault(g => g.Id.SubtypeId.String == groupSubtypeId);

                if (groupDefinition == null)
                {
                    Debug.LogWarning($"Could not find BlockVariantGroup with SubtypeId '{groupSubtypeId}'. Aborting add.");
                    return;
                }

                // Create a HashSet of existing block IDs for efficient duplicate checking.
                var existingBlocks = new HashSet<MyDefinitionId>(groupDefinition.Blocks.Select(b => b.Id));
                var blocksToActuallyAdd = new List<MyCubeBlockDefinition>();

                Debug.LogDebug($"Group '{groupSubtypeId}' currently has {existingBlocks.Count} blocks. Checking for new blocks to add...");

                foreach (var subtypeId in subtypeIdsToAdd)
                {
                    var definitionId = new MyDefinitionId(MyObjectBuilderType.Parse(typeId), subtypeId);

                    // Only add the block if it's not already in the group.
                    if (!existingBlocks.Contains(definitionId))
                    {
                        var blockDef = MyDefinitionManager.Static.GetCubeBlockDefinition(definitionId);
                        if (blockDef != null)
                        {
                            blocksToActuallyAdd.Add(blockDef);
                            Debug.LogDebug($"-- Found new block '{subtypeId}' to add to group.");
                        }
                        else
                        {
                            Debug.LogWarning($"-- Could not find block definition for '{subtypeId}' to add to group.");
                        }
                    }
                    else
                    {
                        Debug.LogDebug($"-- Block '{subtypeId}' is already in group '{groupSubtypeId}'. Skipping.");
                    }
                }

                // If there are new blocks to add, create the new array.
                if (blocksToActuallyAdd.Count > 0)
                {
                    // Combine the original blocks with the new ones.
                    var originalBlocks = groupDefinition.Blocks.ToList();
                    originalBlocks.AddRange(blocksToActuallyAdd);
                    groupDefinition.Blocks = originalBlocks.ToArray();
                    groupDefinition.Postprocess();
                    Debug.LogInfo($"Successfully added {blocksToActuallyAdd.Count} new blocks to group '{groupSubtypeId}'. New total: {groupDefinition.Blocks.Length}.");
                }
                else
                {
                    Debug.LogInfo($"No new blocks were added to group '{groupSubtypeId}' as they were all duplicates or invalid.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while adding blocks to BlockVariantGroup '{groupSubtypeId}': {e.Message}");
            }
        }

        /// <summary>
        /// Checks if a mod with the specified Workshop ID is active in the current session.
        /// </summary>
        /// <param name="workshopId">The Steam Workshop ID of the mod to check for.</param>
        /// <returns>True if the mod is active, otherwise false.</returns>
        public static bool IsModActive(ulong workshopId)
        {
            Debug.LogInfo($"Checking if mod with Workshop ID '{workshopId}' is active...");

            var mods = MyAPIGateway.Session?.Mods;
            if (mods == null)
            {
                Debug.LogWarning("Session.Mods list is null. Cannot check for active mods.");
                return false;
            }

            Debug.LogDebug($"Scanning {mods.Count} active mods...");
            foreach (var mod in mods)
            {
                // For debugging, you can log each mod in the list.
                // Debug.LogDebug($"-- Checking mod: '{mod.FriendlyName}' (ID: {mod.PublishedFileId})");
                if (mod.PublishedFileId == workshopId)
                {
                    Debug.LogInfo($"Found match for workshop ID {workshopId} ('{mod.FriendlyName}'). Mod is active.");
                    return true;
                }
            }

            Debug.LogInfo($"No match found for workshop ID {workshopId}. Mod is not active.");
            return false;
        }


        /// <summary>
        /// Checks if it is part of the enum 
        /// </summary>
        /// <param name="effect"></param>
        /// <returns>true - item has an effect that counts || otherwise false</returns>
        public static bool CheckEffectIfItemCounts(EffectType effectToCheck)
        {
            string effectName = effectToCheck.ToString();
            return Enum.IsDefined(typeof(EffectsToTrack), effectName);
        }


        /// <summary>
        /// Checks if string is in LOC file
        /// </summary>
        /// <param name="s"></param>
        /// <returns>LOC string or input string</returns>
        public static string LOC(string s)
        {
            string e = string.Format(MyTexts.Get(MyStringId.GetOrCompute("{LOC:" + s + "}")).ToString());
            return e != "" ? e : s;
        }

        public static string autoSpaces(string input, string input2, int totalLen)
        {
            // TODO: Better autoSpaces or more labels
            string ret = input;
            int textLength = input.Length + input2.Length;
            int spaces = totalLen - textLength;

            if (spaces < 1)
                return "Error: Text to big";

            for (int i = 0; i < spaces; i++)
                ret += " ";

            return ret += input2;
        }

        /// <summary>
        /// Checks if SubtypeName contains "Cryo"
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool IsCryo(IMyEntity parent)
        {
            // Convention, a Cryo needs Cryo in SubtypeName
            IMyCryoChamber cryo = parent as IMyCryoChamber;
            if (cryo != null && cryo.BlockDefinition.SubtypeName.Contains("Cryo"))
            {
                Debug.Log($"{cryo.BlockDefinition.SubtypeName} IsCryo: {cryo.BlockDefinition.SubtypeName.Contains("Cryo")}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if SubtypeName contains "Bed"
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool IsBed(IMyEntity parent)
        {
            IMyCryoChamber cryo = parent as IMyCryoChamber;
            if (cryo != null && cryo.BlockDefinition.SubtypeName.Contains("Bed"))
            {
                Debug.Log($"{cryo.BlockDefinition.SubtypeName} IsBed: {cryo.BlockDefinition.SubtypeName.Contains("Bed")}");
                return true;
            }
            return false;
        }

        public static bool IsCockpit(IMyEntity parent)
        {
            IMyCockpit chair = parent as IMyCockpit;

            if (chair != null && chair.CanControlShip == true)
            {
                Debug.Log($"{chair.BlockDefinition.SubtypeName} IsCockpit: {chair.CanControlShip}");
                return true;
            }


            return false;
        }

        public static bool IsChair(IMyEntity parent)
        {
            IMyCockpit chair = parent as IMyCockpit;
            IMyCryoChamber cryo = parent as IMyCryoChamber;

            if (chair != null && chair.CanControlShip == false && cryo == null)
            {
                Debug.Log($"{chair.BlockDefinition.SubtypeName} IsChair: {!chair.CanControlShip}");
                return true;
            }

            return false;
        }


        #region ADDON Inventory buff (disabled, I want the code more stable!)
        /// <summary>
        /// Sets the maximum inventory volume for a character.
        /// Check if clients have to call this too!
        /// </summary>
        /// <param name="character">The character entity (IMyCharacter).</param>
        /// <param name="newCapacityInLiters">The desired new capacity in Liters (e.g., 800L).</param>
        public static void SetCharacterInventoryCapacity(IMyCharacter character, float newCapacityInLiters)
        {
            if (character == null)
                return;

            // Get the character's main inventory.
            // Use GetInventory(0) or just GetInventory()
            VRage.Game.ModAPI.IMyInventory inventory = character.GetInventory(0);

            if (inventory == null)
                return;

            // Cast the interface 'IMyInventory' to its concrete class 'MyInventory'.
            // The interface 'MaxVolume' is read-only. The class 'MaxVolume' is settable.
            MyInventory concreteInventory = inventory as MyInventory;

            if (concreteInventory != null)
            {
                // Convert Liters to Cubic Meters (m^3), which the API uses.
                // 1000L = 1 m^3
                float volumeInCubicMeters = newCapacityInLiters / 1000f;

                // Convert the float to MyFixedPoint, the type required by the API.
                MyFixedPoint newMaxVolume = (MyFixedPoint)volumeInCubicMeters;

                // Set the new volume.
                concreteInventory.MaxVolume = newMaxVolume;
            }
        }

        /// <summary>
        /// Multiplies the maximum inventory volume for a character by a specific factor.
        /// Check if clients have to call this too!
        /// </summary>
        /// <param name="character">The character entity (IMyCharacter).</param>
        /// <param name="multiplier">The factor to multiply by (e.g., 2.0f for double capacity, 0.5f for half).</param>
        public static void MultiplyCharacterInventoryCapacity(IMyCharacter character, float multiplier)
        {
            if (character == null)
                return;

            VRage.Game.ModAPI.IMyInventory inventory = character.GetInventory(0);

            if (inventory == null)
                return;

            // Cast the interface 'IMyInventory' to its concrete class 'MyInventory'.
            MyInventory concreteInventory = inventory as MyInventory;

            if (concreteInventory != null)
            {
                // Get the current max volume (which is in m^3 and of type MyFixedPoint)
                MyFixedPoint currentMaxVolume = concreteInventory.MaxVolume;

                // Convert the MyFixedPoint value to a float to perform standard math.
                // (float) cast is the standard way.
                float currentVolumeAsFloat = (float)currentMaxVolume;

                // Apply the multiplier
                float newVolumeAsFloat = currentVolumeAsFloat * multiplier;

                // Ensure the volume doesn't become negative if a negative multiplier is passed.
                if (newVolumeAsFloat < 0f)
                    newVolumeAsFloat = 0f;

                // Convert the result back to MyFixedPoint.
                MyFixedPoint newMaxVolume = (MyFixedPoint)newVolumeAsFloat;

                // Set the new volume. This change will be synced to clients.
                concreteInventory.MaxVolume = newMaxVolume;
            }
        }
        #endregion
    };

    public static class KeybindManager
    {
        public static MyKeys PrimaryKey { get; private set; }
        public static bool NeedsControl { get; private set; }
        public static bool NeedsAlt { get; private set; }
        public static bool NeedsShift { get; private set; }

        /// <summary>
        /// Parses the user's keybind string with a fallback to a default value on failure.
        /// </summary>
        /// <param name="userKeybind">The keybind string from the config file.</param>
        /// <param name="defaultKeybind">The default keybind to use if the user's is invalid.</param>
        public static void Parse(string userKeybind, string defaultKeybind)
        {
            TryParseInternal(userKeybind);

            // Validation check: A valid keybind MUST have a primary key.
            if (PrimaryKey == MyKeys.None)
            {
                // If the primary key is still 'None', the user's string was invalid.
                Debug.LogWarning($"Invalid keybinding '{userKeybind}' found in config. " +
                               $"Please use a valid format (e.g., 'Control+N' or 'Shift+G'). " +
                               $"Falling back to default '{defaultKeybind}'.");

                // Now, parse the safe default keybind instead.
                TryParseInternal(defaultKeybind);
            }

            Debug.LogInfo($"Keybind loaded: Primary={PrimaryKey}, Ctrl={NeedsControl}, Alt={NeedsAlt}, Shift={NeedsShift}");
        }

        /// <summary>
        /// Internal helper to perform the actual parsing logic.
        /// </summary>

        private static void TryParseInternal(string keybindString)
        {
            NeedsControl = false;
            NeedsAlt = false;
            NeedsShift = false;
            PrimaryKey = MyKeys.None;

            if (string.IsNullOrWhiteSpace(keybindString)) return;

            string[] parts = keybindString.Split('+');
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                string lowerPart = trimmedPart.ToLower();

                switch (lowerPart)
                {
                    case "ctrl":
                    case "control":
                        NeedsControl = true;
                        break;
                    
                    case "alt":
                        NeedsAlt = true;
                        break;

                    case "shift":
                        NeedsShift = true;
                        break;

                    default:
                        MyKeys key;
                        if (Enum.TryParse(trimmedPart, true, out key))
                            PrimaryKey = key;
                        break;
                }
            }
        }
    }


    public class Effect
    {
        public IMyCharacter Character { get; }
        public IMyPlayer Player { get; }
        public float Value { get; }
        public float Time { get; private set; }
        public bool IsValid { get; private set; }
        public EffectType Type { get; private set; }
        public bool IsUnique { get; private set; }
        private readonly MyEntityStat _effectStat;

        public Effect(IMyCharacter character, IMyPlayer player, EffectType effect, float _value, float time)
        {
            Debug.LogInfo($"Attempting to create a new effect of type '{effect}'.");

            IsValid = false;
            Character = character;
            Player = player;
            Value = _value;
            Time = time;
            Type = effect;

            if (effect == EffectType.BatteryCharge ||
                effect == EffectType.HydrogenCharge ||
                effect == EffectType.OxygenCharge ||
                effect == EffectType.Health ||
                effect == EffectType.Food ||
                effect == EffectType.Water)
            {
                IsUnique = true;
                Debug.LogDebug($"Effect '{effect}' is a unique effect.");
            }

            if (player == null)
            {
                Debug.LogWarning($"Player is null for effect '{effect}'. Skipping validation.");
                return;
            }

            var statComponent = character.Components.Get<MyCharacterStatComponent>();
            if (statComponent == null)
            {
                Debug.LogWarning($"Character '{character.DisplayName}' has no stat component. Skipping validation.");
                return;
            }

            if (Time > 1)
            {
                Debug.LogWarning($"Effect time '{Time}' is greater than 1. Aborting effect creation to prevent unintended long effects.");
                return;
            }

            Value *= 1000000;
            Time *= 1000000;
            Debug.LogDebug($"Normalized value to '{Value}' and time to '{Time}'.");

            if (effect == EffectType.BatteryCharge || effect == EffectType.HydrogenCharge || effect == EffectType.OxygenCharge)
            {
                IsValid = true;
                Debug.LogInfo($"Effect '{effect}' is a charge type. Validated successfully.");
                return;
            }

            MyStringHash effectStatId = MyStringHash.GetOrCompute(effect.ToString());
            if (!statComponent.TryGetStat(effectStatId, out _effectStat) || _effectStat == null)
            {
                Debug.LogWarning($"Stat for effect '{effect}' (ID: '{effectStatId}') not found on character '{character.DisplayName}'. Effect is not valid.");
                return;
            }

            IsValid = true;

            Debug.LogInfo($"Effect '{effect}' created successfully. IsValid: {IsValid}. Final values: Value={Value}, Time={Time}.");
        }
        /// <summary>
        /// Effects will run even if there is no player online. (Compensate sudden disconnects)
        /// </summary>
        /// <returns>true, effect done || false effect out of time etc.</returns>
        public bool DoEffect()
        {
            Debug.LogDebug($"Attempting to perform effect of type '{Type}' on character '{Character?.DisplayName}'.");
            if (!IsValid || Character.IsDead || Character.MarkedForClose || Character.Closed || Time-- <= 0)
            {
                Debug.LogInfo($"Effect '{Type}' is no longer valid or has expired. Returning false.");
                return false;
            }

            float maxValue = 1f;
            float newValue;
            switch (Type)
            {
                case EffectType.BatteryCharge:
                    Debug.LogDebug($"Applying charge effect '{Type}'.");
                    newValue = Character.SuitEnergyLevel + Value;
                    if (newValue > maxValue)
                        newValue = maxValue;
                    MyVisualScriptLogicProvider.SetPlayersEnergyLevel(Player.IdentityId, newValue);

                    Debug.LogInfo($"Successfully applied '{Type}' effect. New value: {newValue}.");
                    break;

                case EffectType.HydrogenCharge:
                    Debug.LogDebug($"Applying charge effect '{Type}'.");
                    newValue = MyVisualScriptLogicProvider.GetPlayersHydrogenLevel(Player.IdentityId) + Value;
                    if (newValue > maxValue)
                        newValue = maxValue;
                    MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(Player.IdentityId, newValue);

                    Debug.LogInfo($"Successfully applied '{Type}' effect. New value: {newValue}.");
                    break;

                case EffectType.OxygenCharge:
                    Debug.LogDebug($"Applying charge effect '{Type}'.");
                    newValue = MyVisualScriptLogicProvider.GetPlayersOxygenLevel(Player.IdentityId) + Value;
                    if (newValue > maxValue)
                        newValue = maxValue;
                    MyVisualScriptLogicProvider.SetPlayersOxygenLevel(Player.IdentityId, newValue);

                    Debug.LogInfo($"Successfully applied '{Type}' effect. New value: {newValue}.");
                    break;

                default:
                    Debug.LogDebug($"Applying stat effect '{Type}'. Checking for parent status.");
                    float tempValue = Value * 100;
                    string parentType = "None";
                    if (Util.IsChair(Character.Parent))
                    {
                        parentType = "Chair";
                        if (Type == EffectType.Health) ApplyMultiplier(tempValue, 1f);
                        else if (Type == EffectType.Food) ApplyMultiplier(tempValue, 2f);
                        else if (Type == EffectType.Water) ApplyMultiplier(tempValue, 2f);
                        else if (Type == EffectType.Sleep) ApplyMultiplier(tempValue, 1.5f);
                        else if (Type == EffectType.Recovery) ApplyMultiplier(tempValue, 1.5f);
                        else if (Type == EffectType.Fatigue) ApplyMultiplier(tempValue, 0.75f);
                        else if (Type == EffectType.Bloating) ApplyMultiplier(tempValue, 0.5f);
                    }
                    else if (Util.IsCockpit(Character.Parent))
                    {
                        parentType = "Cockpit";
                        if (Type == EffectType.Health) ApplyMultiplier(tempValue, 0.75f);
                        else if (Type == EffectType.Food) ApplyMultiplier(tempValue, 0.5f);
                        else if (Type == EffectType.Water) ApplyMultiplier(tempValue, 0.75f);
                        else if (Type == EffectType.Sleep) ApplyMultiplier(tempValue, 0.75f);
                        else if (Type == EffectType.Recovery) ApplyMultiplier(tempValue, 0.75f);
                        else if (Type == EffectType.Fatigue) ApplyMultiplier(tempValue, 1.25f);
                        else if (Type == EffectType.Bloating) ApplyMultiplier(tempValue, 1.5f);
                    }
                    else if (Util.IsBed(Character.Parent))
                    {
                        parentType = "Bed";
                        if (Type == EffectType.Health) ApplyMultiplier(tempValue, 1.5f);
                        else if (Type == EffectType.Food) ApplyMultiplier(tempValue, 1.5f);
                        else if (Type == EffectType.Water) ApplyMultiplier(tempValue, 1.5f);
                        else if (Type == EffectType.Sleep) ApplyMultiplier(tempValue, 1.5f);
                        else if (Type == EffectType.Recovery) ApplyMultiplier(tempValue, 2f);
                        else if (Type == EffectType.Fatigue) ApplyMultiplier(tempValue, 0.5f);
                        else if (Type == EffectType.Bloating) ApplyMultiplier(tempValue, 0.5f);
                    }
                    else
                    {
                        if (Type == EffectType.Recovery) ApplyMultiplier(tempValue, 0.75f);
                    }

                    Debug.LogDebug($"Character's parent is '{parentType}'. Final tempValue: {tempValue}.");

                    if (!(_effectStat?.StatId != null))
                    {
                        Debug.LogWarning($"Stat component for effect '{Type}' is null or invalid. Cannot apply effect.");
                        return false;
                    }

                    maxValue = _effectStat.MaxValue;
                    newValue = _effectStat.Value + tempValue;

                    if (newValue > maxValue)
                        newValue = maxValue;

                    _effectStat.Value = newValue;
                    _effectStat.Update();
                    Debug.LogInfo($"Successfully applied '{Type}' effect. New value: {_effectStat.Value}.");
                    break;
            }
            return true;
        }

        /// <summary>
        /// Correct multiplier for negative and positive effects
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        private float ApplyMultiplier(float baseValue, float multiplier)
        {
            if (baseValue > 0) // buffs            
                return baseValue * multiplier;

            if (baseValue < 0) // debuffs            
                return baseValue / multiplier;

            return baseValue;
        }
    }


    public enum EffectType
    {
        BatteryCharge,
        HydrogenCharge,
        OxygenCharge,
        Radiation,
        Health,
        Food,
        Water,
        Sleep,
        Recovery,
        Fatigue,  // if you drink coffeeing this rises
        Bloating,
        HelmUse, // 0 Helm needs to be off to use!
        DoNotTrack, // 0 (default) consumable is tracked, 1 DoNotTrack consumable
    }

    // This effects give reduced values if multiple consumed 
    // If an Item has Food and Water - only Food is applied
    public enum EffectsToTrack
    {
        //BatteryCharge,
        //HydrogenCharge,
        //OxygenCharge,
        //Radiation,
        Health,
        Food,
        //Water,
        //Sleep,
        //Recovery,
        //Fatigue,  
        //Bloating,
        //HelmUse, 
        //DoNotTrack,
    }

    // Compatibility layer to precalculate all status effects and values
    // This will get divided by /1000000
    // APEX.Advanced! will handle all stats correct!
    public enum PrecalculateEffectTypes
    {
        BatteryCharge,
        HydrogenCharge,
        OxygenCharge,
        Radiation,
        // RadiationImmunity, // No precalc needed! vanilla game useage
        Health,
        Food,
        Water,
        Sleep,
        Recovery,
        Fatigue,
        Bloating,
        // HelmUse, // NO precalc needed! 0 Helm needs to be off to use!
        // DoNotTrack, // NO precalc needed!
    }

}
