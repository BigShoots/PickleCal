using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PickleCalLG.Meters;

namespace PickleCalLG.Views
{
    /// <summary>
    /// RGB balance / levels tracking chart. Shows R/G/B deviation from target across grayscale.
    /// Equivalent to HCFR's rgbhistoview.
    /// </summary>
    public sealed class RgbBalanceControl : Control
    {
        private readonly List<CalibrationPoint> _points = new();
        private const int ChartMargin = 50;

        public RgbBalanceControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _points.Clear();
            _points.AddRange(points.Where(p => p.RgbError.HasValue));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width - ChartMargin * 2;
            int h = Height - ChartMargin * 2;
            if (w < 10 || h < 10) return;

            DrawAxes(g, w, h);
            DrawRgbCurves(g, w, h);

            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("RGB Balance", titleFont, Brushes.White, ChartMargin, 5);
        }

        private void DrawAxes(Graphics g, int w, int h)
        {
            using var axisPen = new Pen(Color.FromArgb(55, 55, 58), 1);
            using var font = new Font("Segoe UI", 7);

            // X axis: IRE 0–100
            for (int ire = 0; ire <= 100; ire += 10)
            {
                int x = ChartMargin + (int)(ire / 100.0 * w);
                g.DrawLine(axisPen, x, ChartMargin, x, ChartMargin + h);
                g.DrawString($"{ire}", font, Brushes.Gray, x - 8, ChartMargin + h + 3);
            }

            // Y axis: error % (-20 to +20)
            for (int pct = -20; pct <= 20; pct += 5)
            {
                int y = ErrorToY(pct / 100.0, h);
                g.DrawLine(axisPen, ChartMargin, y, ChartMargin + w, y);
                g.DrawString($"{pct}%", font, Brushes.Gray, 5, y - 6);
            }

            // Zero line
            using var zeroPen = new Pen(Color.FromArgb(80, 80, 84), 1) { DashStyle = DashStyle.Dash };
            int zeroY = ErrorToY(0, h);
            g.DrawLine(zeroPen, ChartMargin, zeroY, ChartMargin + w, zeroY);

            // Axis labels
            using var labelFont = new Font("Segoe UI", 8);
            g.DrawString("IRE %", labelFont, Brushes.White, ChartMargin + w / 2 - 15, ChartMargin + h + 18);
        }

        private void DrawRgbCurves(Graphics g, int w, int h)
        {
            if (_points.Count < 2) return;

            var sorted = _points.OrderBy(p => p.TargetIre).ToList();

            var redPts = new List<Point>();
            var greenPts = new List<Point>();
            var bluePts = new List<Point>();

            foreach (var point in sorted)
            {
                var (er, eg, eb) = point.RgbError!.Value;
                int x = ChartMargin + (int)(point.TargetIre / 100.0 * w);
                redPts.Add(new Point(x, ErrorToY(er, h)));
                greenPts.Add(new Point(x, ErrorToY(eg, h)));
                bluePts.Add(new Point(x, ErrorToY(eb, h)));
            }

            using var redPen = new Pen(Color.FromArgb(200, 255, 50, 50), 2f);
            using var greenPen = new Pen(Color.FromArgb(200, 50, 200, 50), 2f);
            using var bluePen = new Pen(Color.FromArgb(200, 50, 50, 255), 2f);

            if (redPts.Count > 1)
            {
                g.DrawLines(redPen, redPts.ToArray());
                g.DrawLines(greenPen, greenPts.ToArray());
                g.DrawLines(bluePen, bluePts.ToArray());
            }

            // Draw points
            for (int i = 0; i < sorted.Count; i++)
            {
                g.FillEllipse(Brushes.Red, redPts[i].X - 3, redPts[i].Y - 3, 6, 6);
                g.FillEllipse(Brushes.Green, greenPts[i].X - 3, greenPts[i].Y - 3, 6, 6);
                g.FillEllipse(Brushes.Blue, bluePts[i].X - 3, bluePts[i].Y - 3, 6, 6);
            }

            // Legend
            using var font = new Font("Segoe UI", 7);
            int lx = ChartMargin + w - 50;
            g.FillRectangle(Brushes.Red, lx, ChartMargin + 5, 8, 8);
            g.DrawString("R", font, Brushes.Gray, lx + 10, ChartMargin + 3);
            g.FillRectangle(Brushes.Green, lx, ChartMargin + 18, 8, 8);
            g.DrawString("G", font, Brushes.Gray, lx + 10, ChartMargin + 16);
            g.FillRectangle(Brushes.Blue, lx, ChartMargin + 31, 8, 8);
            g.DrawString("B", font, Brushes.Gray, lx + 10, ChartMargin + 29);
        }

        private int ErrorToY(double error, int h)
        {
            // -0.20 to +0.20 mapped to h
            double norm = (error + 0.20) / 0.40;
            norm = Math.Clamp(norm, 0, 1);
            return ChartMargin + h - (int)(norm * h);
        }
    }
}
