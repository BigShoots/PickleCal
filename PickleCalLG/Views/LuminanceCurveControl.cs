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
    /// Luminance vs IRE chart. Shows measured luminance against target EOTF curve.
    /// Supports linear and logarithmic Y axis. Equivalent to HCFR's luminancehistoview.
    /// </summary>
    public sealed class LuminanceCurveControl : Control
    {
        private readonly List<CalibrationPoint> _points = new();
        private bool _logScale = false;
        private double _peakNits = 100;
        private const int ChartMargin = 50;

        public LuminanceCurveControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public bool LogScale
        {
            get => _logScale;
            set { _logScale = value; Invalidate(); }
        }

        public double PeakNits
        {
            get => _peakNits;
            set { _peakNits = value > 0 ? value : 100; Invalidate(); }
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _points.Clear();
            _points.AddRange(points);
            if (_points.Any())
                _peakNits = Math.Max(_points.Max(p => p.Luminance), 1);
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
            DrawTargetCurve(g, w, h);
            DrawMeasuredCurve(g, w, h);

            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString(_logScale ? "Luminance (log)" : "Luminance", titleFont, Brushes.White, ChartMargin, 5);
        }

        private void DrawAxes(Graphics g, int w, int h)
        {
            using var axisPen = new Pen(Color.FromArgb(55, 55, 58), 1);
            using var font = new Font("Segoe UI", 7);

            for (int ire = 0; ire <= 100; ire += 10)
            {
                int x = ChartMargin + (int)(ire / 100.0 * w);
                g.DrawLine(axisPen, x, ChartMargin, x, ChartMargin + h);
                g.DrawString($"{ire}", font, Brushes.Gray, x - 8, ChartMargin + h + 3);
            }

            // Y axis: luminance
            int divisions = 5;
            for (int i = 0; i <= divisions; i++)
            {
                double frac = (double)i / divisions;
                double nits = frac * _peakNits;
                int y = NitsToY(nits, h);
                g.DrawLine(axisPen, ChartMargin, y, ChartMargin + w, y);
                g.DrawString($"{nits:F0}", font, Brushes.Gray, 5, y - 6);
            }

            using var labelFont = new Font("Segoe UI", 8);
            g.DrawString("IRE %", labelFont, Brushes.White, ChartMargin + w / 2 - 15, ChartMargin + h + 18);
            g.DrawString("cd/m²", labelFont, Brushes.White, 5, ChartMargin - 18);
        }

        private void DrawTargetCurve(Graphics g, int w, int h)
        {
            using var pen = new Pen(Color.FromArgb(150, 100, 100, 255), 1.5f) { DashStyle = DashStyle.Dash };
            var pts = new List<Point>();
            for (int ire = 0; ire <= 100; ire += 2)
            {
                double signal = ire / 100.0;
                double targetNits = Math.Pow(signal, 2.2) * _peakNits; // default gamma 2.2
                pts.Add(new Point(ChartMargin + (int)(ire / 100.0 * w), NitsToY(targetNits, h)));
            }
            if (pts.Count > 1)
                g.DrawLines(pen, pts.ToArray());
        }

        private void DrawMeasuredCurve(Graphics g, int w, int h)
        {
            if (_points.Count < 2) return;

            var sorted = _points.OrderBy(p => p.TargetIre).ToList();
            var pts = new List<Point>();

            foreach (var point in sorted)
            {
                int x = ChartMargin + (int)(point.TargetIre / 100.0 * w);
                int y = NitsToY(point.Luminance, h);
                pts.Add(new Point(x, y));
            }

            using var linePen = new Pen(Color.FromArgb(200, 255, 140, 0), 2f);
            g.DrawLines(linePen, pts.ToArray());

            using var pointBrush = new SolidBrush(Color.OrangeRed);
            foreach (var pt in pts)
                g.FillEllipse(pointBrush, pt.X - 3, pt.Y - 3, 6, 6);
        }

        private int NitsToY(double nits, int h)
        {
            if (_logScale && nits > 0 && _peakNits > 0)
            {
                double logMin = Math.Log10(0.001);
                double logMax = Math.Log10(_peakNits);
                double logVal = Math.Log10(Math.Max(nits, 0.001));
                double norm = (logVal - logMin) / (logMax - logMin);
                return ChartMargin + h - (int)(Math.Clamp(norm, 0, 1) * h);
            }
            else
            {
                double norm = _peakNits > 0 ? nits / _peakNits : 0;
                return ChartMargin + h - (int)(Math.Clamp(norm, 0, 1) * h);
            }
        }
    }
}
