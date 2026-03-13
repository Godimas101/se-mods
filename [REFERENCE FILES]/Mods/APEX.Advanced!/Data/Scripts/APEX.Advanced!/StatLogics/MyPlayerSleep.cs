using System;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace APEX.Advanced.HUD.SurvivalControl
{
    public class MyPlayerSleep : IMyHudStat
    {
        public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("player_sleep");
        public float MinValue => 0f;
        public float MaxValue => 1f;
        public float CurrentValue { get; private set; }
        public string GetValueString() => (CurrentValue * 100f).ToString("0");

        private static readonly MyStringHash SleepID = MyStringHash.GetOrCompute("Sleep");
        private MyEntityStat Sleep
        {
            get
            {
                MyEntityStat s;
                var _statComponent = MyAPIGateway.Session?.Player?.Character?.Components.Get<MyCharacterStatComponent>();
                return (_statComponent != null && _statComponent.TryGetStat(SleepID, out s)) ? s : null;
            }
        }

        public MyPlayerSleep() { }
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
                MyEntityStat sleep = Sleep;
                if (sleep == null)
                    return;

                CurrentValue = sleep.Value / 100f;

            }
            catch (Exception e)
            {
                Debug.LogError($"MyPlayerSleep unable to compute: {e}");
            }
        }
    }
}