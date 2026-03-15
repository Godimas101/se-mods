using System;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace APEX.Advanced.HUD.SurvivalControl
{
    public class MyPlayerRecovery : IMyHudStat
    {
        public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("player_recovery");
        public float MinValue => 0f;
        public float MaxValue => 1f;
        public float CurrentValue { get; private set; }
        public string GetValueString() => (CurrentValue * 100f).ToString("0");

        private static readonly MyStringHash RecoveryID = MyStringHash.GetOrCompute("Recovery");
        private MyEntityStat Recovery
        {
            get
            {
                MyEntityStat s;
                var _statComponent = MyAPIGateway.Session?.Player?.Character?.Components.Get<MyCharacterStatComponent>();
                return (_statComponent != null && _statComponent.TryGetStat(RecoveryID, out s)) ? s : null;
            }
        }

        public MyPlayerRecovery() { }
        public void Update()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            try
            {
                // Prevent HUD from reading if SurvivalControl is not 1
                if (ConfigManager.Config.SurvivalControl != 1)
                {
                    CurrentValue = MaxValue;
                    return;
                }
                MyEntityStat recovery = Recovery;
                if (recovery == null)
                    return;

                CurrentValue = recovery.Value / 100f;

            }
            catch (Exception e)
            {
                Debug.LogError($"MyPlayerRecovery unable to compute: {e}");
            }
        }
    }
}