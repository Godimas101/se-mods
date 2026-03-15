using System;
using APEX.Advanced.Server;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;


namespace APEX.Advanced.Client.MyAdvancedStat
{

    public class AdvancedStats
    {
        #region Statische Konfigurationswerte
        // Diese Felder sind jetzt wieder 'static', werden aber nicht sofort initialisiert.
        private static float myMultiplier;

        // FOOD (fix no food in cryo when player is online)            1 - 0.5 - 0.25
        private static float FOOD_DECAY_PER_100_TICKS;
        // WATER
        private static float WATER_DECAY_PER_100_TICKS;

        // SLEEP
        // 100 to 0 in 60 min ingame requice                     
        private static float SLEEP_DECAY_PER_100_TICKS;
        // ~ 10 min (BASE value)
        private static float SLEEP_REGEN_RATIO_CHAIR;
        // ~ 20 min
        private static float SLEEP_REGEN_RATIO_CRYO;
        // ~  5 min
        private static float SLEEP_REGEN_RATIO_BED;
        // ~  2,5 min
        private static float SLEEP_REGEN_RATIO_BED_HELMOFF;

        // Bloating
        private static float BLOATING_REGEN;

        // Recovery
        private static float RECOVERY_DECAY;
        private static float RECOVERY_TO_HEALTH_CONVERSION_RATE; // 2%
        private static float RECOVERY_MODIFIER_BED;    // 100% Effektivität
        private static float RECOVERY_MODIFIER_CHAIR;  // 50% Effektivität
        private static float RECOVERY_MODIFIER_CRYO;  // 15% Effektivität
        private static bool _isConfigLoaded = false;

        // Session statics
        private static bool _isDedicated;
        private static bool _isCreativeMode;
        private static float _autoHealingAdd;

        /// <summary>
        /// Diese Methode wird vom ConfigManager aufgerufen, sobald die Konfiguration geladen ist.
        /// </summary>
        public static void ReloadConfigValues()
        {
            if (ConfigManager.Config == null)
            {
                _isConfigLoaded = false;
                return;
            }

            myMultiplier = MyAPIGateway.Session.SessionSettings.FoodConsumptionRate;
            FOOD_DECAY_PER_100_TICKS = 0.062f * myMultiplier;
            WATER_DECAY_PER_100_TICKS = ConfigManager.Config.WaterDecay * myMultiplier;
            SLEEP_DECAY_PER_100_TICKS = ConfigManager.Config.SleepDecay * myMultiplier;
            SLEEP_REGEN_RATIO_CHAIR = ConfigManager.Config.SleepRegenerationChair;
            SLEEP_REGEN_RATIO_CRYO = SLEEP_REGEN_RATIO_CHAIR * ConfigManager.Config.SleepRegenerationFactorCryo;
            SLEEP_REGEN_RATIO_BED = SLEEP_REGEN_RATIO_CHAIR * ConfigManager.Config.SleepRegenerationFactorBed;
            SLEEP_REGEN_RATIO_BED_HELMOFF = SLEEP_REGEN_RATIO_CHAIR * ConfigManager.Config.SleepRegenerationBedHelmOffFactor;
            BLOATING_REGEN = ConfigManager.Config.BloatingRegeneration;
            RECOVERY_DECAY = ConfigManager.Config.RecoveryDecay;
            RECOVERY_TO_HEALTH_CONVERSION_RATE = ConfigManager.Config.RecoveryToHealthConversionRate;
            RECOVERY_MODIFIER_BED = ConfigManager.Config.RecoveryModifierBed;
            RECOVERY_MODIFIER_CHAIR = ConfigManager.Config.RecoveryModifierChair;
            RECOVERY_MODIFIER_CRYO = ConfigManager.Config.RecoveryModifierCryo;

            _isConfigLoaded = true;
            _isDedicated = MyAPIGateway.Utilities.IsDedicated;
            _isCreativeMode = MyAPIGateway.Session.CreativeMode;
            _autoHealingAdd = MyAPIGateway.Session.SessionSettings.AutoHealing ? 1f : 0f;
            Debug.LogInfo("AdvancedStats static config values have been loaded/reloaded.");
        }
        #endregion

        #region Instance vars
        public IMyCharacter Character;
        private IMyPlayer _player = null;
        private bool _noParent = false;
        private bool _isChair = false;
        private bool _isBed = false;
        private bool _isCryo = false;
        private float _recoveryModifier = 0f;
        private MyCharacterStatComponent _statComponent;
        private int _asleep = -1;
        private bool _fellasleep = false;
        private bool _vomit = false;
        #endregion

        #region Statische IDs
        private static readonly MyStringHash HealthID = MyStringHash.GetOrCompute("Health");
        private static readonly MyStringHash FoodID = MyStringHash.GetOrCompute("Food");
        private static readonly MyStringHash WaterID = MyStringHash.GetOrCompute("Water");
        private static readonly MyStringHash SleepID = MyStringHash.GetOrCompute("Sleep");
        private static readonly MyStringHash RecoveryID = MyStringHash.GetOrCompute("Recovery");
        private static readonly MyStringHash FatigueID = MyStringHash.GetOrCompute("Fatigue");
        private static readonly MyStringHash BloatingID = MyStringHash.GetOrCompute("Bloating");
        #endregion

        #region Stats
        private MyEntityStat Health
        {
            get
            {
                MyEntityStat s;
                // Returns the stat object 's' if found, otherwise returns null.
                return (_statComponent != null && _statComponent.TryGetStat(HealthID, out s)) ? s : null;
            }
        }
        private MyEntityStat Food
        {
            get
            {
                MyEntityStat s;
                return (_statComponent != null && _statComponent.TryGetStat(FoodID, out s)) ? s : null;
            }
        }
        private MyEntityStat Water
        {
            get
            {
                MyEntityStat s;
                return (_statComponent != null && _statComponent.TryGetStat(WaterID, out s)) ? s : null;
            }
        }
        private MyEntityStat Sleep
        {
            get
            {
                MyEntityStat s;
                return (_statComponent != null && _statComponent.TryGetStat(SleepID, out s)) ? s : null;
            }
        }
        private MyEntityStat Recovery
        {
            get
            {
                MyEntityStat s;
                return (_statComponent != null && _statComponent.TryGetStat(RecoveryID, out s)) ? s : null;
            }
        }
        private MyEntityStat Fatigue
        {
            get
            {
                MyEntityStat s;
                return (_statComponent != null && _statComponent.TryGetStat(FatigueID, out s)) ? s : null;
            }
        }
        private MyEntityStat Bloating
        {
            get
            {
                MyEntityStat s;
                return (_statComponent != null && _statComponent.TryGetStat(BloatingID, out s)) ? s : null;
            }
        }
        #endregion


        public AdvancedStats(IMyCharacter character)
        {
            Character = character;
            _statComponent = character.Components.Get<MyCharacterStatComponent>();
        }


        public void Update100()
        {
            // Sicherheitsabfragen. Prüfe auch, ob die Konfiguration geladen wurde.
            if (!_isConfigLoaded || Character.IsDead || _statComponent == null || myMultiplier == 0 || _isCreativeMode)
                return;

            MyEntityStat health = Health;
            MyEntityStat food = Food;
            MyEntityStat water = Water;
            MyEntityStat sleep = Sleep;
            MyEntityStat recovery = Recovery;
            MyEntityStat fatigue = Fatigue;
            MyEntityStat bloating = Bloating;

            // It's safer to check if stats are null before using them
            if (health == null || food == null || water == null || sleep == null || recovery == null || fatigue == null || bloating == null)
                return;

            // preChecks false? return - no player there
            if (!CharacterPreChecks())
                return;            

            // ###
            // Regular decays at the end of this mehtod
            // ###

            if (_isBed && ConfigManager.Config.SkipRegenerationInBed && !ConfigManager.Config.FullDecayInBed && !ConfigManager.Config.FullDecayInCryoOrBed)
            {
                Debug.LogDebug("No regeneration in Bed, so skip also regeneration.");
                return;
            }

            if (_isCryo && ConfigManager.Config.SkipRegenerationInCryo && !ConfigManager.Config.FullDecayInCryo && !ConfigManager.Config.FullDecayInCryoOrBed)
            {
                Debug.LogDebug("No regeneration in Cryo, so skip also regeneration.");
                return;
            }

            // ### Debuffs ### (stats affecting others)
            // Fatigue (if above 70 sleep, decays faster)
            float sleep_decay = CalculateSleepDecayWithFatigue(fatigue.Value);
            // Water
            DebuffWater(water.Value);

            // Extra bloating regen in bed
            if (_isBed)
                bloating.Decrease(BLOATING_REGEN * 0.15f, null);

            //Bloating - Vomit
            if (bloating.Value > 92f && !_vomit)
            {
                float vomitChance = (bloating.Value - 92f) * (100f / 8f);
                vomitChance = MathHelper.Clamp(vomitChance, 0f, 100f);

                if (MyRandom.Instance.Next(0, 100) < vomitChance)
                {
                    Debug.LogInfo($"Character '{Character.DisplayName}' vomits! (Chance: {vomitChance:F1}%)");

                    MyAPIGateway.Utilities.ShowNotification(Util.LOC("UI_Bloating_Vomit_Message"), 10000, MyFontEnum.Red);
                    // Instant effect to prevent multiple vomits
                    _vomit = true;

                    // random 10 to 20 damage
                    Character.DoDamage(MyRandom.Instance.NextFloat() * (25f - 10f) + 10f, MyStringHash.GetOrCompute("Hunger"), true);

                    // 1. Food-Debuff: set to 5%
                    if (food.Value > 5f)
                        food.Value = 5f;

                    // 2. Water-Debuff: set to 5%
                    if (water.Value > 5f)
                        water.Value = 5f;

                    // 3. Bloating-Debuff: -10% ~30 sec to prevent any other effects
                    var bloatingEffect = new MyObjectBuilder_EntityStatRegenEffect
                    {
                        Duration = 30f,
                        Interval = 1f,
                        TickAmount = -0.00334f
                    };
                    bloating.AddEffect(bloatingEffect);
                }
            }

            if (!bloating.HasAnyEffect() && _vomit)
                _vomit = false;

            // Recovery
            if (_recoveryModifier > 0f)
            {
                float recoveryToConvert = recovery.Value * RECOVERY_TO_HEALTH_CONVERSION_RATE;
                recoveryToConvert = Math.Min(recovery.Value, recoveryToConvert);
                float healthToRestore = recoveryToConvert * _recoveryModifier;

                recovery.Decrease(recoveryToConvert, null);
                health.Increase(healthToRestore, null);
            }

            // Sleep
            if (_noParent)
                sleep.Decrease(sleep_decay, null);
            else if (_isBed)
            {
                if (Character.EnabledHelmet)
                {
                    // helm closed
                    sleep.Increase(SLEEP_REGEN_RATIO_BED, null);
                    if (sleep.Value > 85)
                        fatigue.Decrease(SLEEP_REGEN_RATIO_BED / 2, null);
                }
                else
                {
                    // helm open
                    sleep.Increase(SLEEP_REGEN_RATIO_BED_HELMOFF, null);
                    if (sleep.Value > 75)
                        fatigue.Decrease(SLEEP_REGEN_RATIO_BED, null);
                }
            }
            else if (_isCryo)
            {
                sleep.Increase(SLEEP_REGEN_RATIO_CRYO, null);

                if (sleep.Value > 99)
                    fatigue.Decrease(SLEEP_REGEN_RATIO_CRYO / 2f, null);
            }
            else if (_isChair)
            {
                sleep.Increase(SLEEP_REGEN_RATIO_CHAIR, null);
            }
            else
            {
                // anything else
                sleep.Decrease(sleep_decay, null);
            }

            // microsleep (sleep low and not resting -> prevent fall asleep in bed etc.)
            if (sleep.Value < 15f && _fellasleep)
            {
                if (sleep.Value <= 0)
                {
                    // possible wakeup depends on asleep ticks
                    float wakeUpChance = Math.Min(_asleep++ * 0.5f, 95f);

                    if (MyRandom.Instance.Next(0, 100) < wakeUpChance)
                    {
                        // Character wake up!
                        float sleepToRestore = MathHelper.Lerp(10f, 30f, MathHelper.Clamp(_asleep / 36f, 0f, 1f));
                        sleep.Value = sleepToRestore;
                        fatigue.Value -= 10f;

                        _asleep = -1;
                    }
                    else
                    {
                        // Print sleep on screen
                        if (MyAPIGateway.Session.LocalHumanPlayer != null)
                            MyAPIGateway.Utilities.ShowNotification(Util.LOC("UI_Sleep_Message"), 1600, MyFontEnum.Red);

                        Debug.LogDebug($"No wakeup. (Chance: {wakeUpChance}%)");
                    }

                }
                else
                {
                    // microsleep core
                    float passOutChance = 0f + (sleep.Value - 15f) * (50f - 0f) / (0f - 15f);
                    passOutChance = MathHelper.Clamp(passOutChance, 0f, 50f);
                    int randomRoll = MyRandom.Instance.Next(0, 100);

                    if (randomRoll < passOutChance)
                    {
                        sleep.Value = 0f;
                        _asleep = 0;
                    }

                }
            }
            else if (!_fellasleep)
            {
                // reset mechanic
                _asleep = -1;
            }

            // ### Decay ### (like sleep decay, natural regeneration)
            // Will not trigger if player is offline!
            if (!_isBed && !_isCryo)
            {               
                Debug.LogDebug("Applying standard decay (player is active).");
                //MyLog.Default.WriteLineAndConsole($"[APEX.Advanced!] Regular decay for {Character.Name}, players online {MyAPIGateway.Multiplayer.Players.Count}");
                //food.Decrease(...); DONE BY VANILLA SYSTEM!
                water.Decrease(WATER_DECAY_PER_100_TICKS, null);
                recovery.Decrease(RECOVERY_DECAY, null);
                bloating.Decrease(BLOATING_REGEN, null);
            }
            // Special behavior for a player in a bed or cryo.
            else
            {
                // First, check the master switch. If this is false, no decay happens in bed/cryo.
                if (ConfigManager.Config.FullDecayInCryoOrBed)
                {
                    // Sub-case 2a: Player is in a bed AND bed-decay is specifically enabled.
                    if (_isBed && ConfigManager.Config.FullDecayInBed)
                    {
                        Debug.LogDebug("Applying decay in Bed as per configuration.");
                        //MyLog.Default.WriteLineAndConsole($"[APEX.Advanced!] --BED -- decay for {Character.Name}, players online {MyAPIGateway.Multiplayer.Players.Count}");
                        food.Decrease(FOOD_DECAY_PER_100_TICKS, null);
                        water.Decrease(WATER_DECAY_PER_100_TICKS, null);
                        recovery.Decrease(RECOVERY_DECAY, null);
                        bloating.Decrease(BLOATING_REGEN, null);
                    }
                    // Sub-case 2b: Player is in a cryo chamber AND cryo-decay is specifically enabled.
                    else if (_isCryo && ConfigManager.Config.FullDecayInCryo)
                    {
                        Debug.LogDebug("Applying decay in Cryo Chamber as per configuration.");
                        //MyLog.Default.WriteLineAndConsole($"[APEX.Advanced!] --CRYO-- decay for {Character.Name}, players online {MyAPIGateway.Multiplayer.Players.Count}");
                        food.Decrease(FOOD_DECAY_PER_100_TICKS, null);
                        water.Decrease(WATER_DECAY_PER_100_TICKS, null);
                        recovery.Decrease(RECOVERY_DECAY, null);
                        bloating.Decrease(BLOATING_REGEN, null);
                    }
                }
            }
        }

        /// <summary>
        /// If water is below 15, character needs more oxygen
        /// If water is 0, character gets slowly damage
        /// </summary>
        /// <param name="value"></param>
        private void DebuffWater(float value)
        {
            // Debuff Water (character needs more oxygen)
            if (value < 15f)
            {
                if (_player != null && !_player.Character.Closed && !_player.Character.MarkedForClose)
                {
                    float _currentOxygenLevel = MyVisualScriptLogicProvider.GetPlayersOxygenLevel(_player.IdentityId);
                    float _nextOxygenLevel = (_currentOxygenLevel - 0.00463f);
                    MyVisualScriptLogicProvider.SetPlayersOxygenLevel(_player.IdentityId, _nextOxygenLevel < 0 ? 0 : _nextOxygenLevel);
                }
                
                if (value == 0f)
                    Character.DoDamage(3f + _autoHealingAdd, MyStringHash.GetOrCompute("Hunger"), true);
            }
        }


        /// <summary>
        /// PreCheck if character has a counting parent or not
        /// set class private variables for full access
        /// </summary>
        private bool CharacterPreChecks()
        {
            if (_player == null)
                if (_isDedicated)
                    _player = APEX_Advanced_Server.Instance.GetPlayerFromCharacterID(Character.EntityId);
                else
                    _player = MyAPIGateway.Session?.LocalHumanPlayer;

            if (_player == null)
                return false;

            if (Character.Parent == null)
            {
                _isChair = false;
                _isCryo = false;
                _isBed = false;
                _noParent = true;
                _recoveryModifier = 0f;
                _fellasleep = true;
                return true;
            }

            if (Util.IsBed(Character.Parent))
            {
                _isChair = false;
                _isCryo = false;
                _isBed = true;
                _noParent = false;
                _recoveryModifier = RECOVERY_MODIFIER_BED;
                _fellasleep = false;
                return true;
            }

            if (Util.IsCryo(Character.Parent))
            {
                _isChair = false;
                _isCryo = true;
                _isBed = false;
                _noParent = false;
                _recoveryModifier = RECOVERY_MODIFIER_CRYO;
                _fellasleep = false;
                return true;
            }

            if (Util.IsChair(Character.Parent))
            {
                _isChair = true;
                _isCryo = false;
                _isBed = false;
                _noParent = false;
                _recoveryModifier = RECOVERY_MODIFIER_CHAIR;
                _fellasleep = false;
                return true;
            }

            return true;
        }

        /// <summary>
        /// In order to introduce fatigue, sleep decays faster when fatigue is high
        /// </summary>
        /// <param name="value"></param>
        private float CalculateSleepDecayWithFatigue(float value)
        {
            // No sleep debuff below 75f
            if (value < 70f)
                return SLEEP_DECAY_PER_100_TICKS;

            // fatigue debuff, sleep decays faster
            return SLEEP_DECAY_PER_100_TICKS * (1.0f + (value - 70f) * (7.0f - 1.0f) / (100f - 70f));
        }
    }
}