using System.Collections.Generic;
using System.Windows.Forms;
using Linage.Core;

namespace Linage.GUI
{
    public class DebugView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public ErrorTrace CurrentTrace { get; set; }
        public List<ExternalResource> Resources { get; set; } = new List<ExternalResource>();

        public DebugView()
        {
            this.BorderStyle = BorderStyle.Fixed3D;
        }
    }
}
