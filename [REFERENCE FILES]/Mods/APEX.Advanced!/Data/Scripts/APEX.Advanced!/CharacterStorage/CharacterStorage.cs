using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.ObjectBuilders;

namespace APEX.Advanced
{
    [ProtoContract]
    public class CharacterStorage
    {
        [ProtoMember(1)]
        public List<ActiveConsumable> ActiveItems { get; set; }

        [ProtoMember(2)]
        public ulong SteamID { get; set; }

        [ProtoMember(3)]
        public long CharacterID { get; set; }

        public CharacterStorage()
        {
            ActiveItems = new List<ActiveConsumable>();
            SteamID = 0;
            CharacterID = 0;
        }

        /// <summary>
        /// Adds a consumable
        /// </summary>
        /// <param name="definitionId"></param>      
        public void AddConsumable(MyDefinitionId definitionId)
        {
            Debug.LogInfo($"Attempting to add consumable with definition ID '{definitionId.SubtypeName}'.");
            if (!MyAPIGateway.Session.IsServer)
            {
                Debug.LogWarning("Method called on the client side. Consumable will not be added.");
                return;
            }

            if (Util.IGNORED_ITEM_SUBTYPES.Contains(definitionId.SubtypeName))
            {
                Debug.LogInfo($"Consumable '{definitionId.SubtypeName}' not added to the list. (Ignored by default)");
                return;
            }

            // If 0 disable this feature :(
            if (ConfigManager.Config.MinutesToTrackConsumables == 0)
            {
                Debug.LogInfo($"Feature disabled. No consumable added.");
                return;
            }

            ActiveConsumable newItem = new ActiveConsumable(definitionId, 0, ConfigManager.Config.MinutesToTrackConsumables);
            ActiveItems.Add(newItem);
            Debug.LogInfo($"Successfully added new consumable '{definitionId.SubtypeName}' to the list.");
        }

        /// <summary>
        /// Adds a consumable
        /// </summary>
        /// <param name="definitionId"></param>
        /// <returns>int, amount of item in list (this includes)</returns>
        public int CountConsumable(MyDefinitionId definitionId)
        {
            Debug.LogDebug($"Attempting to count consumable with definition ID '{definitionId.SubtypeName}'.");
            if (!MyAPIGateway.Session.IsServer)
            {
                Debug.LogWarning("Method called on the client side. Returning 0.");
                return 0;
            }

            int count = 0;
            foreach (var item in ActiveItems)
            {
                if (item.GetDefinitionId() == definitionId)
                {
                    count++;
                    Debug.LogDebug($"Found a match for '{definitionId.SubtypeName}'. Current count is {count}.");
                }
            }

            Debug.LogInfo($"Finished counting. Found {count} instances of '{definitionId.SubtypeName}'.");
            return count;
        }

        /// <summary>
        /// Minute-Timer ticker, remove item if >180
        /// </summary>
        public bool TickAllItems()
        {
            Debug.LogInfo("Starting to tick all active items.");
            if (!MyAPIGateway.Session.IsServer)
            {
                Debug.LogWarning("Method called on the client side. Ticking will not occur.");
                return false;
            }

            bool ret = false;
            for (int i = ActiveItems.Count - 1; i >= 0; i--)
            {
                Debug.LogDebug($"Processing item at index {i}.");
                bool shouldRemove = ActiveItems[i].Tick();

                if (shouldRemove)
                {
                    Debug.LogDebug($"Item at index {i} returned 'true' and will be removed.");
                    ActiveItems.RemoveAt(i);
                    ret = true;
                }
            }

            if (ret)
            {
                Debug.LogInfo("Finished ticking. At least one item was removed.");
            }
            else
            {
                Debug.LogDebug("Finished ticking. No items were removed.");
            }

            return ret;
        }



        /// <summary>
        /// Iterates through all consumables and returns their 
        /// count with localized DisplayNames, e.g., "1x Bread; 2x Meat"
        /// </summary>
        /// <returns>A STRING_SEPERATOR seperated string ordered by count then chars.</returns>
        public string GetActiveConsumableNames()
        {
            Debug.LogInfo("Starting to generate a list of active consumable names.");
            if (ActiveItems.Count == 0)
            {
                Debug.LogDebug("ActiveItems list is empty. Returning empty string.");
                return "";
            }

            var itemCounts = new Dictionary<MyDefinitionId, int>();
            Debug.LogDebug("Grouping and counting active items.");
            foreach (var item in ActiveItems)
            {
                MyDefinitionId id = item.GetDefinitionId();
                if (itemCounts.ContainsKey(id))
                    itemCounts[id]++;
                else
                    itemCounts[id] = 1;
            }

            var sortedList = new List<KeyValuePair<MyDefinitionId, int>>(itemCounts);
            sortedList.Sort((pair1, pair2) =>
            {
                int countComparison = pair2.Value.CompareTo(pair1.Value);
                if (countComparison != 0)
                    return countComparison;

                return pair1.Key.SubtypeName.CompareTo(pair2.Key.SubtypeName);
            });
            Debug.LogDebug("Successfully sorted the grouped items.");


            var stringBuilder = new StringBuilder();
            Debug.LogDebug("Building the final formatted string from sorted list.");

            foreach (var pair in sortedList)
            {
                MyDefinitionId id = pair.Key;
                int count = pair.Value;
                var definition = MyDefinitionManager.Static.GetDefinition(id);
                string displayName = definition?.DisplayNameText ?? id.SubtypeName;

                if (stringBuilder.Length > 0)
                    stringBuilder.Append(Util.STRING_SEPERATOR);

                stringBuilder.Append(count);
                stringBuilder.Append("x ");
                stringBuilder.Append(displayName);

                Debug.LogDebug($"Appended '{count}x {displayName}' to the string builder.");
            }

            string result = stringBuilder.ToString();
            Debug.LogInfo($"Successfully generated consumable names string: '{result}'.");
            return result;
        }

    }

    /// <summary>
    /// A consumable with its time
    /// </summary>
    [ProtoContract]
    public class ActiveConsumable
    {
        [ProtoMember(1)]
        public string TypeIdString { get; set; }
        [ProtoMember(2)]
        public string SubtypeIdString { get; set; }
        [ProtoMember(3)]
        public int Timer { get; set; }
        [ProtoMember(4)]
        private int MaxTime { get; set; }

        public ActiveConsumable()
        {
            Timer = 0;
        }

        public ActiveConsumable(MyDefinitionId definitionId, int timer = 0, int maxTime = 180)
        {
            TypeIdString = definitionId.TypeId.ToString();
            SubtypeIdString = definitionId.SubtypeName;
            Timer = timer;
            MaxTime = maxTime;
        }

        public bool Tick()
        {
            Timer++;
            return Timer >= MaxTime;
        }


        public MyDefinitionId GetDefinitionId()
        {
            MyObjectBuilderType type;
            if (MyObjectBuilderType.TryParse(TypeIdString, out type))
            {
                return new MyDefinitionId(type, SubtypeIdString);
            }

            // Return an invalid ID if TryParse fails
            return default(MyDefinitionId);
        }
    }
}
