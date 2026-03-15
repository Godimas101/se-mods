using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Sisk.Utils.Localization
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class LocalizationComponent : MySessionComponentBase
    {

        /// <summary>
        ///     Create a new instance of this component.
        /// </summary>
        public LocalizationComponent()
        {
            Default = this;
        }
        
        /// <summary>
        /// Store original tooltips to revert changes on unload.
        /// </summary>
        private readonly Dictionary<MyDefinitionId, StringBuilder> _originalTooltips = new Dictionary<MyDefinitionId, StringBuilder>();

        /// <summary>
        ///     Get the default instance.
        /// </summary>
        public static LocalizationComponent Default { get; private set; }

        /// <summary>
        ///     Language used to localize this mod.
        /// </summary>
        public MyLanguagesEnum? Language { get; private set; }

        /// <summary>
        ///     Load mod settings, create localizations and initialize network handler.
        /// </summary>
        public override void LoadData()
        {
            LoadLocalization();
            PatchItemDefinitions();
            MyAPIGateway.Gui.GuiControlRemoved += OnGuiControlRemoved;
        }

        /// <summary>
        ///     When game unloads
        /// </summary>
        protected override void UnloadData()
        {
            MyAPIGateway.Gui.GuiControlRemoved -= OnGuiControlRemoved;
            
            // Restore all modified item definitions to their original state.
            RestoreOriginalTooltips();
            _originalTooltips.Clear(); // Clean up memory

            Default = null;
        }

        /// <summary>
        /// Iterates over the stored original definitions and reverts the changes.
        /// </summary>
        private void RestoreOriginalTooltips()
        {
            foreach (var entry in _originalTooltips)
            {
                MyPhysicalItemDefinition definition;
                // Find the definition again and apply the saved original value.
                if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(entry.Key, out definition) && definition != null)
                {
                    // definition.ExtraInventoryTooltipLine = entry.Value;
                }
            }
        }

        /// <summary>
        ///     Load localizations for this mod.
        /// </summary>
        private void LoadLocalization()
        {
            var path = Path.Combine(ModContext.ModPathData, "Localization");
            var supportedLanguages = new HashSet<MyLanguagesEnum>();
            MyTexts.LoadSupportedLanguages(path, supportedLanguages);

            var currentLanguage = supportedLanguages.Contains(MyAPIGateway.Session.Config.Language) ? MyAPIGateway.Session.Config.Language : MyLanguagesEnum.English;
            if (Language != null && Language == currentLanguage)
            {
                return;
            }

            Language = currentLanguage;
            var languageDescription = MyTexts.Languages.Where(x => x.Key == currentLanguage).Select(x => x.Value).FirstOrDefault();
            if (languageDescription != null)
            {
                var cultureName = string.IsNullOrWhiteSpace(languageDescription.CultureName) ? null : languageDescription.CultureName;
                var subcultureName = string.IsNullOrWhiteSpace(languageDescription.SubcultureName) ? null : languageDescription.SubcultureName;

                MyTexts.LoadTexts(path, cultureName, subcultureName);
            }
        }

        /// <summary>
        ///     Event triggered on gui control removed.
        ///     Used to detect if Option screen is closed and then to reload localization.
        /// </summary>
        /// <param name="obj"></param>
        private void OnGuiControlRemoved(object obj)
        {
            if (obj.ToString().EndsWith("ScreenOptionsSpace"))
            {
                LoadLocalization();
            }
        }

        private void PatchItemDefinitions()
        {
            _originalTooltips.Clear();

            var itemIdsToPatch = MyDefinitionManager.Static.GetAllDefinitions()
                                .OfType<MyConsumableItemDefinition>()
                                .Select(def => def.Id)
                                .ToArray();

            // Keen does not use constant name schema...
            // <ExtraInventoryTooltipLineId>Item_****_Extra_Tooltip</ExtraInventoryTooltipLineId>
            //   --  vs  -- 
            // <ExtraInventoryTooltipLineId>Item_****_Tooltip</ExtraInventoryTooltipLineId>
            // this dict represents the "others"
            var itemTooltips = new Dictionary<string, string>
                                {
                                    { "Algae",              "Item_Algae_Tooltip" },
                                    { "Fruit",              "Item_Fruit_Tooltip" },
                                    { "Grain",              "Item_Grain_Tooltip" },
                                    { "Mushrooms",          "Item_Mushrooms_Tooltip" },
                                    { "Vegetables",         "Item_Vegetables_Tooltip" },
                                    { "MammalMeatRaw",      "Item_MammalMeatRaw_Tooltip" },
                                    { "MammalMeatCooked",   "Item_MammalMeatCooked_Tooltip" },
                                    { "InsectMeatRaw",      "Item_InsectMeatRaw_Tooltip" },
                                    { "InsectMeatCooked",   "Item_InsectMeatCooked_Tooltip" }
                                };            

            foreach (var definitionId in itemIdsToPatch)
            {
                MyPhysicalItemDefinition definition;
                if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(definitionId, out definition) && definition != null)
                {

                    // Store the original value ONLY if we haven't already.
                    if (!_originalTooltips.ContainsKey(definitionId))
                    {
                        // We must create a new StringBuilder, as it's a reference type. Cloning the content.
                        _originalTooltips[definitionId] = definition.ExtraInventoryTooltipLine == null ? null : new StringBuilder(definition.ExtraInventoryTooltipLine.ToString());
                    }

                    // The key for the special tooltip is generated automatically from the SubtypeId.
                    string tooltipKey;
                    if (!itemTooltips.TryGetValue(definitionId.SubtypeId.ToString(), out tooltipKey))                                             
                        tooltipKey = $"Item_{definitionId.SubtypeId.ToString()}_Extra_Tooltip";

                    var tooltipStringId = MyStringId.GetOrCompute(tooltipKey);
                    var correctTooltipText = MyTexts.GetString(tooltipStringId);

                    // For items, the "Extra Tooltip" is usually the main Description.
                    // Need to do this now - if game runs it crashes
                    StringBuilder sb = new StringBuilder().AppendLine();
                    //definition.ExtraInventoryTooltipLine = sb.Append(correctTooltipText);
                }
            }
        }
    }
}