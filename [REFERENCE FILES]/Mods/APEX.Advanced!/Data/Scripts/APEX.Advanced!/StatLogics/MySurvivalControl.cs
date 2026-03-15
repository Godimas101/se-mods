using System;
using VRage.ModAPI;
using VRage.Utils;

namespace APEX.Advanced.HUD.SurvivalControl
{
    public class MySurvivalControl : IMyHudStat
    {
        //public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("player_enable_health_icon");
        public MyStringHash Id { get; private set; } = MyStringHash.GetOrCompute("survival_control");
        public float MinValue => 0f;
        public float MaxValue => 1f;
        public float CurrentValue { get; private set; }
        public string GetValueString() => CurrentValue.ToString("0.00");

        public MySurvivalControl() { }

        public void Update()
        {
            try
            {
                CurrentValue = ConfigManager.Config.SurvivalControl;
            }
            catch (Exception e)
            {
                Debug.LogError($"SurvivalControl unable to compute: {e}");
            }
        }
    }
}