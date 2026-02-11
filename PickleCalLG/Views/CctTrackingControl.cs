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
    /// CCT (Correlated Color Temperature) tracking across grayscale.
    /// Equivalent to HCFR's colortemphistoview.
    /// </summary>
    public sealed class CctTrackingControl : Control
    {
        private readonly List<CalibrationPoint> _points = new();
        private double _targetCctK = 6504; // D65
        private const int ChartMargin = 50;

        public CctTrackingControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public double TargetCctK
        {
            get => _targetCctK;
            set { _targetCctK = value; Invalidate(); }
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _points.Clear();
            _points.AddRange(points);
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
            DrawTargetLine(g, w, h);
            DrawCctCurve(g, w, h);

            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("Color Temperature Tracking", titleFont, Brushes.White, ChartMargin, 5);
        }

        private double CctMin => _targetCctK - 3000;
        private double CctMax => _targetCctK + 3000;

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

            for (double cct = CctMin; cct <= CctMax; cct += 500)
            {
                int y = CctToY(cct, h);
                if (y >= ChartMargin && y <= ChartMargin + h)
                {
                    g.DrawLine(axisPen, ChartMargin, y, ChartMargin + w, y);
                    g.DrawString($"{cct:F0}K", font, Brushes.Gray, 2, y - 6);
                }
            }

            using var labelFont = new Font("Segoe UI", 8);
            g.DrawString("IRE %", labelFont, Brushes.White, ChartMargin + w / 2 - 15, ChartMargin + h + 18);
        }

        private void DrawTargetLine(Graphics g, int w, int h)
        {
            using var pen = new Pen(Color.FromArgb(150, 100, 100, 255), 2f) { DashStyle = DashStyle.Dash };
            int y = CctToY(_targetCctK, h);
            g.DrawLine(pen, ChartMargin, y, ChartMargin + w, y);

            using var font = new Font("Segoe UI", 7);
            g.DrawString($"D65 ({_targetCctK:F0}K)", font, Brushes.Blue, ChartMargin + w - 80, y - 15);
        }

        private void DrawCctCurve(Graphics g, int w, int h)
        {
            if (_points.Count < 2) return;

            var sorted = _points.OrderBy(p => p.TargetIre).ToList();
            var pts = new List<Point>();

            foreach (var point in sorted)
            {
                int x = ChartMargin + (int)(point.TargetIre / 100.0 * w);
                int y = CctToY(point.CCT, h);
                pts.Add(new Point(x, y));
            }

            using var linePen = new Pen(Color.FromArgb(200, 180, 0, 180), 2f);
            g.DrawLines(linePen, pts.ToArray());

            using var pointBrush = new SolidBrush(Color.Purple);
            using var font = new Font("Segoe UI", 6);
            for (int i = 0; i < sorted.Count; i++)
            {
                g.FillEllipse(pointBrush, pts[i].X - 3, pts[i].Y - 3, 6, 6);
                g.DrawString($"{sorted[i].CCT:F0}K", font, Brushes.Gray, pts[i].X + 5, pts[i].Y - 10);
            }
        }

        private int CctToY(double cct, int h)
        {
            double norm = (cct - CctMin) / (CctMax - CctMin);
            norm = Math.Clamp(norm, 0, 1);
            return ChartMargin + h - (int)(norm * h);
        }
    }
}
