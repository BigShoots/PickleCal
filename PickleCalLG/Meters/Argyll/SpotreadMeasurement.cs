using System;
using System.Globalization;

namespace PickleCalLG.Meters.Argyll
{
    public sealed class SpotreadMeasurement
    {
        public SpotreadMeasurement(DateTime timestamp, double x, double y, double z, double luminance, double? cct, double? deltaE)
        {
            Timestamp = timestamp;
            X = x;
            Y = y;
            Z = z;
            Luminance = luminance;
            CorrelatedColorTemperatureK = cct;
            DeltaE2000 = deltaE;
        }

        public DateTime Timestamp { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Luminance { get; }
        public double? CorrelatedColorTemperatureK { get; }
        public double? DeltaE2000 { get; }

        public (double x, double y) Chromaticity
        {
            get
            {
                var sum = X + Y + Z;
                if (sum <= double.Epsilon)
                {
                    return (0d, 0d);
                }
                return (X / sum, Y / sum);
            }
        }

        public MeterReading ToMeterReading(double? targetIre)
        {
            return new MeterReading(Timestamp, X, Y, Z, Luminance, CorrelatedColorTemperatureK, DeltaE2000, targetIre);
        }
    }
}
