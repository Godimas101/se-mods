using System;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace APEX.Advanced.HUD.SurvivalControl
{
    public class MyPlayerFatigue : IMyHudStat
    {
        public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("player_fatigue");
        public float MinValue => 0f;
        public float MaxValue => 1.5f;
        public float CurrentValue { get; private set; }
        public string GetValueString() => (CurrentValue * 100f).ToString("0");

        private static readonly MyStringHash FatigueID = MyStringHash.GetOrCompute("Fatigue");
        private MyEntityStat Fatigue
        {
            get
            {
                MyEntityStat s;
                var _statComponent = MyAPIGateway.Session?.Player?.Character?.Components.Get<MyCharacterStatComponent>();
                return (_statComponent != null && _statComponent.TryGetStat(FatigueID, out s)) ? s : null;
            }
        }

        public MyPlayerFatigue() { }
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
                MyEntityStat fatigue = Fatigue;
                if (fatigue == null)
                    return;

                CurrentValue = fatigue.Value / 100f;

            }
            catch (Exception e)
            {
                Debug.LogError($"MyPlayerFatigue unable to compute: {e}");
            }
        }
    }
}