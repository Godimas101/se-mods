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
    [MyTextSurfaceScript("GroundVehiclePerformance", "[!] Vehicle Performance")]
    public class GroundVehiclePerformance : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        private readonly IMyTerminalBlock TerminalBlock;
        private IMyCubeGrid Grid;
        private readonly List<IMyMotorSuspension> Wheels = new List<IMyMotorSuspension>();
        
        private Config Cfg = new Config();
        private bool ConfigLoaded = false;
        private int ConfigCheckCounter = 0;

        private readonly Color ColorGood = new Color(0, 255, 100);
        private readonly Color ColorWarning = new Color(255, 200, 0);
        private readonly Color ColorDanger = new Color(255, 50, 50);

        private float lastSpeed = 0f;
        private float currentAccel = 0f;
        private int accelUpdateCounter = 0;

        public GroundVehiclePerformance(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            public float TextSize = 0.5f;
            public float ViewPortOffsetX = 10f;
            public float ViewPortOffsetY = 10f;
            public bool UseColors = true;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[VehiclePerformance]"))
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
                if (trimmed == "[VehiclePerformance]")
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
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[VehiclePerformance]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[VehiclePerformance]");
            sb.AppendLine("TextSize=0.5");
            sb.AppendLine("ViewPortOffsetX=10");
            sb.AppendLine("ViewPortOffsetY=10");
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

            // Get wheels (including subgrids)
            Wheels.Clear();
            int totalWheels = 0;
            int functionalWheels = 0;
            int propulsionWheels = 0;
            float totalPowerSetting = 0f;  // Sum of power % settings (0.0 to 1.0)
            float totalFriction = 0f;
            
            // Get all grids connected to this grid (including through rotors/pistons)
            List<IMyCubeGrid> allGrids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Mechanical, allGrids);
            
            if (gts != null)
            {
                List<IMyMotorSuspension> allWheels = new List<IMyMotorSuspension>();
                gts.GetBlocksOfType(allWheels);
                
                foreach (var wheel in allWheels)
                {
                    // Check if wheel is on any mechanically connected grid
                    if (allGrids.Contains(wheel.CubeGrid))
                    {
                        totalWheels++;
                        if (wheel.IsFunctional)
                        {
                            functionalWheels++;
                            Wheels.Add(wheel);
                            if (wheel.Propulsion)
                            {
                                propulsionWheels++;
                                totalPowerSetting += wheel.Power; // Power is 0.0 to 1.0 (0% to 100%)
                                totalFriction += wheel.Friction;
                            }
                        }
                    }
                }
            }
            
            // Calculate averages
            float avgPowerSetting = propulsionWheels > 0 ? totalPowerSetting / propulsionWheels : 0f;
            float avgFriction = propulsionWheels > 0 ? totalFriction / propulsionWheels : 0f;

            if (Wheels.Count == 0)
            {
                DrawMessage("NO WHEELS");
                return;
            }

            // Get vehicle data
            Vector3D velocity = controller.GetShipVelocities().LinearVelocity;
            float speed = (float)velocity.Length();
            
            // Calculate acceleration (every 6 updates = ~1 second)
            accelUpdateCounter++;
            if (accelUpdateCounter >= 6)
            {
                currentAccel = (speed - lastSpeed) / 1.0f; // m/s²
                lastSpeed = speed;
                accelUpdateCounter = 0;
            }
            
            // Get total mass
            float totalMass = 0f;
            foreach (var grid in allGrids)
            {
                if (grid.Physics != null)
                    totalMass += grid.Physics.Mass;
            }
            
            // Calculate max speed estimate
            float maxSpeed = 100f;
            
            // Get current slope and gravity direction
            float interference;
            Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Grid.GetPosition(), out interference);
            float currentSlopeAngle = 0f;
            Vector3D gridUp = Grid.WorldMatrix.Up;
            
            if (gravity.LengthSquared() > 0.01f)
            {
                Vector3D gravityNorm = Vector3D.Normalize(gravity);
                
                // Calculate slope relative to gravity (0° = flat, 90° = vertical wall)
                double dot = Vector3D.Dot(gridUp, -gravityNorm);
                currentSlopeAngle = (float)(Math.Acos(Math.Max(-1, Math.Min(1, dot))) * (180.0 / Math.PI));
                
                // If moving, refine slope based on velocity direction
                if (speed > 0.5f)
                {
                    Vector3D velocityNorm = Vector3D.Normalize(velocity);
                    double slopeDot = Vector3D.Dot(velocityNorm, -gravityNorm);
                    float velocitySlopeAngle = (float)(Math.Asin(Math.Max(-1, Math.Min(1, slopeDot))) * (180.0 / Math.PI));
                    // Use velocity-based slope if significantly different (going uphill/downhill)
                    if (Math.Abs(velocitySlopeAngle) > 5f)
                        currentSlopeAngle = velocitySlopeAngle;
                }
            }
            
            // Calculate performance rating (adjusted from field data)
            // P/W 1.0 = minimum acceptable performance (slow but functional)
            // P/W 2.0+ = good performance (recommended operating range)
            // P/W <1.0 = warning (very slow, barely functional)
            float baseConstant = 15.69f;
            float wheelCapacity = propulsionWheels * avgPowerSetting * avgFriction * baseConstant;
            
            // Base P/W on flat ground
            float flatPW = totalMass > 0 && propulsionWheels > 0 ? wheelCapacity / totalMass : 0f;
            
            // Adjust P/W for current slope (uphill reduces effective P/W)
            // Each degree of uphill slope reduces P/W by ~1.5% (rough approximation)
            float slopeFactor = 1.0f - (Math.Max(0, currentSlopeAngle) * 0.015f);
            float theoreticalPW = flatPW * Math.Max(0.1f, slopeFactor);
            
            // Calculate max climbable slope at current power settings
            // More conservative: rating 0.8 needed to climb, plus safety margin
            // Reduced multiplier for more realistic max slope
            float maxSlopeAngle = flatPW > 0.8f ? (flatPW - 0.8f) / 0.025f : 0f;
            maxSlopeAngle = Math.Min(maxSlopeAngle, 60f); // Cap at 60° (very steep)
            
            // Calculate braking distance
            float brakeDistance = 0f;
            if (speed > 0.5f)
            {
                float brakeDeceleration = 15f; // m/s²
                brakeDistance = (speed * speed) / (2f * brakeDeceleration);
            }
            
            // Determine if parked (only handbrake)
            bool isParked = controller.HandBrake;
            
            // Get current time
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            
            // Draw the layout
            DrawPerformanceLayout(currentTime, isParked, speed, maxSpeed, currentAccel, 
                                 theoreticalPW, currentSlopeAngle, maxSlopeAngle, brakeDistance);
        }

        void DrawPerformanceLayout(string time, bool isParked, float speed, float maxSpeed, 
                                  float accel, float theoreticalPW, float currentSlope, float maxSlope, float brakeDistance)
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

            // Table layout for performance data
            float col1X = leftX + (2 * charWidth);  // Label column (left aligned)
            float col2X = rightX - (2 * charWidth); // Value column (right aligned)
            
            DrawText(frame, "Spd:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{speed:F0} m/s", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight;
            
            DrawText(frame, "Max Spd:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{maxSpeed:F0} m/s", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight * 0.8f;
            
            string accelSign = accel >= 0 ? "+" : "";
            DrawText(frame, "Accel:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{accelSign}{accel:F1} m/s²", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight;
            
            DrawText(frame, "Brk Dist:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{brakeDistance:F0} m", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.3f;

            // Theoretical P/W ratio (adjusted for slope)
            Color pwColor = GetPowerColor(theoreticalPW);
            float pwRatio = Math.Min(theoreticalPW / 2.0f, 1.0f);  // Scale bar: 0-2.0 range
            int barLength = 18;
            
            DrawText(frame, "P/W:", new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{theoreticalPW:F2}", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            
            // Bar on same row, positioned at center
            float barX = centerX - (barLength * charWidth * 0.5f);
            DrawColoredBar(frame, pwRatio, barLength, new Vector2(barX, currentY), Cfg.TextSize, pwColor, textColor);
            currentY += lineHeight * 1.5f;
            
            // Slope bar - centered, full width (max 60° range)
            int slopeBarLength = 40;
            
            StringBuilder slopeBar = new StringBuilder();
            int centerPos = slopeBarLength / 2;
            
            float slopeNormalized = Math.Max(-60f, Math.Min(60f, currentSlope));
            int slopePos = centerPos + (int)((slopeNormalized / 60f) * (slopeBarLength / 2));
            slopePos = Math.Max(0, Math.Min(slopeBarLength - 1, slopePos));
            
            for (int i = 0; i < slopeBarLength; i++)
            {
                if (i == centerPos)
                    slopeBar.Append('|');
                else if (i == slopePos)
                    slopeBar.Append('¡');
                else
                    slopeBar.Append('\'');
            }
            
            Color slopeColor = GetSlopeColor(currentSlope, maxSlope);
            string slopePrefix = currentSlope > 0 ? "+ " : currentSlope < 0 ? "- " : "";
            DrawText(frame, $"Slope: {slopeBar} {slopePrefix}{Math.Abs(currentSlope):F0}°", 
                    new Vector2(centerX, currentY), Cfg.TextSize, slopeColor, TextAlignment.CENTER);
            currentY += lineHeight;
            
            // Slope scale labels - centered
            DrawText(frame, $"-60°              0              +60°   Max: {maxSlope:F0}°", 
                    new Vector2(centerX + 30f, currentY), Cfg.TextSize * 0.75f, textColor, TextAlignment.CENTER);

            frame.Dispose();
        }

        Color GetPowerColor(float rating)
        {
            if (!Cfg.UseColors) return Surface.ScriptForegroundColor;
            
            // New calibration:
            // Rating 1.0 = minimum acceptable performance (slow but functional)
            // Rating 2.0+ = good performance (recommended)
            // Rating <1.0 = warning (very slow, barely functional)
            if (rating < 1.0f) return ColorDanger;    // Red: too slow, add power or reduce weight
            if (rating < 2.0f) return ColorWarning;   // Yellow: acceptable but sluggish
            return ColorGood;                          // Green: good performance
        }

        Color GetSlopeColor(float current, float max)
        {
            if (!Cfg.UseColors) return Surface.ScriptForegroundColor;
            
            float absSlope = Math.Abs(current);
            if (absSlope >= max * 0.8f) return ColorDanger;
            if (absSlope >= max * 0.5f) return ColorWarning;
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
