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
    [MyTextSurfaceScript("ThrustVectorBalance", "[!] Thrust Balance")]
    public class ThrustVectorBalance : MyTSSCommon
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

        public ThrustVectorBalance(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            public float ViewPortOffsetX = 8f;
            public float ViewPortOffsetY = 8f;
            public bool ShowBars = true;
            public bool ShowThrustValues = true;
            public bool UseColors = true;
            public bool IncludeAtmospheric = true;
            public bool IncludeIon = true;
            public bool IncludeHydrogen = true;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[ThrustVector]"))
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
                if (trimmed == "[ThrustVector]")
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
                        case "ShowThrustValues": bool.TryParse(valueStr, out Cfg.ShowThrustValues); break;
                        case "UseColors": bool.TryParse(valueStr, out Cfg.UseColors); break;
                        case "IncludeAtmospheric": bool.TryParse(valueStr, out Cfg.IncludeAtmospheric); break;
                        case "IncludeIon": bool.TryParse(valueStr, out Cfg.IncludeIon); break;
                        case "IncludeHydrogen": bool.TryParse(valueStr, out Cfg.IncludeHydrogen); break;
                    }
                }
                catch { }
            }
        }

        void WriteDefaultConfig()
        {
            StringBuilder sb = new StringBuilder();
            string existing = TerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[ThrustVector]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[ThrustVector]");
            sb.AppendLine("TextSize=0.5");
            sb.AppendLine("ViewPortOffsetX=8");
            sb.AppendLine("ViewPortOffsetY=8");
            sb.AppendLine("ShowBars=true");
            sb.AppendLine("ShowThrustValues=true");
            sb.AppendLine("UseColors=true");
            sb.AppendLine("IncludeAtmospheric=true");
            sb.AppendLine("IncludeIon=true");
            sb.AppendLine("IncludeHydrogen=true");

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

            // Get thrusters
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
            Thrusters.Clear();
            if (gts != null)
            {
                gts.GetBlocksOfType(Thrusters, t =>
                {
                    if (t.CubeGrid != Grid) return false;

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

            // Find max/min thrust
            float maxThrust = 0f;
            float minThrust = float.MaxValue;
            Base6Directions.Direction weakestDir = Base6Directions.Direction.Forward;

            foreach (var kvp in thrustByDirection)
            {
                if (kvp.Value > maxThrust)
                    maxThrust = kvp.Value;
                if (kvp.Value < minThrust)
                {
                    minThrust = kvp.Value;
                    weakestDir = kvp.Key;
                }
            }

            float balance = maxThrust > 0 ? (minThrust / maxThrust * 100f) : 100f;
            float imbalance = 100f - balance;

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
                DrawText(frame, $"H2: {hydroPercent:F0}%", new Vector2(leftX, currentY), Cfg.TextSize * 0.9f, hydroColor, TextAlignment.LEFT);
                DrawText(frame, $"PWR: {batteryPercent:F0}%", new Vector2(rightX, currentY), Cfg.TextSize * 0.9f, battColor, TextAlignment.RIGHT);
            }
            else
            {
                DateTime now = DateTime.Now;
                string timeStr = now.ToString("HH:mm:ss");
                Color battColor = batteryPercent < 20 ? ColorDanger : (batteryPercent < 50 ? ColorWarning : ColorGood);
                DrawText(frame, timeStr, new Vector2(leftX, currentY), Cfg.TextSize * 0.9f, textColor, TextAlignment.LEFT);
                DrawText(frame, $"PWR: {batteryPercent:F0}%", new Vector2(rightX, currentY), Cfg.TextSize * 0.9f, battColor, TextAlignment.RIGHT);
            }
            currentY += lineHeight * 0.7f;
            DrawText(frame, "________________________________________________", new Vector2(centerX, currentY), Cfg.TextSize, textColor, TextAlignment.CENTER);
            currentY += lineHeight * 1.2f;

            // Column setup: Dir | Max Mass | [Bar] | Thrust
            // Columns are: col1X (Dir), col2X (Max Mass), col3X (Bar start), col4X (Value start), col5X (Value end/right align)
            float col1X = leftX;
            float col2X = leftX + 60 * Cfg.TextSize;
            
            // Calculate column positions based on what's visible
            float col3X = 0f; // Bar column start
            float col4X = 0f; // Value column start
            float col5X = rightX; // Right edge for value alignment
            
            if (Cfg.ShowBars && Cfg.ShowThrustValues)
            {
                // Both bars and values: split remaining space, more spacing before bar
                float remainingWidth = screenSize.X - (col2X - screenCorner.X) - offset.X - 180 * Cfg.TextSize;
                col3X = col2X + 160 * Cfg.TextSize; // Increased spacing from 120 to 160
                col4X = col3X + remainingWidth * 0.6f;
            }
            else if (Cfg.ShowBars)
            {
                // Only bars: use more space, more spacing
                col3X = col2X + 160 * Cfg.TextSize; // Increased spacing from 120 to 160
            }
            else if (Cfg.ShowThrustValues)
            {
                // Only values: position near right
                col4X = rightX - 120 * Cfg.TextSize;
            }

            DrawText(frame, "Dir", new Vector2(col1X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
            DrawText(frame, "Max Mass", new Vector2(col2X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
            
            if (Cfg.ShowBars)
            {
                DrawText(frame, "", new Vector2(col3X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
            }
            if (Cfg.ShowThrustValues)
            {
                DrawText(frame, "Thrust", new Vector2(col5X, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.RIGHT);
            }
            currentY += lineHeight;

            // Direction rows
            Base6Directions.Direction[] displayOrder = new Base6Directions.Direction[]
            {
                Base6Directions.Direction.Forward,
                Base6Directions.Direction.Backward,
                Base6Directions.Direction.Left,
                Base6Directions.Direction.Right,
                Base6Directions.Direction.Up,
                Base6Directions.Direction.Down
            };

            // Calculate bar length once for consistency (all bars same total length)
            // Use dynamic calculation based on available column space
            float barAvailableWidth = Cfg.ShowThrustValues ? (col4X - col3X - 20 * Cfg.TextSize) : (screenSize.X - (col3X - screenCorner.X) - offset.X - 60 * Cfg.TextSize);
            float charWidth = 6 * Cfg.TextSize; // Tighter estimate for Debug font
            int barLength = (int)(barAvailableWidth / charWidth);
            barLength = Math.Max(10, Math.Min(barLength, 150));

            foreach (var dir in displayOrder)
            {
                float thrust = thrustByDirection[dir];
                float maxMass = gravityStrength > 0.01f ? thrust / gravityStrength : 0f;
                float barRatio = maxThrust > 0 ? thrust / maxThrust : 0f;

                // Determine color based on thrust percentage (barRatio)
                // 0% = base color, 1-20% = red, 21-50% = yellow, 51-100% = base color
                Color rowColor = textColor;
                if (Cfg.UseColors && barRatio > 0f && barRatio < 0.5f)
                {
                    if (barRatio <= 0.2f)
                        rowColor = ColorDanger; // Red for 0-20%
                    else
                        rowColor = ColorWarning; // Yellow for 21-50%
                }
                // If barRatio is 0, use base color (not colored)

                // Direction label - always base color
                DrawText(frame, GetShortDir(dir), new Vector2(col1X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);

                // Max Mass column with row color
                if (gravityStrength > 0.01f)
                {
                    DrawText(frame, $"{maxMass:N0} kg", new Vector2(col2X, currentY), Cfg.TextSize, rowColor, TextAlignment.LEFT);
                }
                else
                {
                    DrawText(frame, "---", new Vector2(col2X, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                }

                // Bar column (if enabled) with row color - all bars same length
                if (Cfg.ShowBars)
                {
                    DrawColoredBar(frame, barRatio, barLength, new Vector2(col3X, currentY), Cfg.TextSize, rowColor, textColor);
                }

                // Thrust value column (if enabled) with row color
                if (Cfg.ShowThrustValues)
                {
                    DrawText(frame, FormatThrust(thrust), new Vector2(col5X, currentY), Cfg.TextSize, rowColor, TextAlignment.RIGHT);
                }

                currentY += lineHeight;
            }

            // Balance summary - back to single line
            currentY += lineHeight * 0.3f;
            Color balanceColor = ColorGood;
            if (Cfg.UseColors)
            {
                if (imbalance > 30) balanceColor = ColorDanger;
                else if (imbalance > 15) balanceColor = ColorWarning;
            }

            string balanceText = $"Balance: {balance:F0}%  Weakest: {GetShortDir(weakestDir)} (-{imbalance:F0}%)";
            DrawText(frame, balanceText, new Vector2(centerX, currentY), Cfg.TextSize, balanceColor, TextAlignment.CENTER);

            frame.Dispose();
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
                return (thrust / 1000000).ToString("F1") + " MN";
            else if (thrust >= 1000)
                return (thrust / 1000).ToString("F1") + " kN";
            else
                return thrust.ToString("F0") + " N";
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
