using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using APEX.Advanced.Client.MyAdvancedStat;


namespace APEX.Advanced.Server
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class APEX_Advanced_Server : MySessionComponentBase
    {
        public static APEX_Advanced_Server Instance { get; private set; }
        private static bool isServer;
        private static bool isDedicated;
        private ConsumableStatsPatcher consumableStatsPatcher;
        private MyConcurrentList<Effect> _allCharacterEffects = new MyConcurrentList<Effect>();

        /// <summary>
        /// long = IMyCharacterID // EntityID
        /// </summary>
        private MyConcurrentDictionary<long, CharacterStorage> _characterStorageCache = new MyConcurrentDictionary<long, CharacterStorage>();
        /// <summary>
        /// Needed a seconde one for status effects.... very sad...
        /// </summary>
        private MyConcurrentDictionary<long, AdvancedStats> _characterStatusDecay = new MyConcurrentDictionary<long, AdvancedStats>();
        private MyConcurrentQueue<long> _charactersToRemove = new MyConcurrentQueue<long>();
        private MyConcurrentHashSet<long> _dirtyCharacters = new MyConcurrentHashSet<long>();
        /// <summary>
        /// Online human players with a character
        /// </summary>
        private List<IMyPlayer> _currentPlayers = new List<IMyPlayer>();
        private HashSet<IMyPlayer> _newPlayersQuery = new HashSet<IMyPlayer>();
        private readonly object _playerListLock = new object();


        #region Radiation worker
        // A flag to signal the worker thread to stop. 'volatile' ensures visibility across threads.
        private volatile bool _stopWorkerThread = false;

        // Concurrent dictionary to store radiation levels. Key: Player Character EntityId, Value: Radiation Level
        private MyConcurrentDictionary<long, float> _characterRadiationLevels = new MyConcurrentDictionary<long, float>();

        // Configuration for the radiation logic        
        private static readonly MyDefinitionId _uraniumDefId = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");
        #endregion

        private int tick = 0;

        public override void LoadData()
        {
            // Server always patches consumable values
            consumableStatsPatcher = new ConsumableStatsPatcher();

            CompatibilityManager.CreateCompatibilityLayerIfNeeded();
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            Debug.LogInfo("Initializing component.");
            Instance = this;
            isServer = MyAPIGateway.Session.IsServer;
            isDedicated = MyAPIGateway.Utilities.IsDedicated;
            if (!isServer)
            {
                Debug.LogInfo("Client-side component initialized. No event listeners will be added.");
                return;
            }

            _stopWorkerThread = false;
            // Use the game's dedicated background task scheduler. This is the correct way.
            MyAPIGateway.Parallel.StartBackground(RadiationSearchWorker);
            Debug.LogInfo("Radiation search background task started.");
            
            Debug.LogInfo("Server-side component initialized. Subscribing to events.");

            //Network
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.STORAGE_CHANNEL, Server_ClientAskForStorage);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.ADMIN_COMMAND_CHANNEL, Server_ReceiveAdminCommand);

            // Event-Listening
            MyAPIGateway.Players.ItemConsumed += OnItemConsumed;
            Debug.LogDebug("Subscribed to ItemConsumed event.");
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            Debug.LogDebug("Subscribed to OnEntityAdd event.");
            MyVisualScriptLogicProvider.PlayerDied += OnCharacterDied;
            Debug.LogDebug("Subscribed to PlayerDied event.");
            MyAPIGateway.Session.OnSessionReady += OnSessionReady;
            Debug.LogDebug("Subscribed to OnSessionReady event.");
            MyVisualScriptLogicProvider.PlayerConnected += OnPlayerConnected;
            Debug.LogDebug("Subscribed to PlayerConnected event.");
            MyVisualScriptLogicProvider.PlayerDisconnected += OnPlayerDisconnected;
            Debug.LogDebug("Subscribed to PlayerDisconnected event.");

            Debug.LogInfo("All server-side event listeners have been set up.");
        }

        protected override void UnloadData()
        {
            Debug.LogInfo("UnloadData method called. Starting cleanup process.");

            if (isServer)
            {
                // We aren't able to safe data here.... it is already gone use -> public override void SaveData()
                Debug.LogInfo("Running server-side cleanup.");
                
                // Signal the task's while-loop to terminate.
                // We don't have a thread object to "Join", we just tell the loop to stop.
                // The task will finish its current iteration and then exit gracefully.
                _stopWorkerThread = true;
                Debug.LogInfo("Signaled radiation search background task to stop.");
                
                if (MyAPIGateway.Players != null)
                {
                    MyAPIGateway.Players.ItemConsumed -= OnItemConsumed;
                    Debug.LogDebug("Unsubscribed from ItemConsumed event.");
                }
                if (MyAPIGateway.Entities != null)
                {
                    MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
                    Debug.LogDebug("Unsubscribed from OnEntityAdd event.");
                }
                // Network
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Network.STORAGE_CHANNEL, Server_ClientAskForStorage);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Network.ADMIN_COMMAND_CHANNEL, Server_ReceiveAdminCommand);

                // Listener
                MyVisualScriptLogicProvider.PlayerDied -= OnCharacterDied;
                Debug.LogDebug("Unsubscribed from PlayerDied event.");
                MyVisualScriptLogicProvider.PlayerDisconnected -= OnPlayerDisconnected;
                Debug.LogDebug("Unsubscribed from PlayerDisconnected event.");
                MyVisualScriptLogicProvider.PlayerConnected -= OnPlayerConnected;
                Debug.LogDebug("Unsubscribed from PlayerConnected event.");
                MyAPIGateway.Session.OnSessionReady -= OnSessionReady;
                Debug.LogDebug("Unsubscribed from OnSessionReady event.");
                Instance = null;
                Debug.LogDebug("Setting instance to null");

                Debug.LogInfo("Initiated final save of all character storage data.");
            }
            else
            {
                Debug.LogInfo("Running client-side cleanup.");
            }
            _characterRadiationLevels.Clear();
            Debug.LogDebug("Cleared playerRadiationLevels list.");
            _allCharacterEffects.Clear();
            Debug.LogDebug("Cleared allPlayerEffects list.");
            _characterStorageCache.Clear();
            Debug.LogDebug("Cleared characterStorageCache.");
            _charactersToRemove.Clear();
            Debug.LogDebug("Cleared charactersToRemove queue.");

            base.UnloadData();
            Debug.LogInfo("Cleanup process completed.");
        }

        public override void SaveData()
        {
            base.SaveData();
            if (isServer)
            {
                SaveAllCharactersStorageData();
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!isServer) return;
            base.UpdateAfterSimulation();

            tick++;

            // Load local character for server system
            if (tick == 90 && !isDedicated)
            {
                if (MyAPIGateway.Session.LocalHumanPlayer != null)
                    _newPlayersQuery.Add(MyAPIGateway.Session.LocalHumanPlayer);
            }

            // Compute consumables
            if (tick % 60 == 0)
            {
                Debug.LogDebug("1-second tick triggered. Computing all consumable effects.");
                ComputeAllConsumableEffects();
            }
            // or remove not present characters
            else if (tick % 100 == 0 && _charactersToRemove.Count > 0)
            {
                Debug.LogInfo($"100-tick cleanup triggered. Removing {_charactersToRemove.Count} character(s) from cache.");
                long idToRemove;
                while (_charactersToRemove.TryDequeue(out idToRemove))
                {
                    CharacterStorage value_1;
                    if (_characterStorageCache.TryRemove(idToRemove, out value_1))
                        Debug.LogDebug($"Successfully removed character with ID '{idToRemove}' from _characterStorageCache.");
                    else
                        Debug.LogWarning($"Failed to remove character with ID '{idToRemove}' from _characterStorageCache. It might have already been removed.");

                    AdvancedStats value_2;
                    if (_characterStatusDecay.TryRemove(idToRemove, out value_2))
                        Debug.LogDebug($"Successfully removed character with ID '{idToRemove}' from _characterStatusDecay.");
                    else
                        Debug.LogWarning($"Failed to remove character with ID '{idToRemove}' from _characterStatusDecay. It might have already been removed.");
                }
            }

            // Get new players
            if (tick % 100 == 5)
            {
                // Next Action: Prepare to update PlayerOnline list.
                List<IMyPlayer> _tempPlayersOnline = new List<IMyPlayer>();
                List<IMyPlayer> _oldPlayersSnapshot;
                List<IMyPlayer> _newPlayers;

                MyAPIGateway.Players.GetPlayers(_tempPlayersOnline); // Get all players currently in the game.

                // Filter the list: remove nulls, bots, and players with invalid/closed characters.
                // This LINQ query creates a new filtered list.
                _tempPlayersOnline = _tempPlayersOnline
                    .Where(p => p != null &&
                                !p.IsBot &&
                                p.Character != null &&
                                !p.Character.Closed &&
                                !p.Character.MarkedForClose)
                    .ToList();

                // We need a fast copy to be threadsafe
                lock (_playerListLock)
                {
                    _oldPlayersSnapshot = new List<IMyPlayer>(_currentPlayers);
                }

                // Next Action: Thread-safely update the shared PlayerOnline list.
                // This is crucial if PlayerOnline is accessed by network message handlers or other threads.
                lock (_playerListLock)
                {
                    _currentPlayers = _tempPlayersOnline; // Assign the new, filtered list.
                }

                // Now we are able to get new players
                _newPlayers = _tempPlayersOnline.Except(_oldPlayersSnapshot).ToList();

                // Get new added players and send them their character consumables
                // delayed send...
                foreach (var player in _newPlayers)
                    _newPlayersQuery.Add(player);
            }

            // new AdvancedStats decays
            if (tick % 100 == 50)
            {
                foreach (var storage in _characterStatusDecay.Values)
                {

                    if (storage.Character == null || storage.Character.Closed)
                        continue;

                    storage.Update100();
                }
            }

            // We compute new players some ticks after their join in the hope they got an character
            if (tick % 100 == 99 && _newPlayersQuery.Count > 0)
            {
                foreach (var player in _newPlayersQuery)
                    OnEntityAdd(player.Character);

                _newPlayersQuery.Clear();
            }

            // We compute radiation levels from worker thread to characters
            if (tick % 100 == 25)
            {
                if (_characterRadiationLevels.Count > 0)
                {
                    List<long> staleEntriesToRemove = null;

                    foreach (var entry in _characterRadiationLevels)
                    {
                        long characterId = entry.Key;
                        float radiationPerSecond = entry.Value; // This is the ΔS calculated by the worker

                        IMyEntity entity;
                        MyAPIGateway.Entities.TryGetEntityById(characterId, out entity);
                        var character = entity as IMyCharacter;

                        if (character != null && !character.IsDead && !character.Closed)
                        {
                            var statComponent = character.Components.Get<MyCharacterStatComponent>();
                            if (statComponent == null)
                            {
                                Debug.LogWarning($"[Radiation] Character '{character.DisplayName}' is missing MyCharacterStatComponent.");
                                continue;
                            }

                            MyEntityStat radiationStat;
                            MyEntityStat radiationImmunityStat;
                            bool radiationImmunity = false;
                            if (statComponent.TryGetStat(MyStringHash.GetOrCompute("RadiationImmunity"), out radiationImmunityStat) && radiationImmunityStat != null)
                                if (radiationImmunityStat.Value > 0)
                                    radiationImmunity = true;

                            if (statComponent.TryGetStat(MyStringHash.GetOrCompute(EffectType.Radiation.ToString()), out radiationStat) && radiationStat != null)
                            {
                                // The value to add is ΔS * (time since last update).
                                float baseValueToAdd = radiationPerSecond * (100f / 60f);

                                // Reduce the final increase rate by a factor of 4 for better playability.
                                float radiationToAdd = baseValueToAdd / ConfigManager.Config.RadiationGeneralDivisor;

                                if (radiationToAdd > 0)
                                {
                                    // Handel immunity medkit
                                    if (!radiationImmunity)
                                    {
                                        float newValue = radiationStat.Value + radiationToAdd;
                                        if (newValue > radiationStat.MaxValue)
                                            newValue = radiationStat.MaxValue;

                                        radiationStat.Value = newValue;
                                    }

                                    // Keep the geiger running
                                    var player = GetPlayerFromCharacterID(characterId);
                                    Debug.LogDebug($"Increased Radiation stat for player {player?.DisplayName ?? "Unknown"} by {radiationToAdd:F4}. New value: {radiationStat.Value:F2}");

                                    if (player != null)
                                    {
                                        Server_SendRadiationEventToClient(player.SteamUserId, radiationToAdd);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[Radiation] Character '{character.DisplayName}' has a StatComponent but is missing the 'Radiation' stat itself.");
                            }
                        }
                        else
                        {
                            // The character does not exist or is dead. The entry is stale.
                            Debug.LogInfo($"[RadiationCleanup] Stale radiation entry found for character ID {characterId}. Queuing for removal.");

                            if (staleEntriesToRemove == null)
                                staleEntriesToRemove = new List<long>();

                            staleEntriesToRemove.Add(characterId);
                        }
                    }

                    // After iterating, clean up all stale entries we found.
                    if (staleEntriesToRemove != null)
                    {
                        Debug.LogInfo($"[RadiationCleanup] Removing {staleEntriesToRemove.Count} stale radiation entries.");
                        foreach (long idToRemove in staleEntriesToRemove)
                        {
                            float removedValue;
                            _characterRadiationLevels.TryRemove(idToRemove, out removedValue);
                        }
                    }
                }
            }

            // Save dirty characters
            if (tick % 1800 == 0 && _dirtyCharacters.Count > 0)
            {
                Debug.LogInfo($"30-second tick triggered. Processing throttled saves for {_dirtyCharacters.Count} character(s).");
                ProcessThrottledSaves();
            }

            // Tich one minute for consumables
            if (tick % 3600 == 0)
            {
                Debug.LogInfo("1-minute tick triggered. Computing one-minute effects.");
                ComputeOneMinuteTick();
            }
        }


        #region Do_Every_X_Ticks
        /// <summary>
        /// Have to run every 60 ticks!
        /// </summary>
        private void ComputeAllConsumableEffects()
        {
            Debug.LogDebug("Starting to compute all consumable effects.");

            for (int i = _allCharacterEffects.Count - 1; i >= 0; i--)
            {
                var item = _allCharacterEffects[i];

                Debug.LogDebug($"Processing effect at index {i}. Item is null: {item == null}.");

                if (item == null)
                {
                    Debug.LogWarning($"Found and removed a null consumable effect at index {i}.");
                    _allCharacterEffects.RemoveAtFast(i);
                }
                else if (!item.DoEffect())
                {
                    Debug.LogInfo($"Consumable effect '{item.GetType().Name}' returned false and will be removed at index {i}.");
                    _allCharacterEffects.RemoveAtFast(i);
                }
                else
                {
                    Debug.LogDebug($"Consumable effect '{item.GetType().Name}' was successfully applied and will remain.");
                }
            }

            Debug.LogDebug("Finished computing all consumable effects.");
        }

        private void ComputeOneMinuteTick()
        {
            Debug.LogInfo("Starting the one-minute tick calculation.");

            foreach (var entry in _characterStorageCache)
            {
                long entityId = entry.Key;
                CharacterStorage storage = entry.Value;

                Debug.LogDebug($"Processing character storage for entity ID: {entityId}.");

                // Skip if player is offline
                if (GetPlayerFromCharacterID(entityId) == null)
                {
                    Debug.LogDebug($"Player not online - skip for entity ID: {entityId}.");
                    continue;
                }

                bool dataChanged = storage.TickAllItems();

                if (dataChanged)
                {
                    Debug.LogInfo($"Data for entity ID {entityId} has changed. Marking character as dirty.");
                    MakeCharaterDirty(entityId);
                }
            }

            Debug.LogInfo("Finished the one-minute tick calculation.");
        }

        #endregion

        #region ActionEvents
        /// <summary>
        /// We check every entity on startup - again. Needed ?
        /// </summary>
        private void OnSessionReady()
        {
            Debug.LogInfo("Session is ready. Starting to process existing entities.");

            HashSet<IMyEntity> allEntities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allEntities);

            Debug.LogInfo($"Found {allEntities.Count} entities to process.");

            foreach (IMyEntity entity in allEntities)
            {
                Debug.LogDebug($"Processing entity: {entity.DisplayName} ({entity.EntityId})");
                OnEntityAdd(entity);
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                lock (_playerListLock)
                {
                    _currentPlayers.Add(MyAPIGateway.Session.LocalHumanPlayer);
                }
            }

            Debug.LogInfo("Finished processing all existing entities.");
        }

        private void OnCharacterDied(long identityID)
        {
            IMyPlayer player = GetPlayerFromIdentityID(identityID);
            if (player == null)
            {
                Debug.LogInfo($"Unable to find player from identity to remove {identityID}");
                return;
            }

            _charactersToRemove.Enqueue(player.Character.EntityId);
            Debug.LogInfo($"Enqueued character to remove {player.Character.EntityId}   Player.SteamUserId {player.SteamUserId}  Player.IdentityID {player.IdentityId}");
        }

        private void OnItemConsumed(IMyCharacter character, MyDefinitionId id)
        {
            Debug.LogInfo($"Attempting to consume item with ID: {id.SubtypeName} by character ID: {character?.EntityId}.");

            if (character == null)
            {
                Debug.LogError("Character is null. Cannot consume item.");
                return;
            }

            var itemDefinition = MyDefinitionManager.Static.GetDefinition(id) as MyConsumableItemDefinition;
            if (itemDefinition == null)
            {
                Debug.LogError($"Item definition for ID {id.SubtypeName} is null. Cannot consume.");
                return;
            }

            List<Effect> newEffects = new List<Effect>();
            bool applyEffects = true;
            bool helmUse = false;
            bool doNotTrack = false;

            IMyPlayer player = GetPlayerFromCharacterID(character.EntityId);
            if (player == null)
            {
                Debug.LogWarning("No player found for character. Aborting.");
                return;
            }

            // Eating in cryo
            if (Util.IsCryo(character.Parent) && !ConfigManager.Config.AllowEatingInCryo)
            {
                Debug.LogInfo($"Character {character.DisplayName} is in cryo. Cannot consume item. Returning item to inventory.");
                AddItemToPlayerCharacter(character, id);
                Server_SendNotificationToClient(player.SteamUserId, "NW_NO_Consumeables_in_Stasis");
                return;
            }

            // Eating in bed
            if (Util.IsBed(character.Parent) && !ConfigManager.Config.AllowEatingInBed)
            {
                Debug.LogInfo($"Character {character.DisplayName} is in bed. Cannot consume item. Returning item to inventory.");
                AddItemToPlayerCharacter(character, id);
                Server_SendNotificationToClient(player.SteamUserId, "NW_NO_Consumeables_in_Bed");
                return;
            }

            if (itemDefinition.Stats != null && itemDefinition.Stats.Count > 0)
            {
                Debug.LogDebug($"Processing {itemDefinition.Stats.Count} stats for item '{itemDefinition}'.");
                foreach (var stat in itemDefinition.Stats)
                {
                    if (!applyEffects)
                    {
                        Debug.LogDebug("Breaking from stat processing because applyEffects is false.");
                        break;
                    }

                    if (Enum.IsDefined(typeof(EffectType), stat.Name))
                    {
                        EffectType currentEffect = (EffectType)Enum.Parse(typeof(EffectType), stat.Name);

                        Debug.LogDebug($"Processing stat '{stat.Name}' with value '{stat.Value}'.");

                        if (currentEffect == EffectType.HelmUse)
                        {
                            helmUse = stat.Value != 0f;
                            Debug.LogDebug($"Setting helmUse to '{helmUse}'. Skipping effect creation.");
                            continue;
                        }

                        if (currentEffect == EffectType.DoNotTrack)
                        {
                            doNotTrack = stat.Value == 1f;
                            Debug.LogDebug($"Setting DoNotTrack mode to current consumable.");
                            continue;
                        }

                        if (applyEffects = !IsEffectPresent(character, currentEffect))
                        {
                            CharacterStorage storage;
                            float val = stat.Value;

                            if (_characterStorageCache.TryGetValue(character.EntityId, out storage))
                            {
                                val *= 1f / (storage.CountConsumable(id) + 1f);
                                Debug.LogDebug($"Diminishing returns applied. New value: {val}.");
                            }
                            else
                            {
                                val = stat.Value;
                                Debug.LogDebug("Character storage not found. Using original value.");
                            }

                            newEffects.Add(new Effect(character, player, currentEffect, val, stat.Time));
                            Debug.LogInfo($"New effect '{currentEffect}' with value '{val}' added to list.");
                        }
                        else
                        {
                            Debug.LogInfo($"Effect '{currentEffect}' is already active. Not applying.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Stat '{stat.Name}' is not a valid EffectType. Continue for compatibility.");
                        continue;
                    }
                }
            }
            else
            {
                Debug.LogInfo("Item has no stats defined.");
            }

            if (applyEffects)
            {
                if (!helmUse && !ConfigManager.Config.IgnoreClosedHelmet)
                {
                    Debug.LogDebug("Helm use is not allowed. Checking oxygen levels.");
                    if (character.OxygenLevel > 0.5f || character.EnvironmentOxygenLevel > 0.5f)
                    {
                        if (character.EnabledHelmet)
                        {
                            Debug.LogInfo("Helmet enabled. Toggling off for consumption.");
                            character.SwitchHelmet();
                            Server_SendNotificationToClient(player.SteamUserId, "NW_Helmet_Open_To_Eat");
                        }
                        else
                        {
                            Debug.LogDebug("Helmet is already off. Proceeding with consumption.");
                        }
                    }
                    else
                    {
                        Debug.LogInfo("Oxygen level too low. Cannot consume item. Returning item to inventory.");
                        AddItemToPlayerCharacter(character, id);
                        Server_SendNotificationToClient(player.SteamUserId, "NW_Helmet_NOT_Open_To_Eat");
                        return;
                    }
                }

                Debug.LogInfo("All effects are ready to be applied.");
                bool _addToConsumptionStorage = false;
                foreach (var effect in newEffects)
                {
                    // valid or not - add it to consumption list when consumed!
                    if (!_addToConsumptionStorage)
                        _addToConsumptionStorage = Util.CheckEffectIfItemCounts(effect.Type);

                    if (effect.IsValid)
                    {
                        _allCharacterEffects.Add(effect);
                        Debug.LogInfo($"Added valid effect '{effect.Type}' to player's active effects.");
                    }
                    else
                    {
                        Debug.LogWarning($"Effect with type '{effect.Type}' is invalid and will not be added.");
                    }
                }
                Debug.LogInfo("Successfully consumed item and applied effects.");

                if (_addToConsumptionStorage && !doNotTrack)
                    AddConsumableToStorageChache(character, id);
            }
            else
            {
                Debug.LogInfo("Effects cannot be applied. Returning item to inventory and notifying player.");
                AddItemToPlayerCharacter(character, id);
                Server_SendNotificationToClient(player.SteamUserId, "NW_Effect_already_active");
            }
        }

        private void AddConsumableToStorageChache(IMyCharacter character, MyDefinitionId id)
        {
            Debug.LogInfo($"Attempting to add consumable '{id.SubtypeName}' to character '{character.DisplayName}' storage cache.");

            CharacterStorage storage;
            if (_characterStorageCache.TryGetValue(character.EntityId, out storage))
            {
                storage.AddConsumable(id);
                MakeCharaterDirty(character.EntityId);
                Debug.LogInfo($"Successfully added consumable '{id.SubtypeName}' to character '{character.DisplayName}' storage.");
                Debug.LogDebug($"Character with ID '{character.EntityId}' marked as dirty.");
            }
            else
            {
                Debug.LogWarning($"Could not find character storage for entity ID '{character.EntityId}'. Consumable not added.");
            }
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            Debug.LogDebug($"Entity with display name '{entity.DisplayName}' and ID '{entity.EntityId}' was added.");

            // Is entity a IMyCharacter ??
            // Character without player - server start
            IMyCharacter spawnedCharacter = entity as IMyCharacter;
            if (spawnedCharacter != null && spawnedCharacter.IsPlayer && !spawnedCharacter.IsBot && !spawnedCharacter.IsDead)
            {
                Debug.LogInfo($"Recognized as a valid character: '{spawnedCharacter.DisplayName}'   EntityID '{spawnedCharacter.EntityId}'.");

                IMyPlayer player = GetPlayerFromCharacterID(spawnedCharacter.EntityId);
                CharacterStorage storage;

                if (player == null)
                {
                    // Character added without player (server start)
                    Debug.LogInfo($"Characters' player not online.");

                    storage = _characterStorageCache.GetOrAdd(spawnedCharacter.EntityId, (characterId) =>
                    {
                        Debug.LogDebug($"Starting to load/create storage data for character '{spawnedCharacter.DisplayName}' ({characterId}).");

                        if (spawnedCharacter.Storage == null)
                        {
                            Debug.LogInfo($"Character '{spawnedCharacter.DisplayName}' has no storage component. Creating one.");
                            spawnedCharacter.Storage = new MyModStorageComponent();
                        }

                        var storageComponent = spawnedCharacter.Storage;
                        string xmlData;
                        CharacterStorage newStorage; // UMBENANNT: Um Konflikte zu vermeiden, nennen wir diese lokale Variable 'newStorage'.
                        if (storageComponent.TryGetValue(Util.CHARACTER_STORAGE_GUID, out xmlData) && !string.IsNullOrEmpty(xmlData))
                        {
                            try
                            {
                                Debug.LogDebug("Found stored data. Deserializing from XML.");
                                newStorage = MyAPIGateway.Utilities.SerializeFromXML<CharacterStorage>(xmlData);
                                Debug.LogInfo("Successfully loaded storage data.");
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Corrupt storage data for {spawnedCharacter.DisplayName}: {e.Message}. Creating fallback.");
                                newStorage = new CharacterStorage();
                            }
                        }
                        else
                        {
                            Debug.LogInfo("No existing storage data found. Creating new storage data.");
                            newStorage = new CharacterStorage();
                        }
                        return newStorage; // HINWEIS: Hier wird das erstellte Objekt zurückgegeben.
                    });
                }
                else
                {
                    // Character added while player online (character spawn / player login)
                    Debug.LogInfo($"Characters' player online, SteamUserId '{player.SteamUserId}'.");

                    storage = _characterStorageCache.GetOrAdd(spawnedCharacter.EntityId, (entityId) =>
                    {
                        Debug.LogInfo($"Starting to load/create storage data for character '{spawnedCharacter.DisplayName}' ({entityId}).");

                        var storageComponent = spawnedCharacter.Storage;
                        string xmlData;
                        CharacterStorage newStorage; // UMBENANNT: Auch hier umbenannt, um Konsistenz zu wahren.
                        if (storageComponent.TryGetValue(Util.CHARACTER_STORAGE_GUID, out xmlData) && !string.IsNullOrEmpty(xmlData))
                        {
                            try
                            {
                                Debug.LogDebug("Found stored data. Deserializing from XML.");
                                newStorage = MyAPIGateway.Utilities.SerializeFromXML<CharacterStorage>(xmlData);
                                Debug.LogInfo("Successfully loaded storage data.");
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Corrupt storage data for {spawnedCharacter.DisplayName}: {e.Message}. Creating fallback.");
                                newStorage = new CharacterStorage();
                            }
                        }
                        else
                        {
                            Debug.LogInfo("No existing storage data found. Creating new storage data.");
                            newStorage = new CharacterStorage();
                        }

                        // Safety write SteamID to storage
                        newStorage.SteamID = player.SteamUserId;
                        Debug.LogDebug($"Assigned SteamID '{player.SteamUserId}' to the storage.");
                        Debug.LogInfo("Finished loading character storage data.");

                        return newStorage;
                    });

                }
                Debug.LogDebug("Character storage data loaded or created for the character.");


                if (storage != null)
                {
                    Debug.LogInfo($"Create AdvancedStats logic for {spawnedCharacter.DisplayName}");
                    _characterStatusDecay.TryAdd(spawnedCharacter.EntityId, new AdvancedStats(spawnedCharacter));
                }

                if (player != null)
                {
                    // Send storage to player
                    Debug.LogInfo($"Player {player.DisplayName} (ID {player.SteamUserId}) is online with character (EID {spawnedCharacter.EntityId}). Sending storage data to client.");
                    Server_SendStorageToClient(player.SteamUserId, storage);
                }

            }
            else
                Debug.LogDebug($"Entity '{entity.DisplayName}' is not a valid character for caching. Skipping.");

        }

        private void OnPlayerConnected(long identityID)
        {
            IMyPlayer joinedPlayer = MyAPIGateway.Players.TryGetIdentityId(identityID);
            if (joinedPlayer != null && !joinedPlayer.IsBot)
            {
                lock (_playerListLock)
                    _currentPlayers.Add(joinedPlayer);
                Debug.LogInfo($"Valid player '{joinedPlayer.DisplayName}' with ID '{joinedPlayer.SteamUserId}' connected.");

                CharacterStorage _storage;
                if (_characterStorageCache.TryGetValue(joinedPlayer.Character.EntityId, out _storage))
                {
                    // Character is in storage
                    Debug.LogInfo($"Player {joinedPlayer.DisplayName} (ID {joinedPlayer.SteamUserId}) connected with character (EID {joinedPlayer.Character.EntityId}). Data sent to client.");
                    Server_SendStorageToClient(joinedPlayer.SteamUserId, _storage);
                }
                else
                {
                    // Character not in storage (player joined first time)
                    Debug.LogInfo($"Player {joinedPlayer.DisplayName} (ID {joinedPlayer.SteamUserId}) connected without character.");
                }
                return;
            }
            else
                Debug.LogDebug($"Entity '{identityID}' is not a valid player for caching. Skipping.");

            return;
        }


        #endregion

        #region Radiation (parallel worker)

        private void RadiationSearchWorker()
        {
            Debug.LogInfo("[RadiationWorker] Worker task has started its main loop.");

            while (!_stopWorkerThread && ConfigManager.Config.EnableRadiationAdvanced)
            {
                try
                {
                    Debug.LogDebug("[RadiationWorker] ===== Starting new radiation scan cycle =====");

                    List<IMyPlayer> playersSnapshot;
                    lock (_playerListLock)
                    {
                        playersSnapshot = new List<IMyPlayer>(_currentPlayers);
                    }

                    var newRadiationLevels = new Dictionary<long, float>();

                    foreach (var player in playersSnapshot)
                    {
                        if (player?.Character == null || player.Character.Closed)
                            continue;

                        float totalRadiation = 0f;
                        var playerPosition = player.Character.GetPosition();

                        #region Case: 1 - Character inventory
                        var playerInventory = player.Character.GetInventory();
                        if (playerInventory != null)
                        {
                            var uraniumAmount = playerInventory.GetItemAmount(_uraniumDefId);
                            if (uraniumAmount > (VRage.MyFixedPoint)ConfigManager.Config.RadiationUraniumIngotThreshold)
                            {
                                // For items carried by the player, the distance is the defined minimum.
                                float distance = ConfigManager.Config.RadiationMinimumDistance;

                                float radiationIncrease = ConfigManager.Config.RadiationSuitGuardFactor * ((float)Math.Sqrt((double)uraniumAmount) / (distance * distance));

                                // NOTE: No additional shielding is applied for items in the player's suit inventory.

                                if (radiationIncrease >= ConfigManager.Config.RadiationNegligibleDose)
                                {
                                    totalRadiation += radiationIncrease;
                                    Debug.LogInfo($"[RadiationWorker] !! PLAYER INVENTORY SOURCE !! Player: '{player.DisplayName}', Uranium: {(float)uraniumAmount:F2}kg, ΔS Added: {radiationIncrease:F4}");
                                }
                            }
                        }

                        #endregion

                        var searchSphere = new BoundingSphereD(playerPosition, ConfigManager.Config.RadiationUraniumSearchRadius);
                        var entitiesInSphere = MyAPIGateway.Entities.GetEntitiesInSphere(ref searchSphere);

                        Debug.LogDebug($"[RadiationWorker] Player '{player.DisplayName}' has {entitiesInSphere.Count} entities. Starting parallel scan...");

                        // A lock object to safely add to totalRadiation from multiple threads.
                        object radiationLock = new object();

                        MyAPIGateway.Parallel.ForEach(entitiesInSphere, entity =>
                        {
                            #region Case: 2 - Inventory Logic (Grids, Cargo Bags)
                            var inventories = new List<IMyInventory>();
                            var grid = entity as IMyCubeGrid;
                            var cargoBag = entity as MyCargoContainerInventoryBagEntity;
                            var backpack = entity as MyInventoryBagEntity;

                            if (grid != null)
                            {
                                grid.GetBlocks(null, b =>
                                {
                                    var cubeBlock = b.FatBlock;
                                    if (cubeBlock != null && cubeBlock.HasInventory)
                                    {
                                        for (int i = 0; i < cubeBlock.InventoryCount; i++)
                                            inventories.Add(cubeBlock.GetInventory(i));
                                    }
                                    return false;
                                });
                            }
                            else if (cargoBag != null && cargoBag.HasInventory)
                            {
                                inventories.Add(cargoBag.GetInventory());
                            }
                            else if (backpack != null && backpack.HasInventory)
                            {
                                inventories.Add(backpack.GetInventory());
                            }

                            // Compute collected inventories
                            if (inventories.Count > 0)
                            {
                                foreach (var inventory in inventories)
                                {
                                    if (inventory == null) continue;
                                    var uraniumAmount = inventory.GetItemAmount(_uraniumDefId);

                                    if (uraniumAmount > (VRage.MyFixedPoint)ConfigManager.Config.RadiationUraniumIngotThreshold)
                                    {
                                        var inventoryOwner = inventory.Owner as IMyEntity;
                                        if (inventoryOwner == null) continue;

                                        float distance = (float)Vector3D.Distance(playerPosition, inventoryOwner.GetPosition());
                                        if (distance < ConfigManager.Config.RadiationMinimumDistance)
                                            distance = ConfigManager.Config.RadiationMinimumDistance;

                                        float radiationIncrease = ConfigManager.Config.RadiationSuitGuardFactor * ((float)Math.Sqrt((double)uraniumAmount) / (distance * distance));

                                        float shieldingFactor = 1.0f; // Default: No shielding
                                        if (inventoryOwner is IMyReactor)
                                        {
                                            shieldingFactor = ConfigManager.Config.RadiationReactorShielding;
                                        }
                                        else if (inventoryOwner is IMyTerminalBlock) // Any grid container block
                                        {
                                            shieldingFactor = ConfigManager.Config.RadiationContainerShielding;
                                        }
                                        // NOTE: CargoBags are not IMyTerminalBlock, so they get no shielding.

                                        radiationIncrease *= shieldingFactor;

                                        if (radiationIncrease >= ConfigManager.Config.RadiationNegligibleDose)
                                        {
                                            lock (radiationLock)
                                            {
                                                totalRadiation += radiationIncrease;
                                            }
                                        }
                                    }
                                }
                                return;
                            }
                            #endregion

                            #region Case: 3 - Floating Object
                            var floatingObject = entity as MyFloatingObject;
                            if (floatingObject != null)
                            {
                                if (floatingObject.ItemDefinition != null)
                                {
                                    if (floatingObject.ItemDefinition.Id == _uraniumDefId)
                                    {
                                        MyFixedPoint uraniumAmount = floatingObject.Amount;

                                        if (uraniumAmount > (VRage.MyFixedPoint)ConfigManager.Config.RadiationUraniumIngotThreshold)
                                        {
                                            Vector3D objectPosition = floatingObject.PositionComp.GetPosition();
                                            float distance = (float)Vector3D.Distance(playerPosition, objectPosition);

                                            if (distance < ConfigManager.Config.RadiationMinimumDistance)
                                                distance = ConfigManager.Config.RadiationMinimumDistance;

                                            float radiationIncrease = ConfigManager.Config.RadiationSuitGuardFactor * ((float)Math.Sqrt((double)uraniumAmount) / (distance * distance));

                                            if (radiationIncrease >= ConfigManager.Config.RadiationNegligibleDose)
                                            {
                                                lock (radiationLock)
                                                {
                                                    totalRadiation += radiationIncrease;
                                                }
                                                Debug.LogInfo($"[RadiationWorker] !! FLOATING SOURCE FOUND !! Player: '{player.DisplayName}', Uranium: {(float)uraniumAmount:F2}kg, Distance: {distance:F1}m, ΔS Added: {radiationIncrease:F4}");
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion

                        });

                        newRadiationLevels[player.Character.EntityId] = totalRadiation;

                        if (totalRadiation > 0)
                        {
                            Debug.LogInfo($"[RadiationWorker] >>> Player '{player.DisplayName}' has a total calculated radiation level (ΔS) of {totalRadiation:F4} for this cycle (scanned in parallel).");
                        }
                    }

                    _characterRadiationLevels.Clear();
                    foreach (var entry in newRadiationLevels)
                    {
                        _characterRadiationLevels[entry.Key] = entry.Value;
                    }

                    Debug.LogDebug($"[RadiationWorker] ===== Scan cycle complete. Worker is now sleeping. =====");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RadiationWorker] !!! CRITICAL EXCEPTION in worker loop: {e.Message}\n{e.StackTrace}");
                }

                const int sleepTimeMs = (1000 * 100) / 60;
                MyAPIGateway.Parallel.Sleep(sleepTimeMs);
            }
            Debug.LogInfo("[RadiationWorker] Worker task has received stop signal and is shutting down gracefully.");
        }

        #endregion

        #region Network
        /// <summary>
        /// Sends CharacterStorage to player (to display at UI)
        /// </summary>
        /// <param name="steamId"></param>
        /// <param name="storage"></param>
        private void Server_SendStorageToClient(ulong steamId, CharacterStorage storage)
        {
            if (storage == null) return;
            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(storage);
                MyAPIGateway.Multiplayer.SendMessageTo(Network.STORAGE_CHANNEL, data, steamId);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"[APEX.Advanced.Server] Error sending network message: {e.Message}");
            }
        }


        private void Server_ClientAskForStorage(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {
            if (isFromServer) return;

            CharacterStorage possibleCharacter;
            var receivedStorage = MyAPIGateway.Utilities.SerializeFromBinary<CharacterStorage>(data);
            if (receivedStorage != null)
                possibleCharacter = receivedStorage;
            else
            {
                MyLog.Default.WriteLineAndConsole($"[APEX.Advanced!] WARNING - Players character sent wrong data!");
                return;
            }

            CharacterStorage serverStorage;
            if (_characterStorageCache.TryGetValue(possibleCharacter.CharacterID, out serverStorage))
            {
                // Eintrag gefunden, prüfe ob es der identische Player is.
                if (serverStorage.SteamID == steamId)
                    Server_SendStorageToClient(steamId, serverStorage);
                else
                {
                    serverStorage = new CharacterStorage();
                    serverStorage.SteamID = steamId;
                    serverStorage.CharacterID = possibleCharacter.CharacterID;

                    Server_SendStorageToClient(steamId, serverStorage);
                    Debug.LogWarning($"Player {steamId} with character {possibleCharacter.CharacterID} was not found on server, CREATED A NEW ENTRY");
                }
            }
        }

        /// <summary>
        /// Sends string to player
        /// </summary>
        private void Server_SendNotificationToClient(ulong steamId, string message)
        {
            if (message == null) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                MyAPIGateway.Multiplayer.SendMessageTo(Network.NOTIFICATION_CHANNEL, data, steamId);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"[APEX.Advanced.Server] Error sending network message: {e.Message}");
            }
        }

        private void Server_ReceiveAdminCommand(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {

        }

        /// <summary>
        /// Sends a radiation event to the client to trigger a sound effect.
        /// </summary>
        private void Server_SendRadiationEventToClient(ulong steamId, float radiationToAdd)
        {
            byte[] data = BitConverter.GetBytes(radiationToAdd);
            MyAPIGateway.Multiplayer.SendMessageTo(Network.RADIATION_CHANNEL, data, steamId);
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Marks character dirty and send it to player
        /// </summary>
        /// <param name="_characterID">IMyCharacter EntityID</param>
        private void MakeCharaterDirty(long _characterID)
        {
            Debug.LogInfo($"Attempting to mark character with entity ID '{_characterID}' as dirty and send data to client.");

            IMyPlayer player;
            player = GetPlayerFromCharacterID(_characterID);

            _dirtyCharacters.Add(_characterID);

            CharacterStorage storage;
            if (!_characterStorageCache.TryGetValue(_characterID, out storage))
            {
                Debug.LogWarning($"Character storage not found for entity ID '{_characterID}'. Aborting data sync.");
                return;
            }

            if (player != null)
            {
                Server_SendStorageToClient(player.SteamUserId, _characterStorageCache[_characterID]);
                Debug.LogInfo($"Successfully sent updated character storage data for '{player.DisplayName}' to client.");
            }
            else
            {
                Debug.LogWarning($"Player not found for character with entity ID '{_characterID}'. SteamID '{storage.SteamID}'  CharacterID '{storage.CharacterID}'. Aborting data sync.");
            }
        }

        public IMyPlayer GetPlayerFromIdentityID(long identityID)
        {
            lock (_playerListLock)
            {
                foreach (var player in _currentPlayers)
                {
                    if (player.IdentityId == identityID)
                        return player;
                }
            }

            return null;
        }

        /// <param name="characterID"></param>
        /// <returns>IMyPlayer object, if player is online. Otherwise null</returns>
        public IMyPlayer GetPlayerFromCharacterID(long characterID)
        {
            lock (_playerListLock)
            {
                foreach (var player in _currentPlayers)
                {
                    if (player.Character.EntityId == characterID)
                        return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Safe all _characterStorageCache to its entity storage
        /// </summary>
        private void SaveAllCharactersStorageData()
        {
            Debug.LogInfo("Starting to save all character storage data.");

            if (_characterStorageCache == null || _characterStorageCache.Count == 0)
            {
                Debug.LogInfo("Character storage cache is empty. Nothing to save.");
                return;
            }

            foreach (var entry in _characterStorageCache)
            {
                long identityId = entry.Key;
                CharacterStorage storageToSave = entry.Value;

                Debug.LogDebug($"Processing character with ID '{identityId}' for saving.");

                IMyEntity entityToSave;
                if (!MyAPIGateway.Entities.TryGetEntityById(identityId, out entityToSave))
                {
                    Debug.LogWarning($"Entity with ID '{identityId}' not found. Assuming character is no longer present. Marking for removal.");
                    _charactersToRemove.Enqueue(identityId);
                    continue;
                }

                IMyCharacter characterToSave = entityToSave as IMyCharacter;
                if (characterToSave == null)
                {
                    Debug.LogError($"Entity with ID '{identityId}' found, but is not a character. Type is '{entityToSave.GetType().Name}'. This should not happen. Marking for removal.");
                    _charactersToRemove.Enqueue(identityId);
                    continue;
                }

                characterToSave.Storage = new MyModStorageComponent();
                if (ConfigManager.Config != null)
                {
                    try
                    {
                        Debug.LogDebug($"Serializing and saving storage for character '{characterToSave.DisplayName}' ({identityId}).");
                        characterToSave.Storage[Util.CHARACTER_STORAGE_GUID] = MyAPIGateway.Utilities.SerializeToXML(storageToSave);
                        Debug.LogInfo($"Successfully saved storage data for character '{characterToSave.DisplayName}'.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Unable to save character '{characterToSave.DisplayName}' ({identityId}). Exception: {e.Message}.");
                    }
                }
                else
                {
                    // Removes object, then savegame is able to load without mod. So no consumables are tracked over a restart or nexus switch
                    characterToSave.Storage.Clear();
                    Debug.LogInfo($"Cleanup Characters '{characterToSave.DisplayName}' storage. Game is ready for mod removal.");
                }
            }
            Debug.LogInfo("Finished saving all character storage data.");
        }

        private void SaveCharactersStorageData(long foundEntityId, CharacterStorage storageToSave)
        {
            Debug.LogInfo($"Attempting to save storage data for character with ID '{foundEntityId}'.");

            IMyEntity entity;
            if (MyAPIGateway.Entities.TryGetEntityById(foundEntityId, out entity))
            {
                IMyCharacter character = entity as IMyCharacter;
                if (character != null)
                {
                    Debug.LogDebug($"Entity '{character.DisplayName}' ({foundEntityId}) is a valid character. Checking storage.");
                    if (character.Storage != null)
                    {
                        try
                        {
                            character.Storage[Util.CHARACTER_STORAGE_GUID] = MyAPIGateway.Utilities.SerializeToXML(storageToSave);
                            Debug.LogInfo($"Successfully saved storage data for character '{character.DisplayName}' ({foundEntityId}).");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"An error occurred while saving storage for character '{character.DisplayName}' ({foundEntityId}): {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Character '{character.DisplayName}' ({foundEntityId}) has no storage component. Data cannot be saved.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Entity with ID '{foundEntityId}' is not a character. Type is '{entity.GetType().Name}'. Skipping save.");
                }
            }
            else
            {
                Debug.LogWarning($"Entity with ID '{foundEntityId}' not found. Cannot save storage data.");
            }
        }

        private void OnPlayerDisconnected(long identityId)
        {
            Debug.LogInfo($"Processing disconnect for player with identity ID '{identityId}'.");

            if (identityId == 0)
            {
                Debug.LogWarning("Received disconnect event for an invalid identity ID (0). Skipping.");
                return;
            }

            IMyPlayer player = GetPlayerFromIdentityID(identityId);
            Debug.LogDebug($"[Disconnect] Attempt 1: GetPlayerFromIdentityID returned '{(player == null ? "null" : player.DisplayName)}'.");

            if (player == null)
            {
                player = MyAPIGateway.Players.TryGetIdentityId(identityId);
                Debug.LogDebug($"[Disconnect] Attempt 2: MyAPIGateway.Players.TryGetPlayerById returned '{(player == null ? "null" : player.DisplayName)}'.");
            }

            if (player == null)
            {
                Debug.LogWarning($"[Disconnect] Could not find any IMyPlayer object for identity ID '{identityId}'. Aborting.");
                return;
            }

            Debug.LogInfo($"[Disconnect] Found player: '{player.DisplayName}' (SteamID: {player.SteamUserId}).");

            bool removed;
            lock (_playerListLock)
            {
                removed = _currentPlayers.Remove(player);
            }

            if (removed)
                Debug.LogInfo($"[Disconnect] Successfully removed player from _currentPlayers. New count: {_currentPlayers.Count}.");
            else
                Debug.LogWarning($"[Disconnect] Player '{player.DisplayName}' was NOT FOUND in _currentPlayers list for removal.");

            long foundEntityId = 0;
            CharacterStorage storageToSave = null;

            foreach (var entry in _characterStorageCache)
            {
                if (entry.Value.SteamID == player.SteamUserId)
                {
                    foundEntityId = entry.Key;
                    storageToSave = entry.Value;
                    Debug.LogDebug($"Found matching character entity ID '{foundEntityId}' in cache for disconnected player.");
                    break;
                }
            }

            if (foundEntityId == 0)
            {
                Debug.LogWarning($"Could not find a character storage entry for player identity ID '{identityId}'. No data to save.");
                return;
            }

            CharacterStorage trash;
            if (_characterStorageCache.TryRemove(foundEntityId, out trash))
            {
                Debug.LogInfo($"Successfully removed character entity ID '{foundEntityId}' from cache.");
            }
            else
            {
                Debug.LogWarning($"Failed to remove character entity ID '{foundEntityId}' from cache.");
            }

            // remove AdvancedStats entry
            AdvancedStats value;
            if (player.Character != null)
            {
                if (_characterStatusDecay.TryRemove(player.Character.EntityId, out value))
                    Debug.LogDebug($"Found matching character entity ID '{player.Character.EntityId}' in _characterStatusDecay for disconnected player.");
                else
                    Debug.LogDebug($"Found NO matching character entity ID '{player.Character.EntityId}' in _characterStatusDecay for disconnected player.");
            }
            else
            {
                Debug.LogWarning($"[Disconnect] Player '{player.DisplayName}' has a null character object. Skipping removal from _characterStatusDecay.");
            }

            SaveCharactersStorageData(foundEntityId, storageToSave);
            Debug.LogInfo($"SaveCharactersStorageData method called for entity ID '{foundEntityId}'.");
        }

        private bool IsEffectPresent(IMyCharacter character, EffectType type)
        {
            Debug.LogDebug($"Checking if effect '{type}' is already present on character '{character.DisplayName}' ({character.EntityId}).");

            foreach (Effect effect in _allCharacterEffects)
            {
                Debug.LogDebug($"-- Comparing with existing effect: '{effect.Type}' (Unique: {effect.IsUnique}) on character '{effect.Character?.DisplayName}' ({effect.Character?.EntityId}).");

                if (effect.Character == character && effect.Type == type && effect.IsUnique)
                {
                    Debug.LogInfo($"Unique effect '{type}' is already active on character '{character.DisplayName}'. Returning true.");
                    return true;
                }
            }

            Debug.LogDebug($"Effect '{type}' is not present or not unique. Returning false.");
            return false;
        }

        private void AddItemToPlayerCharacter(IMyCharacter character, MyDefinitionId id, int amount = 1)
        {
            Debug.LogInfo($"Attempting to add {amount} of item '{id.SubtypeName}' to character '{character.DisplayName}'.");

            if (character?.GetInventory() == null)
            {
                Debug.LogWarning($"Character '{character.DisplayName}' has no inventory. Cannot add item. Aborting.");
                return;
            }

            var physicalObject = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id);

            if (physicalObject == null)
            {
                Debug.LogError($"Could not create physical object from definition ID '{id.SubtypeName}'. Aborting.");
                return;
            }

            character.GetInventory().AddItems(amount, physicalObject);

            Debug.LogInfo($"Successfully added {amount} of item '{id.SubtypeName}' to character '{character.DisplayName}'.");
        }

        private void ProcessThrottledSaves()
        {
            Debug.LogInfo("Starting throttled save process.");

            var idsToProcess = new List<long>(_dirtyCharacters);

            if (idsToProcess.Count == 0)
            {
                Debug.LogDebug("No characters to save. Skipping save process.");
                return;
            }

            Debug.LogInfo($"Found {idsToProcess.Count} characters to save.");

            foreach (long entityId in idsToProcess)
            {
                CharacterStorage storageToSave;
                if (_characterStorageCache.TryGetValue(entityId, out storageToSave))
                {
                    Debug.LogDebug($"Processing throttled save for character with Entity ID '{entityId}'.");
                    SaveCharactersStorageData(entityId, storageToSave);
                }
                else
                {
                    Debug.LogWarning($"Character with Entity ID '{entityId}' was marked as dirty but not found in cache. Skipping save.");
                }

                _dirtyCharacters.Remove(entityId);
                Debug.LogDebug($"Removed Entity ID '{entityId}' from the dirty list.");
            }

            Debug.LogInfo("Throttled save process finished.");
        }

        #endregion

    }
}