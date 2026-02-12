using System;

namespace PickleCalLG.Meters
{
    public sealed class MeterReading
    {
        public MeterReading(DateTime timestamp, double x, double y, double z, double luminance, double? correlatedColorTemperatureK = null, double? deltaE2000 = null, double? targetIre = null)
        {
            Timestamp = timestamp;
            X = x;
            Y = y;
            Z = z;
            Luminance = luminance;
            CorrelatedColorTemperatureK = correlatedColorTemperatureK;
            DeltaE2000 = deltaE2000;
            TargetIre = targetIre;
        }

        public DateTime Timestamp { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Luminance { get; }
        public double? CorrelatedColorTemperatureK { get; }
        public double? DeltaE2000 { get; }
        public double? TargetIre { get; }

        public (double x, double y) Chromaticity
        {
            get
            {
                double sum = X + Y + Z;
                if (sum <= double.Epsilon)
                {
                    return (0d, 0d);
                }
                return (X / sum, Y / sum);
            }
        }

        public override string ToString()
        {
            var (x, y) = Chromaticity;
            return $"XYZ({X:F3}, {Y:F3}, {Z:F3}) xyY({x:F4}, {y:F4}, {Luminance:F2})";
        }
    }
}
