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
    [MyTextSurfaceScript("ThrustToWeight", "[!] Thrust/Weight Ratio")]
    public class ThrustToWeightRatio : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        private readonly IMyTerminalBlock TerminalBlock;
        private IMyCubeGrid Grid;
        private readonly List<IMyThrust> Thrusters = new List<IMyThrust>();
        
        // Config
        private Config Cfg = new Config();
        private bool ConfigLoaded = false;
        private int ConfigCheckCounter = 0;

        // Visual colors
        private readonly Color ColorGood = new Color(0, 255, 100);
        private readonly Color ColorWarning = new Color(255, 200, 0);
        private readonly Color ColorDanger = new Color(255, 50, 50);
        private readonly Color ColorNeutral = new Color(150, 150, 255);

        public ThrustToWeightRatio(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose;
            Grid = TerminalBlock.CubeGrid;
            
            // Load initial config
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
            public float TextSize = 0.65f;
            public float ViewPortOffsetX = 8f;
            public float ViewPortOffsetY = 8f;
            public bool ShowHeader = true;
            public bool ShowBars = true;
            public bool ShowAllDirections = false;
            public bool UseColors = true;
            public bool UsePercentages = true;
            public bool IncludeAtmospheric = true;
            public bool IncludeIon = true;
            public bool IncludeHydrogen = true;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;

            string customData = TerminalBlock.CustomData;
            
            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[TWRatio]"))
            {
                // First time setup - write default config
                if (!ConfigLoaded)
                {
                    WriteDefaultConfig();
                    ConfigLoaded = true;
                }
                return;
            }

            ConfigLoaded = true;
            
            // Parse config - only read lines between [TWRatio] and next section
            string[] lines = customData.Split('\n');
            bool inSection = false;
            
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                
                if (trimmed == "[TWRatio]")
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
                        case "TextSize":
                            float.TryParse(valueStr, out Cfg.TextSize);
                            break;
                        case "ViewPortOffsetX":
                            float.TryParse(valueStr, out Cfg.ViewPortOffsetX);
                            break;
                        case "ViewPortOffsetY":
                            float.TryParse(valueStr, out Cfg.ViewPortOffsetY);
                            break;
                        case "ShowHeader":
                            bool.TryParse(valueStr, out Cfg.ShowHeader);
                            break;
                        case "ShowBars":
                            bool.TryParse(valueStr, out Cfg.ShowBars);
                            break;
                        case "ShowAllDirections":
                            bool.TryParse(valueStr, out Cfg.ShowAllDirections);
                            break;
                        case "UseColors":
                            bool.TryParse(valueStr, out Cfg.UseColors);
                            break;
                        case "UsePercentages":
                            bool.TryParse(valueStr, out Cfg.UsePercentages);
                            break;
                        case "IncludeAtmospheric":
                            bool.TryParse(valueStr, out Cfg.IncludeAtmospheric);
                            break;
                        case "IncludeIon":
                            bool.TryParse(valueStr, out Cfg.IncludeIon);
                            break;
                        case "IncludeHydrogen":
                            bool.TryParse(valueStr, out Cfg.IncludeHydrogen);
                            break;
                    }
                }
                catch { }
            }
        }

        void WriteDefaultConfig()
        {
            StringBuilder sb = new StringBuilder();
            
            // Check if CustomData already has content
            string existing = TerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[TWRatio]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }
            
            sb.AppendLine("[TWRatio]");
            sb.AppendLine("TextSize=0.65");
            sb.AppendLine("ViewPortOffsetX=8");
            sb.AppendLine("ViewPortOffsetY=8");
            sb.AppendLine("ShowHeader=true");
            sb.AppendLine("ShowBars=true");
            sb.AppendLine("ShowAllDirections=false");
            sb.AppendLine("UseColors=true");
            sb.AppendLine("UsePercentages=true");
            sb.AppendLine("IncludeAtmospheric=true");
            sb.AppendLine("IncludeIon=true");
            sb.AppendLine("IncludeHydrogen=true");

            TerminalBlock.CustomData = sb.ToString();
        }

        void Draw()
        {
            if (Grid?.Physics == null)
                return;

            // Get grid data
            float gridMass = Grid.Physics.Mass;
            Vector3D gridPosition = Grid.GetPosition();

            // Calculate gravity
            float interference;
            Vector3 naturalGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(gridPosition, out interference);
            float gravityStrength = naturalGravity.Length();
            Vector3 gravityDir = gravityStrength > 0 ? naturalGravity / gravityStrength : Vector3.Zero;

            // Get thrusters based on filter settings
            Thrusters.Clear();
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
            if (gts != null)
            {
                gts.GetBlocksOfType(Thrusters, t => 
                {
                    if (t.CubeGrid != Grid) return false;
                    
                    // Apply thruster type filters
                    var subtypeId = t.BlockDefinition.SubtypeId;
                    bool isAtmo = subtypeId.Contains("Atmospheric") || subtypeId.Contains("Atmo");
                    bool isHydro = subtypeId.Contains("Hydrogen") || subtypeId.Contains("Hydro");
                    bool isIon = !isAtmo && !isHydro;
                    
                    if (isAtmo && !Cfg.IncludeAtmospheric) return false;
                    if (isHydro && !Cfg.IncludeHydrogen) return false;
                    if (isIon && !Cfg.IncludeIon) return false;
                    
                    return true;
                });
            }

            // Calculate thrust in each direction
            Dictionary<Base6Directions.Direction, float> thrustByDirection = new Dictionary<Base6Directions.Direction, float>();
            foreach (Base6Directions.Direction dir in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                thrustByDirection[dir] = 0f;
            }

            foreach (var thruster in Thrusters)
            {
                if (!thruster.IsFunctional)
                    continue;

                Base6Directions.Direction thrustDir = Base6Directions.GetOppositeDirection(thruster.Orientation.Forward);
                float thrustValue = thruster.MaxEffectiveThrust;
                thrustByDirection[thrustDir] += thrustValue;
            }

            // Find max thrust for scaling bars
            float maxThrust = 0f;
            foreach (var kvp in thrustByDirection)
            {
                if (kvp.Value > maxThrust)
                    maxThrust = kvp.Value;
            }

            // Setup drawing
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;
            Vector2 offset = new Vector2(Cfg.ViewPortOffsetX, Cfg.ViewPortOffsetY);
            var frame = Surface.DrawFrame();

            // Background
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Surface.ScriptBackgroundColor));

            float currentY = screenCorner.Y + offset.Y;
            float lineHeight = 24 * Cfg.TextSize * 1.4f;
            Color textColor = Surface.ScriptForegroundColor;
            float leftX = screenCorner.X + offset.X;
            float centerX = screenCorner.X + screenSize.X * 0.5f;
            float rightX = screenCorner.X + screenSize.X - offset.X;

            // Check for hydrogen thrusters
            bool hasHydroThrusters = false;
            foreach (var thruster in Thrusters)
            {
                var subtypeId = thruster.BlockDefinition.SubtypeId;
                if (subtypeId.Contains("Hydrogen") || subtypeId.Contains("Hydro"))
                {
                    hasHydroThrusters = true;
                    break;
                }
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

            if (gravityStrength > 0.01f)
            {
                // Find "up" direction
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

                float upThrust = thrustByDirection[bestUpDir];
                float requiredThrust = gridMass * gravityStrength;
                float thrustRatio = upThrust / Math.Max(requiredThrust, 0.001f);
                float maxMassValue = upThrust / gravityStrength;
                float netAccel = (upThrust - requiredThrust) / gridMass;

                // Determine status color
                Color statusColor = ColorGood;
                string statusText = "OK";
                if (Cfg.UseColors)
                {
                    if (thrustRatio < 1.0f)
                    {
                        statusColor = ColorDanger;
                        statusText = "CRIT";
                    }
                    else if (thrustRatio < 1.2f)
                    {
                        statusColor = ColorWarning;
                        statusText = "WARN";
                    }
                }
                else
                {
                    statusColor = textColor;
                }

                // Mass header row
                DrawText(frame, "Max Mass", new Vector2(leftX, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
                DrawText(frame, "Current Mass", new Vector2(rightX, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.RIGHT);
                currentY += lineHeight * 0.9f;

                // Mass values row
                Color maxMassColor = Cfg.UseColors ? statusColor : textColor;
                DrawText(frame, $"{maxMassValue:N0} kg", new Vector2(leftX, currentY), Cfg.TextSize, maxMassColor, TextAlignment.LEFT);
                DrawText(frame, $"{gridMass:N0} kg", new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                currentY += lineHeight * 1.3f;

                // T/W Ratio row with bar
                DrawText(frame, $"T/W: {thrustRatio:F2}", new Vector2(leftX, currentY), Cfg.TextSize, statusColor, TextAlignment.LEFT);
                if (Cfg.ShowBars)
                {
                    // Calculate maximum available space for bar
                    float textWidth = 140 * Cfg.TextSize; // Space for "T/W: 1.45" with margin
                    float statusWidth = 60 * Cfg.TextSize; // Space for "OK" with margin
                    float availableWidth = (rightX - leftX) - textWidth - statusWidth;
                    int barLength = (int)(availableWidth / (10 * Cfg.TextSize)); // Max bar that fits
                    barLength = Math.Max(10, barLength);
                    
                    float barStartX = leftX + textWidth;
                    DrawColoredBar(frame, Math.Min(thrustRatio, 1.0f), barLength, new Vector2(barStartX, currentY), Cfg.TextSize, statusColor, textColor);
                }
                DrawText(frame, statusText, new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
                currentY += lineHeight * 1.3f;

                // Available and Required Thrust (compact, same row)
                DrawText(frame, $"Av: {FormatThrust(upThrust)}", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                DrawText(frame, $"Rq: {FormatThrust(requiredThrust)}", new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                currentY += lineHeight;

                // Info row (direction and gravity)
                DrawText(frame, $"Up Dir: {GetShortDir(bestUpDir)}", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                if (Cfg.UsePercentages)
                {
                    // Display gravity as percentage of Earth gravity
                    float gravityPercent = (gravityStrength / 9.81f) * 100f;
                    DrawText(frame, $"G: {gravityPercent:F0}%", new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                }
                else
                {
                    DrawText(frame, $"G: {gravityStrength:F2}", new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                }
                currentY += lineHeight;

                // Accel row
                string accelSign = netAccel >= 0 ? "+" : "";
                DrawText(frame, $"Accel: {accelSign}{netAccel:F2} m/s²", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                currentY += lineHeight;
            }
            else
            {
                // Zero-G: Show thrust balance with colors
                // Find min/max thrust for balance info
                float minDirThrust = float.MaxValue;
                float maxDirThrust = 0f;
                
                foreach (var kvp in thrustByDirection)
                {
                    if (kvp.Value < minDirThrust) minDirThrust = kvp.Value;
                    if (kvp.Value > maxDirThrust) maxDirThrust = kvp.Value;
                }
                
                // Calculate thrust balance
                float balance = minDirThrust / Math.Max(maxDirThrust, 0.001f);
                Color balanceColor = ColorGood;
                if (Cfg.UseColors)
                {
                    if (balance < 0.3f) balanceColor = ColorDanger;
                    else if (balance < 0.6f) balanceColor = ColorWarning;
                }
                
                // Mass and Balance
                DrawText(frame, $"Mass: {gridMass:N0} kg", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                DrawText(frame, $"Bal: {(balance * 100):F0}%", new Vector2(rightX, currentY), Cfg.TextSize, balanceColor, TextAlignment.RIGHT);
                currentY += lineHeight * 1.3f;

                // Table header
                float col1X = leftX;
                float col2X = leftX + 80 * Cfg.TextSize;
                float col3X = rightX - 80 * Cfg.TextSize;
                
                DrawText(frame, "Dir", new Vector2(col1X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
                if (Cfg.UsePercentages)
                {
                    DrawText(frame, "% Max", new Vector2(col3X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.RIGHT);
                }
                else
                {
                    DrawText(frame, "Thrust", new Vector2(col2X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
                    DrawText(frame, "Accel", new Vector2(col3X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.RIGHT);
                }
                currentY += lineHeight * 0.9f;

                // Direction rows
                foreach (var kvp in thrustByDirection)
                {
                    if (kvp.Value > 0 || Cfg.ShowAllDirections)
                    {
                        float percentage = (kvp.Value / Math.Max(maxDirThrust, 0.001f)) * 100f;
                        float accel = kvp.Value / gridMass;
                        
                        // Color based on how close to max thrust
                        Color dirColor = textColor;
                        if (Cfg.UseColors)
                        {
                            if (percentage < 40f) dirColor = ColorDanger;
                            else if (percentage < 70f) dirColor = ColorWarning;
                            else dirColor = ColorGood;
                        }
                        
                        DrawText(frame, GetShortDir(kvp.Key), new Vector2(col1X, currentY), Cfg.TextSize, dirColor, TextAlignment.LEFT);
                        
                        if (Cfg.UsePercentages)
                        {
                            DrawText(frame, $"{percentage:F0}%", new Vector2(col3X, currentY), Cfg.TextSize, dirColor, TextAlignment.RIGHT);
                        }
                        else
                        {
                            DrawText(frame, FormatThrust(kvp.Value), new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                            DrawText(frame, $"{accel:F1} m/s²", new Vector2(col3X, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                        }
                        
                        currentY += lineHeight;
                    }
                }
            }

            frame.Dispose();
        }

        string CreateASCIIBar(float ratio, int length, out int filledCount)
        {
            ratio = Math.Max(0, Math.Min(ratio, 1.0f));
            filledCount = (int)(ratio * length);
            StringBuilder bar = new StringBuilder();
            bar.Append('[');
            for (int i = 0; i < length; i++)
            {
                bar.Append(i < filledCount ? '|' : '\'');
            }
            bar.Append(']');
            return bar.ToString();
        }

        int CalculateBarLength(float barStartX, float valueEndX, float scale)
        {
            // Calculate available space for bar
            float charWidth = 10 * scale; // Approximate character width for Debug font
            float availableSpace = valueEndX - barStartX - (20 * scale); // Reserve space for value text
            
            // Calculate max characters that fit (subtract 2 for brackets)
            int maxLength = (int)((availableSpace / charWidth) - 2);
            
            // Clamp to reasonable range
            return Math.Max(8, Math.Min(20, maxLength));
        }

        void DrawColoredBar(MySpriteDrawFrame frame, float ratio, int length, Vector2 position, float scale, Color filledColor, Color emptyColor)
        {
            ratio = Math.Max(0, Math.Min(ratio, 1.0f));
            int filled = (int)(ratio * length);
            
            // Calculate character width for Debug font (approximate)
            float charWidth = 10 * scale;
            
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

        string GetShortDir(Base6Directions.Direction dir)
        {
            switch (dir)
            {
                case Base6Directions.Direction.Forward: return "Fwd";
                case Base6Directions.Direction.Backward: return "Bck";
                case Base6Directions.Direction.Left: return "Lft";
                case Base6Directions.Direction.Right: return "Rgt";
                case Base6Directions.Direction.Up: return "Up";
                case Base6Directions.Direction.Down: return "Dwn";
                default: return dir.ToString();
            }
        }

        string FormatThrust(float thrust)
        {
            if (thrust >= 1000000)
                return (thrust / 1000000).ToString("F2") + " MN";
            else if (thrust >= 1000)
                return (thrust / 1000).ToString("F2") + " kN";
            else
                return thrust.ToString("F2") + " N";
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

                var text = MySprite.CreateText($"ERROR: {e.Message}\n\nPlease report to mod author.", "Debug", Color.Red, 0.7f, TextAlignment.LEFT);
                text.Position = screenCorner + new Vector2(16, 16);
                frame.Add(text);

                frame.Dispose();
            }
            catch (Exception e2)
            {
                MyLog.Default.WriteLineAndConsole($"Failed to draw error: {e2.Message}\n{e2.StackTrace}");
            }
        }
    }
}
