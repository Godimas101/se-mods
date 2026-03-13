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
    [MyTextSurfaceScript("AtmoMaxAlt", "[!] Atmo Max Altitude")]
    public class AtmosphericMaxAltitude : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        private readonly IMyTerminalBlock TerminalBlock;
        private IMyCubeGrid Grid;
        private readonly List<IMyThrust> Thrusters = new List<IMyThrust>();

        private Config Cfg = new Config();
        private bool ConfigLoaded = false;
        private int ConfigCheckCounter = 0;

        private readonly Color ColorGood = new Color(0, 255, 100);
        private readonly Color ColorWarning = new Color(255, 200, 0);
        private readonly Color ColorDanger = new Color(255, 50, 50);

        public AtmosphericMaxAltitude(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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

                // Periodically check for config changes
                ConfigCheckCounter++;
                if (ConfigCheckCounter >= 30) // Check every 5 seconds
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
            public float ViewPortOffsetY = 8f;
            public bool ShowBars = true;
            public bool UseColors = true;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[AtmoMaxAlt]"))
            {
                // First time setup - write default config ONLY ONCE
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
                if (trimmed == "[AtmoMaxAlt]")
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
                        case "ShowBars": bool.TryParse(valueStr, out Cfg.ShowBars); break;
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
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[AtmoMaxAlt]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[AtmoMaxAlt]");
            sb.AppendLine("TextSize=0.6");
            sb.AppendLine("ViewPortOffsetX=8");
            sb.AppendLine("ViewPortOffsetY=8");
            sb.AppendLine("ShowBars=true");
            sb.AppendLine("UseColors=true");

            TerminalBlock.CustomData = sb.ToString();
        }

        void Draw()
        {
            if (Grid?.Physics == null)
                return;

            float gridMass = Grid.Physics.Mass;
            Vector3D gridPosition = Grid.GetPosition();

            // Calculate gravity
            float interference;
            Vector3 naturalGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(gridPosition, out interference);
            float gravityStrength = naturalGravity.Length();

            if (gravityStrength < 0.01f)
            {
                DrawMessage("NO ATMOSPHERE DETECTED");
                return;
            }

            // Find ship controller to get actual altitude
            IMyShipController shipController = null;
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
            if (gts != null)
            {
                List<IMyShipController> controllers = new List<IMyShipController>();
                gts.GetBlocksOfType(controllers, c => c.CubeGrid == Grid && c.IsFunctional);
                if (controllers.Count > 0)
                    shipController = controllers[0];
            }

            // Get atmospheric thrusters
            Thrusters.Clear();
            if (gts != null)
            {
                gts.GetBlocksOfType<IMyThrust>(Thrusters, t =>
                    t.CubeGrid == Grid &&
                    t.IsFunctional &&
                    t.BlockDefinition.SubtypeId.Contains("Atmo"));
            }

            if (Thrusters.Count == 0)
            {
                DrawMessage("NO ATMOSPHERIC THRUSTERS");
                return;
            }

            // Find "up" direction and calculate thrust
            Vector3 gravityDir = gravityStrength > 0 ? naturalGravity / gravityStrength : Vector3.Zero;
            MatrixD gridMatrix = Grid.WorldMatrix;
            Vector3D upWorldDir = -gravityDir;
            Base6Directions.Direction bestUpDir = Base6Directions.Direction.Up;
            double bestDot = -1;

            foreach (Base6Directions.Direction dir in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                Vector3D dirWorld = gridMatrix.GetDirectionVector(dir);
                double dot = Vector3D.Dot(dirWorld, upWorldDir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestUpDir = dir;
                }
            }

            float maxThrustAtSurface = 0f;
            foreach (var thruster in Thrusters)
            {
                Base6Directions.Direction thrustDir = Base6Directions.GetOppositeDirection(thruster.Orientation.Forward);
                if (thrustDir == bestUpDir)
                {
                    maxThrustAtSurface += thruster.MaxThrust;
                }
            }

            // Get current altitude from atmospheric density
            double currentAltitude = 0;
            double currentDensity = 1.0;

            // Get atmospheric density and calculate altitude from thruster
            IMyThrust refThruster = null;
            foreach (var t in Thrusters)
            {
                var def = t.BlockDefinition.SubtypeId;
                if (def.Contains("Atmospheric"))
                {
                    refThruster = t;
                    break;
                }
            }

            if (refThruster != null)
            {
                // Current effectiveness reflects atmospheric density
                float maxThrust = refThruster.MaxThrust;
                float effectiveThrust = refThruster.MaxEffectiveThrust;
                if (maxThrust > 0)
                {
                    currentDensity = effectiveThrust / maxThrust;

                    // Calculate altitude from density using exponential atmospheric model
                    // density = exp(-altitude / scaleHeight)
                    // altitude = -scaleHeight * ln(density)
                    // Calibrated for Earth-like planets in SE
                    double atmoScaleHeight = 5100; // meters - calibrated value
                    if (currentDensity > 0.01 && currentDensity < 0.999)
                    {
                        currentAltitude = -atmoScaleHeight * Math.Log(currentDensity);
                    }
                    else if (currentDensity <= 0.01)
                    {
                        currentAltitude = 20000; // Beyond atmosphere
                    }
                }
            }

            // Calculate density margin (how much density we have vs need)
            float requiredThrust = gridMass * gravityStrength;
            double minDensity = requiredThrust / Math.Max(maxThrustAtSurface, 0.001f);

            // Density margin shows how close we are to failure
            double densityMargin = currentDensity - minDensity;
            double densityMarginPercent = (densityMargin / Math.Max(minDensity, 0.001)) * 100.0;

            // Setup drawing
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;
            Vector2 offset = new Vector2(Cfg.ViewPortOffsetX, Cfg.ViewPortOffsetY);
            var frame = Surface.DrawFrame();
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Surface.ScriptBackgroundColor));

            float currentY = screenCorner.Y + offset.Y;
            float lineHeight = 24 * Cfg.TextSize * 1.4f;
            Color textColor = Surface.ScriptForegroundColor;
            float leftX = screenCorner.X + offset.X;
            float centerX = screenCorner.X + screenSize.X * 0.5f;
            float rightX = screenCorner.X + screenSize.X - offset.X;

            // Check for hydrogen thrusters
            bool hasHydroThrusters = false;
            if (gts != null)
            {
                List<IMyThrust> hydroThrusters = new List<IMyThrust>();
                gts.GetBlocksOfType(hydroThrusters, t => 
                    t.CubeGrid == Grid && 
                    t.IsFunctional && 
                    (t.BlockDefinition.SubtypeId.Contains("Hydrogen") || t.BlockDefinition.SubtypeId.Contains("Hydro")));
                hasHydroThrusters = hydroThrusters.Count > 0;
            }

            // Get hydrogen and battery info
            float hydroPercent = 0f;
            float batteryPercent = 0f;
            if (gts != null)
            {
                if (hasHydroThrusters)
                {
                    List<IMyGasTank> hydroTanks = new List<IMyGasTank>();
                    gts.GetBlocksOfType(hydroTanks, t => t.CubeGrid == Grid && t.IsFunctional);
                    if (hydroTanks.Count > 0)
                    {
                        double totalCapacity = 0;
                        double totalStored = 0;
                        foreach (var tank in hydroTanks)
                        {
                            totalCapacity += tank.Capacity;
                            totalStored += tank.Capacity * tank.FilledRatio;
                        }
                        hydroPercent = totalCapacity > 0 ? (float)(totalStored / totalCapacity * 100) : 0f;
                    }
                }
                
                List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
                gts.GetBlocksOfType(batteries, b => b.CubeGrid == Grid && b.IsFunctional);
                if (batteries.Count > 0)
                {
                    float totalCapacity = 0;
                    float totalStored = 0;
                    foreach (var battery in batteries)
                    {
                        totalCapacity += battery.MaxStoredPower;
                        totalStored += battery.CurrentStoredPower;
                    }
                    batteryPercent = totalCapacity > 0 ? (totalStored / totalCapacity * 100) : 0f;
                }
            }

            // Header with resource info or time
            if (hasHydroThrusters)
            {
                Color hydroColor = hydroPercent < 20 ? ColorDanger : (hydroPercent < 50 ? ColorWarning : ColorGood);
                Color battColor = batteryPercent < 20 ? ColorDanger : (batteryPercent < 50 ? ColorWarning : ColorGood);
                DrawText(frame, $"Hydrogen: {hydroPercent:F0}%", new Vector2(leftX, currentY), Cfg.TextSize * 0.9f, hydroColor, TextAlignment.LEFT);
                DrawText(frame, $"PWR: {batteryPercent:F0}%", new Vector2(rightX, currentY), Cfg.TextSize * 0.9f, battColor, TextAlignment.RIGHT);
            }
            else
            {
                // Show time and battery
                DateTime now = DateTime.Now;
                string timeStr = now.ToString("HH:mm:ss");
                Color battColor = batteryPercent < 20 ? ColorDanger : (batteryPercent < 50 ? ColorWarning : ColorGood);
                DrawText(frame, timeStr, new Vector2(leftX, currentY), Cfg.TextSize * 0.9f, textColor, TextAlignment.LEFT);
                DrawText(frame, $"PWR: {batteryPercent:F0}%", new Vector2(rightX, currentY), Cfg.TextSize * 0.9f, battColor, TextAlignment.RIGHT);
            }
            currentY += lineHeight * 0.7f;
            DrawText(frame, "________________________________________________", new Vector2(centerX, currentY), Cfg.TextSize, textColor, TextAlignment.CENTER);
            currentY += lineHeight * 1.2f;

            // Determine status based on density margin
            Color statusColor = ColorGood;
            string statusText = "SAFE";
            float barRatio = 0f;

            if (Cfg.UseColors)
            {
                if (densityMargin < 0 || minDensity >= 1.0)
                {
                    statusColor = ColorDanger;
                    statusText = "CRITICAL";
                    barRatio = 0f;
                }
                else if (densityMarginPercent < 20)
                {
                    statusColor = ColorDanger;
                    statusText = "DANGER";
                    barRatio = (float)(densityMarginPercent / 20.0);
                }
                else if (densityMarginPercent < 50)
                {
                    statusColor = ColorWarning;
                    statusText = "WARNING";
                    barRatio = 0.2f + (float)((densityMarginPercent - 20) / 30.0) * 0.3f;
                }
                else
                {
                    statusColor = ColorGood;
                    statusText = "SAFE";
                    barRatio = 0.5f + Math.Min((float)((densityMarginPercent - 50) / 100.0) * 0.5f, 0.5f);
                }
            }
            else
            {
                statusColor = textColor;
                barRatio = (float)Math.Max(0, Math.Min(1.0, densityMarginPercent / 100.0));
            }

            // Density header
            DrawText(frame, "Required", new Vector2(leftX, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
            DrawText(frame, "Current", new Vector2(rightX, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.RIGHT);
            currentY += lineHeight * 0.9f;

            // Density values (as percentages)
            DrawText(frame, $"{(minDensity * 100):F1}%", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{(currentDensity * 100):F1}%", new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.3f;

            // Status with bar - bar first, then status text
            if (Cfg.ShowBars)
            {
                // Calculate how many characters fit in the available space
                float textWidth = 140 * Cfg.TextSize; // Space for status text with margin
                float availableWidth = (rightX - leftX) - textWidth;
                float charWidth = 6 * Cfg.TextSize; // Tighter estimate for Debug font
                int barLength = (int)(availableWidth / charWidth);
                barLength = Math.Max(10, Math.Min(barLength, 150));

                float barStartX = leftX;
                DrawColoredBar(frame, barRatio, barLength, new Vector2(barStartX, currentY), Cfg.TextSize, statusColor, textColor);
            }
            DrawText(frame, statusText, new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
            currentY += lineHeight * 1.3f;

            // Density margin
            DrawText(frame, "Margin:", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{densityMarginPercent:F0}%", new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
            currentY += lineHeight;

            // Current thrust effectiveness
            float currentThrust = maxThrustAtSurface * (float)currentDensity;
            DrawText(frame, "Effective Thrust:", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, FormatThrust(currentThrust), new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
            currentY += lineHeight;

            // Thrust-to-Weight ratio
            float currentTW = currentThrust / Math.Max(requiredThrust, 0.001f);
            DrawText(frame, "T/W Ratio:", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
            DrawText(frame, $"{currentTW:F2}", new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
            currentY += lineHeight;

            frame.Dispose();
        }

        string FormatThrust(float thrust)
        {
            if (thrust >= 1000000)
                return (thrust / 1000000).ToString("F2") + " MN";
            else if (thrust >= 1000)
                return (thrust / 1000).ToString("F1") + " kN";
            else
                return thrust.ToString("F0") + " N";
        }

        void DrawColoredBar(MySpriteDrawFrame frame, float ratio, int length, Vector2 position, float scale, Color filledColor, Color emptyColor)
        {
            ratio = Math.Max(0, Math.Min(ratio, 1.0f));
            int filled = (int)(ratio * length);
            
            // Build the full bar with fixed width
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
            
            // Draw the entire bar at once
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
            DrawText(frame, message, screenCorner + screenSize * 0.5f, 0.7f, ColorWarning, TextAlignment.CENTER);
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
                var text = MySprite.CreateText($"ERROR: {e.Message}", "Debug", Color.Red, 0.5f, TextAlignment.LEFT);
                text.Position = screenCorner + new Vector2(16, 16);
                frame.Add(text);
                frame.Dispose();
            }
            catch { }
        }
    }
}
