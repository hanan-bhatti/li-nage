using System;
using System.Drawing;
using System.Windows.Forms;

namespace Linage.GUI.Helpers
{
    public static class WatermarkHelper
    {
        public static void AddWatermarkLabel(Control control, string fileName)
        {
            if (control == null) return;

            var label = new Label
            {
                Text = $"{control.GetType().Name} ({fileName})",
                AutoSize = true,
                ForeColor = Color.DimGray,
                BackColor = Color.FromArgb(240, 240, 240), // Light gray background for visibility
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                TextAlign = ContentAlignment.BottomRight,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(2)
            };

            // Add to control
            control.Controls.Add(label);
            label.BringToFront();

            // Initial positioning
            UpdatePosition(control, label);

            // Keep it in corner on resize
            control.Resize += (s, e) => UpdatePosition(control, label);
        }

        private static void UpdatePosition(Control parent, Label label)
        {
            // 5px margin from bottom-right
            label.Location = new Point(
                parent.ClientSize.Width - label.Width - 5,
                parent.ClientSize.Height - label.Height - 5
            );
        }
    }
}
