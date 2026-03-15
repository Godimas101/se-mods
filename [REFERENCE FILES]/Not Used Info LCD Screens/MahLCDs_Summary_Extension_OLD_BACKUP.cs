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
using VRage.Utils;
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

        public override void Dispose()
        {
        }

        void CreateConfig()
        {
            config.Clear();

            StringBuilder sb = new StringBuilder();
            
            // Always preserve existing CustomData
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
                    MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Config Syntax error at Line {result}");
                    configError = true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Caught Exception while loading config: {e.ToString()}");
                configError = true;
            }
        }

        public override void Run()
        {
            // Prevent execution on dedicated server
            if (Sandbox.ModAPI.MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;

            // Check if config exists
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
                    DrawError(ref myFrame, ref myPosition, "<< Config error. Please check CustomData >>");
                }
                else if (string.IsNullOrWhiteSpace(searchId))
                {
                    DrawError(ref myFrame, ref myPosition, "Extension Screen\n\nPlease configure SearchId to specify\nthe parent LCD block name.");
                }
                else
                {
                    // Find immediate parent block
                    int duplicateCount;
                    IMyTerminalBlock parentBlock = FindParentBlock(searchId, out duplicateCount);
                    
                    if (duplicateCount > 1)
                    {
                        DrawError(ref myFrame, ref myPosition, $"ERROR: Duplicate block names detected!\n\n{duplicateCount} blocks found with name:\n'{searchId}'\n\nPlease rename blocks to ensure unique names.");
                    }
                    else if (parentBlock == null)
                    {
                        DrawError(ref myFrame, ref myPosition, $"Unable to find parent block:\n'{searchId}'");
                    }
                    else if (parentBlock.EntityId == myTerminalBlock.EntityId)
                    {
                        DrawError(ref myFrame, ref myPosition, $"ERROR: Circular reference detected!\n\nExtension screen cannot point to itself.\nSearchId: '{searchId}'");
                    }
                    else if (!parentBlock.IsFunctional)
                    {
                        DrawError(ref myFrame, ref myPosition, $"Parent LCD screen damaged\n'{searchId}'");
                    }
                    else if (!parentBlock.IsWorking)
                    {
                        DrawError(ref myFrame, ref myPosition, $"Parent LCD screen powered off\n'{searchId}'");
                    }
                    else
                    {
                        // Verify parent is an LCD
                        IMyTextSurfaceProvider parentSurfaceProvider = parentBlock as IMyTextSurfaceProvider;
                        if (parentSurfaceProvider == null)
                        {
                            DrawError(ref myFrame, ref myPosition, $"Block '{searchId}'\nhas no LCD screen");
                        }
                        else
                        {
                            // Get parent's surface using configured index
                            if (surfaceIndex < 0 || surfaceIndex >= parentSurfaceProvider.SurfaceCount)
                            {
                                DrawError(ref myFrame, ref myPosition, $"Block '{searchId}'\nSurfaceIndex {surfaceIndex} out of range\n(Valid: 0-{parentSurfaceProvider.SurfaceCount - 1})");
                                myFrame.Dispose();
                                return;
                            }
                            
                            IMyTextSurface parentSurface = parentSurfaceProvider.GetSurface(surfaceIndex) as IMyTextSurface;
                            
                            // Verify parent is running an InfoLCD app
                            string parentScript = parentSurface?.Script ?? "";
                            if (string.IsNullOrWhiteSpace(parentScript) || !parentScript.StartsWith("LCDInfoScreen"))
                            {
                                DrawError(ref myFrame, ref myPosition, $"Block '{searchId}'\nis not running an Info LCD App");
                            }
                            else
                            {
                                // If parent is an Extension, check if its parent chain is broken
                                if (parentScript == "LCDInfoScreenExtension")
                                {
                                    string parentOfParentId = GetExtensionSearchId(parentBlock.CustomData);
                                    if (!string.IsNullOrWhiteSpace(parentOfParentId))
                                    {
                                        int parentOfParentDuplicates;
                                        IMyTerminalBlock parentOfParent = FindParentBlock(parentOfParentId, out parentOfParentDuplicates);
                                        if (parentOfParentDuplicates > 1)
                                        {
                                            DrawError(ref myFrame, ref myPosition, $"ERROR: Duplicate block names in chain!\n\n{parentOfParentDuplicates} blocks found with name:\n'{parentOfParentId}'\n\nPlease rename blocks to ensure unique names.");
                                            myFrame.Dispose();
                                            return;
                                        }
                                        else if (parentOfParent == null)
                                        {
                                            DrawError(ref myFrame, ref myPosition, $"Parent's parent block not found\n'{parentOfParentId}'");
                                            myFrame.Dispose();
                                            return;
                                        }
                                        else if (!parentOfParent.IsFunctional)
                                        {
                                            DrawError(ref myFrame, ref myPosition, $"Parent LCD screen damaged\n'{parentOfParentId}'");
                                            myFrame.Dispose();
                                            return;
                                        }
                                        else if (!parentOfParent.IsWorking)
                                        {
                                            DrawError(ref myFrame, ref myPosition, $"Parent LCD screen powered off\n'{parentOfParentId}'");
                                            myFrame.Dispose();
                                            return;
                                        }
                                    }
                                }
                                
                                // Detect parent screen type and render overflow
                                RenderExtension(ref myFrame, ref myPosition, parentBlock, parentSurface, parentScript);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Caught Exception in Run: {e.ToString()}");
                DrawError(ref myFrame, ref myPosition, $"Exception:\n{e.Message}");
            }

            myFrame.Dispose();
        }

        IMyTerminalBlock FindParentBlock(string name, out int duplicateCount)
        {
            duplicateCount = 0;
            try
            {
                var myCubeGrid = myTerminalBlock.CubeGrid;
                if (myCubeGrid == null) return null;

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(myCubeGrid).GetBlocksOfType<IMyTerminalBlock>(blocks);

                List<IMyTerminalBlock> matchingBlocks = new List<IMyTerminalBlock>();
                
                foreach (var block in blocks)
                {
                    if (block == null || block.CustomName == null) continue;
                    
                    // Exact name match, case-insensitive
                    if (block.CustomName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingBlocks.Add(block);
                    }
                }
                
                duplicateCount = matchingBlocks.Count;
                
                // Return first match if found (or null if none)
                return matchingBlocks.Count > 0 ? matchingBlocks[0] : null;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Exception in FindParentBlock: {e.ToString()}");
            }

            return null;
        }

        void RenderExtension(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTerminalBlock parentBlock, IMyTextSurface parentSurface, string parentScript)
        {
            try
            {
                // Follow chain to find root parent (if this Extension extends another Extension)
                IMyTerminalBlock rootParent = parentBlock;
                IMyTextSurface rootSurface = parentSurface;
                string rootScript = parentScript;
                int chainDepth = 0;
                List<string> chainNames = new List<string> { parentBlock.CustomName };

                // Follow Extension chain up to root
                while (rootScript == "LCDInfoScreenExtension" && chainDepth < 10)
                {
                    chainDepth++;
                    
                    // Parse parent Extension's SearchId to find its parent
                    string parentSearchId = GetExtensionSearchId(rootParent.CustomData);
                    if (string.IsNullOrWhiteSpace(parentSearchId)) break;

                    int nextParentDuplicates;
                    IMyTerminalBlock nextParent = FindParentBlock(parentSearchId, out nextParentDuplicates);
                    if (nextParentDuplicates > 1)
                    {
                        DrawError(ref frame, ref position, $"ERROR: Duplicate block names in chain!\n\n{nextParentDuplicates} blocks found with name:\n'{parentSearchId}'\n\nPlease rename blocks to ensure unique names.");
                        return;
                    }
                    if (nextParent == null) break;
                    
                    // Check if this block in the chain is functional and working
                    if (!nextParent.IsFunctional || !nextParent.IsWorking) break;

                    chainNames.Insert(0, nextParent.CustomName);
                    
                    IMyTextSurfaceProvider nextProvider = nextParent as IMyTextSurfaceProvider;
                    if (nextProvider == null) break;

                    // Use same surfaceIndex for chain traversal
                    if (surfaceIndex >= nextProvider.SurfaceCount) break;
                    IMyTextSurface nextSurface = nextProvider.GetSurface(surfaceIndex) as IMyTextSurface;
                    if (nextSurface == null) break;

                    string nextScript = nextSurface.Script ?? "";
                    if (string.IsNullOrWhiteSpace(nextScript)) break;

                    rootParent = nextParent;
                    rootSurface = nextSurface;
                    rootScript = nextScript;
                }

                // Parse root parent's CustomData to determine screen type
                string parentCustomData = rootParent.CustomData ?? "";
                string parentScreenType = DetectScreenType(parentCustomData, rootScript);

                if (string.IsNullOrWhiteSpace(parentScreenType))
                {
                    DrawError(ref frame, ref position, $"Unable to detect parent screen type");
                    return;
                }

                // Parse parent config to get text size
                float parentTextSize = 0.4f;
                float parentViewPortOffsetY = 10f;
                MyIni parentIni = new MyIni();
                MyIniParseResult parentParseResult;
                
                if (parentIni.TryParse(parentCustomData, out parentParseResult))
                {
                    // Try to get textSize from parent config (different screens use different section IDs)
                    string[] possibleSections = { "SettingsWeaponsSummary", "SettingsItemsSummary", "SettingsPowerStatus", "SettingsComponentsInventory", 
                        "SettingsIngotsInventory", "SettingsOresInventory", "SettingsCargoStatus" };
                    
                    foreach (string section in possibleSections)
                    {
                        if (parentIni.ContainsSection(section))
                        {
                            if (parentIni.ContainsKey(section, "TextSize"))
                                parentTextSize = parentIni.Get(section, "TextSize").ToSingle();
                            if (parentIni.ContainsKey(section, "ViewPortOffsetY"))
                                parentViewPortOffsetY = parentIni.Get(section, "ViewPortOffsetY").ToSingle();
                            break;
                        }
                    }
                }

                // Calculate how many lines fit on parent screen (using parent's dimensions and offsets)
                // Match the main screen's viewport calculation: only top margin, bottom is at viewport boundary
                float parentHeight = parentSurface.SurfaceSize.Y;
                float lineHeight = 30 * parentTextSize;
                float usableHeight = parentHeight - parentViewPortOffsetY; // Only top margin (main screen checks viewport bottom boundary)
                int linesPerScreen = (int)(usableHeight / lineHeight);
                
                // Reduce significantly to ensure overlap - Extension will show items that might be cut off on main screen
                // This accounts for: space checking on main screen, headers, variable content per item
                linesPerScreen = Math.Max(1, linesPerScreen - 6);
                
                // Calculate total lines to skip (all previous screens in chain)
                int skipLines = linesPerScreen * (chainDepth + 1);

                // Render based on parent screen type
                if (parentScreenType == "Items")
                {
                    RenderItemsOverflow(ref frame, ref position, rootParent, parentIni, parentTextSize, skipLines, linesPerScreen, chainNames);
                }
                else if (parentScreenType == "Weapons")
                {
                    RenderWeaponsOverflow(ref frame, ref position, rootParent, parentIni, parentTextSize, skipLines, linesPerScreen, chainNames);
                }
                else
                {
                    // Fallback demo rendering for unsupported screen types
                    StringBuilder sb = new StringBuilder();
                    if (showHeader)
                    {
                        sb.AppendLine($"Extension (Page {chainDepth + 2})");
                        if (chainDepth > 0)
                            sb.AppendLine($"Chain: {string.Join(" -> ", chainNames)} -> {myTerminalBlock.CustomName}");
                        else
                            sb.AppendLine($"Parent: {rootParent.CustomName}");
                        sb.AppendLine($"Type: {parentScreenType}");
                        sb.AppendLine($"Lines/Screen: ~{linesPerScreen}");
                        sb.AppendLine($"Skip: {skipLines} lines");
                        sb.AppendLine();
                    }
                    
                    sb.AppendLine("=== OVERFLOW CONTENT ===");
                    sb.AppendLine();
                    sb.AppendLine($"Virtual Screen rendering for");
                    sb.AppendLine($"{parentScreenType} not yet implemented.");
                    sb.AppendLine();
                    sb.AppendLine("Currently supported:");
                    sb.AppendLine("- Items");
                    sb.AppendLine("- Weapons");
                    sb.AppendLine();
                    sb.AppendLine("Coming soon:");
                    sb.AppendLine("- Production");
                    sb.AppendLine("- Power");
                    sb.AppendLine("- Systems");
                    sb.AppendLine("- All other screens");

                    var sprite = new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = sb.ToString(),
                        Position = position,
                        RotationOrScale = parentTextSize,
                        Color = mySurface.ScriptForegroundColor,
                        Alignment = TextAlignment.LEFT,
                        FontId = "White"
                    };
                    frame.Add(sprite);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Exception in RenderExtension: {e.ToString()}");
                DrawError(ref frame, ref position, $"Render error:\n{e.Message}");
            }
        }

        void RenderItemsOverflow(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTerminalBlock parentBlock, MyIni parentConfig, float textSize, int skipLines, int linesPerScreen, List<string> chainNames)
        {
            try
            {
                // Read parent Items screen config
                string searchId = "*";
                bool showHeader = true;
                bool showSummary = true;
                bool showBars = true;
                bool showSubgrids = false;
                bool useSubtypeId = false;
                int minVisibleAmount = 1;
                List<string> excludeIds = new List<string>();

                if (parentConfig.ContainsKey("SettingsItemsSummary", "SearchId"))
                    searchId = parentConfig.Get("SettingsItemsSummary", "SearchId").ToString();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ShowHeader"))
                    showHeader = parentConfig.Get("SettingsItemsSummary", "ShowHeader").ToBoolean();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ShowSummary"))
                    showSummary = parentConfig.Get("SettingsItemsSummary", "ShowSummary").ToBoolean();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ShowBars"))
                    showBars = parentConfig.Get("SettingsItemsSummary", "ShowBars").ToBoolean();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ShowSubgrids"))
                    showSubgrids = parentConfig.Get("SettingsItemsSummary", "ShowSubgrids").ToBoolean();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "UseSubtypeId"))
                    useSubtypeId = parentConfig.Get("SettingsItemsSummary", "UseSubtypeId").ToBoolean();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "MinVisibleAmount"))
                    minVisibleAmount = parentConfig.Get("SettingsItemsSummary", "MinVisibleAmount").ToInt32();

                // Parse ExcludeIds
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ExcludeIds"))
                {
                    string[] exclude = parentConfig.Get("SettingsItemsSummary", "ExcludeIds").ToString().Split(',');
                    foreach (string s in exclude)
                    {
                        string t = s.Trim();
                        if (!string.IsNullOrEmpty(t) && t != "*" && t.Length >= 3)
                            excludeIds.Add(t);
                    }
                }

                // Get inventories from parent's grid
                var myCubeGrid = parentBlock.CubeGrid as Sandbox.Game.Entities.MyCubeGrid;
                if (myCubeGrid == null)
                {
                    DrawError(ref frame, ref position, "Cannot access parent grid");
                    return;
                }

                Sandbox.ModAPI.Ingame.MyShipMass gridMass = new Sandbox.ModAPI.Ingame.MyShipMass();
                var blocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, showSubgrids);
                List<IMyInventory> inventories = new List<IMyInventory>();

                foreach (var block in blocks)
                {
                    if (block == null) continue;
                    var entity = block as VRage.Game.ModAPI.IMyCubeBlock;
                    if (entity == null || !entity.HasInventory) continue;
                    for (int i = 0; i < entity.InventoryCount; i++)
                    {
                        var inv = entity.GetInventory(i);
                        if (inv != null) inventories.Add(inv);
                    }
                }

                // Collect all items
                Dictionary<string, CargoItem> cargo = new Dictionary<string, CargoItem>();
                List<string> item_types = new List<string> { "PhysicalGunObject", "OxygenContainerObject", "GasContainerObject", 
                    "PhysicalObject", "ConsumableItem", "Package", "SeedItem" };

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;
                    var items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    inventory.GetItems(items);

                    foreach (var item in items)
                    {
                        if (item == null) continue;
                        string typeId = item.Type.TypeId.Split('_')[1];
                        if (!item_types.Contains(typeId)) continue;

                        string subtypeId = item.Type.SubtypeId;
                        int amount = item.Amount.ToIntSafe();

                        if (cargo.ContainsKey(subtypeId))
                            cargo[subtypeId].amount += amount;
                        else
                            cargo.Add(subtypeId, new CargoItem { amount = amount, item = item });
                    }
                }

                // Create SurfaceData for proper rendering
                float viewPortOffsetX = 20f;
                float viewPortOffsetY = 10f;
                float titleOffset = 280f;
                float ratioOffset = 82f;
                
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ViewPortOffsetX"))
                    viewPortOffsetX = parentConfig.Get("SettingsItemsSummary", "ViewPortOffsetX").ToSingle();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "ViewPortOffsetY"))
                    viewPortOffsetY = parentConfig.Get("SettingsItemsSummary", "ViewPortOffsetY").ToSingle();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "TitleFieldWidth"))
                    titleOffset = parentConfig.Get("SettingsItemsSummary", "TitleFieldWidth").ToSingle();
                if (parentConfig.ContainsKey("SettingsItemsSummary", "RatioFieldWidth"))
                    ratioOffset = parentConfig.Get("SettingsItemsSummary", "RatioFieldWidth").ToSingle();

                var surfaceData = new SurfaceDrawer.SurfaceData
                {
                    surface = mySurface,
                    textSize = textSize,
                    titleOffset = titleOffset,
                    ratioOffset = ratioOffset,
                    viewPortOffsetX = viewPortOffsetX,
                    viewPortOffsetY = viewPortOffsetY,
                    newLine = new Vector2(0, 30 * textSize),
                    showBars = showBars,
                    useColors = true
                };

                // Render with line skipping
                int currentLine = 0;
                int renderedLines = 0;

                // Header lines
                if (showHeader)
                {
                    currentLine += 1; // Header takes 1 line
                }

                if (showSummary)
                {
                    currentLine += 4; // Summary takes ~4 lines (title + blank + 2 bars + blank)
                }

                // Column header line
                currentLine += 1;

                // Now render items, skipping lines that fit on previous screens
                var sortedCargo = cargo.OrderBy(kvp => kvp.Key).ToList();

                foreach (var item in sortedCargo)
                {
                    if (item.Value.amount < minVisibleAmount) continue;

                    // Check if this line should be skipped
                    if (currentLine < skipLines)
                    {
                        currentLine++;
                        continue;
                    }

                    // Check if we've filled this Extension screen
                    if (renderedLines >= linesPerScreen)
                        break;

                    // Get display name from definitions
                    string subtypeId = item.Key;
                    string typeId = item.Value.item.Type.TypeId.ToString().Split('_')[1];
                    var definition = MahDefinitions.GetDefinition(typeId, subtypeId);
                    string displayText = (definition != null && !useSubtypeId) ? definition.displayName : subtypeId;
                    
                    // Read desired amount from parent config using typeId_subtypeId format (defaults to definition's minAmount or 1000)
                    int desiredAmount = (definition != null) ? definition.minAmount : 1000;
                    string configKey = $"{typeId}_{subtypeId}";
                    if (parentConfig.ContainsKey("SettingsItemsSummary", configKey))
                        desiredAmount = parentConfig.Get("SettingsItemsSummary", configKey).ToInt32();
                    
                    // Use DrawItemSprite for proper formatting with bars
                    SurfaceDrawer.DrawItemSprite(ref frame, ref position, surfaceData, 
                        subtypeId, displayText, item.Value.amount, desiredAmount, true);

                    currentLine++;
                    renderedLines++;
                }

                // If no items to show, display chain info
                if (renderedLines == 0)
                {
                    DrawChainInfo(ref frame, ref position, chainNames);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Exception in RenderItemsOverflow: {e.ToString()}");
                DrawError(ref frame, ref position, $"Render error:\\n{e.Message}");
            }
        }

        // Helper class for cargo tracking
        class CargoItem
        {
            public int amount;
            public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        }

        void DrawChainInfo(ref MySpriteDrawFrame frame, ref Vector2 position, List<string> chainNames)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Screen Extension Chain");
            
            // Show the full chain with proper numbering
            if (chainNames.Count > 0)
            {
                sb.AppendLine($"- Primary screen: {chainNames[0]}");
                for (int i = 1; i < chainNames.Count; i++)
                {
                    sb.AppendLine($"- Extension {i:00}: {chainNames[i]}");
                }
                // Add current screen
                sb.AppendLine($"- Extension {chainNames.Count:00}: {myTerminalBlock.CustomName}");
            }
            
            sb.AppendLine();
            sb.AppendLine("No extension content to display on this LCD");

            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = position,
                RotationOrScale = 0.4f,
                Color = mySurface.ScriptForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            };
            frame.Add(sprite);
        }

        void RenderWeaponsOverflow(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTerminalBlock parentBlock, MyIni parentConfig, float textSize, int skipLines, int linesPerScreen, List<string> chainNames)
        {
            try
            {
                // Read parent Weapons screen config
                string searchId = "*";
                bool showHeader = true;
                bool showSubgrids = false;
                bool detailedInfo = true;
                bool compactMode = false;
                bool showCustom = true;
                bool showTurrets = true;
                bool showInteriors = true;
                bool showCannons = true;
                List<string> excludeIds = new List<string>();

                if (parentConfig.ContainsKey("SettingsWeapons", "SearchId"))
                    searchId = parentConfig.Get("SettingsWeapons", "SearchId").ToString();
                if (parentConfig.ContainsKey("SettingsWeapons", "ShowHeader"))
                    showHeader = parentConfig.Get("SettingsWeapons", "ShowHeader").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "ShowSubgrids"))
                    showSubgrids = parentConfig.Get("SettingsWeapons", "ShowSubgrids").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "DetailedInfo"))
                    detailedInfo = parentConfig.Get("SettingsWeapons", "DetailedInfo").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "CompactMode"))
                    compactMode = parentConfig.Get("SettingsWeapons", "CompactMode").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "ShowCustomTurretControllers"))
                    showCustom = parentConfig.Get("SettingsWeapons", "ShowCustomTurretControllers").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "ShowTurrets"))
                    showTurrets = parentConfig.Get("SettingsWeapons", "ShowTurrets").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "ShowInteriorTurrets"))
                    showInteriors = parentConfig.Get("SettingsWeapons", "ShowInteriorTurrets").ToBoolean();
                if (parentConfig.ContainsKey("SettingsWeapons", "ShowCannons"))
                    showCannons = parentConfig.Get("SettingsWeapons", "ShowCannons").ToBoolean();

                // Parse ExcludeIds
                if (parentConfig.ContainsKey("SettingsWeapons", "ExcludeIds"))
                {
                    string[] exclude = parentConfig.Get("SettingsWeapons", "ExcludeIds").ToString().Split(',');
                    foreach (string s in exclude)
                    {
                        string t = s.Trim();
                        if (!string.IsNullOrEmpty(t) && t != "*" && t.Length >= 3)
                            excludeIds.Add(t);
                    }
                }

                // Get blocks from parent's grid
                var myCubeGrid = parentBlock.CubeGrid as Sandbox.Game.Entities.MyCubeGrid;
                if (myCubeGrid == null)
                {
                    DrawError(ref frame, ref position, "Cannot access parent grid");
                    return;
                }

                Sandbox.ModAPI.Ingame.MyShipMass gridMass = new Sandbox.ModAPI.Ingame.MyShipMass();
                var blocks = MahUtillities.GetBlocks(myCubeGrid, searchId, excludeIds, ref gridMass, showSubgrids);

                // Collect weapons by type
                List<IMyInventory> inventories = new List<IMyInventory>();
                List<IMyLargeTurretBase> turrets = new List<IMyLargeTurretBase>();
                List<IMyLargeInteriorTurret> interiorTurrets = new List<IMyLargeInteriorTurret>();
                List<IMyUserControllableGun> cannons = new List<IMyUserControllableGun>();
                List<IMyTerminalBlock> customTurretControllers = new List<IMyTerminalBlock>();

                foreach (var myBlock in blocks)
                {
                    if (myBlock == null) continue;

                    if (myBlock is IMyLargeInteriorTurret)
                    {
                        interiorTurrets.Add((IMyLargeInteriorTurret)myBlock);
                    }
                    else if (myBlock is IMyLargeTurretBase)
                    {
                        turrets.Add((IMyLargeTurretBase)myBlock);
                    }
                    else if (myBlock is IMyUserControllableGun)
                    {
                        cannons.Add((IMyUserControllableGun)myBlock);
                    }

                    // Custom Turret Controller detection
                    if (myBlock is IMyTerminalBlock)
                    {
                        var termBlock = (IMyTerminalBlock)myBlock;
                        string subtype = termBlock.BlockDefinition.SubtypeName ?? "";
                        if (subtype.IndexOf("CustomTurretController", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            subtype.IndexOf("CustomTurret", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            customTurretControllers.Add(termBlock);
                        }
                    }

                    if (myBlock is VRage.Game.ModAPI.IMyCubeBlock)
                    {
                        var entity = myBlock as VRage.Game.ModAPI.IMyCubeBlock;
                        if (entity.HasInventory)
                        {
                            for (int i = 0; i < entity.InventoryCount; i++)
                            {
                                var inv = entity.GetInventory(i);
                                if (inv != null) inventories.Add(inv);
                            }
                        }
                    }
                }

                // Setup rendering state matching parent's SurfaceDrawer.SurfaceData
                SurfaceDrawer.SurfaceData surfaceData = new SurfaceDrawer.SurfaceData();
                surfaceData.surface = mySurface;
                surfaceData.textSize = textSize;
                surfaceData.newLine = new Vector2(0, 30 * textSize);
                surfaceData.useColors = true;

                // Track current line for pagination
                int currentLine = 0;

                // Optional header (counts as lines to consider)
                if (showHeader)
                {
                    if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                    {
                        SurfaceDrawer.DrawHeader(ref frame, ref position, surfaceData, $"Weapons Summary [{(searchId == "*" ? "All" : searchId)} -{excludeIds.Count}] (Page {(skipLines / linesPerScreen) + 1})");
                        if (compactMode) position -= 1 * surfaceData.newLine;
                    }
                    currentLine += (compactMode ? 1 : 2); // Header takes 2 lines normally, 1 in compact
                }

                // Render in same order as main Weapons screen
                // Each detailed turret takes 3 lines, compact summaries take 2-3 lines
                
                if (compactMode || !detailedInfo)
                {
                    // Compact mode - each category is a summary (2-3 lines)
                    if (showCustom && customTurretControllers.Count > 0)
                    {
                        RenderWeaponsCompactCategory(ref frame, ref position, surfaceData, "Custom Turret Controllers", customTurretControllers.Count, ref currentLine, skipLines, linesPerScreen);
                    }
                    if (showTurrets && turrets.Count > 0)
                    {
                        RenderWeaponsCompactCategory(ref frame, ref position, surfaceData, "Turrets", turrets.Count, ref currentLine, skipLines, linesPerScreen);
                    }
                    if (showInteriors && interiorTurrets.Count > 0)
                    {
                        RenderWeaponsCompactCategory(ref frame, ref position, surfaceData, "Interior Turrets", interiorTurrets.Count, ref currentLine, skipLines, linesPerScreen);
                    }
                    if (showCannons && cannons.Count > 0)
                    {
                        RenderWeaponsCompactCategory(ref frame, ref position, surfaceData, "Cannons", cannons.Count, ref currentLine, skipLines, linesPerScreen);
                    }
                }
                else
                {
                    // Detailed mode - each turret takes 3 lines
                    if (showCustom && customTurretControllers.Count > 0)
                    {
                        RenderWeaponsDetailedCategory(ref frame, ref position, surfaceData, "Custom Turret Controllers", customTurretControllers, ref currentLine, skipLines, linesPerScreen, parentConfig);
                    }
                    if (showTurrets && turrets.Count > 0)
                    {
                        RenderWeaponsDetailedTurrets(ref frame, ref position, surfaceData, "Turrets", turrets, ref currentLine, skipLines, linesPerScreen);
                    }
                    if (showInteriors && interiorTurrets.Count > 0)
                    {
                        RenderWeaponsDetailedTurrets(ref frame, ref position, surfaceData, "Interior Turrets", interiorTurrets.Cast<IMyLargeTurretBase>().ToList(), ref currentLine, skipLines, linesPerScreen);
                    }
                    if (showCannons && cannons.Count > 0)
                    {
                        RenderWeaponsDetailedCannons(ref frame, ref position, surfaceData, "Cannons", cannons, ref currentLine, skipLines, linesPerScreen);
                    }
                }

                // If we haven't rendered anything yet, show message
                if (currentLine <= skipLines)
                {
                    DrawChainInfo(ref frame, ref position, chainNames);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Exception in RenderWeaponsOverflow: {e.ToString()}");
                DrawError(ref frame, ref position, $"Weapons render error:\n{e.Message}");
            }
        }

        void RenderWeaponsCompactCategory(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceDrawer.SurfaceData surfaceData, string categoryName, int count, ref int currentLine, int skipLines, int linesPerScreen)
        {
            // Compact category: 2 lines (category name + summary)
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{categoryName} [{count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine; // Only advance when rendering
            }
            currentLine++;

            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"(See main screen for details)", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine; // Only advance when rendering
            }
            currentLine++;
        }

        void RenderWeaponsDetailedCategory(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceDrawer.SurfaceData surfaceData, string categoryName, List<IMyTerminalBlock> controllers, ref int currentLine, int skipLines, int linesPerScreen, MyIni parentConfig)
        {
            // Category header (1 line)
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{categoryName} [{controllers.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine; // Only advance when rendering
            }
            currentLine++;

            // Each controller: 4-line display to match main screen (name+badge, armed, ammo bar, blank)
            foreach (var controller in controllers)
            {
                if (controller == null) continue;

                // Stop if this controller starts at or after the end of our viewport
                if (currentLine >= skipLines + linesPerScreen)
                    break;

                // Each controller takes 4 lines - render if any part overlaps with visible window
                bool controllerVisible = (currentLine + 3 >= skipLines) && (currentLine < skipLines + linesPerScreen);
                
                if (controllerVisible)
                {
                    // Line 1: Badge + name + status
                    if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                    {
                        string name = controller.CustomName.Length > 25 ? controller.CustomName.Substring(0, 25) : controller.CustomName;
                        string status = !controller.IsWorking ? "Off" : controller.IsFunctional ? "Ready" : "Damaged";
                        Color statusColor = !controller.IsWorking ? Color.Gray : controller.IsFunctional ? Color.GreenYellow : Color.Red;

                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[{status}] {name}", TextAlignment.LEFT, statusColor);
                        position += surfaceData.newLine;
                    }
                    currentLine++;

                    // Line 2: Armed status
                    if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  [Armed]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                    currentLine++;

                    // Line 3: Simplified ammo bar placeholder
                    if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  (Custom turret)", TextAlignment.LEFT, Color.Gray);
                        position += surfaceData.newLine;
                    }
                    currentLine++;

                    // Line 4: Blank line
                    if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                    {
                        position += surfaceData.newLine;
                    }
                    currentLine++;
                }
                else
                {
                    currentLine += 4; // Skip all 4 lines
                }
            }

            // Blank line separator
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                position += surfaceData.newLine; // Only advance when rendering
            }
            currentLine++;
        }

        void RenderWeaponsDetailedTurrets(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceDrawer.SurfaceData surfaceData, string categoryName, List<IMyLargeTurretBase> turrets, ref int currentLine, int skipLines, int linesPerScreen)
        {
            // Category header (1 line)
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{categoryName} [{turrets.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Settings", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine; // Only advance when actually rendering
            }
            currentLine++;

            // Each turret: 3 lines (badge+name, ammo, bar)
            int turretIndex = 0;
            foreach (var turret in turrets)
            {
                if (turret == null) continue;

                // Stop if this turret starts at or after the end of our viewport
                if (currentLine >= skipLines + linesPerScreen)
                    break;

                // Render if any part of this turret overlaps with our visible window
                // Turret occupies lines: currentLine, currentLine+1, currentLine+2
                // Visible window is: skipLines to (skipLines + linesPerScreen - 1)
                // Check if turret's last line (currentLine+2) >= skipLines AND turret's first line (currentLine) < skipLines + linesPerScreen
                bool turretVisible = (currentLine + 2 >= skipLines) && (currentLine < skipLines + linesPerScreen);
                
                if (turretVisible)
                {
                    RenderDetailedTurretUnit(ref frame, ref position, surfaceData, turret);
                }
                // Don't advance position for skipped lines - position only tracks rendered content
                currentLine += 3;
                turretIndex++;
            }

            // Blank line separator
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                position += surfaceData.newLine; // Only advance when actually rendering
            }
            currentLine++;
        }

        void RenderWeaponsDetailedCannons(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceDrawer.SurfaceData surfaceData, string categoryName, List<IMyUserControllableGun> cannons, ref int currentLine, int skipLines, int linesPerScreen)
        {
            // Category header (1 line)
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{categoryName} [{cannons.Count}]", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                position += surfaceData.newLine; // Only advance when rendering
            }
            currentLine++;

            // Each cannon: 2 lines (name+status, ammo bar)
            foreach (var cannon in cannons)
            {
                if (cannon == null) continue;

                // Stop if this cannon starts at or after the end of our viewport
                if (currentLine >= skipLines + linesPerScreen)
                    break;

                // Check if first line (name+status) is visible
                if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                {
                    string name = cannon.CustomName.Length > 25 ? cannon.CustomName.Substring(0, 25) : cannon.CustomName;
                    float currentVolume = (float)cannon.GetInventory(0).CurrentVolume;
                    float maximumVolume = (float)cannon.GetInventory(0).MaxVolume;
                    string status = !cannon.IsWorking ? "Off" : currentVolume <= 0 ? "NoAmmo" : cannon.IsShooting ? "Firing" : "Ready";
                    Color statusColor = status == "Off" ? Color.Gray : status == "NoAmmo" ? Color.Orange : status == "Firing" ? Color.Red : Color.GreenYellow;

                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"  {name}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, status, TextAlignment.RIGHT, statusColor);
                    position += surfaceData.newLine; // Only advance when rendering
                }
                currentLine++;

                // Check if second line (ammo bar) is visible
                if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
                {
                    float currentVolume = (float)cannon.GetInventory(0).CurrentVolume;
                    float maximumVolume = (float)cannon.GetInventory(0).MaxVolume;
                    SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, Color.Orange);
                    position += surfaceData.newLine; // Only advance when rendering
                }
                currentLine++;
            }

            // Blank line separator
            if (currentLine >= skipLines && currentLine < skipLines + linesPerScreen)
            {
                position += surfaceData.newLine; // Only advance when rendering
            }
            currentLine++;
        }

        const float BadgeInnerWidth = 14f;
        const float BadgeCenterBiasChars = -0.5f;

        void RenderDetailedTurretUnit(ref MySpriteDrawFrame frame, ref Vector2 position, SurfaceDrawer.SurfaceData surfaceData, IMyLargeTurretBase turret)
        {
            float pixelPerChar = MahDefinitions.pixelPerChar * surfaceData.textSize;
            var maxNameLength = (int)(surfaceData.surface.SurfaceSize.X > 300 ? 35 : 20);
            var turretName = turret.CustomName.Length > maxNameLength ? turret.CustomName.Substring(0, maxNameLength) : turret.CustomName;
            var currentVolume = (float)turret.GetInventory(0).CurrentVolume;
            var maximumVolume = (float)turret.GetInventory(0).MaxVolume;
            var ammoType = "No Ammo";
            var ammoCount = 0;

            if (turret.GetInventory(0).CurrentVolume > 0)
            {
                List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                turret.GetInventory(0).GetItems(inventoryItems);

                foreach (var item in inventoryItems)
                {
                    if (item == null) continue;
                    if (ammoType == "No Ammo")
                    {
                        ammoType = item.Type.SubtypeId;
                        CargoItemDefinition itemDefinition = MahDefinitions.GetDefinition("AmmoMagazine", ammoType);
                        if (itemDefinition != null)
                            ammoType = itemDefinition.displayName;
                    }
                    ammoCount += (int)item.Amount;
                }
            }

            var targetCharacters = $"[{(turret.TargetCharacters ? "X" : "  ")}]";
            var targetEnemies = $"[{(turret.TargetEnemies ? "X" : "  ")}]";
            var targetLargeGrids = $"[{(turret.TargetLargeGrids ? "X" : "  ")}]";
            var targetMeteors = $"[{(turret.TargetMeteors ? "X" : "  ")}]";
            var targetMissiles = $"[{(turret.TargetMissiles ? "X" : "  ")}]";
            var targetNeutrals = $"[{(turret.TargetNeutrals ? "X" : "  ")}]";
            var targetSmallGrids = $"[{(turret.TargetSmallGrids ? "X" : "  ")}]";
            var targetStations = $"[{(turret.TargetStations ? "X" : "  ")}]";
            var targetSet = $"Ch En LG Me Mi Ne SG St ";
            var targetting = $"{targetCharacters} {targetEnemies} {targetLargeGrids} {targetMeteors} {targetMissiles} {targetNeutrals} {targetSmallGrids} {targetStations}";

            ammoType = ammoType.Length >= maxNameLength ? ammoType.Substring(0, maxNameLength) : ammoType;
            var state = $"{(!turret.IsWorking ? "     Off     " : currentVolume <= 0 ? "NoAmmo" : turret.IsShooting ? "Firing" : turret.IsUnderControl ? "Manned" : turret.HasTarget && !turret.IsAimed ? "Follow" : turret.HasTarget && turret.IsAimed ? "Locked" : "Idle")}";

            // Line 1: Badge + name + target settings
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[                ] {turretName}", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
            
            float ppc = MahDefinitions.pixelPerChar * surfaceData.textSize;
            float desiredCenterX = position.X + ppc * (1f + (BadgeInnerWidth * 0.5f) + BadgeCenterBiasChars);
            var centerPos = new Vector2(desiredCenterX - (surfaceData.surface.SurfaceSize.X * 0.5f), position.Y);
            var overlayColor = !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : state.Contains("Off") || state.Contains("NoAmmo") ? Color.Orange : state.Contains("Manned") ? Color.Magenta : state.Contains("Follow") ? Color.Yellow : state.Contains("Firing") ? Color.Red : Color.GreenYellow;
            SurfaceDrawer.WriteTextSprite(ref frame, centerPos, surfaceData, state.Trim(), TextAlignment.CENTER, overlayColor);
            
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetSet}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
            position += surfaceData.newLine;

            // Line 2: Ammo count/type + target checkboxes
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{MahDefinitions.KiloFormat(ammoCount)}x <{ammoType}> ", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"{targetting}", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
            position += surfaceData.newLine;

            // Line 3: Ammo bar + range
            SurfaceDrawer.DrawHalfBar(ref frame, position, surfaceData, TextAlignment.LEFT, currentVolume, maximumVolume, Unit.Percent, !surfaceData.useColors ? surfaceData.surface.ScriptForegroundColor : Color.Orange);
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, $"[Range: {turret.Range.ToString("#,0")} m]", TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
            position += surfaceData.newLine;
        }

        string GetExtensionSearchId(string customData)
        {
            try
            {
                MyIni ini = new MyIni();
                MyIniParseResult result;
                if (ini.TryParse(customData, CONFIG_SECTION_ID, out result))
                {
                    if (ini.ContainsKey(CONFIG_SECTION_ID, "SearchId"))
                    {
                        return ini.Get(CONFIG_SECTION_ID, "SearchId").ToString().Trim();
                    }
                }
            }
            catch { }
            return "";
        }

        string DetectScreenType(string customData, string scriptName)
        {
            try
            {
                // Map of section IDs to screen types
                Dictionary<string, string> sectionToType = new Dictionary<string, string>
                {
                    { "SettingsPowerStatus", "Power" },
                    { "SettingsSystemsStatus", "Systems" },
                    { "SettingsProductionStatus", "Production" },
                    { "SettingsGridInfo", "GridInfo" },
                    { "SettingsDamageMonitor", "DamageMonitor" },
                    { "SettingsContainerStatus", "Container" },
                    { "SettingsCargoStatus", "Cargo" },
                    { "SettingsAmmoStatus", "Ammo" },
                    { "SettingsItemsInventory", "Items" },
                    { "SettingsComponentsInventory", "Components" },
                    { "SettingsIngotsInventory", "Ingots" },
                    { "SettingsOresInventory", "Ores" },
                    { "SettingsDoorMonitor", "DoorMonitor" },
                    { "SettingsFarmingStatus", "Farming" },
                    { "SettingsGasProduction", "GasProduction" },
                    { "SettingsLifeSupport", "LifeSupport" },
                    { "SettingsWeapons", "Weapons" },
                    { "SettingsAirlockMonitor", "AirlockMonitor" },
                    { "SettingsDetailedInfo", "DetailedInfo" }
                };

                // Check which section exists in parent's CustomData
                foreach (var kvp in sectionToType)
                {
                    if (customData.Contains($"[{kvp.Key}]"))
                    {
                        return kvp.Value;
                    }
                }

                // Fallback: try to parse from script name
                // LCDInfoScreenPowerSummary -> Power
                if (scriptName.StartsWith("LCDInfoScreen"))
                {
                    string remainder = scriptName.Substring(13); // Skip "LCDInfoScreen"
                    if (!string.IsNullOrWhiteSpace(remainder))
                    {
                        return remainder.Replace("Summary", "").Replace("Inventory", "").Replace("Monitor", "").Replace("Status", "").Trim();
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"MahrianeIndustries.LCDInfo.LCDInfoScreenExtension: Exception in DetectScreenType: {e.ToString()}");
            }

            return "";
        }

        void DrawError(ref MySpriteDrawFrame frame, ref Vector2 position, string message)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = message,
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
