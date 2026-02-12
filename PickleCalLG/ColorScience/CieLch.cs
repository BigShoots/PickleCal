using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// CIE LCh (Lightness, Chroma, Hue) cylindrical representation of CIELAB.
    /// </summary>
    public readonly struct CieLch
    {
        public double L { get; }
        public double C { get; }
        /// <summary>Hue in degrees [0,360).</summary>
        public double H { get; }

        public CieLch(double l, double c, double h)
        {
            L = l;
            C = c;
            H = ((h % 360.0) + 360.0) % 360.0;
        }

        /// <summary>Convert back to CIE L*a*b*.</summary>
        public CieLab ToLab()
        {
            double hRad = H * (Math.PI / 180.0);
            double a = C * Math.Cos(hRad);
            double b = C * Math.Sin(hRad);
            return new CieLab(L, a, b);
        }

        public override string ToString() => $"LCh({L:F2}, {C:F2}, {H:F1}°)";
    }
}
