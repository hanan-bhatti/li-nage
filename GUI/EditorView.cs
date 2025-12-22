using System.Windows.Forms;
using Linage.Core;

namespace Linage.GUI
{
    public class EditorView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public AutoCompletionService AutoCompleter { get; set; } = new AutoCompletionService();

        public EditorView()
        {
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
        }
    }
}
