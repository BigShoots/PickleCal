using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// CIE xy chromaticity coordinate.
    /// </summary>
    public readonly struct CieXy : IEquatable<CieXy>
    {
        public double X { get; }
        public double Y { get; }

        public CieXy(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Convert xy to XYZ with given luminance Y.</summary>
        public CieXyz ToXyz(double luminance = 1.0)
        {
            if (Y <= double.Epsilon)
                return new CieXyz(0, 0, 0);
            double bigX = (X / this.Y) * luminance;
            double bigZ = ((1.0 - X - this.Y) / this.Y) * luminance;
            return new CieXyz(bigX, luminance, bigZ);
        }

        public double DistanceTo(CieXy other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool Equals(CieXy other) => Math.Abs(X - other.X) < 1e-10 && Math.Abs(Y - other.Y) < 1e-10;
        public override bool Equals(object? obj) => obj is CieXy other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"xy({X:F4}, {Y:F4})";

        public static bool operator ==(CieXy a, CieXy b) => a.Equals(b);
        public static bool operator !=(CieXy a, CieXy b) => !a.Equals(b);
    }
}
