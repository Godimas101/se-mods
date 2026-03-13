using System;
using ProtoBuf;

namespace APEX.Advanced
{
    // This class holds all server-side configurations.
    // It is marked as Serializable to be easily converted to XML or binary formats.
    [ProtoContract]
    [Serializable]
    public class ServerConfig
    {
        [ProtoMember(1)] public int MinutesToTrackConsumables { get; set; }
        [ProtoIgnore] public bool FullDecayInCryoOrBed { get; set; }
        [ProtoIgnore] public bool FullDecayInCryo { get; set; }
        [ProtoIgnore] public bool FullDecayInBed { get; set; }
        [ProtoIgnore] public bool SkipRegenerationInCryo { get; set; }
        [ProtoIgnore] public bool SkipRegenerationInBed { get; set; }
        [ProtoMember(2)] public bool EnableVignetteFirstPerson { get; set; }

        #region Water
        [ProtoIgnore] public float WaterDecay { get; set; }
        #endregion

        #region Sleep
        [ProtoMember(3)] public bool EnableSleepEffect { get; set; }
        [ProtoIgnore] public float SleepDecay { get; set; }
        [ProtoIgnore] public float SleepRegenerationChair { get; set; }
        [ProtoIgnore] public float SleepRegenerationFactorCryo { get; set; }
        [ProtoIgnore] public float SleepRegenerationFactorBed { get; set; }
        [ProtoIgnore] public float SleepRegenerationBedHelmOffFactor { get; set; }
        #endregion

        #region Bloating
        [ProtoIgnore] public float BloatingRegeneration { get; set; }
        #endregion

        #region Recovery
        [ProtoIgnore] public float RecoveryDecay { get; set; }
        [ProtoIgnore] public float RecoveryToHealthConversionRate { get; set; }
        [ProtoIgnore] public float RecoveryModifierBed { get; set; }
        [ProtoIgnore] public float RecoveryModifierChair { get; set; }
        [ProtoIgnore] public float RecoveryModifierCryo { get; set; }
        #endregion

        #region Medical Room settings
        [ProtoMember(4)] public bool MedicalRoomWorksWithoutOxygenToHeal { get; set; }
        [ProtoMember(5)] public float MedicalRoomRegenerationPer100Ticks { get; set; }
        [ProtoMember(6)] public float MedicalRoomCanHealUpToPercent { get; set; }
        #endregion

        #region Radiation
        [ProtoIgnore] public bool EnableRadiationAdvanced { get; set; }
        [ProtoIgnore] public float RadiationGeneralDivisor { get; set; }
        [ProtoIgnore] public float RadiationMinimumDistance { get; set; }
        [ProtoIgnore] public int RadiationUraniumSearchRadius { get; set; }
        [ProtoIgnore] public float RadiationUraniumIngotThreshold { get; set; }
        [ProtoIgnore] public float RadiationContainerShielding { get; set; }
        [ProtoIgnore] public float RadiationReactorShielding { get; set; }
        [ProtoIgnore] public float RadiationSuitGuardFactor { get; set; }
        [ProtoIgnore] public float RadiationNegligibleDose { get; set; }
        #endregion

        #region Eat settings
        [ProtoMember(7)] public bool AllowEatingInCryo { get; set; }

        [ProtoMember(8)] public bool AllowEatingInBed { get; set; }

        [ProtoMember(9)] public bool IgnoreClosedHelmet { get; set; }
        #endregion

        #region UI
        [ProtoMember(10)] public float SurvivalControl { get; set; }
        #endregion

        /// <summary>
        /// This is used when no config file is found on the server.
        /// </summary>
        public ServerConfig()
        {
            MinutesToTrackConsumables = 180;
            FullDecayInCryoOrBed = true;
            FullDecayInCryo = true;
            FullDecayInBed = true;
            SkipRegenerationInCryo = false;
            SkipRegenerationInCryo = false;

            EnableVignetteFirstPerson = true;

            WaterDecay = 0.0837f;

            EnableSleepEffect = true;
            SleepDecay = 0.0463f;
            SleepRegenerationChair = 0.277f;
            SleepRegenerationFactorCryo = 0.5f;
            SleepRegenerationFactorBed = 2f;
            SleepRegenerationBedHelmOffFactor = 4f;

            BloatingRegeneration = 0.031f;

            RecoveryDecay = 0.01155f;
            RecoveryToHealthConversionRate = 0.02f;
            RecoveryModifierBed = 1.0f;
            RecoveryModifierChair = 0.5f;
            RecoveryModifierCryo = 0.15f;

            MedicalRoomWorksWithoutOxygenToHeal = false;
            MedicalRoomRegenerationPer100Ticks = 5f;
            MedicalRoomCanHealUpToPercent = 0.75f;

            EnableRadiationAdvanced = false;
            RadiationGeneralDivisor = 16f;
            RadiationMinimumDistance = 3f;
            RadiationUraniumSearchRadius = 200;
            RadiationUraniumIngotThreshold = 2f;
            RadiationContainerShielding = 0.75f;
            RadiationReactorShielding = 0.25f;
            RadiationSuitGuardFactor = 30f;
            RadiationNegligibleDose = 2f;

            AllowEatingInCryo = false;
            AllowEatingInBed = false;
            IgnoreClosedHelmet = false;

            SurvivalControl = 1f;
        }
    }


    [Serializable]
    public class ClientConfig
    {
        public int UserInterfaceWarningIconSquareLength { get; set; }
        public string OpenMenuKeybind { get; set; }
        public bool HoldKeyToOpenGUI { get; set; }

        /// <summary>
        /// This is used when no config file is found on the client's machine.
        /// </summary>
        public ClientConfig()
        {
            UserInterfaceWarningIconSquareLength = 64;
            OpenMenuKeybind = "N";
            HoldKeyToOpenGUI = true;
        }
    }



}