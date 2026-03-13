using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using System;
using VRage.Utils;
using VRageMath;


namespace APEX.Advanced.Client.MyWarnings
{
    public class Warnings : WindowBase
    {
        private readonly HudChain _warningChain;
        private TexturedBox _foodWarning;
        private TexturedBox _waterWarning;
        private TexturedBox _bloatingWarning;
        private int _iconSize = ConfigManager.CConfig.UserInterfaceWarningIconSquareLength;


        public int Tick { get; internal set; }

        #region Status values
        private int _food;
        public int Food
        {
            get { return _food; }
            set
            {
                _food = value;
                UpdateWarningIcon(_foodWarning, _food, 10, 3);
            }
        }

        private int _water;
        public int Water
        {
            get { return _water; }
            set
            {
                _water = value;
                UpdateWarningIcon(_waterWarning, _water, 12, 4);
            }
        }

        private int _bloating;
        public int Bloating
        {
            get { return _bloating; }
            set
            {
                _bloating = value;                
                UpdateWarningIcon(_bloatingWarning, 100 - _bloating, 18, 10); // inverted values: 100-80=20, 100-90=10
            }
        }


        /// <summary>
        /// Aktualisiert ein Warn-Icon basierend auf einem Schwellenwert und wendet einen Blink-Effekt an.
        /// </summary>
        /// <param name="warningIcon">Das zu aktualisierende TexturedBox-Element.</param>
        /// <param name="currentValue">Der aktuelle Wert des zu prüfenden Status (z.B. Food).</param>
        /// <param name="warningThreshold">Der Schwellenwert für die orange Warnung (z.B. 13).</param>
        /// <param name="criticalThreshold">Der Schwellenwert für die rote, blinkende Warnung (z.B. 4).</param>
        private void UpdateWarningIcon(TexturedBox warningIcon, int currentValue, int warningThreshold, int criticalThreshold)
        {
            if (currentValue < criticalThreshold)
            {
                // Level 3: Critical (red, flashy)
                warningIcon.Visible = true;

                // --- BLINK-LOGIK MIT MODULO UND SINUS ---
                // 'Tick % 60' erzeugt einen Zyklus, der jede Sekunde von 0 bis 59 läuft.
                // Math.Sin erzeugt daraus eine sanfte Welle von -1 bis 1.
                double sin = Math.Sin((Tick % 60) * (Math.PI * 2) / 60.0);

                // Wir wandeln die Welle [-1, 1] in einen Alpha-Bereich [100, 255] um.
                float percent = (float)(sin + 1.0) / 2f;
                byte alpha = (byte)MathHelper.Lerp(100, 255, percent);

                warningIcon.Color = new Color(255, 0, 0, alpha); // Rot mit variablem Alpha
            }
            else if (currentValue < warningThreshold)
            {
                // Stufe 2: Warnung (Orange, statisch)
                warningIcon.Visible = true;
                warningIcon.Color = Color.Red;
            }
            else
            {
                // Stufe 1: Alles OK
                warningIcon.Visible = false;
            }
        }

        #endregion

        public Warnings(HudParentBase parent) : base(parent)
        {
            // --- Configure the WindowBase container ---
            // Make the window itself invisible and non-interactive, as it's just a holder.
            BodyColor = Color.Transparent;            
            border.Color = Color.Transparent;
            header.Color = Color.Transparent;
            header.Text = "";
            ZOffset = 0;
            CanDrag = false;
            AllowResizing = false;
            Size = new Vector2(700f, 128f);
            Offset = new Vector2(0f, 275f);
            ShareCursor = true;
            UseCursor = false;
            MouseInput.InputEnabled = false;


            // Warning TexturedBox'es 
            _foodWarning = new TexturedBox(null)
            {
                Material = new Material(MyStringId.GetOrCompute("FoodWarning"), Vector2.One),
                Color = Color.Red,
                ZOffset = 10,
                ParentAlignment = ParentAlignments.Center | ParentAlignments.Inner,
                Visible = false,
                Size = new Vector2(_iconSize, _iconSize),
                ShareCursor = true,
                UseCursor = false
            };

            _waterWarning = new TexturedBox(null)
            {
                Material = new Material(MyStringId.GetOrCompute("WaterWarning"), Vector2.One),
                Color = Color.Red,
                ZOffset = 10,
                ParentAlignment = ParentAlignments.Center | ParentAlignments.Inner,
                Visible = false,
                Size = new Vector2(_iconSize, _iconSize),
                ShareCursor = true,
                UseCursor = false
            };

            _bloatingWarning = new TexturedBox(null)
            {
                Material = new Material(MyStringId.GetOrCompute("BloatingWarning"), Vector2.One),
                Color = Color.Red,
                ZOffset = 10,
                ParentAlignment = ParentAlignments.Center | ParentAlignments.Inner,
                Visible = false,
                Size = new Vector2(_iconSize, _iconSize),
                ShareCursor = true,
                UseCursor = false
            };

            // --- HudChain for the boxes ---
            _warningChain = new HudChain(false, this)
            {
                DimAlignment = DimAlignments.Height,
                // The width of the parent could very well be greater than the width of the controls.
                ParentAlignment = ParentAlignments.Center | ParentAlignments.Inner,
                // Spacing between icons.
                Spacing = 15f,

                CollectionContainer =
                {
                    _foodWarning,
                    _waterWarning,
                    _bloatingWarning,
                }
            };
        }

    }
}