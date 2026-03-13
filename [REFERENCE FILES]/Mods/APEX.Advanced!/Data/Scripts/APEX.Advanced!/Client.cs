using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APEX.Advanced.Client.MySleep;
using APEX.Advanced.Client.MyStatus;
using APEX.Advanced.Client.MyVisor;
using APEX.Advanced.Client.MyWarnings;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;


namespace APEX.Advanced.Client
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class APEX_Advanced_Client : MySessionComponentBase
    {
        private static bool IsDedicated;
        private static bool IsServer;
        private static bool IsNotCreativeMode;
        private readonly MyDefinitionId HydrogenID = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Hydrogen");
        private readonly MyDefinitionId OxygenID = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
        private ConsumableStatsPatcher consumableStatsPatcher;
        private bool richHudInitialized = false;

        private CharacterStatus _myGUIWindow;

        private SleepOverlay _sleepEffect;
        private float? _debugSleepOverride = null;

        private IMyCharacter _character = null;
        private IMyPlayer _player = null;
        private MyCharacterStatComponent _statComponent = null;
        private CharacterStorage _characterStorage = null; // ... from server
        private MyConcurrentList<Effect> _myCharacterEffects = new MyConcurrentList<Effect>();

        private Dictionary<EffectType, MyEntityStat> _characterStats = new Dictionary<EffectType, MyEntityStat>();
        private Visor _visorOverlay;
        private Warnings _warnings;
        private int tick = 0;

        // Sound
        private MyEntity3DSoundEmitter _soundEmitter = null;
        private MySoundPair _soundEat = null;
        private MySoundPair _soundDrink = null;
        private MySoundPair _soundPowerKit = null;
        private MySoundPair _soundMedKit = null;

        private int _incomingRadiation = 0;
        private MySoundPair _soundRadiationLOW = null;
        private MySoundPair _soundRadiationHIGH = null;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
            {
                consumableStatsPatcher = new ConsumableStatsPatcher();
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            IsDedicated = MyAPIGateway.Utilities.IsDedicated;
            IsServer = MyAPIGateway.Session.IsServer;

            // creative mode or settings wrong
            IsNotCreativeMode = MyAPIGateway.Session.SessionSettings.FoodConsumptionRate > 0 || !MyAPIGateway.Session.CreativeMode;

            if (IsDedicated)
                return;

            EffectType[] statsToTrack = new[]
                {
                    EffectType.Health, EffectType.Food, EffectType.Water, EffectType.Sleep,
                    EffectType.Recovery, EffectType.Fatigue, EffectType.Bloating
                };

            foreach (EffectType statType in statsToTrack)
                _characterStats[statType] = null;

            //RichHudClient.Init("APEX.Advanced!", HudInit, ClientReset);
            // Network
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.NOTIFICATION_CHANNEL, Client_ReceiveNotification);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.STORAGE_CHANNEL, Client_ReceiveStorage);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.RADIATION_CHANNEL, Client_ReceiveRadiationEvent);

            // Event-Listening
            MyAPIGateway.Players.ItemConsumed += OnItemConsumed;
            MyVisualScriptLogicProvider.PlayerDied += OnCharacterDied;
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            InitializeAudio();

        }

        protected override void UnloadData()
        {
            if (!IsDedicated)
            {
                RichHudClient.Reset();
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Network.NOTIFICATION_CHANNEL, Client_ReceiveNotification);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Network.STORAGE_CHANNEL, Client_ReceiveStorage);
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
                MyAPIGateway.Players.ItemConsumed -= OnItemConsumed;
                MyVisualScriptLogicProvider.PlayerDied -= OnCharacterDied;
            }
            base.UnloadData();
        }

        public override void UpdateAfterSimulation()
        {
            #region Basic init process
            if (IsDedicated) return;

            if (!richHudInitialized && ConfigManager.Config != null)
            {
                RichHudClient.Init("APEX.Advanced!", HudInit, ClientReset);
                richHudInitialized = true;
            }

            if (!richHudInitialized) return;

            base.UpdateAfterSimulation();
            // Start ticking ...
            tick++;

            // Always get current character // player
            if (_character == null || MyAPIGateway.Session.LocalHumanPlayer?.Character != _character)
            {
                _player = MyAPIGateway.Session.LocalHumanPlayer;
                _character = MyAPIGateway.Session.LocalHumanPlayer?.Character;
            }
            // No chareacter - no update
            if (_character == null) return;

            // Try to get _characterStorage
            if (_characterStorage == null && tick % 250 == 0)
            {
                Client_AskServerForStorage();
                return;
            }
            if (_characterStorage == null)
                return;

            // TODO Check if needed, Safety when RHF is not present 
            //if (_visorOverlay == null || _sleepEffect == null || _myGUIWindow == null || _warnings == null)
            //    return;
            #endregion

            DoGetStatusValues();
            DoUIStuff();

            if (IsNotCreativeMode)
            {
                DoHelmStuff();
                if (ConfigManager.Config.EnableSleepEffect)
                    DoSleepStuff();
                DoWarningStuff();
            }
        }

        #region Do_Every_X_Ticks
        private void DoHelmStuff()
        {
            if (_visorOverlay != null && ConfigManager.Config.EnableVignetteFirstPerson)
            {
                bool isHelmetOn = _character.EnabledHelmet;
                bool isFirstPerson = MyAPIGateway.Session.CameraController.IsInFirstPersonView;
                
                // Aktualisiere das Visier
                _visorOverlay.UpdateVisorState(isHelmetOn, isFirstPerson);
            }
        }

        private void DoSleepStuff()
        {
            if (_sleepEffect != null)
            {
                float sleepValueToUse;

                // Wenn ein Test-Wert gesetzt ist, verwende ihn.
                if (_debugSleepOverride.HasValue)
                    sleepValueToUse = _debugSleepOverride.Value;
                else
                    sleepValueToUse = _characterStats[EffectType.Sleep]?.Value ?? 100f;

                if (tick % 60 == 0)
                {
                    // Nur blinzeln, wenn der Schlaf niedrig ist
                    if (sleepValueToUse < 20f)
                    {
                        if (MyRandom.Instance.Next(0, 100) < SleepOverlay.BlinkChancePerSecond)
                        {
                            _sleepEffect.TriggerBlink();
                        }
                    }
                }
                _sleepEffect.UpdateEffect(sleepValueToUse);
            }
        }

        private void DoWarningStuff()
        {
            _warnings.Tick = tick;
            //_warnings.SuitPower = (int)(_character?.SuitEnergyLevel * 100 ?? -1);
            //_warnings.SuitOxygen = (int)(_character?.GetSuitGasFillLevel(OxygenID) * 100 ?? -1);
            //_warnings.Health = (int)(_characterStats[EffectType.Health]?.Value ?? -1);
            _warnings.Food = (int)(_characterStats[EffectType.Food]?.Value ?? -1);
            _warnings.Water = (int)(_characterStats[EffectType.Water]?.Value ?? -1);
            _warnings.Bloating = (int)(_characterStats[EffectType.Bloating]?.Value ?? -1);
        }

        private void DoUIStuff()
        {
            // Quit if UI is not present (this check is slightly redundant due to the check in UpdateAfterSimulation, but harmless).
            if (_myGUIWindow == null)
                return;

            // Do not open UI when mouse is visible and UI is not visible
            if (MyAPIGateway.Gui.IsCursorVisible && !_myGUIWindow.Visible)
                return;

            // Update things when UI is visible
            if (_myGUIWindow.Visible)
            {
                // If Stat is null, return -1
                if (!_myGUIWindow._consumableChain.Visible)  // TODO: secure chain access
                    _myGUIWindow.MyConsumedItems = _characterStorage.GetActiveConsumableNames();

                _myGUIWindow.SuitPower = (int)(_character?.SuitEnergyLevel * 100 ?? -1);
                _myGUIWindow.SuitOxygen = (int)(_character?.GetSuitGasFillLevel(OxygenID) * 100 ?? -1);
                _myGUIWindow.SuitHydrogen = (int)(_character?.GetSuitGasFillLevel(HydrogenID) * 100 ?? -1);
                _myGUIWindow.Health = (int)(_characterStats[EffectType.Health]?.Value ?? -1);
                _myGUIWindow.Food = (int)(_characterStats[EffectType.Food]?.Value ?? -1);
                _myGUIWindow.Water = (int)(_characterStats[EffectType.Water]?.Value ?? -1);

                if (ConfigManager.Config.EnableSleepEffect)
                    _myGUIWindow.Sleep = (int)(_characterStats[EffectType.Sleep]?.Value ?? -1);
                else
                    _myGUIWindow.Sleep = -1;

                _myGUIWindow.Recovery = (int)(_characterStats[EffectType.Recovery]?.Value ?? -1);
                _myGUIWindow.Fatigue = (int)(_characterStats[EffectType.Fatigue]?.Value ?? -1);
                _myGUIWindow.Bloating = (int)(_characterStats[EffectType.Bloating]?.Value ?? -1);
            }

            // TODO: Controller approach

            // Open/Close UI on CTRL+N
            /*
            if (KeybindManager.PrimaryKey != MyKeys.None && MyAPIGateway.Input.IsNewKeyPressed(KeybindManager.PrimaryKey))
            {
                bool ctrlPressed = MyAPIGateway.Input.IsAnyCtrlKeyPressed();
                bool altPressed = MyAPIGateway.Input.IsAnyAltKeyPressed();
                bool shiftPressed = MyAPIGateway.Input.IsAnyShiftKeyPressed();

                if (ctrlPressed == KeybindManager.NeedsControl &&
                    altPressed == KeybindManager.NeedsAlt &&
                    shiftPressed == KeybindManager.NeedsShift)
                {
                    if (_myGUIWindow.Visible)
                        CloseWindow();
                    else
                        OpenWindow();
                }
            }*/

            // no key? skip!
            if (KeybindManager.PrimaryKey == MyKeys.None ||
                MyAPIGateway.Gui.ChatEntryVisible ||
                (!_myGUIWindow.Visible && MyAPIGateway.Gui.IsCursorVisible))
                return;

            // Mode 1: Toggle
            if (!ConfigManager.CConfig.HoldKeyToOpenGUI)
            {
                if (MyAPIGateway.Input.IsNewKeyPressed(KeybindManager.PrimaryKey))
                {
                    bool ctrlPressed = MyAPIGateway.Input.IsAnyCtrlKeyPressed();
                    bool altPressed = MyAPIGateway.Input.IsAnyAltKeyPressed();
                    bool shiftPressed = MyAPIGateway.Input.IsAnyShiftKeyPressed();

                    if (ctrlPressed == KeybindManager.NeedsControl &&
                        altPressed == KeybindManager.NeedsAlt &&
                        shiftPressed == KeybindManager.NeedsShift)
                    {
                        if (_myGUIWindow.Visible)
                            CloseWindow();
                        else
                            OpenWindow();
                    }
                }
            }
            // Modue 2: hold to open
            else
            {
                bool ctrlPressed = MyAPIGateway.Input.IsAnyCtrlKeyPressed();
                bool altPressed = MyAPIGateway.Input.IsAnyAltKeyPressed();
                bool shiftPressed = MyAPIGateway.Input.IsAnyShiftKeyPressed();

                bool combinationIsHeld = (ctrlPressed == KeybindManager.NeedsControl &&
                                            altPressed == KeybindManager.NeedsAlt &&
                                            shiftPressed == KeybindManager.NeedsShift &&
                                            MyAPIGateway.Input.IsKeyPress(KeybindManager.PrimaryKey));

                if (combinationIsHeld)
                {
                    if (!_myGUIWindow.Visible)
                        OpenWindow();
                }
                else
                {
                    if (_myGUIWindow.Visible)
                        CloseWindow();
                }
            }
        }

        private void DoGetStatusValues()
        {
            if (_statComponent == null || _statComponent != _character.Components.Get<MyCharacterStatComponent>())
            {
                _statComponent = _character.Components.Get<MyCharacterStatComponent>();

                if (_statComponent == null)
                {
                    foreach (var key in _characterStats.Keys.ToList())
                        _characterStats[key] = null;
                    return;
                }

                foreach (var key in _characterStats.Keys.ToList())
                {
                    MyEntityStat foundStat;
                    if (_statComponent.TryGetStat(MyStringHash.GetOrCompute(key.ToString()), out foundStat) && foundStat != null)
                        _characterStats[key] = foundStat;
                    else
                        _characterStats[key] = null;
                }
            }
        }

        private void DoComputeMyConsumableEffects()
        {
            Debug.LogDebug("Starting to compute all consumable effects.");

            for (int i = _myCharacterEffects.Count - 1; i >= 0; i--)
            {
                var item = _myCharacterEffects[i];

                Debug.LogDebug($"Processing effect at index {i}. Item is null: {item == null}.");

                if (item == null)
                {
                    Debug.LogWarning($"Found and removed a null consumable effect at index {i}.");
                    _myCharacterEffects.RemoveAtFast(i);
                }
                else if (!item.DoEffect())
                {
                    Debug.LogInfo($"Consumable effect '{item.GetType().Name}' returned false and will be removed at index {i}.");
                    _myCharacterEffects.RemoveAtFast(i);
                }
                else
                {
                    Debug.LogDebug($"Consumable effect '{item.GetType().Name}' was successfully applied and will remain.");
                }
            }

            Debug.LogDebug("Finished computing all consumable effects.");
        }
        #endregion

        #region UI
        private void HudInit()
        {
            _visorOverlay = new Visor(HudMain.HighDpiRoot);
            _sleepEffect = new SleepOverlay(HudMain.HighDpiRoot);
            _myGUIWindow = new CharacterStatus(HudMain.HighDpiRoot);
            _warnings = new Warnings(HudMain.HighDpiRoot);

            _myGUIWindow.RequestClose += CloseWindow;
            _myGUIWindow.Visible = false; // so GUI is foreground
        }
        private void ClientReset()
        {
            _warnings?.Unregister();
            _visorOverlay?.Unregister();
            _sleepEffect?.Unregister();
            _myGUIWindow?.Unregister();
        }

        private void OpenWindow()
        {
            if (_myGUIWindow == null || _myGUIWindow.Visible) return;

            _myGUIWindow.Visible = true;
            HudMain.EnableCursor = true;
        }

        private void CloseWindow()
        {
            if (_myGUIWindow == null || !_myGUIWindow.Visible) return;

            _myGUIWindow.Visible = false;
            HudMain.EnableCursor = false;
        }


        // Controller integration
        #endregion

        #region ActionEvents
        private void OnCharacterDied(long playerId)
        {
            _characterStorage = null;
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


            if (Util.IsCryo(character.Parent) && !ConfigManager.Config.AllowEatingInCryo)
            {
                Debug.LogInfo($"Character {character.DisplayName} is in cryo. Cannot consume item. Returning item to inventory.");
                //AddItemToPlayerCharacter(character, id);
                //Server_SendNotificationToClient(player.SteamUserId, "NW_NO_Consumeables_in_Stasis");
                return;
            }

            if (Util.IsBed(character.Parent) && !ConfigManager.Config.AllowEatingInBed)
            {
                Debug.LogInfo($"Character {character.DisplayName} is in bed. Cannot consume item. Returning item to inventory.");
                //AddItemToPlayerCharacter(character, id);
                //Server_SendNotificationToClient(player.SteamUserId, "NW_NO_Consumeables_in_Bed");
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

                        if (applyEffects = !IsEffectPresent(currentEffect))
                        {
                            //CharacterStorage storage;
                            float val = stat.Value;

                            //if (_characterStorageCache.TryGetValue(character.EntityId, out storage))
                            if (_characterStorage != null)
                            {
                                val *= 1 / (_characterStorage.CountConsumable(id) + 1);
                                Debug.LogDebug($"Diminishing returns applied. New value: {val}.");
                            }
                            else
                            {
                                val = stat.Value;
                                Debug.LogDebug("Character storage not found. Using original value.");
                            }

                            newEffects.Add(new Effect(character, _player, currentEffect, val, stat.Time));
                            Debug.LogInfo($"New effect '{currentEffect}' with value '{val}' added to list.");
                        }
                        else
                        {
                            Debug.LogInfo($"Effect '{currentEffect}' is already active. Not applying.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Stat '{stat.Name}' is not a valid EffectType. Returning.");
                        return;
                    }
                }
            }
            else
            {
                Debug.LogInfo("Item has no stats defined.");
            }

            if (!helmUse && !ConfigManager.Config.IgnoreClosedHelmet)
            {
                Debug.LogDebug("Helm use is not allowed. Checking oxygen levels.");
                if (character.OxygenLevel > 0.5f || character.EnvironmentOxygenLevel > 0.5f)
                {
                    if (character.EnabledHelmet)
                    {
                        Debug.LogInfo("Helmet enabled. Toggling off for consumption.");
                        //character.SwitchHelmet();
                        //Server_SendNotificationToClient(player.SteamUserId, "NW_Helmet_Open_To_Eat");
                    }
                    else
                    {
                        Debug.LogDebug("Helmet is already off. Proceeding with consumption.");
                    }
                }
                else
                {
                    Debug.LogInfo("Oxygen level too low. Cannot consume item. Returning item to inventory.");
                    //AddItemToPlayerCharacter(character, id);
                    //Server_SendNotificationToClient(player.SteamUserId, "NW_Helmet_NOT_Open_To_Eat");
                    return;
                }
            }

            if (applyEffects)
            {
                Debug.LogInfo("All effects are ready to be applied.");
                FindSoundToPlay(id);

                foreach (var effect in newEffects)
                {
                    if (effect.IsValid)
                    {
                        _myCharacterEffects.Add(effect);
                        Debug.LogInfo($"Added valid effect '{effect.Type}' to player's active effects.");

                    }
                    else
                    {
                        Debug.LogWarning($"Effect with type '{effect.Type}' is invalid and will not be added.");
                    }
                }
                Debug.LogInfo("Successfully consumed item and applied effects.");
            }
            else
            {
                Debug.LogInfo("Effects cannot be applied. Returning item to inventory and notifying player.");
                //AddItemToPlayerCharacter(character, id);
                //Server_SendNotificationToClient(player.SteamUserId, "NW_Effect_already_active");
            }
        }

        private bool IsEffectPresent(EffectType type)
        {
            Debug.LogDebug($"Checking if effect '{type}' is already present on character '{_character.DisplayName}' ({_character.EntityId}).");

            foreach (Effect effect in _myCharacterEffects)
            {
                Debug.LogDebug($"-- Comparing with existing effect: '{effect.Type}' (Unique: {effect.IsUnique}) on character '{effect.Character?.DisplayName}' ({effect.Character?.EntityId}).");

                if (effect.Type == type && effect.IsUnique)
                {
                    Debug.LogInfo($"Unique effect '{type}' is already active on character '{_character.DisplayName}'. Returning true.");
                    return true;
                }
            }

            Debug.LogDebug($"Effect '{type}' is not present or not unique. Returning false.");
            return false;
        }
        #endregion

        #region Sound
        private void InitializeAudio()
        {
            // Erstelle das MySoundPair aus dem SubtypeId in deiner .sbc
            _soundEat = new MySoundPair("AdvancedArcPlayEat");
            _soundDrink = new MySoundPair("AdvancedArcPlayDrink");
            _soundPowerKit = new MySoundPair("AdvancedArcPlayUsePowerKit");
            _soundMedKit = new MySoundPair("AdvancedArcPlayUseMedKit");
            _soundRadiationLOW = new MySoundPair("AdvancedArcHudVocRadiation");
            _soundRadiationHIGH = new MySoundPair("ArcHudVocRadiationCritical");

            Debug.LogInfo("Audio system initialized.");
        }

        private void FindSoundToPlay(MyDefinitionId _itemName)
        {
            switch (_itemName.SubtypeName)
            {
                case ("Medkit"):
                    PlayHudSound(_soundMedKit, 0.5f);
                    break;

                case ("Powerkit"):
                    PlayHudSound(_soundPowerKit, 0.5f);
                    break;

                // drinks
                case ("CosmicCoffee"):
                case ("ClangCola"):
                case ("MycoBoost"):
                    PlayHudSound(_soundDrink, 0.5f);
                    break;

                // food is always default
                default:
                    PlayHudSound(_soundEat, 0.5f);
                    break;
            }
        }

        private void PlayHudSound(MySoundPair soundPair, float volume)
        {

            if (_soundEmitter == null)
            {
                _soundEmitter = new MyEntity3DSoundEmitter(null);

                // remove all effects and conditions from this emitter; must not clear the dictionary itself!
                _soundEmitter.EmitterMethods[(int)MyEntity3DSoundEmitter.MethodsEnum.CanHear].ClearImmediate();
                _soundEmitter.EmitterMethods[(int)MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].ClearImmediate();
                _soundEmitter.EmitterMethods[(int)MyEntity3DSoundEmitter.MethodsEnum.CueType].ClearImmediate();
                _soundEmitter.EmitterMethods[(int)MyEntity3DSoundEmitter.MethodsEnum.ImplicitEffect].ClearImmediate();
            }

            _soundEmitter.SetPosition(MyAPIGateway.Session.Camera.WorldMatrix.Translation);
            _soundEmitter.CustomVolume = volume;
            _soundEmitter.PlaySound(soundPair, stopPrevious: false, alwaysHearOnRealistic: true, force2D: true);
        }


        #endregion

        #region Networks

        private void Client_AskServerForStorage()
        {
            IMyCharacter myCharacter = MyAPIGateway.Session?.LocalHumanPlayer.Character;
            IMyPlayer myPlayer = MyAPIGateway.Session?.LocalHumanPlayer;
            if (myCharacter == null || myCharacter.Closed || myCharacter.MarkedForClose || myCharacter.IsDead || myCharacter.IsBot) return;

            CharacterStorage storage = new CharacterStorage();
            storage.SteamID = myPlayer.SteamUserId;
            storage.CharacterID = myCharacter.EntityId;

            try
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(storage);
                MyAPIGateway.Multiplayer.SendMessageToServer(Network.STORAGE_CHANNEL, data);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"[APEX.Advanced.Client] Error sending network message: {e.Message}");
            }
        }

        /// <summary>
        /// Receive string Messages from Server
        /// </summary>
        /// <param name="handlerId"></param>
        /// <param name="data"></param>
        /// <param name="steamId"></param>
        /// <param name="isFromServer"></param>
        public static void Client_ReceiveNotification(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {
            if (!isFromServer) return;
            try
            {
                string message = Encoding.UTF8.GetString(data);
                MyAPIGateway.Utilities.ShowNotification(Util.LOC(message), 2000, MyFontEnum.White);
            }
            catch (System.Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"[APEX.Advanced.Client] Error processing network message: {e.Message}");
            }
        }


        /// <summary>
        /// Receive StorageComponent (eaten consumables) from server
        /// </summary>
        /// <param name="handlerId"></param>
        /// <param name="data"></param>
        /// <param name="steamId"></param>
        /// <param name="isFromServer"></param>
        private void Client_ReceiveStorage(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {
            if (!isFromServer) return;
            try
            {
                var receivedStorage = MyAPIGateway.Utilities.SerializeFromBinary<CharacterStorage>(data);
                if (receivedStorage != null)
                {
                    _characterStorage = receivedStorage; // CHECK: works now. Does not save data or data was never transmitted
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"[APEX.Advanced.Client] Error processing network message: {e.Message}");
            }
        }

        // In der #region Networks, die neue Handler-Methode
        /// <summary>
        /// Receives a radiation event from the server to play a sound.
        /// </summary>
        private void Client_ReceiveRadiationEvent(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {
            if (!isFromServer || data == null || data.Length != 4) return;

            float incomingRadiation = BitConverter.ToSingle(data, 0);

            float volume = MathHelper.Clamp(incomingRadiation * 0.07f, 0f, 1f);

            // This check ensures that very low, effectively silent volumes don't trigger the sound at all.
            if (volume > 0.01f)
            {
                PlayHudSound(_soundRadiationLOW, volume);
                Debug.LogDebug($"Received radiation event. Playing Geiger sound with volume {volume}.");
            }
        }

        #endregion

        #region DEBUG / cli

        private void OnMessageEntered(string message, ref bool sendToOthers)
        {
            if (_player == null || _player.PromoteLevel < MyPromoteLevel.Admin)
                return;

            string command = message.ToLower();
            if (command.Equals("/apex.advanced help"))
            {
                sendToOthers = false;
                var sb = new StringBuilder();
                sb.AppendLine("-- Admin Commands --");
                sb.AppendLine("/apex.advanced help - Shows this command list.");
                sb.AppendLine("/apex.advanced sleep [value] - (Debug) Overrides the fatigue value.");
                sb.AppendLine("/apex.advanced sleep_reset - (Debug) Disables the override.");

                MyAPIGateway.Utilities.ShowMessage("APEX.Advanced", sb.ToString());
            }
            else if (command.StartsWith("/apex.advanced sleep "))
            {
                sendToOthers = false;
                string[] parts = message.Split(' ');
                if (parts.Length == 3)
                {
                    float value;
                    if (float.TryParse(parts[2], out value))
                    {
                        _debugSleepOverride = value;
                        MyAPIGateway.Utilities.ShowNotification($"Sleep effect override to test value: {value}", 2000, MyFontEnum.Blue);
                    }
                }
            }
            else if (command.Equals("/apex.advanced sleep_reset"))
            {
                sendToOthers = false;
                _debugSleepOverride = null;
                MyAPIGateway.Utilities.ShowNotification("Sleep effect reset. Genuine data is now being used.", 2000, MyFontEnum.Blue);
            }

            // Nothing to do? Send regular chat message.
        }
        #endregion
    }
}