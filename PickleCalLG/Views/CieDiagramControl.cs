using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PickleCalLG.ColorScience;
using PickleCalLG.Meters;

namespace PickleCalLG.Views
{
    /// <summary>
    /// CIE 1931 xy chromaticity diagram with gamut triangle, measurement points,
    /// and Planckian locus. Equivalent to HCFR's CIEChartView.
    /// </summary>
    public sealed class CieDiagramControl : Control
    {
        private ColorSpace _targetColorSpace = ColorSpace.Rec709;
        private readonly List<CalibrationPoint> _points = new();

        // Diagram bounds in CIE xy space
        private const double XMin = 0.0;
        private const double XMax = 0.8;
        private const double YMin = 0.0;
        private const double YMax = 0.9;
        private const int ChartMargin = 40;

        public CieDiagramControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public ColorSpace TargetColorSpace
        {
            get => _targetColorSpace;
            set { _targetColorSpace = value; Invalidate(); }
        }

        public void SetPoints(IEnumerable<CalibrationPoint> points)
        {
            _points.Clear();
            _points.AddRange(points);
            Invalidate();
        }

        public void ClearPoints()
        {
            _points.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = Width - ChartMargin * 2;
            int h = Height - ChartMargin * 2;
            if (w < 10 || h < 10) return;

            // Draw axes
            DrawAxes(g, w, h);

            // Draw spectral locus (simplified outline)
            DrawSpectralLocus(g, w, h);

            // Draw target gamut triangle
            DrawGamutTriangle(g, w, h, _targetColorSpace, Color.FromArgb(180, 100, 100, 255), 2f);

            // Draw white point target
            var wpPt = CieXyToScreen(_targetColorSpace.WhitePoint, w, h);
            DrawCrossHair(g, wpPt, Color.Gray, 8);

            // Draw Planckian locus
            DrawPlanckianLocus(g, w, h);

            // Draw measurement points
            DrawMeasurementPoints(g, w, h);

            // Title
            using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("CIE 1931 xy Chromaticity", titleFont, Brushes.White, ChartMargin, 5);
        }

        private void DrawAxes(Graphics g, int w, int h)
        {
            using var pen = new Pen(Color.FromArgb(55, 55, 58), 1);
            using var font = new Font("Segoe UI", 7);

            // Grid lines
            for (double v = 0; v <= 0.8; v += 0.1)
            {
                var p1 = CieXyToScreen(new CieXy(v, YMin), w, h);
                var p2 = CieXyToScreen(new CieXy(v, YMax), w, h);
                g.DrawLine(pen, p1, p2);
                g.DrawString(v.ToString("F1"), font, Brushes.Gray, p1.X - 10, p1.Y + 2);

                p1 = CieXyToScreen(new CieXy(XMin, v), w, h);
                p2 = CieXyToScreen(new CieXy(XMax, v), w, h);
                g.DrawLine(pen, p1, p2);
                if (v > 0)
                    g.DrawString(v.ToString("F1"), font, Brushes.Gray, p1.X - 30, p1.Y - 5);
            }

            // Axis labels
            using var axisFont = new Font("Segoe UI", 8);
            g.DrawString("x", axisFont, Brushes.White, ChartMargin + w / 2, ChartMargin + h + 15);
            g.DrawString("y", axisFont, Brushes.White, 5, ChartMargin + h / 2 - 10);
        }

        private void DrawSpectralLocus(Graphics g, int w, int h)
        {
            // Simplified CIE 1931 spectral locus points (wavelength nm → xy)
            var locus = new (double x, double y)[]
            {
                (0.1741, 0.0050), (0.1740, 0.0050), (0.1738, 0.0049),
                (0.1736, 0.0049), (0.1733, 0.0048), (0.1730, 0.0048),
                (0.1726, 0.0048), (0.1714, 0.0051), (0.1689, 0.0069),
                (0.1644, 0.0109), (0.1566, 0.0177), (0.1440, 0.0297),
                (0.1241, 0.0578), (0.0913, 0.1327), (0.0454, 0.2950),
                (0.0082, 0.5384), (0.0139, 0.7502), (0.0743, 0.8338),
                (0.1547, 0.8059), (0.2296, 0.7543), (0.3016, 0.6923),
                (0.3731, 0.6245), (0.4441, 0.5547), (0.5125, 0.4866),
                (0.5752, 0.4242), (0.6270, 0.3725), (0.6658, 0.3340),
                (0.6915, 0.3083), (0.7079, 0.2920), (0.7190, 0.2809),
                (0.7260, 0.2740), (0.7300, 0.2700), (0.7320, 0.2680),
                (0.7334, 0.2666), (0.7347, 0.2653), (0.7347, 0.2653)
            };

            var pts = new List<Point>();
            foreach (var (x, y) in locus)
            {
                pts.Add(CieXyToScreen(new CieXy(x, y), w, h));
            }
            // Close the locus (line of purples)
            pts.Add(pts[0]);

            if (pts.Count > 2)
            {
                using var pen = new Pen(Color.DarkGray, 1.5f);
                g.DrawLines(pen, pts.ToArray());

                // Fill with very light gradient
                using var brush = new SolidBrush(Color.FromArgb(15, 100, 100, 100));
                g.FillPolygon(brush, pts.ToArray());
            }
        }

        private void DrawGamutTriangle(Graphics g, int w, int h, ColorSpace cs, Color color, float width)
        {
            var pR = CieXyToScreen(cs.Red, w, h);
            var pG = CieXyToScreen(cs.Green, w, h);
            var pB = CieXyToScreen(cs.Blue, w, h);

            using var pen = new Pen(color, width);
            g.DrawPolygon(pen, new[] { pR, pG, pB });

            // Fill with semi-transparent
            using var brush = new SolidBrush(Color.FromArgb(30, color));
            g.FillPolygon(brush, new[] { pR, pG, pB });

            // Label primaries
            using var font = new Font("Segoe UI", 7);
            using var labelBrush = new SolidBrush(color);
            g.DrawString("R", font, labelBrush, pR.X + 3, pR.Y - 3);
            g.DrawString("G", font, labelBrush, pG.X - 3, pG.Y - 15);
            g.DrawString("B", font, labelBrush, pB.X - 12, pB.Y + 3);
        }

        private void DrawPlanckianLocus(Graphics g, int w, int h)
        {
            var pts = new List<Point>();
            using var pen = new Pen(Color.FromArgb(120, 180, 120, 0), 1.5f);
            using var font = new Font("Segoe UI", 6);

            for (int cct = 1000; cct <= 15000; cct += 100)
            {
                var (u, v) = CctCalculator.PlanckianUv(cct);
                // CIE 1960 UCS (u,v) → CIE xy
                double x = 3.0 * u / (2.0 * u - 8.0 * v + 4.0);
                double y = 2.0 * v / (2.0 * u - 8.0 * v + 4.0);

                if (x >= XMin && x <= XMax && y >= YMin && y <= YMax)
                {
                    pts.Add(CieXyToScreen(new CieXy(x, y), w, h));
                }
            }

            if (pts.Count > 1)
                g.DrawLines(pen, pts.ToArray());
        }

        private void DrawMeasurementPoints(Graphics g, int w, int h)
        {
            using var goodBrush = new SolidBrush(Color.FromArgb(200, 0, 180, 0));
            using var warnBrush = new SolidBrush(Color.FromArgb(200, 255, 165, 0));
            using var badBrush = new SolidBrush(Color.FromArgb(200, 255, 0, 0));
            using var font = new Font("Segoe UI", 6);

            foreach (var point in _points)
            {
                var pt = CieXyToScreen(point.MeasuredXy, w, h);
                var brush = point.DeltaE2000 < 1.0 ? goodBrush :
                           point.DeltaE2000 < 3.0 ? warnBrush : badBrush;
                g.FillEllipse(brush, pt.X - 4, pt.Y - 4, 8, 8);
                g.DrawString(point.Name, font, Brushes.White, pt.X + 5, pt.Y - 5);
            }
        }

        private void DrawCrossHair(Graphics g, Point pt, Color color, int size)
        {
            using var pen = new Pen(color, 1.5f);
            g.DrawLine(pen, pt.X - size, pt.Y, pt.X + size, pt.Y);
            g.DrawLine(pen, pt.X, pt.Y - size, pt.X, pt.Y + size);
        }

        private Point CieXyToScreen(CieXy xy, int w, int h)
        {
            int sx = ChartMargin + (int)((xy.X - XMin) / (XMax - XMin) * w);
            int sy = ChartMargin + h - (int)((xy.Y - YMin) / (YMax - YMin) * h);
            return new Point(sx, sy);
        }
    }
}
