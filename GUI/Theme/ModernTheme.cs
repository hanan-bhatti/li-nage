using System.Drawing;
using System.Windows.Forms;

namespace Linage.GUI.Theme
{
    public static class ModernTheme
    {
        // VS Code Dark Modern Palette
        public static Color BackColor = Color.FromArgb(30, 30, 30);        // Editor Background #1E1E1E (Using slightly lighter for WinForms contrast)
        public static Color SurfaceColor = Color.FromArgb(37, 37, 38);     // Side Bar #252526
        public static Color SurfaceLight = Color.FromArgb(51, 51, 51);     // Activity Bar #333333
        public static Color ActivityBarColor = Color.FromArgb(51, 51, 51); // Activity Bar Background

        public static Color TextPrimary = Color.FromArgb(204, 204, 204);   // #CCCCCC
        public static Color TextSecondary = Color.FromArgb(150, 150, 150); // #969696
        public static Color TextDisabled = Color.FromArgb(100, 100, 100);
        
        public static Color PrimaryColor = Color.FromArgb(0, 122, 204);    // VS Code Blue #007ACC
        public static Color PrimaryDark = Color.FromArgb(0, 90, 158);
        public static Color StatusBarColor = Color.FromArgb(0, 122, 204);  // Status Bar Blue
        
        public static Color ErrorColor = Color.FromArgb(244, 135, 113);    // Error Red
        public static Color SuccessColor = Color.FromArgb(137, 209, 133);  // Success Green
        public static Color WarningColor = Color.FromArgb(204, 167, 0);    // Warning Yellow

        public static Color BorderColor = Color.FromArgb(64, 64, 64);      // Borders #404040
        public static Color SplitterColor = Color.FromArgb(45, 45, 45);    // Splitter

        // Tabs
        public static Color TabActive = Color.FromArgb(30, 30, 30);        // Match BackColor
        public static Color TabInactive = Color.FromArgb(45, 45, 45);      // Slightly lighter
        public static Color TabHover = Color.FromArgb(40, 40, 40);

        // Scrollbars
        public static Color ScrollBarBack = Color.FromArgb(30, 30, 30);
        public static Color ScrollBarThumb = Color.FromArgb(66, 66, 66);
        public static Color ScrollBarThumbHover = Color.FromArgb(79, 79, 79);
        public static Color ScrollBarThumbActive = Color.FromArgb(100, 100, 100);

        // --- Backward Compatibility Mappings for Legacy Views ---
        public static Color PanelColor => SurfaceColor;
        public static Color TextColor => TextPrimary;
        public static Color MutedText => TextSecondary;
        public static Color AccentColor => PrimaryColor;
        public static Color AccentHover => PrimaryDark;
        public static Color InputBack => Color.FromArgb(60, 60, 60); // Input field bg
        public static Color SelectionBack => Color.FromArgb(9, 71, 113); // Selection


        // Typography
        // Segoe UI is standard, but we adjust sizes and weights for hierarchy
        private static Font _fontH1, _fontH2, _fontBody, _fontSmall, _fontCode, _fontIcon;

        public static Font FontH1 => _fontH1 ?? (_fontH1 = new Font("Segoe UI", 16f, FontStyle.Bold));
        public static Font FontH2 => _fontH2 ?? (_fontH2 = new Font("Segoe UI", 12f, FontStyle.Bold));
        public static Font FontBody => _fontBody ?? (_fontBody = new Font("Segoe UI", 10f, FontStyle.Regular));
        public static Font FontSmall => _fontSmall ?? (_fontSmall = new Font("Segoe UI", 8.5f, FontStyle.Regular));
        
        public static Font FontCode 
        {
            get 
            {
                if (_fontCode == null)
                {
                    try { _fontCode = new Font("Consolas", 10f, FontStyle.Regular); }
                    catch { _fontCode = new Font(FontFamily.GenericMonospace, 10f, FontStyle.Regular); }
                }
                return _fontCode;
            }
        }
        
        public static Font FontIcon 
        {
             get 
             {
                 if (_fontIcon == null)
                 {
                     try { _fontIcon = new Font("Segoe MDL2 Assets", 12f, FontStyle.Regular); }
                     catch { _fontIcon = new Font("Arial", 12f, FontStyle.Regular); }
                 }
                 return _fontIcon;
             }
        }

        // --- Backward Compatibility Fonts ---
        public static Font MainFont => FontBody;
        public static Font HeaderFont => FontH2;
        public static Font CodeFont => FontCode;

        public static void Apply(Control control)
        {
            control.BackColor = BackColor;
            control.ForeColor = TextPrimary;
            control.Font = FontBody;

            foreach (Control child in control.Controls)
            {
                Apply(child);
            }
        }
    }
}