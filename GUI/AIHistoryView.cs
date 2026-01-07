using System.Collections.Generic;
using System.Windows.Forms;
using Linage.Core;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public class AIHistoryView : UserControl, IThemable
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public List<AIAccessLog> Logs { get; set; } = new List<AIAccessLog>();
        
        public AIHistoryView()
        {
            this.BackColor = System.Drawing.Color.AliceBlue;
        }

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;
        }
    }
}
