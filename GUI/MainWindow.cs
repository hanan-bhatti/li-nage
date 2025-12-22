using System.Windows.Forms;

namespace Linage.GUI
{
    public partial class MainWindow : Form
    {
        public string Id { get; set; } = "MAIN_001";
        public string State { get; set; } = "Normal";

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
