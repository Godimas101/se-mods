using System;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace APEX.Advanced.HUD.SurvivalControl
{
    public class MyPlayerBloating : IMyHudStat
    {
        public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("player_bloating");
        public float MinValue => 0f;
        public float MaxValue => 1f;
        public float CurrentValue { get; private set; }
        public string GetValueString() => (CurrentValue * 100f).ToString("0");

        private static readonly MyStringHash BloatingID = MyStringHash.GetOrCompute("Bloating");
        private MyEntityStat Bloating
        {
            get
            {
                MyEntityStat s;
                var _statComponent = MyAPIGateway.Session?.Player?.Character?.Components.Get<MyCharacterStatComponent>();
                return (_statComponent != null && _statComponent.TryGetStat(BloatingID, out s)) ? s : null;
            }
        }

        public MyPlayerBloating() { }
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
                MyEntityStat bloating = Bloating;
                if (bloating == null)
                    return;

                CurrentValue = bloating.Value / 100f;

            }
            catch (Exception e)
            {
                Debug.LogError($"MyPlayerBloating unable to compute: {e}");
            }
        }
    }
}