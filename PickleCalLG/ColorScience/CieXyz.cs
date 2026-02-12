using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// CIE 1931 XYZ tristimulus values.
    /// </summary>
    public readonly struct CieXyz : IEquatable<CieXyz>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public CieXyz(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Luminance (cd/m²) — same as Y.</summary>
        public double Luminance => Y;

        /// <summary>Convert to CIE xy chromaticity.</summary>
        public CieXy ToCieXy()
        {
            double sum = X + Y + Z;
            if (sum <= double.Epsilon)
                return new CieXy(0, 0);
            return new CieXy(X / sum, Y / sum);
        }

        /// <summary>Convert to CIE xyY.</summary>
        public CieXyY ToCieXyY()
        {
            var xy = ToCieXy();
            return new CieXyY(xy.X, xy.Y, Y);
        }

        /// <summary>Convert XYZ to CIE L*a*b* relative to given white point.</summary>
        public CieLab ToLab(CieXyz whitePoint)
        {
            double fx = LabF(X / whitePoint.X);
            double fy = LabF(Y / whitePoint.Y);
            double fz = LabF(Z / whitePoint.Z);

            double L = 116.0 * fy - 16.0;
            double a = 500.0 * (fx - fy);
            double b = 200.0 * (fy - fz);

            return new CieLab(L, a, b);
        }

        private static double LabF(double t)
        {
            const double delta = 6.0 / 29.0;
            const double delta3 = delta * delta * delta;
            if (t > delta3)
                return Math.Cbrt(t);
            return t / (3.0 * delta * delta) + 4.0 / 29.0;
        }

        public static CieXyz operator +(CieXyz a, CieXyz b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static CieXyz operator -(CieXyz a, CieXyz b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static CieXyz operator *(CieXyz a, double s) => new(a.X * s, a.Y * s, a.Z * s);

        public bool Equals(CieXyz other) =>
            Math.Abs(X - other.X) < 1e-10 &&
            Math.Abs(Y - other.Y) < 1e-10 &&
            Math.Abs(Z - other.Z) < 1e-10;

        public override bool Equals(object? obj) => obj is CieXyz other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"XYZ({X:F4}, {Y:F4}, {Z:F4})";
    }
}
