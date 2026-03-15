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
    [MyTextSurfaceScript("GroundVehicleCargoMonitor", "[!] Vehicle Cargo Monitor")]
    public class GroundVehicleCargoMonitor : MyTSSCommon
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

        public GroundVehicleCargoMonitor(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            public float TextSize = 0.6f;
            public float ViewPortOffsetX = 8f;
            public float ViewPortOffsetY = 10f;
            public bool UseColors = true;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[VehicleCargo]"))
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
                if (trimmed == "[VehicleCargo]")
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
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[VehicleCargo]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[VehicleCargo]");
            sb.AppendLine("TextSize=0.6");
            sb.AppendLine("ViewPortOffsetX=8");
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
                    if (allGrids.Contains(wheel.CubeGrid) && wheel.IsFunctional)
                    {
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
            
            // Get total mass and dry mass
            float totalMass = 0f;
            float cargoMass = 0f;
            
            foreach (var grid in allGrids)
            {
                if (grid.Physics != null)
                    totalMass += grid.Physics.Mass;
            }
            
            // Calculate cargo mass from all cargo containers
            if (gts != null)
            {
                List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
                gts.GetBlocksOfType(cargoContainers);
                
                foreach (var cargo in cargoContainers)
                {
                    if (allGrids.Contains(cargo.CubeGrid) && cargo.IsFunctional)
                    {
                        var inventory = cargo.GetInventory();
                        if (inventory != null)
                        {
                            cargoMass += (float)inventory.CurrentMass;
                        }
                    }
                }
            }
            
            float dryMass = totalMass - cargoMass;
            
            // Get current slope for P/W adjustment
            float interference;
            Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Grid.GetPosition(), out interference);
            float currentSlopeAngle = 0f;
            Vector3D gridUp = Grid.WorldMatrix.Up;
            
            if (gravity.LengthSquared() > 0.01f)
            {
                Vector3D gravityNorm = Vector3D.Normalize(gravity);
                double dot = Vector3D.Dot(gridUp, -gravityNorm);
                currentSlopeAngle = (float)(Math.Acos(Math.Max(-1, Math.Min(1, dot))) * (180.0 / Math.PI));
                
                if (speed > 0.5f)
                {
                    Vector3D velocityNorm = Vector3D.Normalize(velocity);
                    double slopeDot = Vector3D.Dot(velocityNorm, -gravityNorm);
                    float velocitySlopeAngle = (float)(Math.Asin(Math.Max(-1, Math.Min(1, slopeDot))) * (180.0 / Math.PI));
                    if (Math.Abs(velocitySlopeAngle) > 5f)
                        currentSlopeAngle = velocitySlopeAngle;
                }
            }
            
            // Calculate performance P/W (adjusted for better information)
            // P/W 1.0 = minimum acceptable performance (slow but functional)
            // P/W 2.0+ = good performance (recommended operating range)
            // P/W <1.0 = warning (very slow, barely functional)
            float baseConstant = 15.69f;
            float wheelCapacity = propulsionWheels * avgPowerSetting * avgFriction * baseConstant;
            float flatPW = totalMass > 0 && propulsionWheels > 0 ? wheelCapacity / totalMass : 0f;
            
            // Adjust P/W for current slope
            float slopeFactor = 1.0f - (Math.Max(0, currentSlopeAngle) * 0.015f);
            float theoreticalPW = flatPW * Math.Max(0.1f, slopeFactor);
            
            // Calculate max mass: P/W of 1.0 is minimum acceptable (not absolute minimum)
            // Max mass = wheel capacity for P/W 1.0
            float maxMass = propulsionWheels > 0 && avgPowerSetting > 0 ? wheelCapacity / 1.0f : totalMass;
            float loadPercent = (totalMass / Math.Max(maxMass, 1f)) * 100f;
            float remainingCapacity = Math.Max(0, maxMass - totalMass);
            
            // Determine if parked (only handbrake)
            bool isParked = controller.HandBrake;
            
            // Get current time
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            
            // Draw the layout
            DrawCargoLayout(currentTime, isParked, dryMass, cargoMass, totalMass, maxMass, 
                           loadPercent, remainingCapacity, theoreticalPW);
        }

        void DrawCargoLayout(string time, bool isParked, float dryMass, float cargoMass, 
                            float totalMass, float maxMass, float loadPercent, float remainingCapacity,
                            float theoreticalPW)
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

            // 3-column layout: Label (left) | Bar (center) | Value (right)
            int barLength = 14;
            float barCenterX = centerX;
            
            // Dry Mass - no bar
            DrawText(frame, "Dry Mass:", new Vector2(leftX + (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{dryMass:N0} kg", new Vector2(rightX - (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight;
            
            // Cargo - with bar and colored value
            float cargoRatio = Math.Min(cargoMass / Math.Max(maxMass * 0.5f, 1f), 1.0f);
            Color cargoColor = GetLoadColor((cargoMass / Math.Max(maxMass, 1f)) * 100f);
            DrawText(frame, "Cargo:", new Vector2(leftX + (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawCenteredBar(frame, cargoRatio, barLength, new Vector2(barCenterX, currentY), Cfg.TextSize, cargoColor, textColor);
            DrawText(frame, $"{cargoMass:N0} kg", new Vector2(rightX - (2 * charWidth), currentY), Cfg.TextSize, cargoColor, TextAlignment.RIGHT);
            currentY += lineHeight;
            
            // Total - no bar
            DrawText(frame, "Total Mass:", new Vector2(leftX + (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{totalMass:N0} kg", new Vector2(rightX - (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight * 0.8f;
            
            // Max Mass - no bar
            DrawText(frame, "Max Mass:", new Vector2(leftX + (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{maxMass:N0} kg", new Vector2(rightX - (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight;
            
            // Free - with colored bar (inverse of total)
            float totalRatio = Math.Min(totalMass / Math.Max(maxMass, 1f), 1.0f);
            float freeRatio = Math.Max(0, 1.0f - totalRatio);
            Color freeColor = GetPowerColor(theoreticalPW);
            DrawText(frame, "Free:", new Vector2(leftX + (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawCenteredBar(frame, freeRatio, barLength, new Vector2(barCenterX, currentY), Cfg.TextSize, freeColor, textColor);
            DrawText(frame, $"{remainingCapacity:N0} kg", new Vector2(rightX - (2 * charWidth), currentY), Cfg.TextSize, freeColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.3f;
            
            // P/W - with bar
            Color pwColor = GetPowerColor(theoreticalPW);
            float pwRatio = Math.Min(theoreticalPW / 2.0f, 1.0f);
            DrawText(frame, "P/W:", new Vector2(leftX + (2 * charWidth), currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawCenteredBar(frame, pwRatio, barLength, new Vector2(barCenterX, currentY), Cfg.TextSize, pwColor, textColor);
            DrawText(frame, $"{theoreticalPW:F2}", new Vector2(rightX - (2 * charWidth), currentY), Cfg.TextSize, pwColor, TextAlignment.RIGHT);

            frame.Dispose();
        }
        
        void DrawCenteredBar(MySpriteDrawFrame frame, float ratio, int length, Vector2 centerPos, float scale, Color filledColor, Color emptyColor)
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

            DrawText(frame, bar.ToString(), centerPos, scale, filledColor, TextAlignment.CENTER);
        }

        Color GetLoadColor(float loadPercent)
        {
            if (!Cfg.UseColors) return Surface.ScriptForegroundColor;
            
            if (loadPercent >= 100f) return ColorDanger;
            if (loadPercent >= 80f) return ColorWarning;
            return ColorGood;
        }

        Color GetPowerColor(float rating)
        {
            if (!Cfg.UseColors) return Surface.ScriptForegroundColor;
            
            // New calibration:
            // Rating 1.0 = minimum acceptable performance
            // Rating 2.0+ = good performance (recommended)
            // Rating <1.0 = warning (very slow)
            if (rating < 1.0f) return ColorDanger;    // Red: too slow
            if (rating < 2.0f) return ColorWarning;   // Yellow: acceptable
            return ColorGood;                          // Green: good
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

            var text = MySprite.CreateText(message, "Debug", Surface.ScriptForegroundColor, 1f, TextAlignment.CENTER);
            text.Position = screenCorner + screenSize * 0.5f;
            frame.Add(text);
            frame.Dispose();
        }

        void DrawError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

            try
            {
                Vector2 screenSize = Surface.SurfaceSize;
                Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

                var frame = Surface.DrawFrame();
                var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Color.Black);
                frame.Add(bg);

                var text = MySprite.CreateText($"ERROR: {e.Message}", "Debug", Color.Red, 0.7f, TextAlignment.LEFT);
                text.Position = screenCorner + new Vector2(16, 16);
                frame.Add(text);

                frame.Dispose();
            }
            catch { }
        }
    }
}
