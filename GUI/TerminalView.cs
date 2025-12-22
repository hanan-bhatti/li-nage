using System.Windows.Forms;

namespace Linage.GUI
{
    public class TerminalView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public CommandDispatcher Dispatcher { get; set; } = new CommandDispatcher();

        public TerminalView()
        {
            this.BackColor = System.Drawing.Color.Black;
            this.ForeColor = System.Drawing.Color.White;
        }
    }
}
