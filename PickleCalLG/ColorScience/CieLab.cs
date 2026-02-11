using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// CIE L*a*b* (CIELAB) perceptual color coordinate.
    /// </summary>
    public readonly struct CieLab : IEquatable<CieLab>
    {
        public double L { get; }
        public double A { get; }
        public double B { get; }

        public CieLab(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }

        /// <summary>Chroma (C*) in LCh.</summary>
        public double Chroma => Math.Sqrt(A * A + B * B);

        /// <summary>Hue angle (h) in degrees [0, 360).</summary>
        public double HueAngle
        {
            get
            {
                double h = Math.Atan2(B, A) * (180.0 / Math.PI);
                if (h < 0) h += 360.0;
                return h;
            }
        }

        /// <summary>Convert to CIE LCh.</summary>
        public CieLch ToLch() => new(L, Chroma, HueAngle);

        /// <summary>Convert back to CIE XYZ relative to given white point.</summary>
        public CieXyz ToXyz(CieXyz whitePoint)
        {
            double fy = (L + 16.0) / 116.0;
            double fx = A / 500.0 + fy;
            double fz = fy - B / 200.0;

            double x = LabFInverse(fx) * whitePoint.X;
            double y = LabFInverse(fy) * whitePoint.Y;
            double z = LabFInverse(fz) * whitePoint.Z;

            return new CieXyz(x, y, z);
        }

        private static double LabFInverse(double t)
        {
            const double delta = 6.0 / 29.0;
            if (t > delta)
                return t * t * t;
            return 3.0 * delta * delta * (t - 4.0 / 29.0);
        }

        public bool Equals(CieLab other) =>
            Math.Abs(L - other.L) < 1e-10 &&
            Math.Abs(A - other.A) < 1e-10 &&
            Math.Abs(B - other.B) < 1e-10;

        public override bool Equals(object? obj) => obj is CieLab other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(L, A, B);
        public override string ToString() => $"Lab({L:F2}, {A:F2}, {B:F2})";
    }
}
