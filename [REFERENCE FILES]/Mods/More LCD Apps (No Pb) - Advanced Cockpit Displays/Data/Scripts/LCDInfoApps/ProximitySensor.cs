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
    [MyTextSurfaceScript("ProximitySensor", "[!] Proximity Sensor")]
    public class ProximitySensor : MyTSSCommon
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

        public ProximitySensor(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
            public float TextSize = 0.7f;
            public float ViewPortOffsetX = 8f;
            public float ViewPortOffsetY = 8f;
            public bool ShowBars = true;
            public bool UseColors = true;
            public float WarnDistance = 100f;
            public float DangerDistance = 50f;
            public float RaycastRange = 500f;
            public string RaycastDirection = "Forward"; // Forward, Backward, Left, Right, Up, Down
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[ProxSensor]"))
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
                if (trimmed == "[ProxSensor]")
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
                        case "WarnDistance": float.TryParse(valueStr, out Cfg.WarnDistance); break;
                        case "DangerDistance": float.TryParse(valueStr, out Cfg.DangerDistance); break;
                        case "RaycastRange": float.TryParse(valueStr, out Cfg.RaycastRange); break;
                        case "RaycastDirection": Cfg.RaycastDirection = valueStr; break;
                    }
                }
                catch { }
            }
        }

        void WriteDefaultConfig()
        {
            StringBuilder sb = new StringBuilder();
            string existing = TerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[ProxSensor]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[ProxSensor]");
            sb.AppendLine("TextSize=0.7");
            sb.AppendLine("ViewPortOffsetX=8");
            sb.AppendLine("ViewPortOffsetY=8");
            sb.AppendLine("ShowBars=true");
            sb.AppendLine("UseColors=true");
            sb.AppendLine("WarnDistance=100");
            sb.AppendLine("DangerDistance=20");
            sb.AppendLine("RaycastRange=500");
            sb.AppendLine("RaycastDirection=Forward // Forward, Backward, Left, Right, Up, Down");

            TerminalBlock.CustomData = sb.ToString();
        }

        void Draw()
        {
            if (Grid?.Physics == null)
                return;

            // Parse raycast direction
            Base6Directions.Direction rayDir = Base6Directions.Direction.Forward;
            try
            {
                rayDir = (Base6Directions.Direction)Enum.Parse(typeof(Base6Directions.Direction), Cfg.RaycastDirection, true);
            }
            catch
            {
                rayDir = Base6Directions.Direction.Forward;
            }

            // Raycast for obstacle detection
            float obstacleDistance = 0f;
            bool hasDetection = false;

            // Use the block's own orientation for more reliable direction
            Vector3D rayDirection = TerminalBlock.WorldMatrix.GetDirectionVector(rayDir);
            Vector3D blockPos = TerminalBlock.GetPosition();

            // Find the furthest block in the raycast direction (including subgrids)
            double maxDistance = 0;
            List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Physical, grids);

            foreach (var grid in grids)
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);

                foreach (var block in blocks)
                {
                    Vector3D blockWorldPos = grid.GridIntegerToWorld(block.Position);
                    Vector3D toBlock = blockWorldPos - blockPos;

                    // Project this block's position onto the raycast direction
                    double projection = Vector3D.Dot(toBlock, rayDirection);

                    // Only consider blocks in the positive direction
                    if (projection > maxDistance)
                    {
                        maxDistance = projection;
                    }
                }
            }

            // Start raycast from beyond the furthest block edge
            double edgeOffset = maxDistance;
            Vector3D rayStart = blockPos + (rayDirection * edgeOffset);
            Vector3D rayEnd = rayStart + (rayDirection * Cfg.RaycastRange);

            IHitInfo hitInfo;
            if (MyAPIGateway.Physics.CastRay(rayStart, rayEnd, out hitInfo))
            {
                if (hitInfo.HitEntity != null && hitInfo.HitEntity != Grid)
                {
                    hasDetection = true;
                    // Measure distance and subtract 0.5m to prevent raycast from penetrating through obstacle
                    float rawDistance = (float)Vector3D.Distance(rayStart, hitInfo.Position);
                    // Clamp to minimum 0.1m so it never goes negative
                    obstacleDistance = Math.Max(0f, rawDistance);
                }
            }

            // Determine status
            Color statusColor = Surface.ScriptForegroundColor;
            string statusText = "NO TARGET";
            float barRatio = 0f;

            if (hasDetection && Cfg.UseColors)
            {
                if (obstacleDistance < Cfg.DangerDistance)
                {
                    statusColor = ColorDanger;
                    statusText = "DANGER";
                    barRatio = obstacleDistance / Cfg.DangerDistance * 0.33f;
                }
                else if (obstacleDistance < Cfg.WarnDistance)
                {
                    statusColor = ColorWarning;
                    statusText = "WARNING";
                    float warningRange = Cfg.WarnDistance - Cfg.DangerDistance;
                    float distInRange = obstacleDistance - Cfg.DangerDistance;
                    barRatio = 0.33f + (distInRange / warningRange * 0.33f);
                }
                else
                {
                    statusColor = ColorGood;
                    statusText = "SAFE";
                    barRatio = 0.66f + Math.Min((obstacleDistance - Cfg.WarnDistance) / Cfg.WarnDistance * 0.34f, 0.34f);
                }
            }

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

            // Get terminal system for resource info
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);

            // Check for hydrogen thrusters
            bool hasHydroThrusters = false;
            List<IMyThrust> thrusters = new List<IMyThrust>();
            if (gts != null)
            {
                gts.GetBlocksOfType(thrusters, t => t.CubeGrid == Grid && t.IsFunctional);
                foreach (var thruster in thrusters)
                {
                    var subtypeId = thruster.BlockDefinition.SubtypeId;
                    if (subtypeId.Contains("Hydrogen") || subtypeId.Contains("Hydro"))
                    {
                        hasHydroThrusters = true;
                        break;
                    }
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
            currentY += lineHeight * 1.3f;

            // Distance display - show <1m when at minimum to indicate very close
            string distanceText;
            if (!hasDetection)
            {
                distanceText = "--- m";
            }
            else
            {
                distanceText = $"{obstacleDistance:F0} m";
            }
            DrawText(frame, distanceText, new Vector2(centerX, currentY), Cfg.TextSize * 1.4f, statusColor, TextAlignment.CENTER);
            currentY += lineHeight * 1.8f;

            // Bar - dynamic width based on viewport and font size
            if (Cfg.ShowBars)
            {
                // Calculate how many characters fit in the available width
                // Debug font: ~6 pixels per character effective width after scaling
                float charWidth = 6 * Cfg.TextSize * 1.2f; // Tighter estimate
                float availableWidth = screenSize.X - (offset.X * 2);
                int barLength = (int)(availableWidth / charWidth);
                barLength = Math.Max(15, Math.Min(barLength, 120)); // Clamp to reasonable range

                DrawColoredBar(frame, barRatio, barLength, new Vector2(leftX, currentY), Cfg.TextSize * 1.2f, statusColor, textColor);
                currentY += lineHeight * 1.8f;
            }
            else
            {
                currentY += lineHeight;
            }

            // Three column headers (smaller, centered)
            float col1X = centerX - (screenSize.X * 0.3f);
            float col2X = centerX;
            float col3X = centerX + (screenSize.X * 0.3f);

            DrawText(frame, "Direction", new Vector2(col1X, currentY), Cfg.TextSize * 0.7f, textColor, TextAlignment.CENTER);
            DrawText(frame, "Range", new Vector2(col2X, currentY), Cfg.TextSize * 0.7f, textColor, TextAlignment.CENTER);
            DrawText(frame, "Status", new Vector2(col3X, currentY), Cfg.TextSize * 0.7f, textColor, TextAlignment.CENTER);
            currentY += lineHeight * 0.8f;

            // Direction, Range and Status values
            DrawText(frame, Cfg.RaycastDirection, new Vector2(col1X, currentY), Cfg.TextSize * 0.85f, textColor, TextAlignment.CENTER);
            DrawText(frame, $"{Cfg.RaycastRange:F0}m", new Vector2(col2X, currentY), Cfg.TextSize * 0.85f, textColor, TextAlignment.CENTER);
            DrawText(frame, statusText, new Vector2(col3X, currentY), Cfg.TextSize * 0.85f, statusColor, TextAlignment.CENTER);

            frame.Dispose();
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
