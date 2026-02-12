using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PickleCalLG
{
    /// <summary>
    /// Modern dark theme utility for WinForms controls.
    /// Provides consistent VS Code-inspired dark styling across the application.
    /// </summary>
    public static class DarkTheme
    {
        // ── Color Palette ──
        public static readonly Color Background = Color.FromArgb(30, 30, 30);
        public static readonly Color Surface = Color.FromArgb(45, 45, 48);
        public static readonly Color SurfaceLight = Color.FromArgb(62, 62, 66);
        public static readonly Color SurfaceDark = Color.FromArgb(22, 22, 22);
        public static readonly Color Border = Color.FromArgb(67, 67, 70);
        public static readonly Color BorderLight = Color.FromArgb(80, 80, 84);

        public static readonly Color TextPrimary = Color.FromArgb(230, 230, 230);
        public static readonly Color TextSecondary = Color.FromArgb(160, 160, 160);
        public static readonly Color TextMuted = Color.FromArgb(110, 110, 110);

        public static readonly Color Accent = Color.FromArgb(0, 122, 204);
        public static readonly Color AccentLight = Color.FromArgb(28, 151, 234);
        public static readonly Color AccentDark = Color.FromArgb(0, 88, 160);

        public static readonly Color Success = Color.FromArgb(76, 175, 80);
        public static readonly Color SuccessDark = Color.FromArgb(56, 142, 60);
        public static readonly Color Error = Color.FromArgb(244, 67, 54);
        public static readonly Color ErrorDark = Color.FromArgb(211, 47, 47);
        public static readonly Color Warning = Color.FromArgb(255, 152, 0);

        public static readonly Color InputBackground = Color.FromArgb(51, 51, 55);
        public static readonly Color ListBackground = Color.FromArgb(25, 25, 28);

        // ── Fonts ──
        public static readonly Font DefaultFont = new("Segoe UI", 9.5f);
        public static readonly Font HeaderFont = new("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font SubHeaderFont = new("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font MonoFont = new("Cascadia Mono", 9f);
        public static readonly Font SmallFont = new("Segoe UI", 8.5f);

        /// <summary>
        /// Apply dark theme to a form and all its child controls recursively.
        /// </summary>
        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;
            form.Font = DefaultFont;
            ApplyRecursive(form.Controls);
        }

        /// <summary>
        /// Apply dark theme to a collection of controls recursively.
        /// </summary>
        public static void ApplyRecursive(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                ApplyToControl(control);
            }
        }

        private static void ApplyToControl(Control control)
        {
            switch (control)
            {
                case TabControl tab:
                    tab.DrawMode = TabDrawMode.OwnerDrawFixed;
                    tab.SizeMode = TabSizeMode.Fixed;
                    tab.ItemSize = new Size(110, 36);
                    tab.Padding = new Point(8, 4);
                    tab.DrawItem -= TabControl_DrawItem;
                    tab.DrawItem += TabControl_DrawItem;
                    tab.BackColor = Background;
                    break;

                case TabPage page:
                    page.BackColor = Background;
                    page.ForeColor = TextPrimary;
                    break;

                case GroupBox group:
                    group.BackColor = Surface;
                    group.ForeColor = AccentLight;
                    group.FlatStyle = FlatStyle.Flat;
                    group.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    break;

                case Button button:
                    ApplyToButton(button);
                    break;

                case TextBox textBox:
                    textBox.BackColor = InputBackground;
                    textBox.ForeColor = TextPrimary;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox combo:
                    combo.BackColor = InputBackground;
                    combo.ForeColor = TextPrimary;
                    combo.FlatStyle = FlatStyle.Flat;
                    break;

                case ListBox listBox:
                    listBox.BackColor = ListBackground;
                    listBox.ForeColor = TextPrimary;
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                    listBox.Font = MonoFont;
                    break;

                case CheckBox cb:
                    cb.ForeColor = TextPrimary;
                    cb.BackColor = Color.Transparent;
                    break;

                case RadioButton rb:
                    rb.ForeColor = TextPrimary;
                    rb.BackColor = Color.Transparent;
                    break;

                case ProgressBar:
                    // ProgressBar doesn't support BackColor well on Windows
                    break;

                case Label lbl:
                    // Preserve labels with explicit status colors
                    if (lbl.ForeColor != Color.Green &&
                        lbl.ForeColor != Color.Red &&
                        lbl.ForeColor != Success &&
                        lbl.ForeColor != Error &&
                        lbl.ForeColor != Warning)
                    {
                        lbl.ForeColor = TextPrimary;
                    }
                    lbl.BackColor = Color.Transparent;
                    break;

                case Panel panel:
                    panel.BackColor = Background;
                    break;

                default:
                    control.BackColor = Background;
                    control.ForeColor = TextPrimary;
                    break;
            }

            // Recurse into children
            if (control.HasChildren)
            {
                ApplyRecursive(control.Controls);
            }
        }

        private static void ApplyToButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 9f);

            // Check if this button already has accent/custom styling
            bool isCustom = button.BackColor == Color.FromArgb(26, 115, 232) ||
                            button.BackColor == Accent ||
                            button.BackColor == AccentLight;

            if (isCustom)
            {
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = AccentLight;
                button.FlatAppearance.MouseDownBackColor = AccentDark;
            }
            else
            {
                button.BackColor = SurfaceLight;
                button.ForeColor = TextPrimary;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(75, 75, 79);
                button.FlatAppearance.MouseDownBackColor = AccentDark;
            }
        }

        // ── Tab Drawing ──

        private static void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tab) return;
            if (e.Index < 0 || e.Index >= tab.TabCount) return;

            var page = tab.TabPages[e.Index];
            bool selected = tab.SelectedIndex == e.Index;

            // Background
            using var bgBrush = new SolidBrush(selected ? Surface : SurfaceDark);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // Bottom accent line for selected tab
            if (selected)
            {
                using var accentPen = new Pen(Accent, 3);
                e.Graphics.DrawLine(accentPen,
                    e.Bounds.Left + 2, e.Bounds.Bottom - 2,
                    e.Bounds.Right - 2, e.Bounds.Bottom - 2);
            }

            // Text
            using var textBrush = new SolidBrush(selected ? Color.White : TextSecondary);
            using var font = new Font("Segoe UI", 9.5f, selected ? FontStyle.Bold : FontStyle.Regular);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(page.Text, font, textBrush, e.Bounds, sf);
        }

        // ── Button Style Helpers ──

        /// <summary>Style a button as a primary accent action.</summary>
        public static void MakeAccentButton(Button button)
        {
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentLight;
            button.FlatAppearance.MouseDownBackColor = AccentDark;
            button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>Style a button as a success/connect action.</summary>
        public static void MakeSuccessButton(Button button)
        {
            button.BackColor = SuccessDark;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Success;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(46, 125, 50);
            button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>Style a button as a danger/stop/disconnect action.</summary>
        public static void MakeDangerButton(Button button)
        {
            button.BackColor = ErrorDark;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Error;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(183, 28, 28);
            button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>Style a button as a large prominent wizard/feature button.</summary>
        public static void MakeHeroButton(Button button, string text)
        {
            button.Text = text;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentLight;
            button.FlatAppearance.MouseDownBackColor = AccentDark;
            button.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
        }
    }
}
