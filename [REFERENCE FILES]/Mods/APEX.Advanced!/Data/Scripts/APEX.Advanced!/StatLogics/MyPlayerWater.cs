using System;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace APEX.Advanced.HUD.SurvivalControl
{
    public class MyPlayerWater : IMyHudStat
    {
        public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("player_water");
        public float MinValue => 0f;
        public float MaxValue => 1f;
        public float CurrentValue { get; private set; }
        public string GetValueString() => (CurrentValue * 100f).ToString("0");

        private static readonly MyStringHash WaterID = MyStringHash.GetOrCompute("Water");
        private MyEntityStat Water
        {
            get
            {
                MyEntityStat s;
                var _statComponent = MyAPIGateway.Session?.Player?.Character?.Components.Get<MyCharacterStatComponent>();
                return (_statComponent != null && _statComponent.TryGetStat(WaterID, out s)) ? s : null;
            }
        }

        public MyPlayerWater() { }
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
                MyEntityStat water = Water;
                if (water == null)
                    return;

                CurrentValue = water.Value / 100f;

            }
            catch (Exception e)
            {
                Debug.LogError($"MyPlayerWater unable to compute: {e}");
            }
        }
    }
}