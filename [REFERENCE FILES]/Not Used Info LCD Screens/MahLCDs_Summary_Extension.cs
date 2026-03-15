using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace MahrianeIndustries.LCDInfo
{
    [MyTextSurfaceScript("LCDInfoScreenExtension", "$IOS LCD - Extension")]
    public class LCDExtension : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();
        public static string CONFIG_SECTION_ID = "SettingsExtension";

        string searchId = "";
        int surfaceIndex = 0;
        bool showHeader = true;

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;
        bool configError = false;

        public LCDExtension(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Dispose() { }

        void CreateConfig()
        {
            config.Clear();
            StringBuilder sb = new StringBuilder();
            
            // Preserve existing CustomData
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
            }

            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; SearchId: Exact name of parent LCD block to extend");
            sb.AppendLine($"SearchId={searchId}");
            sb.AppendLine($"SurfaceIndex={surfaceIndex}");
            sb.AppendLine($"ShowHeader={showHeader}");
            sb.AppendLine();
            sb.AppendLine("; This screen displays overflow content from the parent screen.");
            sb.AppendLine("; All other config options are inherited from the parent.");
            sb.AppendLine("; SurfaceIndex:");
            sb.AppendLine(";   Which LCD to read from parent block (0=first, 1=second, etc.)");
            sb.AppendLine(";   Use on block with multiple screens (cockpits!)");
            sb.AppendLine(";   Leave 0 for all other blocks");
            sb.AppendLine();

            myTerminalBlock.CustomData = sb.ToString();
        }

        void LoadConfig()
        {
            try
            {
                configError = false;
                MyIniParseResult result;

                if (config.TryParse(myTerminalBlock.CustomData, CONFIG_SECTION_ID, out result))
                {
                    if (config.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString();
                        searchId = string.IsNullOrWhiteSpace(searchId) ? "" : searchId.Trim();
                    }
                    else
                        configError = true;

                    if (config.ContainsKey(CONFIG_SECTION_ID, "SurfaceIndex"))
                        surfaceIndex = config.Get(CONFIG_SECTION_ID, "SurfaceIndex").ToInt32();
                    else
                        surfaceIndex = 0;

                    MahUtillities.TryGetConfigBool(config, CONFIG_SECTION_ID, "ShowHeader", ref showHeader, ref configError);
                }
                else
                {
                    configError = true;
                }
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine($"LCDExtension LoadConfig error: {e}");
                configError = true;
            }
        }

        public override void Run()
        {
            if (Sandbox.ModAPI.MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;

            if (myTerminalBlock.CustomData.Length <= 0 || !myTerminalBlock.CustomData.Contains(CONFIG_SECTION_ID))
                CreateConfig();

            LoadConfig();

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(10, 10) + myViewport.Position;

            try
            {
                if (configError)
                {
                    DrawText(ref myFrame, ref myPosition, "<< Config error. Please check CustomData >>");
                }
                else if (string.IsNullOrWhiteSpace(searchId))
                {
                    DrawText(ref myFrame, ref myPosition, "Extension Screen\n\nPlease configure SearchId to specify\nthe parent LCD block name.");
                }
                else
                {
                    // Find parent block
                    IMyTerminalBlock parentBlock = FindParentBlock(searchId);
                    
                    if (parentBlock == null)
                    {
                        DrawText(ref myFrame, ref myPosition, $"Unable to find parent block:\n'{searchId}'");
                    }
                    else if (parentBlock.EntityId == myTerminalBlock.EntityId)
                    {
                        DrawText(ref myFrame, ref myPosition, $"ERROR: Cannot extend itself!");
                    }
                    else if (!parentBlock.IsFunctional || !parentBlock.IsWorking)
                    {
                        DrawText(ref myFrame, ref myPosition, $"Parent LCD not operational");
                    }
                    else
                    {
                        // Get parent surface
                        IMyTextSurfaceProvider parentSurfaceProvider = parentBlock as IMyTextSurfaceProvider;
                        if (parentSurfaceProvider == null || surfaceIndex >= parentSurfaceProvider.SurfaceCount)
                        {
                            DrawText(ref myFrame, ref myPosition, $"Invalid parent surface");
                        }
                        else
                        {
                            IMyTextSurface parentSurface = parentSurfaceProvider.GetSurface(surfaceIndex) as IMyTextSurface;
                            string parentScript = parentSurface?.Script ?? "";
                            
                            if (string.IsNullOrWhiteSpace(parentScript) || !parentScript.StartsWith("LCDInfoScreen"))
                            {
                                DrawText(ref myFrame, ref myPosition, $"Parent not running InfoLCD app");
                            }
                            else
                            {
                                // Render extension content
                                RenderExtension(ref myFrame, ref myPosition, parentBlock, parentSurface, parentScript);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine($"LCDExtension Run error: {e}");
                DrawText(ref myFrame, ref myPosition, $"Error:\n{e.Message}");
            }

            myFrame.Dispose();
        }

        IMyTerminalBlock FindParentBlock(string name)
        {
            try
            {
                var myCubeGrid = myTerminalBlock.CubeGrid;
                if (myCubeGrid == null) return null;

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(myCubeGrid).GetBlocksOfType<IMyTerminalBlock>(blocks);

                foreach (var block in blocks)
                {
                    if (block != null && block.CustomName != null && 
                        block.CustomName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return block;
                    }
                }
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine($"LCDExtension FindParentBlock error: {e}");
            }
            return null;
        }

        void RenderExtension(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTerminalBlock parentBlock, IMyTextSurface parentSurface, string parentScript)
        {
            try
            {
                // Follow Extension chain to root
                IMyTerminalBlock rootParent = parentBlock;
                IMyTextSurface rootSurface = parentSurface;
                string rootScript = parentScript;
                int chainDepth = 0;
                
                while (rootScript == "LCDInfoScreenExtension" && chainDepth < 10)
                {
                    chainDepth++;
                    string parentSearchId = GetExtensionSearchId(rootParent.CustomData);
                    if (string.IsNullOrWhiteSpace(parentSearchId)) break;

                    IMyTerminalBlock nextParent = FindParentBlock(parentSearchId);
                    if (nextParent == null || !nextParent.IsFunctional || !nextParent.IsWorking) break;

                    IMyTextSurfaceProvider nextProvider = nextParent as IMyTextSurfaceProvider;
                    if (nextProvider == null || surfaceIndex >= nextProvider.SurfaceCount) break;

                    IMyTextSurface nextSurface = nextProvider.GetSurface(surfaceIndex) as IMyTextSurface;
                    if (nextSurface == null) break;

                    rootParent = nextParent;
                    rootSurface = nextSurface;
                    rootScript = nextSurface.Script ?? "";
                }

                // Detect screen type
                string parentCustomData = rootParent.CustomData ?? "";
                string screenType = DetectScreenType(parentCustomData, rootScript);

                if (string.IsNullOrWhiteSpace(screenType))
                {
                    DrawText(ref frame, ref position, "Unable to detect parent screen type");
                    return;
                }

                // Get parent config values for accurate line calculation
                float parentTextSize = GetParentTextSize(parentCustomData);
                float parentViewPortOffsetY = GetParentViewportOffsetY(parentCustomData);
                bool parentShowHeader = GetParentShowHeader(parentCustomData);
                
                // Calculate exact lines that fit on parent screen
                float lineHeight = 30 * parentTextSize;
                float parentHeight = rootSurface.SurfaceSize.Y;
                
                // Account for top margin only (parent renders until it hits bottom of viewport)
                float usableHeight = parentHeight - parentViewPortOffsetY;
                int totalLinesPerScreen = (int)(usableHeight / lineHeight);
                
                // Subtract header lines if parent shows header
                int headerLines = parentShowHeader ? 2 : 0; // Header typically takes 2 lines
                int dataLinesPerScreen = Math.Max(1, totalLinesPerScreen - headerLines);
                
                // Calculate skip lines (all previous screens in chain)
                int skipLines = dataLinesPerScreen * (chainDepth + 1) + headerLines;

                // Collect all data lines from parent screen
                List<string> allLines = GetScreenDataLines(screenType, rootParent, parentCustomData);

                // Render paginated lines with debug info
                RenderPaginatedLines(ref frame, ref position, allLines, skipLines, dataLinesPerScreen, screenType, chainDepth, 
                    totalLinesPerScreen, headerLines, parentTextSize, parentHeight, parentViewPortOffsetY);
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine($"LCDExtension RenderExtension error: {e}");
                DrawText(ref frame, ref position, $"Render error:\n{e.Message}");
            }
        }

        List<string> GetScreenDataLines(string screenType, IMyTerminalBlock parentBlock, string parentConfig)
        {
            List<string> lines = new List<string>();

            try
            {
                // Route to screen-specific data collector
                switch (screenType)
                {
                    case "Items":
                        lines = GetItemsLines(parentBlock, parentConfig);
                        break;
                    case "Power":
                        lines = GetPowerLines(parentBlock, parentConfig);
                        break;
                    case "Components":
                        lines = GetComponentsLines(parentBlock, parentConfig);
                        break;
                    case "Ingots":
                        lines = GetIngotsLines(parentBlock, parentConfig);
                        break;
                    case "Ores":
                        lines = GetOresLines(parentBlock, parentConfig);
                        break;
                    // TODO: Add more screen types as needed
                    default:
                        lines.Add($"Screen type '{screenType}' not yet supported");
                        lines.Add("Coming soon!");
                        break;
                }
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine($"LCDExtension GetScreenDataLines error for {screenType}: {e}");
                lines.Add($"Error collecting {screenType} data");
            }

            return lines;
        }

        List<string> GetItemsLines(IMyTerminalBlock parentBlock, string parentConfig)
        {
            List<string> lines = new List<string>();
            
            try
            {
                // Parse parent config
                MyIni ini = new MyIni();
                ini.TryParse(parentConfig);
                
                string searchId = ini.Get("SettingsItemsSummary", "SearchId").ToString("*");
                bool useSubtypeId = ini.Get("SettingsItemsSummary", "UseSubtypeId").ToBoolean(false);
                bool showSubgrids = ini.Get("SettingsItemsSummary", "ShowSubgrids").ToBoolean(false);
                int minVisibleAmount = ini.Get("SettingsItemsSummary", "MinVisibleAmount").ToInt32(1);
                
                List<string> excludeIds = new List<string>();
                string[] excludeArray = ini.Get("SettingsItemsSummary", "ExcludeIds").ToString("").Split(',');
                foreach (string s in excludeArray)
                {
                    string t = s.Trim();
                    if (!string.IsNullOrEmpty(t) && t != "*" && t.Length >= 3)
                        excludeIds.Add(t);
                }

                // Get blocks
                var myCubeGrid = parentBlock.CubeGrid as MyCubeGrid;
                if (myCubeGrid == null) return lines;

                Sandbox.ModAPI.Ingame.MyShipMass gridMass = new Sandbox.ModAPI.Ingame.MyShipMass();
                var blocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, showSubgrids);

                // Collect items
                Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();
                List<string> itemTypes = new List<string> { "PhysicalGunObject", "OxygenContainerObject", "GasContainerObject", 
                    "PhysicalObject", "ConsumableItem", "Package", "SeedItem" };

                foreach (var block in blocks)
                {
                    if (block == null) continue;
                    var entity = block as VRage.Game.ModAPI.IMyCubeBlock;
                    if (entity == null || !entity.HasInventory) continue;

                    for (int i = 0; i < entity.InventoryCount; i++)
                    {
                        var inv = entity.GetInventory(i);
                        if (inv == null) continue;

                        var invItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        inv.GetItems(invItems);

                        foreach (var item in invItems)
                        {
                            if (item == null) continue;
                            string typeId = item.Type.TypeId.Split('_')[1];
                            if (!itemTypes.Contains(typeId)) continue;

                            string subtypeId = item.Type.SubtypeId;
                            int amount = item.Amount.ToIntSafe();

                            if (items.ContainsKey(subtypeId))
                                items[subtypeId].amount += amount;
                            else
                                items.Add(subtypeId, new ItemData { amount = amount, typeId = typeId });
                        }
                    }
                }

                // Build item list with display names, then sort (matching main Items screen logic)
                var itemList = new List<ItemDisplayData>();
                
                foreach (var kvp in items)
                {
                    if (kvp.Value.amount < minVisibleAmount) continue;
                    
                    string displayName = kvp.Key; // Default to subtypeId
                    
                    // Look up display name from definitions using correct typeId
                    var def = MahDefinitions.GetDefinition(kvp.Value.typeId, kvp.Key);
                    if (def != null)
                        displayName = def.displayName;
                    
                    itemList.Add(new ItemDisplayData 
                    { 
                        subtypeId = kvp.Key, 
                        displayName = displayName, 
                        amount = kvp.Value.amount 
                    });
                }
                
                // Sort by displayName or subtypeId (matching Items screen behavior)
                var sortedItems = useSubtypeId 
                    ? itemList.OrderBy(x => x.subtypeId) 
                    : itemList.OrderBy(x => x.displayName);
                
                // Convert to lines
                foreach (var item in sortedItems)
                {
                    string nameToShow = useSubtypeId ? item.subtypeId : item.displayName;
                    lines.Add($"{nameToShow}: {item.amount:N0}");
                }
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine($"LCDExtension GetItemsLines error: {e}");
                lines.Add($"Error: {e.Message}");
            }

            return lines;
        }

        List<string> GetPowerLines(IMyTerminalBlock parentBlock, string parentConfig)
        {
            // TODO: Implement power data collection
            return new List<string> { "Power screen data collection coming soon..." };
        }

        List<string> GetComponentsLines(IMyTerminalBlock parentBlock, string parentConfig)
        {
            // TODO: Implement components data collection  
            return new List<string> { "Components screen data collection coming soon..." };
        }

        List<string> GetIngotsLines(IMyTerminalBlock parentBlock, string parentConfig)
        {
            // TODO: Implement ingots data collection
            return new List<string> { "Ingots screen data collection coming soon..." };
        }

        List<string> GetOresLines(IMyTerminalBlock parentBlock, string parentConfig)
        {
            // TODO: Implement ores data collection
            return new List<string> { "Ores screen data collection coming soon..." };
        }

        void RenderPaginatedLines(ref MySpriteDrawFrame frame, ref Vector2 position, List<string> allLines, int skipLines, int linesPerScreen, string screenType, int chainDepth,
            int totalLinesPerScreen, int headerLines, float parentTextSize, float parentHeight, float parentViewPortOffsetY)
        {
            StringBuilder sb = new StringBuilder();

            if (showHeader)
            {
                sb.AppendLine($"Extension (Page {chainDepth + 2})");
                sb.AppendLine($"Parent Type: {screenType}");
                sb.AppendLine();
                sb.AppendLine("=== DEBUG INFO ===");
                sb.AppendLine($"Parent Screen Height: {parentHeight:F1} px");
                sb.AppendLine($"Parent Top Margin: {parentViewPortOffsetY:F1} px");
                sb.AppendLine($"Parent TextSize: {parentTextSize:F2}");
                sb.AppendLine($"Line Height: {(30 * parentTextSize):F1} px");
                sb.AppendLine($"Total Lines Per Screen: {totalLinesPerScreen}");
                sb.AppendLine($"Header Lines: {headerLines}");
                sb.AppendLine($"Data Lines Per Screen: {linesPerScreen}");
                sb.AppendLine($"Total Data Lines Available: {allLines.Count}");
                sb.AppendLine($"Lines Skipped (prev screens): {skipLines}");
                sb.AppendLine($"Lines Displayed This Screen: {Math.Min(linesPerScreen, Math.Max(0, allLines.Count - skipLines))}");
                sb.AppendLine($"Lines Remaining: {Math.Max(0, allLines.Count - skipLines - linesPerScreen)}");
                sb.AppendLine();
                sb.AppendLine("=== OVERFLOW DATA ===");
            }

            int displayedLines = 0;
            for (int i = skipLines; i < allLines.Count && displayedLines < linesPerScreen; i++)
            {
                sb.AppendLine(allLines[i]);
                displayedLines++;
            }

            if (displayedLines == 0)
            {
                sb.AppendLine("No overflow content to display");
            }

            DrawText(ref frame, ref position, sb.ToString());
        }

        // Helper class to track item data with typeId
        class ItemData
        {
            public int amount;
            public string typeId;
        }
        
        class ItemDisplayData
        {
            public string subtypeId;
            public string displayName;
            public int amount;
        }

        float GetParentTextSize(string customData)
        {
            MyIni ini = new MyIni();
            ini.TryParse(customData);
            
            string[] sections = { "SettingsPowerStatus", "SettingsItemsSummary", "SettingsWeapons", "SettingsComponentsInventory" };
            foreach (string section in sections)
            {
                if (ini.ContainsKey(section, "TextSize"))
                    return ini.Get(section, "TextSize").ToSingle(0.4f);
            }
            return 0.4f;
        }

        float GetParentViewportOffsetY(string customData)
        {
            MyIni ini = new MyIni();
            ini.TryParse(customData);
            
            string[] sections = { "SettingsPowerStatus", "SettingsItemsSummary", "SettingsWeapons", "SettingsComponentsInventory",
                "SettingsIngotsInventory", "SettingsOresInventory", "SettingsAmmoStatus", "SettingsCargoStatus" };
            foreach (string section in sections)
            {
                if (ini.ContainsKey(section, "ViewPortOffsetY"))
                    return ini.Get(section, "ViewPortOffsetY").ToSingle(10f);
            }
            return 10f;
        }

        bool GetParentShowHeader(string customData)
        {
            MyIni ini = new MyIni();
            ini.TryParse(customData);
            
            string[] sections = { "SettingsPowerStatus", "SettingsItemsSummary", "SettingsWeapons", "SettingsComponentsInventory",
                "SettingsIngotsInventory", "SettingsOresInventory", "SettingsAmmoStatus", "SettingsCargoStatus" };
            foreach (string section in sections)
            {
                if (ini.ContainsKey(section, "ShowHeader"))
                    return ini.Get(section, "ShowHeader").ToBoolean(true);
            }
            return true;
        }

        string GetExtensionSearchId(string customData)
        {
            try
            {
                MyIni ini = new MyIni();
                if (ini.TryParse(customData, CONFIG_SECTION_ID))
                {
                    if (ini.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                        return ini.Get(CONFIG_SECTION_ID, "SearchId").ToString().Trim();
                }
            }
            catch { }
            return "";
        }

        string DetectScreenType(string customData, string scriptName)
        {
            Dictionary<string, string> sectionToType = new Dictionary<string, string>
            {
                { "SettingsPowerStatus", "Power" },
                { "SettingsItemsInventory", "Items" },
                { "SettingsItemsSummary", "Items" },
                { "SettingsComponentsInventory", "Components" },
                { "SettingsIngotsInventory", "Ingots" },
                { "SettingsOresInventory", "Ores" },
                { "SettingsWeapons", "Weapons" },
                { "SettingsCargoStatus", "Cargo" },
                { "SettingsAmmoStatus", "Ammo" }
            };

            foreach (var kvp in sectionToType)
            {
                if (customData.Contains($"[{kvp.Key}]"))
                    return kvp.Value;
            }

            return "";
        }

        void DrawText(ref MySpriteDrawFrame frame, ref Vector2 position, string text)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = 0.4f,
                Color = mySurface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            };
            frame.Add(sprite);
        }
    }
}
