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
    [MyTextSurfaceScript("StopDistance", "[!] Stop Distance")]
    public class StopDistanceCalculator : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        private readonly IMyTerminalBlock TerminalBlock;
        private IMyCubeGrid Grid;
        private readonly List<IMyThrust> Thrusters = new List<IMyThrust>();
        
        private Config Cfg = new Config();
        private bool ConfigLoaded = false;
        private int ConfigCheckCounter = 0;
        private int FlashCounter = 0;

        private readonly Color ColorGood = new Color(0, 255, 100);
        private readonly Color ColorWarning = new Color(255, 200, 0);
        private readonly Color ColorDanger = new Color(255, 50, 50);

        public StopDistanceCalculator(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                FlashCounter++;
                
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
            public float ViewPortOffsetY = 8f;
            public bool ShowBars = true;
            public bool UseColors = true;
            public bool ShowProximityInfo = true;
            public float RaycastRange = 2000f;
            public float SafetyMultiplier = 2.0f;
        }

        void LoadConfig()
        {
            if (TerminalBlock == null) return;
            string customData = TerminalBlock.CustomData;

            if (string.IsNullOrWhiteSpace(customData) || !customData.Contains("[StopCalc]"))
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
                if (trimmed == "[StopCalc]")
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
                        case "ShowProximityInfo": bool.TryParse(valueStr, out Cfg.ShowProximityInfo); break;
                        case "RaycastRange": float.TryParse(valueStr, out Cfg.RaycastRange); break;
                        case "SafetyMultiplier": float.TryParse(valueStr, out Cfg.SafetyMultiplier); break;
                    }
                }
                catch { }
            }
        }

        void WriteDefaultConfig()
        {
            StringBuilder sb = new StringBuilder();
            string existing = TerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing) && !existing.Contains("[StopCalc]"))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("[StopCalc]");
            sb.AppendLine("TextSize=0.6");
            sb.AppendLine("ViewPortOffsetX=8");
            sb.AppendLine("ViewPortOffsetY=8");
            sb.AppendLine("ShowBars=true");
            sb.AppendLine("UseColors=true");
            sb.AppendLine("ShowProximityInfo=true");
            sb.AppendLine("RaycastRange=2000");
            sb.AppendLine("SafetyMultiplier=2.0");

            TerminalBlock.CustomData = sb.ToString();
        }

        void Draw()
        {
            if (Grid?.Physics == null)
                return;

            // Get ship controller
            IMyShipController controller = null;
            var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
            if (gts != null)
            {
                List<IMyShipController> controllers = new List<IMyShipController>();
                gts.GetBlocksOfType(controllers, c => c.CubeGrid == Grid && c.IsFunctional);
                if (controllers.Count > 0)
                    controller = controllers[0];
            }

            if (controller == null)
            {
                DrawMessage("NO SHIP CONTROLLER");
                return;
            }

            Vector3D velocity = controller.GetShipVelocities().LinearVelocity;
            float speed = (float)velocity.Length();
            bool dampenersOn = controller.DampenersOverride;

            // Get direction of travel
            string travelDirection = "None";
            Base6Directions.Direction travelDir = Base6Directions.Direction.Forward;
            
            if (speed > 0.1f)
            {
                Vector3D velocityNorm = Vector3D.Normalize(velocity);
                MatrixD gridMatrix = Grid.WorldMatrix;
                double bestDot = 0;

                foreach (Base6Directions.Direction dir in Enum.GetValues(typeof(Base6Directions.Direction)))
                {
                    Vector3D dirWorld = gridMatrix.GetDirectionVector(dir);
                    double dot = Math.Abs(Vector3D.Dot(dirWorld, velocityNorm));
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        travelDir = dir;
                    }
                }
                travelDirection = GetShortDir(travelDir);
            }

            // Get thrusters for stopping (opposite direction of travel)
            Base6Directions.Direction brakeDir = Base6Directions.GetOppositeDirection(travelDir);
            float brakeThrust = 0f;

            Thrusters.Clear();
            if (gts != null && speed > 0.1f)
            {
                gts.GetBlocksOfType(Thrusters, t => t.CubeGrid == Grid && t.IsFunctional);
                foreach (var thruster in Thrusters)
                {
                    Base6Directions.Direction thrustDir = Base6Directions.GetOppositeDirection(thruster.Orientation.Forward);
                    if (thrustDir == brakeDir)
                    {
                        brakeThrust += thruster.MaxEffectiveThrust;
                    }
                }
            }

            float gridMass = Grid.Physics.Mass;
            float deceleration = brakeThrust / Math.Max(gridMass, 1f);
            float stopTime = deceleration > 0.1f ? speed / deceleration : 0f;
            float stopDistance = deceleration > 0.1f ? (speed * speed) / (2f * deceleration) : 0f;

            // Raycast-based collision detection - exact and long range
            float obstacleDistance = 0f;
            bool obstacleDetected = false;
            
            if (controller != null && speed > 0.5f && Cfg.ShowProximityInfo)
            {
                Vector3D rayDirection = Vector3D.Normalize(velocity);
                
                // Start from controller position, but offset to edge of grid in travel direction
                Vector3D controllerPos = controller.GetPosition();
                BoundingBoxD gridBox = Grid.WorldAABB;
                
                // Calculate offset to grid edge in the direction of travel
                double gridRadius = gridBox.HalfExtents.Length();
                Vector3D rayStart = controllerPos + (rayDirection * gridRadius);
                
                // Cast ray in direction of travel
                float raycastRange = Math.Min(Math.Max(stopDistance * Cfg.SafetyMultiplier, 100f), Cfg.RaycastRange);
                Vector3D rayEnd = rayStart + (rayDirection * raycastRange);
                
                IHitInfo hitInfo;
                if (MyAPIGateway.Physics.CastRay(rayStart, rayEnd, out hitInfo))
                {
                    // Make sure we didn't hit our own grid
                    if (hitInfo.HitEntity != Grid)
                    {
                        obstacleDetected = true;
                        // Calculate distance from controller position (more intuitive)
                        obstacleDistance = (float)Vector3D.Distance(controllerPos, hitInfo.Position) - (float)gridRadius;
                    }
                }
            }

            // Check for collision danger
            bool collisionDanger = obstacleDetected && obstacleDistance < stopDistance && speed > 1f;

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

            if (collisionDanger)
            {
                // DANGER MODE - Large flashing warning
                bool flash = (FlashCounter / 3) % 2 == 0;
                Color warningColor = flash ? ColorDanger : Surface.ScriptBackgroundColor;

                DrawText(frame, "!! COLLISION WARNING !!", new Vector2(centerX, currentY), Cfg.TextSize * 1.3f, warningColor, TextAlignment.CENTER);
                currentY += lineHeight * 1.5f;

                DrawText(frame, $"Speed: {speed:F1} m/s  Dir: {travelDirection}", new Vector2(centerX, currentY), Cfg.TextSize, textColor, TextAlignment.CENTER);
                currentY += lineHeight * 1.2f;

                DrawText(frame, $"Obstacle: {obstacleDistance:F0} m", new Vector2(leftX, currentY), Cfg.TextSize * 1.1f, ColorDanger, TextAlignment.LEFT);
                currentY += lineHeight * 1.8f;

                float timeToImpact = obstacleDistance / Math.Max(speed, 0.1f);
                DrawText(frame, $"IMPACT IN {timeToImpact:F1} SEC", new Vector2(centerX, currentY), Cfg.TextSize * 1.2f, warningColor, TextAlignment.CENTER);
                currentY += lineHeight * 1.5f;

                if (Cfg.ShowBars)
                {
                    // Calculate how many characters fit in the available width
                    float charWidth = 6 * Cfg.TextSize; // Tighter estimate for Debug font
                    float availableWidth = screenSize.X - (offset.X * 2);
                    int barLength = (int)(availableWidth / charWidth);
                    barLength = Math.Max(15, Math.Min(barLength, 150)); // Clamp to reasonable range
                    
                    DrawColoredBar(frame, 1.0f, barLength, new Vector2(leftX, currentY), Cfg.TextSize, ColorDanger, textColor);
                    currentY += lineHeight * 1.5f;
                }
            }
            else
            {
                // NORMAL MODE - Compact layout
                
                // Speed & Direction row
                DrawText(frame, $"Speed: {speed:F1} m/s", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                Color dampColor = dampenersOn ? ColorGood : ColorDanger;
                string dampText = dampenersOn ? "DAMP ON" : "DAMP OFF";
                DrawText(frame, dampText, new Vector2(rightX, currentY), Cfg.TextSize, dampColor, TextAlignment.RIGHT);
                currentY += lineHeight;

                DrawText(frame, $"Dir: {travelDirection}", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                DrawText(frame, $"Decel: {deceleration:F1} m/s²", new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                currentY += lineHeight * 1.3f;

                DrawText(frame, $"Time: {stopTime:F1} sec", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                DrawText(frame, $"Distance: {stopDistance:F0} m", new Vector2(rightX, currentY), Cfg.TextSize, textColor, TextAlignment.RIGHT);
                currentY += lineHeight * 0.6f;

                // Proximity info (Raycast)
                if (Cfg.ShowProximityInfo)
                {
                    DrawText(frame, "________________________________________________", new Vector2(leftX, currentY), Cfg.TextSize * 0.9f, textColor, TextAlignment.LEFT);
                    currentY += lineHeight;

                    if (obstacleDetected)
                    {
                        float safeDistance = stopDistance * Cfg.SafetyMultiplier;
                        float margin = obstacleDistance - stopDistance;
                        float marginPercent = safeDistance > 0 ? (margin / safeDistance) * 100f : 0f;

                        Color statusColor = ColorGood;
                        string statusText = "SAFE";
                        float barRatio = 0.5f;

                        if (Cfg.UseColors)
                        {
                            if (obstacleDistance < stopDistance)
                            {
                                statusColor = ColorDanger;
                                statusText = "DANGER";
                                barRatio = 0f;
                            }
                            else if (marginPercent < 50)
                            {
                                statusColor = ColorWarning;
                                statusText = "WARNING";
                                barRatio = marginPercent / 100f;
                            }
                            else
                            {
                                statusColor = ColorGood;
                                statusText = "SAFE";
                                barRatio = 0.5f + Math.Min((marginPercent - 50f) / 100f * 0.5f, 0.5f);
                            }
                        }

                        DrawText(frame, $"Obstacle: {obstacleDistance:F0} m", new Vector2(leftX, currentY), Cfg.TextSize, textColor, TextAlignment.LEFT);
                        DrawText(frame, statusText, new Vector2(rightX, currentY), Cfg.TextSize, statusColor, TextAlignment.RIGHT);
                        currentY += lineHeight;

                        if (Cfg.ShowBars)
                        {
                            // Calculate how many characters fit in the available width
                            float charWidth = 6 * Cfg.TextSize; // Tighter estimate for Debug font
                            float availableWidth = screenSize.X - (offset.X * 2);
                            int barLength = (int)(availableWidth / charWidth);
                            barLength = Math.Max(15, Math.Min(barLength, 150)); // Clamp to reasonable range
                            
                            DrawColoredBar(frame, barRatio, barLength, new Vector2(leftX, currentY), Cfg.TextSize, statusColor, textColor);
                            currentY += lineHeight;
                        }
                    }
                    else
                    {
                        DrawText(frame, "No obstacles detected", new Vector2(leftX, currentY), Cfg.TextSize, ColorGood, TextAlignment.LEFT);
                        currentY += lineHeight;
                        DrawText(frame, $"Scan range: {Cfg.RaycastRange:F0} m", new Vector2(leftX, currentY), Cfg.TextSize * 0.8f, textColor, TextAlignment.LEFT);
                    }
                }
            }

            frame.Dispose();
        }

        string GetShortDir(Base6Directions.Direction dir)
        {
            switch (dir)
            {
                case Base6Directions.Direction.Forward: return "FWD";
                case Base6Directions.Direction.Backward: return "BCK";
                case Base6Directions.Direction.Left: return "LFT";
                case Base6Directions.Direction.Right: return "RGT";
                case Base6Directions.Direction.Up: return "UP";
                case Base6Directions.Direction.Down: return "DWN";
                default: return "None";
            }
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
