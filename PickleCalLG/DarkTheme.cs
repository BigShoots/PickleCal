using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PickleCalLG
{
    /// <summary>
    /// VS Code Dark+ inspired theme for PickleCal.
    /// DPI-aware with dark title bar support.
    /// </summary>
    public static class DarkTheme
    {
        // ── VS Code Dark+ Color Palette ──
        public static readonly Color Background = Color.FromArgb(30, 30, 30);       // #1e1e1e editor
        public static readonly Color Surface = Color.FromArgb(37, 37, 38);          // #252526 sidebar
        public static readonly Color SurfaceLight = Color.FromArgb(51, 51, 51);     // #333333 activity bar
        public static readonly Color SurfaceDark = Color.FromArgb(24, 24, 24);      // #181818
        public static readonly Color SurfaceElevated = Color.FromArgb(45, 45, 45);  // #2d2d2d inactive tab
        public static readonly Color Border = Color.FromArgb(60, 60, 60);           // #3c3c3c
        public static readonly Color BorderLight = Color.FromArgb(70, 70, 70);      // #464646
        public static readonly Color BorderSubtle = Color.FromArgb(48, 48, 48);     // #303030

        public static readonly Color TextPrimary = Color.FromArgb(212, 212, 212);   // #d4d4d4
        public static readonly Color TextSecondary = Color.FromArgb(150, 150, 150); // #969696
        public static readonly Color TextMuted = Color.FromArgb(110, 110, 110);     // #6e6e6e

        public static readonly Color Accent = Color.FromArgb(0, 122, 204);          // #007acc VS Code blue
        public static readonly Color AccentLight = Color.FromArgb(17, 119, 187);    // #1177bb hover
        public static readonly Color AccentDark = Color.FromArgb(14, 99, 156);      // #0e639c button

        public static readonly Color Success = Color.FromArgb(72, 199, 142);
        public static readonly Color SuccessDark = Color.FromArgb(52, 168, 118);
        public static readonly Color Error = Color.FromArgb(244, 71, 71);           // #f44747
        public static readonly Color ErrorDark = Color.FromArgb(200, 50, 50);
        public static readonly Color Warning = Color.FromArgb(205, 160, 0);         // #cda000

        public static readonly Color InputBackground = Color.FromArgb(60, 60, 60);  // #3c3c3c
        public static readonly Color ListBackground = Color.FromArgb(37, 37, 38);   // #252526

        // ── Fonts ──
        public static readonly Font DefaultFont = new("Segoe UI", 9f);
        public static readonly Font HeaderFont = new("Segoe UI Semibold", 13f, FontStyle.Bold);
        public static readonly Font SubHeaderFont = new("Segoe UI Semibold", 10.5f, FontStyle.Bold);
        public static readonly Font MonoFont = new("Cascadia Mono", 9f);
        public static readonly Font SmallFont = new("Segoe UI", 8.5f);
        public static readonly Font ButtonFont = new("Segoe UI Semibold", 9f);

        private const int GroupBoxRadius = 6;

        // ── Dark Title Bar P/Invoke ──
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>Enable Windows dark title bar for a form.</summary>
        public static void EnableDarkTitleBar(Form form)
        {
            try
            {
                int value = 1;
                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Win10 20H1+)
                int hr = DwmSetWindowAttribute(form.Handle, 20, ref value, sizeof(int));
                if (hr != 0)
                    DwmSetWindowAttribute(form.Handle, 19, ref value, sizeof(int));
            }
            catch { }
        }

        /// <summary>
        /// Apply the VS Code dark theme to a form and all child controls recursively.
        /// </summary>
        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;
            form.Font = DefaultFont;
            EnableDoubleBuffering(form);
            ApplyRecursive(form.Controls);
            EnableDarkTitleBar(form);
        }

        /// <summary>
        /// Apply dark theme to a collection of controls recursively.
        /// </summary>
        public static void ApplyRecursive(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
                ApplyToControl(control);
        }

        private static void EnableDoubleBuffering(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true);
        }

        private static void ApplyToControl(Control control)
        {
            EnableDoubleBuffering(control);

            switch (control)
            {
                case TabControl tab:
                    StyleTabControl(tab);
                    break;

                case TabPage page:
                    page.BackColor = Background;
                    page.ForeColor = TextPrimary;
                    page.Padding = new Padding(4);
                    break;

                case GroupBox group:
                    group.BackColor = Surface;
                    group.ForeColor = TextPrimary;
                    group.FlatStyle = FlatStyle.Flat;
                    group.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    group.Paint -= GroupBox_Paint;
                    group.Paint += GroupBox_Paint;
                    break;

                case Button button:
                    ApplyToButton(button);
                    break;

                case TextBox textBox:
                    textBox.BackColor = InputBackground;
                    textBox.ForeColor = TextPrimary;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.Font = DefaultFont;
                    break;

                case ComboBox combo:
                    combo.BackColor = InputBackground;
                    combo.ForeColor = TextPrimary;
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.Font = DefaultFont;
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
                    cb.Font = DefaultFont;
                    break;

                case RadioButton rb:
                    rb.ForeColor = TextPrimary;
                    rb.BackColor = Color.Transparent;
                    rb.Font = DefaultFont;
                    break;

                case ProgressBar prg:
                    prg.BackColor = SurfaceDark;
                    break;

                case Label lbl:
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

            if (control.HasChildren)
                ApplyRecursive(control.Controls);
        }

        // ── DPI-aware Tab Sizing ──

        private static void StyleTabControl(TabControl tab)
        {
            tab.DrawMode = TabDrawMode.OwnerDrawFixed;
            tab.SizeMode = TabSizeMode.Fixed;

            // Calculate DPI-scaled tab dimensions by measuring actual text
            float dpiScale = tab.DeviceDpi / 96f;
            if (dpiScale < 1f) dpiScale = 1f;
            int tabHeight = (int)(32 * dpiScale);
            int tabWidth;

            try
            {
                using var g = tab.CreateGraphics();
                using var font = new Font("Segoe UI", 9f);
                float maxWidth = 0f;
                foreach (TabPage page in tab.TabPages)
                {
                    var sz = g.MeasureString(page.Text, font);
                    maxWidth = Math.Max(maxWidth, sz.Width);
                }
                int padding = (int)(30 * dpiScale);
                tabWidth = Math.Max((int)maxWidth + padding, (int)(80 * dpiScale));
            }
            catch
            {
                tabWidth = (int)(120 * dpiScale);
            }

            tab.ItemSize = new Size(tabWidth, tabHeight);
            tab.Padding = new Point(0, 0);
            tab.DrawItem -= TabControl_DrawItem;
            tab.DrawItem += TabControl_DrawItem;
            tab.BackColor = SurfaceDark;
        }

        // ── Button Styling ──

        private static void ApplyToButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.Font = ButtonFont;

            bool isCustom = button.BackColor == Accent ||
                            button.BackColor == AccentLight ||
                            button.BackColor == AccentDark ||
                            button.BackColor == Color.FromArgb(56, 132, 244) ||
                            button.BackColor == Color.FromArgb(26, 115, 232) ||
                            button.BackColor == Color.FromArgb(0, 122, 204);

            if (isCustom)
            {
                button.BackColor = AccentDark;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = AccentLight;
                button.FlatAppearance.MouseDownBackColor = Accent;
            }
            else
            {
                button.BackColor = SurfaceLight;
                button.ForeColor = TextPrimary;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 62);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 72, 72);
            }
        }

        // ── Tab Drawing ── VS Code style ──

        private static void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tab) return;
            if (e.Index < 0 || e.Index >= tab.TabCount) return;

            var page = tab.TabPages[e.Index];
            bool selected = tab.SelectedIndex == e.Index;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background: active tab = editor bg, inactive = elevated surface
            var bgColor = selected ? Background : SurfaceElevated;
            using var bgBrush = new SolidBrush(bgColor);
            g.FillRectangle(bgBrush, e.Bounds);

            // VS Code accent line at top of active tab
            if (selected)
            {
                float dpiScale = tab.DeviceDpi / 96f;
                int lineH = Math.Max(2, (int)(2 * dpiScale));
                using var accentBrush = new SolidBrush(Accent);
                g.FillRectangle(accentBrush, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width, lineH);
            }

            // Subtle right-edge separator between tabs
            if (!selected && e.Index > 0)
            {
                using var sepPen = new Pen(BorderSubtle, 1);
                g.DrawLine(sepPen, e.Bounds.Left, e.Bounds.Top + 6, e.Bounds.Left, e.Bounds.Bottom - 6);
            }

            // Text
            var textColor = selected ? TextPrimary : TextSecondary;
            using var textBrush = new SolidBrush(textColor);
            using var font = new Font("Segoe UI", 9f, FontStyle.Regular);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(page.Text, font, textBrush, e.Bounds, sf);
        }

        // ── GroupBox Custom Paint ──

        private static void GroupBox_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not GroupBox group) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var titleFont = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            var titleSize = g.MeasureString(group.Text, titleFont);
            int titleHeight = (int)titleSize.Height;

            g.Clear(group.BackColor);

            // Rounded border
            var borderRect = new Rectangle(0, titleHeight / 2,
                group.Width - 1, group.Height - titleHeight / 2 - 1);
            using var path = CreateRoundedRectPath(borderRect, GroupBoxRadius);
            using var borderPen = new Pen(Border, 1);
            g.DrawPath(borderPen, path);

            // Title background (covers border)
            var titleRect = new RectangleF(10, 0, titleSize.Width + 8, titleHeight);
            using var titleBgBrush = new SolidBrush(group.BackColor);
            g.FillRectangle(titleBgBrush, titleRect);

            // Title text in accent color
            using var titleBrush = new SolidBrush(Accent);
            g.DrawString(group.Text, titleFont, titleBrush, 14, 0);
        }

        // ── Button Style Helpers ──

        /// <summary>Style a button as a primary accent action.</summary>
        public static void MakeAccentButton(Button button)
        {
            button.BackColor = AccentDark;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentLight;
            button.FlatAppearance.MouseDownBackColor = Accent;
            button.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
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
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 140, 100);
            button.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
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
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 30, 30);
            button.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        /// <summary>Style a button as a large prominent wizard/feature button.</summary>
        public static void MakeHeroButton(Button button, string text)
        {
            button.Text = text;
            button.BackColor = AccentDark;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentLight;
            button.FlatAppearance.MouseDownBackColor = Accent;
            button.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        /// <summary>Style a button as a secondary action.</summary>
        public static void MakeSecondaryButton(Button button)
        {
            button.BackColor = SurfaceLight;
            button.ForeColor = TextPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 62);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 72, 72);
            button.Font = new Font("Segoe UI", 9f);
            button.Cursor = Cursors.Hand;
        }

        // ── Geometry ──

        internal static GraphicsPath CreateRoundedRectPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter > bounds.Width) diameter = bounds.Width;
            if (diameter > bounds.Height) diameter = bounds.Height;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Draw a subtle card-like elevated panel background.</summary>
        public static void PaintCardBackground(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectPath(bounds, 6);
            using var brush = new SolidBrush(SurfaceElevated);
            g.FillPath(brush, path);
            using var borderPen = new Pen(Border, 1);
            g.DrawPath(borderPen, path);
        }

        /// <summary>Paint a horizontal gradient separator line.</summary>
        public static void PaintSeparator(Graphics g, int x, int y, int width)
        {
            using var brush = new LinearGradientBrush(
                new Point(x, y), new Point(x + width, y),
                Border, Color.Transparent);
            g.FillRectangle(brush, x, y, width, 1);
        }
    }

    /// <summary>
    /// A TabControl subclass that paints its background dark,
    /// eliminating the native white/gray strip behind tab headers.
    /// </summary>
    public class DarkTabControl : TabControl
    {
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_PAINT = 0x000F;

        public DarkTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            using var brush = new SolidBrush(DarkTheme.SurfaceDark);
            pevent.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Paint dark background for entire control
            using var bgBrush = new SolidBrush(DarkTheme.SurfaceDark);
            e.Graphics.FillRectangle(bgBrush, ClientRectangle);

            // Paint each tab via the DrawItem handler
            for (int i = 0; i < TabCount; i++)
            {
                var bounds = GetTabRect(i);
                var args = new DrawItemEventArgs(e.Graphics, Font, bounds, i,
                    i == SelectedIndex ? DrawItemState.Selected : DrawItemState.Default);
                OnDrawItem(args);
            }

            // Paint a border line below the tab strip to separate from content
            if (TabCount > 0)
            {
                var firstTab = GetTabRect(0);
                int lineY = firstTab.Bottom;
                using var pen = new Pen(DarkTheme.Border, 1);
                e.Graphics.DrawLine(pen, 0, lineY, Width, lineY);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
