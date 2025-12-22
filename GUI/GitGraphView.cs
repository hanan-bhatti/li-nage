using System.Collections.Generic;
using System.Windows.Forms;
using Linage.Core;

namespace Linage.GUI
{
    public class GitGraphView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public List<Commit> Commits { get; set; } = new List<Commit>();
        public List<Branch> Branches { get; set; } = new List<Branch>();

        public GitGraphView()
        {
            this.BackColor = System.Drawing.Color.LightGray;
        }
    }
}
