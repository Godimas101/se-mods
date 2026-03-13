using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace LCDInfoApps
{
    [MyTextSurfaceScript("GroundVehicleEfficiency", "[!] Vehicle Efficiency")]
    public class GroundVehicleEfficiency : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        private readonly IMyTerminalBlock TerminalBlock;
        private IMyCubeGrid Grid;
        
        private Config Cfg = new Config();
        private bool ConfigLoaded = false;
        private int ConfigCheckCounter = 0;

        private readonly Color ColorGood = new Color(0, 255, 100);
        private readonly Color ColorWarning = new Color(255, 200, 0);
        private readonly Color ColorDanger = new Color(255, 50, 50);

        public GroundVehicleEfficiency(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;
            Grid = TerminalBlock.CubeGrid;
            LoadConfig();
        }

        public override void Dispose()
        {
            base.Dispose();
            TerminalBlock.OnMarkForClose -= BlockMarkedForClose;
        }

        void BlockMarkedForClose(IMyEntity ent)
        {
            Dispose();
        }

        public override void Run()
        {
            try
            {
                base.Run();
                ConfigCheckCounter++;
                
                if (ConfigCheckCounter >= 30)
                {
                    ConfigCheckCounter = 0;
                    LoadConfig();
                }
                
                Draw();
            }
            catch (Exception e)
            {
                DrawError(e);
            }
        }

        class Config
        {
            public float TextSize = 0.55f;
            public float ViewPortOffsetX = 8f;
            public float ViewPortOffsetY = 8f;
            public bool UseColors = true;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[VehicleEfficiency]"))
            {
                if (!ConfigLoaded)
                {
                    WriteDefaultConfig();
                    ConfigLoaded = true;
                }
                return;
            }

            ConfigLoaded = true;
            string[] lines = customData.Split('\n');
            bool inSection = false;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed == "[VehicleEfficiency]")
                {
                    inSection = true;
                    continue;
                }
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inSection = false;
                    continue;
                }
                if (!inSection || string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                    continue;

                int equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex < 0) continue;

                string key = trimmed.Substring(0, equalsIndex).Trim();
                string valueStr = trimmed.Substring(equalsIndex + 1).Split(new[] { "//" }, StringSplitOptions.None)[0].Trim();

                try
                {
                    switch (key)
                    {
                        case "TextSize": float.TryParse(valueStr, out Cfg.TextSize); break;
                        case "ViewPortOffsetX": float.TryParse(valueStr, out Cfg.ViewPortOffsetX); break;
                        case "ViewPortOffsetY": float.TryParse(valueStr, out Cfg.ViewPortOffsetY); break;
                        case "UseColors": bool.TryParse(valueStr, out Cfg.UseColors); break;
                    }
                }
                catch { }
            }
        }

        void WriteDefaultConfig()
        {
            StringBuilder sb = new StringBuilder();
            string existing = TerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[VehicleEfficiency]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[VehicleEfficiency]");
            sb.AppendLine("TextSize=0.55");
            sb.AppendLine("ViewPortOffsetX=8");
            sb.AppendLine("ViewPortOffsetY=8");
            sb.AppendLine("UseColors=true");

            TerminalBlock.CustomData = sb.ToString();
        }

        void Draw()
        {
            // Get controller
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
            IMyShipController controller = null;
            
            if (gts != null)
            {
                List<IMyShipController> controllers = new List<IMyShipController>();
                gts.GetBlocksOfType(controllers, c => c.CubeGrid == Grid && c.IsFunctional);
                if (controllers.Count > 0)
                    controller = controllers[0];
            }

            if (controller == null)
            {
                DrawMessage("NO CONTROLLER");
                return;
            }

            // Get all grids connected to this grid
            List<IMyCubeGrid> allGrids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Mechanical, allGrids);

            // Get speed for determining parked status
            Vector3D velocity = controller.GetShipVelocities().LinearVelocity;
            float speed = (float)velocity.Length();
            
            // Battery stats
            float batteryStored = 0f;
            float batteryMax = 0f;
            float batteryInput = 0f;
            float batteryOutput = 0f;
            
            if (gts != null)
            {
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                gts.GetBlocksOfType(batteries);
                
                foreach (var battery in batteries)
                {
                    if (allGrids.Contains(battery.CubeGrid) && battery.IsFunctional)
                    {
                        batteryStored += battery.CurrentStoredPower;
                        batteryMax += battery.MaxStoredPower;
                        batteryInput += battery.CurrentInput;
                        batteryOutput += battery.CurrentOutput;
                    }
                }
            }
            
            float batteryPercent = batteryMax > 0 ? (batteryStored / batteryMax) * 100f : 0f;
            float batteryNetPower = batteryInput - batteryOutput; // Positive = charging, negative = discharging
            
            // Calculate battery time remaining
            float batteryHours = 0f;
            if (batteryNetPower < -0.001f) // Discharging
            {
                // Time = stored / discharge rate
                batteryHours = batteryStored / Math.Abs(batteryNetPower);
            }
            else if (batteryNetPower > 0.001f) // Charging
            {
                // Time to full = remaining / charge rate
                float remaining = batteryMax - batteryStored;
                batteryHours = remaining / batteryNetPower;
            }
            
            // Hydrogen stats
            float hydrogenStored = 0f;
            float hydrogenMax = 0f;
            
            if (gts != null)
            {
                List<IMyGasTank> tanks = new List<IMyGasTank>();
                gts.GetBlocksOfType(tanks);
                
                foreach (var tank in tanks)
                {
                    if (allGrids.Contains(tank.CubeGrid) && tank.IsFunctional)
                    {
                        // Check if it's hydrogen (not oxygen)
                        if (tank.DetailedInfo.Contains("Hydrogen") || tank.DetailedInfo.Contains("H2"))
                        {
                            hydrogenStored += (float)tank.FilledRatio * tank.Capacity;
                            hydrogenMax += tank.Capacity;
                        }
                    }
                }
            }
            
            float hydrogenPercent = hydrogenMax > 0 ? (hydrogenStored / hydrogenMax) * 100f : 0f;
            
            // Estimate hydrogen consumption from engines
            float hydrogenConsumption = 0f; // L/min
            
            if (gts != null)
            {
                List<IMyPowerProducer> engines = new List<IMyPowerProducer>();
                gts.GetBlocksOfType(engines);
                
                foreach (var engine in engines)
                {
                    if (allGrids.Contains(engine.CubeGrid) && engine.IsFunctional && engine.Enabled)
                    {
                        // Hydrogen engine consumes approximately 1L/s per MW at full load
                        float engineOutput = engine.CurrentOutput;
                        hydrogenConsumption += engineOutput * 60f; // L/min
                    }
                }
            }
            
            // Calculate hydrogen time/range
            float hydrogenMinutes = 0f;
            if (hydrogenConsumption > 0.001f)
            {
                hydrogenMinutes = hydrogenStored / hydrogenConsumption;
            }
            
            // Estimate range based on current speed
            float rangeKm = 0f;
            if (speed > 0.5f && hydrogenMinutes > 0)
            {
                float rangeMeters = speed * (hydrogenMinutes * 60f); // speed * seconds
                rangeKm = rangeMeters / 1000f;
            }
            
            // Power consumption
            float totalPowerDraw = batteryOutput;
            float totalPowerCapacity = 0f;
            
            if (gts != null)
            {
                List<IMyPowerProducer> producers = new List<IMyPowerProducer>();
                gts.GetBlocksOfType(producers);
                
                foreach (var producer in producers)
                {
                    if (allGrids.Contains(producer.CubeGrid) && producer.IsFunctional)
                    {
                        totalPowerCapacity += producer.MaxOutput;
                    }
                }
            }
            
            // Determine if parked
            bool isParked = controller.HandBrake;
            
            // Get current time
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            
            // Draw the layout
            DrawEfficiencyLayout(currentTime, isParked, batteryPercent, batteryHours, 
                                hydrogenPercent, rangeKm, hydrogenMinutes, hydrogenConsumption,
                                totalPowerDraw, totalPowerCapacity);
        }

        void DrawEfficiencyLayout(string time, bool isParked, float batteryPercent, float batteryHours,
                                 float hydrogenPercent, float rangeKm, float hydrogenMinutes, float fuelRate,
                                 float powerDraw, float powerCapacity)
        {
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;
            Vector2 offset = new Vector2(Cfg.ViewPortOffsetX, Cfg.ViewPortOffsetY);
            var frame = Surface.DrawFrame();
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Surface.ScriptBackgroundColor));

            float currentY = screenCorner.Y + offset.Y;
            float lineHeight = 24 * Cfg.TextSize * 1.4f;
            float charWidth = 6 * Cfg.TextSize;
            Color textColor = Surface.ScriptForegroundColor;
            float leftX = screenCorner.X + offset.X;
            float centerX = screenCorner.X + screenSize.X * 0.5f;
            float rightX = screenCorner.X + screenSize.X - offset.X;

            // Header: Time and Status
            string statusText = isParked ? "PARKED" : "DRIVE";
            Color statusColor = isParked ? ColorDanger : ColorGood;
            DrawText(frame, time, new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, "Status: ", new Vector2(rightX - (31 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, statusText, new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
            currentY += lineHeight * 0.45f;
            
            DrawText(frame, "_________________________________________________", new Vector2(centerX, currentY), Cfg.TextSize, textColor, TextAlignment.CENTER);
            currentY += lineHeight * 1.2f;

            // Table layout for efficiency data
            float col1X = leftX + (2 * charWidth);  // Label column (left aligned)
            float col2X = rightX - (2 * charWidth); // Value column (right aligned)
            
            // Battery
            Color batteryColor = GetResourceColor(batteryPercent);
            float batteryRatio = batteryPercent / 100f;
            int barLength = 18;
            float barX = centerX - (barLength * charWidth * 0.5f);
            
            DrawText(frame, "Battery:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{batteryPercent:F0}%", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            DrawColoredBar(frame, batteryRatio, barLength, new Vector2(barX, currentY), Cfg.TextSize, batteryColor, textColor);
            currentY += lineHeight;
            
            string batteryTimeText = batteryHours > 0 ? $"{batteryHours:F1}h" : "--";
            DrawText(frame, "Time Left:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, batteryTimeText, new Vector2(col2X, currentY), Cfg.TextSize, batteryColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.2f;
            
            // Hydrogen
            Color hydrogenColor = GetResourceColor(hydrogenPercent);
            float hydrogenRatio = hydrogenPercent / 100f;
            
            DrawText(frame, "Hydrogen:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{hydrogenPercent:F0}%", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            DrawColoredBar(frame, hydrogenRatio, barLength, new Vector2(barX, currentY), Cfg.TextSize, hydrogenColor, textColor);
            currentY += lineHeight;
            
            string hydrogenRangeText = rangeKm > 0 ? $"{rangeKm:F0} km" : "--";
            DrawText(frame, "Range:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, hydrogenRangeText, new Vector2(col2X, currentY), Cfg.TextSize, hydrogenColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.2f;
            
            // Power usage
            float powerRatio = powerCapacity > 0 ? Math.Min(powerDraw / powerCapacity, 1.0f) : 0f;
            Color powerColor = powerRatio > 0.9f ? ColorDanger : powerRatio > 0.7f ? ColorWarning : ColorGood;
            
            DrawText(frame, "Power:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{powerDraw:F1} MW", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            DrawColoredBar(frame, powerRatio, barLength, new Vector2(barX, currentY), Cfg.TextSize, powerColor, textColor);
            currentY += lineHeight;
            
            DrawText(frame, "Max Pwr:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{powerCapacity:F1} MW", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.2f;
            
            // Fuel rate
            string fuelTimeText = hydrogenMinutes > 0 ? $"({hydrogenMinutes / 60f:F1}h)" : "";
            DrawText(frame, "Fuel Rate:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{fuelRate:F0} L/m {fuelTimeText}", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);

            frame.Dispose();
        }

        Color GetResourceColor(float percent)
        {
            if (!Cfg.UseColors) return Surface.ScriptForegroundColor;
            
            if (percent <= 20f) return ColorDanger;
            if (percent <= 40f) return ColorWarning;
            return ColorGood;
        }

        void DrawColoredBar(MySpriteDrawFrame frame, float ratio, int length, Vector2 position, float scale, Color filledColor, Color emptyColor)
        {
            ratio = Math.Max(0, Math.Min(ratio, 1.0f));
            int filled = (int)(ratio * length);

            StringBuilder bar = new StringBuilder();
            bar.Append('[');
            for (int i = 0; i < length; i++)
            {
                if (i < filled)
                    bar.Append('|');
                else
                    bar.Append('\'');
            }
            bar.Append(']');

            DrawText(frame, bar.ToString(), position, scale, filledColor, TextAlignment.LEFT);
        }

        void DrawText(MySpriteDrawFrame frame, string text, Vector2 position, float scale, Color color, TextAlignment alignment)
        {
            var sprite = MySprite.CreateText(text, "Debug", color, scale, alignment);
            sprite.Position = position;
            frame.Add(sprite);
        }

        void DrawMessage(string message)
        {
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;
            var frame = Surface.DrawFrame();
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Surface.ScriptBackgroundColor));

            var sprite = MySprite.CreateText(message, "Debug", Surface.ScriptForegroundColor, 1.2f, TextAlignment.CENTER);
            sprite.Position = new Vector2(screenCorner.X + screenSize.X * 0.5f, screenCorner.Y + screenSize.Y * 0.5f - 30);
            frame.Add(sprite);

            frame.Dispose();
        }

        void DrawError(Exception e)
        {
            DrawMessage($"ERROR\n{e.Message}");
        }
    }
}
