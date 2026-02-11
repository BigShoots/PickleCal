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
    /// Delta E bar chart / histogram showing ΔE2000 per measurement point.
    /// Color-coded: green (<1), yellow (1-3), red (>3).
    /// Equivalent to HCFR's measureshistoview.
    /// </summary>
    public sealed class DeltaEChartControl : Control
    {
        private readonly List<CalibrationPoint> _points = new();
        private double _yMax = 5.0;
        private const int ChartMargin = 50;

        public DeltaEChartControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _points.Clear();
            _points.AddRange(points);
            if (_points.Any())
                _yMax = Math.Max(_points.Max(p => p.DeltaE2000) * 1.2, 3);
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
            DrawThresholdLines(g, w, h);
            DrawBars(g, w, h);

            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("ΔE₂₀₀₀ per Measurement", titleFont, Brushes.White, ChartMargin, 5);

            // Averages
            if (_points.Any())
            {
                double avg = _points.Average(p => p.DeltaE2000);
                double max = _points.Max(p => p.DeltaE2000);
                using var statsFont = new Font("Segoe UI", 8);
                g.DrawString($"Avg: {avg:F2}  Max: {max:F2}", statsFont, Brushes.Gray,
                    ChartMargin + w - 120, ChartMargin + 5);
            }
        }

        private void DrawAxes(Graphics g, int w, int h)
        {
            using var axisPen = new Pen(Color.FromArgb(55, 55, 58), 1);
            using var font = new Font("Segoe UI", 7);

            // Y axis
            for (double v = 0; v <= _yMax; v += 1)
            {
                int y = DeltaEToY(v, h);
                g.DrawLine(axisPen, ChartMargin, y, ChartMargin + w, y);
                g.DrawString(v.ToString("F0"), font, Brushes.Gray, ChartMargin - 25, y - 6);
            }

            // X axis baseline
            using var basePen = new Pen(Color.FromArgb(80, 80, 84), 1);
            g.DrawLine(basePen, ChartMargin, ChartMargin + h, ChartMargin + w, ChartMargin + h);

            using var labelFont = new Font("Segoe UI", 8);
            g.DrawString("ΔE₂₀₀₀", labelFont, Brushes.White, 3, ChartMargin + h / 2 - 5);
        }

        private void DrawThresholdLines(Graphics g, int w, int h)
        {
            // ΔE = 1 (imperceptible)
            using var pen1 = new Pen(Color.FromArgb(100, 0, 180, 0), 1) { DashStyle = DashStyle.Dot };
            int y1 = DeltaEToY(1.0, h);
            g.DrawLine(pen1, ChartMargin, y1, ChartMargin + w, y1);

            // ΔE = 3 (noticeable)
            using var pen3 = new Pen(Color.FromArgb(100, 255, 0, 0), 1) { DashStyle = DashStyle.Dot };
            int y3 = DeltaEToY(3.0, h);
            g.DrawLine(pen3, ChartMargin, y3, ChartMargin + w, y3);

            using var font = new Font("Segoe UI", 6);
            g.DrawString("imperceptible", font, Brushes.Green, ChartMargin + 2, y1 - 12);
            g.DrawString("noticeable", font, Brushes.Red, ChartMargin + 2, y3 - 12);
        }

        private void DrawBars(Graphics g, int w, int h)
        {
            if (_points.Count == 0) return;

            int barWidth = Math.Max((w - 10) / _points.Count - 2, 4);
            int barSpacing = (w - 10) / _points.Count;
            using var font = new Font("Segoe UI", 6);

            for (int i = 0; i < _points.Count; i++)
            {
                var point = _points[i];
                int barX = ChartMargin + 5 + i * barSpacing;
                int barTop = DeltaEToY(point.DeltaE2000, h);
                int barHeight = ChartMargin + h - barTop;

                var color = point.DeltaE2000 < 1.0 ? Color.FromArgb(200, 0, 180, 0) :
                           point.DeltaE2000 < 3.0 ? Color.FromArgb(200, 255, 165, 0) :
                           Color.FromArgb(200, 255, 50, 50);

                using var brush = new SolidBrush(color);
                g.FillRectangle(brush, barX, barTop, barWidth, barHeight);

                // Label on top
                g.DrawString(point.DeltaE2000.ToString("F1"), font, Brushes.White, barX - 2, barTop - 12);

                // Name below
                var state = g.Save();
                g.TranslateTransform(barX + barWidth / 2, ChartMargin + h + 3);
                g.RotateTransform(45);
                g.DrawString(point.Name, font, Brushes.Gray, 0, 0);
                g.Restore(state);
            }
        }

        private int DeltaEToY(double deltaE, int h)
        {
            double norm = Math.Clamp(deltaE / _yMax, 0, 1);
            return ChartMargin + h - (int)(norm * h);
        }
    }
}
