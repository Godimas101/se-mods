using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace APEX.Advanced.Client.MyStatus
{
    public class CharacterStatus : WindowBase
    {
        // FEATURE: Status value changes shows + or -

        public event Action RequestClose;

        private readonly LabelBoxButton _bottomCloseButton;
        private readonly HudChain _statusChain;
        public readonly HudChain _consumableChain;
        
        private readonly Label _suitPowerLabel;
        private readonly Label _suitOxygenLabel;
        private readonly Label _suitHydrogenLabel;

        private readonly Label _healthLabel;
        private readonly Label _foodLabel;
        private readonly Label _waterLabel;
        private readonly Label _sleepLabel;
        private readonly Label _recoveryLabel;
        private readonly Label _fatigueLabel;
        private readonly Label _bloatingLabel;

        // FEATURE: Nicer HUD
        //private Material _suitPowerIcon = new Material(MyStringId.GetOrCompute("APEXHelmetVignette"), Vector2.One);
        //private Material _suitPowerBar = new Material(MyStringId.GetOrCompute("APEXHelmetVignette"), Vector2.One);

        private string _healthTooltipText;
        private string _foodTooltipText;
        private string _waterTooltipText;
        private string _sleepTooltipText;
        private string _recoveryTooltipText;
        private string _fatigueTooltipText;
        private string _bloatingTooltipText;

        private readonly LabelBox _tooltipBox;

        #region Status_Values (getter / setter)
        // Consumables
        private string _myConsumedItems;
        public string MyConsumedItems
        {
            get { return _myConsumedItems; }
            set
            {
                _myConsumedItems = value;
                UpdateMyConsumedItems();
            }
        }

        // Status Values
        private int _suitPower;
        public int SuitPower
        {
            get { return _suitPower; }
            set
            {
                _suitPower = value;
                UpdateSuitPowerLabel();
            }
        }
        private int _suitOxygen;
        public int SuitOxygen
        {
            get { return _suitOxygen; }
            set
            {
                _suitOxygen = value;
                UpdateSuitOxygenLabel();
            }
        }
        private int _suitHydrogen;
        public int SuitHydrogen
        {
            get { return _suitHydrogen; }
            set
            {
                _suitHydrogen = value;
                UpdateSuitHydrogenLabel();
            }
        }

        private int _health;
        public int Health
        {
            get { return _health; }
            set
            {
                _health = value;
                UpdateHealthLabel();
            }
        }

        private int _food;
        public int Food
        {
            get { return _food; }
            set
            {
                _food = value;
                UpdateFoodLabel();
            }
        }

        private int _water;
        public int Water
        {
            get { return _water; }
            set
            {
                _water = value;
                UpdateWaterLabel();
            }
        }

        private int _sleep;
        public int Sleep
        {
            get { return _sleep; }
            set
            {
                _sleep = value;
                UpdateSleepLabel();
            }
        }

        private int _recovery;
        public int Recovery
        {
            get { return _recovery; }
            set
            {
                _recovery = value;
                UpdateRecoveryLabel();
            }
        }

        private int _fatigue;
        public int Fatigue
        {
            get { return _fatigue; }
            set
            {
                _fatigue = value;
                UpdateFatigueLabel();
            }
        }

        private int _bloating;
        public int Bloating
        {
            get { return _bloating; }
            set
            {
                _bloating = value;
                UpdateBloatingLabel();
            }
        }
        #endregion

        // Consumables
        private void UpdateMyConsumedItems()
        {
            if (_consumableChain.Visible)
                return;
            // TODO: SCROLL THIS FIELDS
            // chain cleanUp 
            _consumableChain.RemoveRange(0,_consumableChain.Count);

            // Kind of a Header / Starting text
            HudElementContainer _myContainer = new HudElementContainer();            
            _myContainer.SetElement(new Label(null) { Text = Util.LOC("UI_consumableChain_Header_Text"), Format= new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Left, 1f), ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner, });
            _consumableChain.Add(_myContainer);

            foreach (string item in _myConsumedItems.Split(Util.STRING_SEPERATOR))
            {
                _myContainer = new HudElementContainer();

                _myContainer.SetElement(new Label(null) { Text = item, Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Left, 1f), ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner, });
                _consumableChain.Add(_myContainer);
            }

            _consumableChain.Visible = true;
        }

        #region Status_Labels (text / tooltips)


        private void UpdateSuitPowerLabel()
        {
            _suitPowerLabel.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Left, 1.1f);
            _suitPowerLabel.Text = Util.autoSpaces(Util.LOC("UI_Suit_Power"), " " + _suitPower, 25);
        }

        private void UpdateSuitOxygenLabel()
        {
            _suitOxygenLabel.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Left, 1.1f);
            _suitOxygenLabel.Text = Util.autoSpaces(Util.LOC("UI_Suit_Oxygen"), " " + _suitOxygen, 25);
        }

        private void UpdateSuitHydrogenLabel()
        {
            _suitHydrogenLabel.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Left, 1.1f);
            _suitHydrogenLabel.Text = Util.autoSpaces(Util.LOC("UI_Suit_Hydrogen"), " " + _suitHydrogen, 25);
        }

        private void UpdateHealthLabel()
        {
            if (_health > 90)
            {
                _healthLabel.Format = new GlyphFormat(Color.DarkGreen, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_90");

                _healthTooltipText = Util.LOC("UI_HealthTooltip_90") + " (" +_health+")";
            }
            else if (_health > 70)
            {
                _healthLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_70");

                _healthTooltipText = Util.LOC("UI_HealthTooltip_70") + " (" + _health + ")";
            }
            else if (_health > 50)
            {
                _healthLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_50");

                _healthTooltipText = Util.LOC("UI_HealthTooltip_50") + " (" + _health + ")";
            }
            else if (_health > 30)
            {
                _healthLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_30");

                _healthTooltipText = Util.LOC("UI_HealthTooltip_30") + " (" + _health + ")";
            }
            else if (_health > 10)
            {
                _healthLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_10");

                _healthTooltipText = Util.LOC("UI_HealthTooltip_10") + " (" + _health + ")";

            }
            else if (_health >= 0)
            {
                _healthLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_00");

                _healthTooltipText = Util.LOC("UI_HealthTooltip_00") + " (" + _health + ")";
            }
            else
            {
                _healthLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _healthLabel.Text = Util.LOC("UI_HealthLabel_na");

                _healthTooltipText = Util.LOC("UI_Tooltip_na");
            }

            //if (Debug.IS_DEBUG)
            //    _healthLabel.Text += " " + _health;
        }

        private void UpdateFoodLabel()
        {
            if (_food > 90)
            {
                _foodLabel.Format = new GlyphFormat(Color.DarkGreen, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_90");

                _foodTooltipText = Util.LOC("UI_FoodTooltip_90") + " (" + _food + ")";
            }
            else if (_food > 70)
            {
                _foodLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_70");

                _foodTooltipText = Util.LOC("UI_FoodTooltip_70") + " (" + _food + ")";
            }
            else if (_food > 50)
            {
                _foodLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_50");

                _foodTooltipText = Util.LOC("UI_FoodTooltip_50") + " (" + _food + ")";
            }
            else if (_food > 30)
            {
                _foodLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_30");

                _foodTooltipText = Util.LOC("UI_FoodTooltip_30") + " (" + _food + ")";
            }
            else if (_food > 10)
            {
                _foodLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_10");

                _foodTooltipText = Util.LOC("UI_FoodTooltip_10") + " (" + _food + ")";
            }
            else if (_food >= 0)
            {
                _foodLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_00");

                _foodTooltipText = Util.LOC("UI_FoodTooltip_00") + " (" + _food + ")";
            }
            else
            {
                _foodLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _foodLabel.Text = Util.LOC("UI_FoodLabel_na");

                _foodTooltipText = Util.LOC("UI_Tooltip_na") + " (" + _food + ")";
            }
            //if (Debug.IS_DEBUG)
            //    _foodLabel.Text += " " + _food;
        }

        private void UpdateWaterLabel()
        {
            if (_water > 90)
            {
                _waterLabel.Format = new GlyphFormat(Color.DarkGreen, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_90");

                _waterTooltipText = Util.LOC("UI_WaterTooltip_90") + " (" + _water + ")";
            }
            else if (_water > 70)
            {
                _waterLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_70");

                _waterTooltipText = Util.LOC("UI_WaterTooltip_70") + " (" + _water + ")";
            }
            else if (_water > 50)
            {
                _waterLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_50");

                _waterTooltipText = Util.LOC("UI_WaterTooltip_50") + " (" + _water + ")";
            }
            else if (_water > 30)
            {
                _waterLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_30");

                _waterTooltipText = Util.LOC("UI_WaterTooltip_30") + " (" + _water + ")";
            }
            else if (_water > 10)
            {
                _waterLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_10");

                _waterTooltipText = Util.LOC("UI_WaterTooltip_10") + " (" + _water + ")";
            }
            else if (_water >= 0)
            {
                _waterLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_00");

                _waterTooltipText = Util.LOC("UI_WaterTooltip_00") + " (" + _water + ")";
            }
            else
            {
                _waterLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _waterLabel.Text = Util.LOC("UI_WaterLabel_na");

                _waterTooltipText = Util.LOC("UI_Tooltip_na");
            }
            //if (Debug.IS_DEBUG)
            //    _waterLabel.Text += " " + _water;
        }

        private void UpdateSleepLabel()
        {
            if (_sleep > 90)
            {
                _sleepLabel.Format = new GlyphFormat(Color.DarkGreen, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_90");
                _sleepLabel.Visible = true;
                _sleepTooltipText = Util.LOC("UI_SleepTooltip_90") + " (" + _sleep + ")";
            }
            else if (_sleep > 70)
            {
                _sleepLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_70");
                _sleepLabel.Visible = true;
                _sleepTooltipText = Util.LOC("UI_SleepTooltip_70") + " (" + _sleep + ")";
            }
            else if (_sleep > 50)
            {
                _sleepLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_50");
                _sleepLabel.Visible = true;
                _sleepTooltipText = Util.LOC("UI_SleepTooltip_50") + " (" + _sleep + ")";
            }
            else if (_sleep > 30)
            {
                _sleepLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_30");
                _sleepLabel.Visible = true;
                _sleepTooltipText = Util.LOC("UI_SleepTooltip_30") + " (" + _sleep + ")";
            }
            else if (_sleep > 10)
            {
                _sleepLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_10");
                _sleepLabel.Visible = true;
                _sleepTooltipText = Util.LOC("UI_SleepTooltip_10") + " (" + _sleep + ")";
            }
            else if (_sleep >= 0)
            {
                _sleepLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_00");
                _sleepLabel.Visible = true;
                _sleepTooltipText = Util.LOC("UI_SleepTooltip_00") + " (" + _sleep + ")";
            }
            else  // Not available or disabled by option
            {
                _sleepLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _sleepLabel.Text = Util.LOC("UI_SleepLabel_na");
                _sleepLabel.Visible = false;
                _sleepTooltipText = Util.LOC("UI_Tooltip_na");
            }
            //if (Debug.IS_DEBUG)
            //    _sleepLabel.Text += " " + _sleep;
        }

        private void UpdateRecoveryLabel()
        {
            if (_recovery > 90)
            {
                _recoveryLabel.Format = new GlyphFormat(Color.DarkGreen, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_90");
                _recoveryLabel.Visible = true;
                _recoveryTooltipText = Util.LOC("UI_RecoveryTooltip_90") + " (" + _recovery + ")";
            }
            else if (_recovery > 70)
            {
                _recoveryLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_70");
                _recoveryLabel.Visible = true;
                _recoveryTooltipText = Util.LOC("UI_RecoveryTooltip_70") + " (" + _recovery + ")";
            }
            else if (_recovery > 50)
            {
                _recoveryLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_50");
                _recoveryLabel.Visible = true;
                _recoveryTooltipText = Util.LOC("UI_RecoveryTooltip_50") + " (" + _recovery + ")";
            }
            else if (_recovery > 30)
            {
                _recoveryLabel.Format = new GlyphFormat(Color.White, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_30");
                _recoveryLabel.Visible = true;
                _recoveryTooltipText = Util.LOC("UI_RecoveryTooltip_30") + " (" + _recovery + ")";
            }
            else if (_recovery > 10)
            {
                _recoveryLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_10");
                _recoveryLabel.Visible = true;
                _recoveryTooltipText = Util.LOC("UI_RecoveryTooltip_10") + " (" + _recovery + ")";
            }
            else if (_recovery >= 0)
            {
                _recoveryLabel.Format = new GlyphFormat(Color.White, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_00");
                _recoveryLabel.Visible = false;
                _recoveryTooltipText = Util.LOC("UI_RecoveryTooltip_00") + " (" + _recovery + ")";
            }
            else
            {
                _recoveryLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _recoveryLabel.Text = Util.LOC("UI_RecoveryLabel_na");
                _recoveryLabel.Visible = false;
                _recoveryTooltipText = Util.LOC("UI_Tooltip_na");
            }
            //if (Debug.IS_DEBUG)
            //    _recoveryLabel.Text += " " + _recovery;
        }

        private void UpdateFatigueLabel()
        {
            if (_fatigue > 180)
            {
                _fatigueLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_180");
                _fatigueLabel.Visible = true;
                _fatigueTooltipText = Util.LOC("UI_FatigueTooltip_180") + " (" + _fatigue + ")";
            }
            else if (_fatigue > 140)
            {
                _fatigueLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_140");
                _fatigueLabel.Visible = true;
                _fatigueTooltipText = Util.LOC("UI_FatigueTooltip_140") + " (" + _fatigue + ")";
            }
            else if (_fatigue > 100)
            {
                _fatigueLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_100");
                _fatigueLabel.Visible = true;
                _fatigueTooltipText = Util.LOC("UI_FatigueTooltip_100") + " (" + _fatigue + ")";
            }
            else if (_fatigue > 60)
            {
                _fatigueLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_60");
                _fatigueLabel.Visible = true;
                _fatigueTooltipText = Util.LOC("UI_FatigueTooltip_60") + " (" + _fatigue + ")";
            }
            else if (_fatigue > 20)
            {
                _fatigueLabel.Format = new GlyphFormat(Color.White, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_20");
                _fatigueLabel.Visible = true;
                _fatigueTooltipText = Util.LOC("UI_FatigueTooltip_20") + " (" + _fatigue + ")";
            }
            else if (_fatigue >= 0)
            {
                _fatigueLabel.Format = new GlyphFormat(Color.White, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_00");
                _fatigueLabel.Visible = false;
                _fatigueTooltipText = Util.LOC("UI_FatigueTooltip_00") + " (" + _fatigue + ")";
            }
            else
            {
                _fatigueLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _fatigueLabel.Text = Util.LOC("UI_FatigueLabel_na");
                _fatigueLabel.Visible = false;
                _fatigueTooltipText = Util.LOC("UI_Tooltip_na");
            }
            //if (Debug.IS_DEBUG)
            //    _fatigueLabel.Text += " " + _fatigue;
        }

        private void UpdateBloatingLabel()
        {
            if (_bloating > 90)
            {
                _bloatingLabel.Format = new GlyphFormat(Color.Red, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_90");
                _bloatingLabel.Visible = true;
                _bloatingTooltipText = Util.LOC("UI_BloatingTooltip_90") + " (" + _bloating + ")";
            }
            else if (_bloating > 70)
            {
                _bloatingLabel.Format = new GlyphFormat(Color.Yellow, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_70");
                _bloatingLabel.Visible = true;
                _bloatingTooltipText = Util.LOC("UI_BloatingTooltip_70") + " (" + _bloating + ")";
            }
            else if (_bloating > 50)
            {
                _bloatingLabel.Format = new GlyphFormat(Color.LightGreen, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_50");
                _bloatingLabel.Visible = true;
                _bloatingTooltipText = Util.LOC("UI_BloatingTooltip_50") + " (" + _bloating + ")";
            }
            else if (_bloating > 30)
            {
                _bloatingLabel.Format = new GlyphFormat(Color.Green, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_30");
                _bloatingLabel.Visible = true;
                _bloatingTooltipText = Util.LOC("UI_BloatingTooltip_30") + " (" + _bloating + ")";
            }
            else if (_bloating > 10)
            {
                _bloatingLabel.Format = new GlyphFormat(Color.White, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_10");
                _bloatingLabel.Visible = true;
                _bloatingTooltipText = Util.LOC("UI_BloatingTooltip_10") + " (" + _bloating + ")";
            }
            else if (_bloating >= 0)
            {
                _bloatingLabel.Format = new GlyphFormat(Color.White, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_00");
                _bloatingLabel.Visible = false;
                _bloatingTooltipText = Util.LOC("UI_BloatingTooltip_00") + " (" + _bloating + ")";
            }
            else
            {
                _bloatingLabel.Format = new GlyphFormat(Color.DarkViolet, TextAlignment.Left, 1.1f);
                _bloatingLabel.Text = Util.LOC("UI_BloatingLabel_na");
                _bloatingLabel.Visible = false;
                _bloatingTooltipText = Util.LOC("UI_Tooltip_na");
            }

            //if (Debug.IS_DEBUG)
            //    _bloatingLabel.Text += " " + _bloating;
        }

        #endregion


        public CharacterStatus(HudParentBase parent) : base(parent)
        {
            BodyColor = new Color(37, 46, 53);
            Size = new Vector2(700f, 300f);
            //ZOffset = 0;
            header.Height = 30f;
            header.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Center, textSize: 1.4f);
            header.Color = new Color(37, 46, 53);
            header.Text = Util.LOC("UI_Header_Text") + deactivationHint();
            border.Color = new Color(59, 70, 89);
            border.Thickness = 1f;
            Offset = new Vector2(0f, -200f);
            ShareCursor = true;

            // BROKE: Why close button does only work one time? after close all overlays/tooltips/ buttons are without function!
            /*_bottomCloseButton = new LabelBoxButton(header)
            {
                //Text = Util.LOC("UI_Button_Close"),
                Format = new GlyphFormat(GlyphFormat.Blueish.Color, textSize: 1.4f),
                Text = "X",
                //ZOffset = 0,
                TextPadding = new Vector2(40, 0),
                ZOffset = 100,
                ParentAlignment = ParentAlignments.Center | ParentAlignments.Right | ParentAlignments.Inner,
                Color = TerminalFormatting.OuterSpace,
                HighlightColor = TerminalFormatting.Atomic,
            };
            _bottomCloseButton.MouseInput.LeftClicked += OnCloseClicked;
            */

            _tooltipBox = new LabelBox(this)
            {
                Visible = false,
                Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Left, 1.1f),
                ZOffset = 10,
                ShareCursor = true,
                Color = TerminalFormatting.OuterSpace,
                //Color = new Color(41, 54, 62, 230),
                VertCenterText = true,
                TextPadding = new Vector2(5f, 5f),
                AutoResize = true,
                ParentAlignment = ParentAlignments.Left | ParentAlignments.Inner,
                CanIgnoreMasking = true,
            };


            // Status labels (left)
            _healthLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };
            _foodLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };
            _waterLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };
            _sleepLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };
            _recoveryLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };
            _fatigueLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };
            _bloatingLabel = new Label(null) { ParentAlignment = ParentAlignments.Left, Padding = new Vector2(40f, 0f), ShareCursor = true, UseCursor = true };

            // Labels on the right section
            // TODO: besser center und nur y-korrektur?
            _suitPowerLabel = new Label(this) { ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner, Offset = new Vector2(350f, -40f) };
            _suitOxygenLabel = new Label(this) { ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner, Offset = new Vector2(350f, -70f), };
            _suitHydrogenLabel = new Label(this) { ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner, Offset = new Vector2(350f, -100f) };


            _statusChain = new HudChain(true, this)
            {
                DimAlignment = DimAlignments.Width,
                ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner,
                Offset = new Vector2(10f, -40f),
                Spacing = 15f,
                ShareCursor = true,

                CollectionContainer =
                {
                    _healthLabel,
                    _foodLabel,
                    _waterLabel,
                    _sleepLabel,
                    _recoveryLabel,
                    _fatigueLabel,
                    _bloatingLabel,
                }
            };

            _consumableChain = new HudChain(true, this)
            {
                DimAlignment = DimAlignments.Width,
                ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner,
                Offset = new Vector2(350f, -130f),
                Spacing = 5f,
                ShareCursor = true,  
                Visible = false,                

            };       

        }

        private string deactivationHint()
        {
            string ret = " ";
            if (MyAPIGateway.Session.CreativeMode)
                ret += "Creative ON";
            if (MyAPIGateway.Session.SessionSettings.FoodConsumptionRate == 0)
                ret += "  Food DISABLED";

            return ret;
        }

        protected override void HandleInput(Vector2 cursorPos)
        {

            base.HandleInput(cursorPos);
            // TODO: Check correct offset by different status
            if (_healthLabel.IsMousedOver)
            {
                _tooltipBox.Text = _healthTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _healthLabel.Offset.Y);

            }
            else if (_foodLabel.IsMousedOver)
            {
                _tooltipBox.Text = _foodTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _foodLabel.Offset.Y);
            }
            else if (_waterLabel.IsMousedOver)
            {
                _tooltipBox.Text = _waterTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _waterLabel.Offset.Y);
            }
            else if (_sleepLabel.IsMousedOver)
            {
                _tooltipBox.Text = _sleepTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _sleepLabel.Offset.Y);
            }
            else if (_recoveryLabel.IsMousedOver)
            {
                _tooltipBox.Text = _recoveryTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _recoveryLabel.Offset.Y);
            }
            else if (_fatigueLabel.IsMousedOver)
            {
                _tooltipBox.Text = _fatigueTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _fatigueLabel.Offset.Y);
            }
            else if (_bloatingLabel.IsMousedOver)
            {
                _tooltipBox.Text = _bloatingTooltipText;
                _tooltipBox.Visible = true;
                _tooltipBox.Offset = new Vector2(25f, _bloatingLabel.Offset.Y);
            }
            else
            {
                _tooltipBox.Visible = false;
            }

        }

        private void OnCloseClicked(object sender, EventArgs args)
        {
            RequestClose?.Invoke();
            //this.Visible = false;
            //RequestClose(); 
        }

        protected override void Layout()
        {
            base.Layout();
        }




    }
}