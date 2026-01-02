using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Linage.GUI.Controls
{
    public class EnhancedRichTextBox : RichTextBox
    {
        public event EventHandler VScrollHappened;
        public event EventHandler PaintHappened;

        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_PAINT = 0x000F;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                VScrollHappened?.Invoke(this, EventArgs.Empty);
            }
            
            if (m.Msg == WM_PAINT)
            {
                PaintHappened?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
