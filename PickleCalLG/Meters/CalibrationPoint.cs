using System;
using PickleCalLG.ColorScience;

namespace PickleCalLG.Meters
{
    /// <summary>
    /// A single calibration measurement with full color science analysis.
    /// Extends MeterReading with reference comparison data (ΔE, gamma, target chromaticity).
    /// </summary>
    public sealed class CalibrationPoint
    {
        public CalibrationPoint(
            string name,
            MeasurementCategory category,
            double targetIre,
            CieXyz measuredXyz,
            CieXyz referenceXyz,
            CieXyz whitePointXyz,
            DateTime timestamp)
        {
            Name = name;
            Category = category;
            TargetIre = targetIre;
            MeasuredXyz = measuredXyz;
            ReferenceXyz = referenceXyz;
            WhitePointXyz = whitePointXyz;
            Timestamp = timestamp;

            // Derived color science
            MeasuredXy = measuredXyz.ToCieXy();
            ReferenceXy = referenceXyz.ToCieXy();
            MeasuredLab = measuredXyz.ToLab(whitePointXyz);
            ReferenceLab = referenceXyz.ToLab(whitePointXyz);
            MeasuredLch = MeasuredLab.ToLch();
            ReferenceLch = ReferenceLab.ToLch();

            DeltaE76 = DeltaE.CIE76(ReferenceLab, MeasuredLab);
            DeltaE94 = DeltaE.CIE94(ReferenceLab, MeasuredLab);
            DeltaE2000 = DeltaE.CIE2000(ReferenceLab, MeasuredLab);
            DeltaEICtCp = DeltaE.ICtCp(referenceXyz, measuredXyz);

            Luminance = measuredXyz.Y;
            CCT = CctCalculator.McCamy(MeasuredXy);
        }

        // Identity
        public string Name { get; }
        public MeasurementCategory Category { get; }
        public double TargetIre { get; }
        public DateTime Timestamp { get; }

        // Raw data
        public CieXyz MeasuredXyz { get; }
        public CieXyz ReferenceXyz { get; }
        public CieXyz WhitePointXyz { get; }

        // Chromaticity
        public CieXy MeasuredXy { get; }
        public CieXy ReferenceXy { get; }

        // CIELAB
        public CieLab MeasuredLab { get; }
        public CieLab ReferenceLab { get; }

        // LCh
        public CieLch MeasuredLch { get; }
        public CieLch ReferenceLch { get; }

        // Delta E
        public double DeltaE76 { get; }
        public double DeltaE94 { get; }
        public double DeltaE2000 { get; }
        public double DeltaEICtCp { get; }

        // Photometric
        public double Luminance { get; }
        public double CCT { get; }

        /// <summary>Effective gamma if this is a grayscale point.</summary>
        public double? EffectiveGamma { get; internal set; }

        /// <summary>RGB error normalized to peak (for RGB balance tracking).</summary>
        public (double R, double G, double B)? RgbError { get; internal set; }

        public override string ToString() =>
            $"{Name}: ΔE₂₀₀₀={DeltaE2000:F2}, Y={Luminance:F2} cd/m², CCT={CCT:F0}K";
    }
}
