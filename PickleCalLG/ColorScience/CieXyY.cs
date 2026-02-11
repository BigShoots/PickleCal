using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// CIE xyY color coordinate.
    /// </summary>
    public readonly struct CieXyY
    {
        public double x { get; }
        public double y { get; }
        public double Y { get; }

        public CieXyY(double x, double y, double bigY)
        {
            this.x = x;
            this.y = y;
            Y = bigY;
        }

        public CieXyz ToXyz()
        {
            if (y <= double.Epsilon)
                return new CieXyz(0, 0, 0);
            double X = (x / y) * Y;
            double Z = ((1.0 - x - y) / y) * Y;
            return new CieXyz(X, Y, Z);
        }

        public CieXy ToCieXy() => new(x, y);

        public override string ToString() => $"xyY({x:F4}, {y:F4}, {Y:F2})";
    }
}
