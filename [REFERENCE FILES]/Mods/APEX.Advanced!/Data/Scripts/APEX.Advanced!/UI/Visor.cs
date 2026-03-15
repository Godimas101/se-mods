using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using VRage.Utils;
using VRageMath;

namespace APEX.Advanced.Client.MyVisor
{
    public class Visor : TexturedBox
    {
        public Visor(HudParentBase parent) : base(parent)
        {
            Size = new Vector2(HudMain.ScreenWidth / HudMain.ResScale, HudMain.ScreenHeight / HudMain.ResScale);
            ParentAlignment = ParentAlignments.Top | ParentAlignments.Bottom | ParentAlignments.Left | ParentAlignments.Right | ParentAlignments.Inner;

            Material = new Material(MyStringId.GetOrCompute("APEXHelmetVignette"), Vector2.One);
            ShareCursor = true;

            Color = Color.White;
            Visible = false;
        }

        /// <summary>
        /// Schaltet die Sichtbarkeit des Visiers um.
        /// </summary>
        public void UpdateVisorState(bool isHelmetOn, bool isFirstPerson)
        {
            if (isFirstPerson)
                this.Visible = isHelmetOn;
            else
                this.Visible = false;
        }
    }
}