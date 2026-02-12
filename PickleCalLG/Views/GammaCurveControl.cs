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
    /// Gamma/EOTF tracking chart. Shows measured gamma vs target gamma across grayscale IRE levels.
    /// Equivalent to HCFR's GammaHistoView.
    /// </summary>
    public sealed class GammaCurveControl : Control
    {
        private readonly List<CalibrationPoint> _points = new();
        private double _targetGamma = 2.2;
        private double _yMin = 1.0;
        private double _yMax = 3.5;

        public GammaCurveControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public double TargetGamma
        {
            get => _targetGamma;
            set { _targetGamma = value; Invalidate(); }
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _points.Clear();
            _points.AddRange(points.Where(p => p.EffectiveGamma.HasValue && !double.IsNaN(p.EffectiveGamma.Value)));
            Invalidate();
        }

        private const int ChartMargin = 50;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width - ChartMargin * 2;
            int h = Height - ChartMargin * 2;
            if (w < 10 || h < 10) return;

            DrawAxes(g, w, h);
            DrawTargetLine(g, w, h);
            DrawGammaCurve(g, w, h);

            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("Gamma / EOTF Tracking", titleFont, Brushes.White, ChartMargin, 5);
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

            // Y axis: gamma values
            for (double gv = _yMin; gv <= _yMax; gv += 0.2)
            {
                int y = GammaToY(gv, h);
                g.DrawLine(axisPen, ChartMargin, y, ChartMargin + w, y);
                g.DrawString(gv.ToString("F1"), font, Brushes.Gray, 5, y - 6);
            }

            // Axis labels
            using var labelFont = new Font("Segoe UI", 8);
            g.DrawString("IRE %", labelFont, Brushes.White, ChartMargin + w / 2 - 15, ChartMargin + h + 18);
            g.DrawString("γ", labelFont, Brushes.White, ChartMargin - 40, ChartMargin + h / 2 - 5);
        }

        private void DrawTargetLine(Graphics g, int w, int h)
        {
            using var pen = new Pen(Color.FromArgb(150, 100, 100, 255), 2f) { DashStyle = DashStyle.Dash };
            int y = GammaToY(_targetGamma, h);
            g.DrawLine(pen, ChartMargin, y, ChartMargin + w, y);

            using var font = new Font("Segoe UI", 7);
            g.DrawString($"Target γ={_targetGamma:F1}", font, Brushes.Blue, ChartMargin + w - 70, y - 15);
        }

        private void DrawGammaCurve(Graphics g, int w, int h)
        {
            if (_points.Count < 2) return;

            var sorted = _points.OrderBy(p => p.TargetIre).ToList();
            var pts = new List<Point>();
            foreach (var point in sorted)
            {
                double gamma = point.EffectiveGamma!.Value;
                int x = ChartMargin + (int)(point.TargetIre / 100.0 * w);
                int y = GammaToY(gamma, h);
                pts.Add(new Point(x, y));
            }

            // Draw line
            using var linePen = new Pen(Color.FromArgb(200, 0, 150, 0), 2f);
            if (pts.Count > 1)
                g.DrawLines(linePen, pts.ToArray());

            // Draw points with ΔE coloring
            using var goodBrush = new SolidBrush(Color.Green);
            using var warnBrush = new SolidBrush(Color.Orange);
            using var badBrush = new SolidBrush(Color.Red);
            using var font = new Font("Segoe UI", 6);

            for (int i = 0; i < sorted.Count; i++)
            {
                var brush = sorted[i].DeltaE2000 < 1.0 ? goodBrush :
                           sorted[i].DeltaE2000 < 3.0 ? warnBrush : badBrush;
                g.FillEllipse(brush, pts[i].X - 4, pts[i].Y - 4, 8, 8);
                g.DrawString(sorted[i].EffectiveGamma!.Value.ToString("F2"), font, Brushes.White, pts[i].X + 5, pts[i].Y - 10);
            }
        }

        private int GammaToY(double gamma, int h)
        {
            double norm = (gamma - _yMin) / (_yMax - _yMin);
            return ChartMargin + h - (int)(norm * h);
        }
    }
}
