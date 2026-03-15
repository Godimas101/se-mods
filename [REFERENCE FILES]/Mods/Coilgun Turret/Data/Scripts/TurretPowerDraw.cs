using System;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Elindis.GaussTurretPowerDraw

// This script detects a shot by a turret, and then
// causes it to draw power for the length of a
// countdown after its last shot. 

// The countdown is measured in hundreds of ticks,
// with each tick lasting 16.67 milliseconds.
// A length of 4 is equivalent to 6.668 seconds.

{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "ElindisGaussTurret")]

    public class Example_OreDetector : MyGameLogicComponent
    {
        const float POWER_REQUIRED_MW = 7.2f;
		const int COUNTDOWN_LENGTH = 4;

        private IMyFunctionalBlock Block;
        private IMyTerminalBlock Terminal;
        private long lastShotTime;
		private int countdown = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = (IMyFunctionalBlock)Entity;
            var gun = (IMyGunObject<MyGunBase>)Entity;
            lastShotTime = gun.GunBase.LastShootTime.Ticks;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            Terminal = (IMyTerminalBlock)Entity;
            if (Terminal.CubeGrid?.Physics == null)
                return; // ignore ghost grids
            Terminal.AppendingCustomInfo += AppendingCustomInfo;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            try
            {
                sb.Append("Type: Coilgun Turret").Append("\n").Append("Max Required Input: 7.20 MW").Append("\n");
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        public override void UpdateAfterSimulation100()
        {
            try // keep the performance cost low
            {
                var sink = Entity.Components.Get<MyResourceSinkComponent>();
                if (sink != null)
                {
                    sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, ComputePowerRequired);
                    sink.Update();
                }
                if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
                {
                    Terminal.RefreshCustomInfo();
                    Terminal.SetDetailedInfoDirty();
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        void LogError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR on {GetType().FullName}: {e}");

            if (MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ERROR on {GetType().FullName}: Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
        }

        private float ComputePowerRequired()
        {
            
            if (!Block.Enabled || !Block.IsFunctional)
                return 0f;

            var gun = (IMyGunObject<MyGunBase>)Entity;
            var shotTime = gun.GunBase.LastShootTime.Ticks;

            if (shotTime > lastShotTime)
            {
				// MyAPIGateway.Utilities.ShowNotification($"Shot detected", 1000);
                lastShotTime = shotTime;
                countdown = COUNTDOWN_LENGTH;
            }
			
            if (countdown > 0)
            {
				// MyAPIGateway.Utilities.ShowNotification($"{countdown}", 1000);
				countdown--;
                return POWER_REQUIRED_MW;
            }

			// Required so the turret won't operate without power
            return 0.002f;

        }

    }

}