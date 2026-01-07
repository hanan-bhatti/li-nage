using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace Linage.GUI.Theme
{
    public class ThemeEditorDialog : Form
    {
        private ComboBox _themeSelector;
        private FlowLayoutPanel _colorsPanel;
        private ThemeConfig _editingTheme;
        private bool _isDirty;

        public ThemeEditorDialog()
        {
            InitializeComponent();
            LoadThemes();
        }

        private void InitializeComponent()
        {
            this.Text = "Theme Editor";
            this.Size = new Size(500, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;

            // Top Panel (Selector)
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            var lblTheme = new Label { Text = "Preset:", Location = new Point(10, 15), AutoSize = true, ForeColor = ModernTheme.TextPrimary };
            
            _themeSelector = new ComboBox 
            { 
                Location = new Point(70, 12), 
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary
            };
            _themeSelector.SelectedIndexChanged += OnThemeChanged;

            var btnSave = new Button { Text = "Save", Location = new Point(290, 10), Width = 80, FlatStyle = FlatStyle.Flat, BackColor = ModernTheme.PrimaryColor, ForeColor = Color.White };
            btnSave.Click += OnSave;
            
            var btnApply = new Button { Text = "Apply", Location = new Point(380, 10), Width = 80, FlatStyle = FlatStyle.Flat, BackColor = ModernTheme.SurfaceLight, ForeColor = ModernTheme.TextPrimary };
            btnApply.Click += (s, e) => ThemeManager.Instance.SwitchTheme(_editingTheme);

            topPanel.Controls.AddRange(new Control[] { lblTheme, _themeSelector, btnSave, btnApply });

            // Main Panel (Colors)
            _colorsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = ModernTheme.BackColor
            };

            this.Controls.Add(_colorsPanel);
            this.Controls.Add(topPanel);
        }

        private void LoadThemes()
        {
            _themeSelector.Items.Add("Current");
            _themeSelector.Items.Add("VS Code Dark (Default)");
            _themeSelector.Items.Add("VS Code Light");
            _themeSelector.SelectedIndex = 0;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (_themeSelector.SelectedIndex == 0) // Current
            {
                // Clone current to avoid direct mutation until applied
                var current = ThemeManager.Instance.CurrentTheme;
                _editingTheme = CloneTheme(current);
            }
            else if (_themeSelector.SelectedIndex == 1) // Dark
            {
                _editingTheme = ThemeManager.GetDefaultDarkTheme();
            }
            else if (_themeSelector.SelectedIndex == 2) // Light
            {
                _editingTheme = ThemeManager.GetLightTheme();
            }

            GenerateColorEditors();
        }

        private ThemeConfig CloneTheme(ThemeConfig source)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ThemeConfig>(
                Newtonsoft.Json.JsonConvert.SerializeObject(source));
        }

        private void GenerateColorEditors()
        {
            _colorsPanel.Controls.Clear();
            if (_editingTheme == null) return;

            // Use reflection to get all string properties (colors)
            var properties = typeof(ThemeConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == "Name") continue;
                if (prop.PropertyType != typeof(string)) continue;

                string hexColor = (string)prop.GetValue(_editingTheme);
                AddColorEditor(prop.Name, hexColor, (newColor) => 
                {
                    prop.SetValue(_editingTheme, newColor);
                    _isDirty = true;
                });
            }
        }

        private void AddColorEditor(string name, string hexColor, Action<string> onColorChanged)
        {
            var panel = new Panel { Width = 440, Height = 40, Margin = new Padding(0, 0, 0, 5) };
            
            var lblName = new Label 
            { 
                Text = SplitCamelCase(name), 
                Location = new Point(5, 10), 
                AutoSize = true,
                ForeColor = ModernTheme.TextSecondary 
            };

            var pnlPreview = new Panel 
            { 
                Location = new Point(200, 5), 
                Size = new Size(60, 30), 
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            
            try { pnlPreview.BackColor = ColorTranslator.FromHtml(hexColor); } catch { }

            var txtHex = new TextBox
            {
                Text = hexColor,
                Location = new Point(270, 8),
                Width = 100,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Event Handlers
            pnlPreview.Click += (s, e) => 
            {
                using (var cd = new ColorDialog())
                {
                    cd.Color = pnlPreview.BackColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        string newHex = ThemeManager.ColorToHex(cd.Color);
                        pnlPreview.BackColor = cd.Color;
                        txtHex.Text = newHex;
                        onColorChanged(newHex);
                    }
                }
            };

            txtHex.TextChanged += (s, e) =>
            {
                try 
                {
                    var color = ColorTranslator.FromHtml(txtHex.Text);
                    pnlPreview.BackColor = color;
                    onColorChanged(txtHex.Text);
                }
                catch { }
            };

            panel.Controls.Add(lblName);
            panel.Controls.Add(pnlPreview);
            panel.Controls.Add(txtHex);
            _colorsPanel.Controls.Add(panel);
        }

        private string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        private void OnSave(object sender, EventArgs e)
        {
            ThemeManager.Instance.SwitchTheme(_editingTheme);
            Linage.Infrastructure.Services.NotificationManager.Instance.ShowSuccess("Theme Manager", "Theme saved successfully!");
        }
    }
}
