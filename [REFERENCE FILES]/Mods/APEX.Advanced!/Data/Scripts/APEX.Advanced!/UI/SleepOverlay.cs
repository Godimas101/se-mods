using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using VRageMath;

namespace APEX.Advanced.Client.MySleep
{
    /// <summary>
    /// Erzeugt einen "Augenlid"-Effekt, indem mehrere TexturedBoxen mit
    /// unterschiedlicher Transparenz gestapelt werden, um einen weichen Rand zu simulieren.
    /// </summary>
    public class SleepOverlay : WindowBase
    {
        private const int LAYERS_PER_BAR = 30;
        private readonly List<TexturedBox> _topBarLayers;
        private readonly List<TexturedBox> _bottomBarLayers;

        public static int BlinkChancePerSecond = 15; // z.B. 15%
        private const int BLINK_DURATION_TICKS = 17; // ca. 300ms
        private const int FADE_DURATION_TICKS = 4;
        private bool _isBlinking = false;
        private int _blinkTimer = 0;

        public SleepOverlay(HudParentBase parent) : base(parent)
        {
            // --- Konfiguriere den Container ---
            Size = new Vector2(HudMain.ScreenWidth, HudMain.ScreenHeight);
            //Size = new Vector2(HudMain.ScreenWidth / HudMain.ResScale, HudMain.ScreenHeight / HudMain.ResScale);
            header.Text = "";
            CanDrag = false;
            AllowResizing = false;
            border.Color = new Color(0, 0, 0, 0);
            header.Color = new Color(0, 0, 0, 0);
            BodyColor = new Color(0, 0, 0, 0);
            ShareCursor = true;
            Visible = false;
            UseCursor = false;
            MouseInput.InputEnabled = false;

            // --- Erstelle die Ebenen für die Balken ---
            _topBarLayers = new List<TexturedBox>();
            _bottomBarLayers = new List<TexturedBox>();

            for (int i = 0; i < LAYERS_PER_BAR; i++)
            {
                // Erstelle eine TexturedBox für den oberen Balken
                var topLayer = new TexturedBox(this)
                {
                    ParentAlignment = ParentAlignments.Top | ParentAlignments.Inner,
                };
                _topBarLayers.Add(topLayer);

                // Erstelle eine TexturedBox für den unteren Balken
                var bottomLayer = new TexturedBox(this)
                {
                    ParentAlignment = ParentAlignments.Bottom | ParentAlignments.Inner,
                };
                _bottomBarLayers.Add(bottomLayer);
            }
        }

        public void TriggerBlink()
        {
            // Starte keinen neuen Blink, wenn bereits einer läuft
            if (_isBlinking) return;

            _isBlinking = true;
            _blinkTimer = BLINK_DURATION_TICKS;
        }

        /// <summary>
        /// Aktualisiert die Höhe und Transparenz der Balken.
        /// </summary>
        public void UpdateEffect(float sleepValue)
        {
            // TODO: Correct scaling for sleep 1080p != 4k visuals, its semi ok...
            sleepValue /= HudMain.ResScale;

            if (sleepValue <= 0f)
            {
                this.Visible = true;
                this.BodyColor = Color.Black;
                foreach (var bar in _topBarLayers) bar.Visible = false;
                foreach (var bar in _bottomBarLayers) bar.Visible = false;
                return;
            }
            if (sleepValue > 20f)
            {
                this.Visible = false;
                return;
            }

            this.Visible = true;
            this.BodyColor = new Color(0, 0, 0, 0);
            foreach (var bar in _topBarLayers) bar.Visible = true;
            foreach (var bar in _bottomBarLayers) bar.Visible = true;

            // Stärke von 0.0 (offen) bis 1.0 (geschlossen)
            float strength = 1f - (sleepValue / 20f);
            strength = MathHelper.Clamp(strength, 0f, 1f);

            // Die Gesamthöhe, die der Effekt-Balken abdecken soll
            float totalBarHeight = (HudMain.ScreenHeight) / 2f * strength;

            // Aktualisiere jede der 5 Ebenen pro Balken
            for (int i = 0; i < LAYERS_PER_BAR; i++)
            {
                // 't' geht von 1.0 (am Bildschirmrand) bis 0.0 (zur Mitte hin)
                float t = 1f - ((float)i / (LAYERS_PER_BAR - 1));

                // 1. Berechne die Transparenz
                // Die TexturedBox am Bildschirmrand (t=1) ist am dunkelsten,
                // die TexturedBox zur Mitte hin (t=0) ist am durchsichtigsten.
                byte alpha = (byte)MathHelper.Lerp(0, 110, t); // Jede Schicht ist transparent (110)


                // 2. Berechne die Größe
                // Die TexturedBox am Rand (t=1) hat die volle Höhe, die zur Mitte hin (t=0) hat die Höhe 0
                float currentHeight = totalBarHeight * t;
                Vector2 currentSize = new Vector2(HudMain.ScreenWidth, currentHeight);

                _topBarLayers[i].Color = new Color(0, 0, 0, alpha);
                _topBarLayers[i].Size = currentSize;

                _bottomBarLayers[i].Color = new Color(0, 0, 0, alpha);
                _bottomBarLayers[i].Size = currentSize;
            }

            if (_isBlinking)
            {
                _blinkTimer--;
                if (_blinkTimer <= 0)
                {
                    _isBlinking = false;
                }
                else
                {
                    float alphaPercent = 0f;

                    if (_blinkTimer > BLINK_DURATION_TICKS - FADE_DURATION_TICKS)
                    {
                        int ticksIntoFade = BLINK_DURATION_TICKS - _blinkTimer;
                        alphaPercent = (float)ticksIntoFade / FADE_DURATION_TICKS;
                    }
                    else if (_blinkTimer <= FADE_DURATION_TICKS)
                    {
                        alphaPercent = (float)_blinkTimer / FADE_DURATION_TICKS;
                    }
                    else
                    {
                        alphaPercent = 1f;
                    }

                    // Clamp the value to ensure it's between 0 and 1.
                    alphaPercent = MathHelper.Clamp(alphaPercent, 0f, 1f);
                    byte alpha = (byte)(alphaPercent * 255);

                    this.Visible = true;
                    Color blinkColor = new Color(0, 0, 0, alpha);
                    this.BodyColor = blinkColor;
                    this.header.Color = blinkColor;
                    this.border.Color = blinkColor;
                    return;
                }
            }
        }
    }
}