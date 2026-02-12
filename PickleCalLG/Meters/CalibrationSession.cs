using System;
using System.Collections.Generic;
using System.Linq;
using PickleCalLG.ColorScience;

namespace PickleCalLG.Meters
{
    /// <summary>
    /// The central document holding all calibration measurements for a display.
    /// Equivalent to HCFR's CDataSetDoc — stores grayscale, primaries, secondaries,
    /// saturations, near-black/white, color checker, and contrast measurements.
    /// </summary>
    public sealed class CalibrationSession
    {
        private readonly List<CalibrationPoint> _points = new();

        public CalibrationSession(string displayName, ColorSpace targetColorSpace, EotfType targetEotf, double targetGamma = 2.2)
        {
            Id = Guid.NewGuid().ToString("N");
            DisplayName = displayName;
            TargetColorSpace = targetColorSpace;
            TargetEotf = targetEotf;
            TargetGamma = targetGamma;
            CreatedUtc = DateTime.UtcNow;
            WhitePoint = targetColorSpace.WhitePoint.ToXyz(1.0);
        }

        public string Id { get; }
        public string DisplayName { get; set; }
        public ColorSpace TargetColorSpace { get; }
        public EotfType TargetEotf { get; }
        public double TargetGamma { get; }
        public DateTime CreatedUtc { get; }
        public CieXyz WhitePoint { get; set; }

        /// <summary>Peak white luminance measured from 100% IRE.</summary>
        public double PeakWhiteLuminance { get; set; } = 100.0;

        /// <summary>Black level luminance measured from 0% IRE.</summary>
        public double BlackLuminance { get; set; } = 0.0;

        public IReadOnlyList<CalibrationPoint> Points => _points;

        // ---------- Filtered views ----------

        public IEnumerable<CalibrationPoint> Grayscale =>
            _points.Where(p => p.Category == MeasurementCategory.Grayscale).OrderBy(p => p.TargetIre);

        public IEnumerable<CalibrationPoint> NearBlack =>
            _points.Where(p => p.Category == MeasurementCategory.NearBlack).OrderBy(p => p.TargetIre);

        public IEnumerable<CalibrationPoint> NearWhite =>
            _points.Where(p => p.Category == MeasurementCategory.NearWhite).OrderBy(p => p.TargetIre);

        public IEnumerable<CalibrationPoint> Primaries =>
            _points.Where(p => p.Category == MeasurementCategory.Primary);

        public IEnumerable<CalibrationPoint> Secondaries =>
            _points.Where(p => p.Category == MeasurementCategory.Secondary);

        public IEnumerable<CalibrationPoint> Saturations =>
            _points.Where(p => p.Category == MeasurementCategory.Saturation).OrderBy(p => p.Name);

        public IEnumerable<CalibrationPoint> ColorChecker =>
            _points.Where(p => p.Category == MeasurementCategory.ColorChecker);

        public IEnumerable<CalibrationPoint> FreeMeasures =>
            _points.Where(p => p.Category == MeasurementCategory.Free);

        // ---------- Adding measurements ----------

        /// <summary>Add a grayscale point from a meter reading.</summary>
        public CalibrationPoint AddGrayscalePoint(string name, double targetIre, CieXyz measured)
        {
            double signal = targetIre / 100.0;
            var reference = ComputeGrayscaleReference(signal);
            var point = new CalibrationPoint(name, MeasurementCategory.Grayscale, targetIre, measured, reference, WhitePoint, DateTime.UtcNow);

            // Compute effective gamma
            if (targetIre > 0 && targetIre < 100)
            {
                point.EffectiveGamma = Eotf.EffectiveGamma(signal, measured.Y, PeakWhiteLuminance, BlackLuminance);
            }

            // Compute RGB error
            point.RgbError = ComputeRgbError(measured, reference);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a near-black point (0–10 IRE range).</summary>
        public CalibrationPoint AddNearBlackPoint(string name, double targetIre, CieXyz measured)
        {
            double signal = targetIre / 100.0;
            var reference = ComputeGrayscaleReference(signal);
            var point = new CalibrationPoint(name, MeasurementCategory.NearBlack, targetIre, measured, reference, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a near-white point (90–100 IRE range).</summary>
        public CalibrationPoint AddNearWhitePoint(string name, double targetIre, CieXyz measured)
        {
            double signal = targetIre / 100.0;
            var reference = ComputeGrayscaleReference(signal);
            var point = new CalibrationPoint(name, MeasurementCategory.NearWhite, targetIre, measured, reference, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a primary color measurement.</summary>
        public CalibrationPoint AddPrimaryPoint(string name, double stimulusPercent, CieXyz measured)
        {
            var reference = ComputeColorReference(name, stimulusPercent);
            var point = new CalibrationPoint(name, MeasurementCategory.Primary, stimulusPercent, measured, reference, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a secondary color measurement.</summary>
        public CalibrationPoint AddSecondaryPoint(string name, double stimulusPercent, CieXyz measured)
        {
            var reference = ComputeColorReference(name, stimulusPercent);
            var point = new CalibrationPoint(name, MeasurementCategory.Secondary, stimulusPercent, measured, reference, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a saturation sweep point.</summary>
        public CalibrationPoint AddSaturationPoint(string name, double saturationPercent, CieXyz measured)
        {
            var reference = ComputeColorReference(name, saturationPercent);
            var point = new CalibrationPoint(name, MeasurementCategory.Saturation, saturationPercent, measured, reference, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a color checker patch measurement.</summary>
        public CalibrationPoint AddColorCheckerPoint(string name, CieXyz measured, CieXyz reference)
        {
            var point = new CalibrationPoint(name, MeasurementCategory.ColorChecker, 100, measured, reference, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Add a free arbitrary measurement.</summary>
        public CalibrationPoint AddFreePoint(string name, CieXyz measured)
        {
            // For free measures, reference = measured (ΔE = 0)
            var point = new CalibrationPoint(name, MeasurementCategory.Free, 0, measured, measured, WhitePoint, DateTime.UtcNow);
            _points.Add(point);
            return point;
        }

        /// <summary>Clear all measurements in a given category.</summary>
        public void ClearCategory(MeasurementCategory category)
        {
            _points.RemoveAll(p => p.Category == category);
        }

        /// <summary>Clear all measurements.</summary>
        public void ClearAll()
        {
            _points.Clear();
        }

        // ---------- Aggregate stats ----------

        /// <summary>Contrast ratio (on/off) = peak white / black level luminance.</summary>
        public double ContrastRatio => BlackLuminance > 0 ? PeakWhiteLuminance / BlackLuminance : double.PositiveInfinity;

        /// <summary>Average ΔE2000 across all grayscale points.</summary>
        public double AverageGrayscaleDeltaE =>
            Grayscale.Any() ? Grayscale.Average(p => p.DeltaE2000) : 0;

        /// <summary>Maximum ΔE2000 across all grayscale points.</summary>
        public double MaxGrayscaleDeltaE =>
            Grayscale.Any() ? Grayscale.Max(p => p.DeltaE2000) : 0;

        /// <summary>Average ΔE2000 across all measurement types.</summary>
        public double AverageDeltaE =>
            _points.Any() ? _points.Average(p => p.DeltaE2000) : 0;

        // ---------- Internal reference computation ----------

        private CieXyz ComputeGrayscaleReference(double signal)
        {
            double linear = ApplyEotf(signal);
            double targetY = BlackLuminance + linear * (PeakWhiteLuminance - BlackLuminance);
            return WhitePoint.ToCieXy().ToXyz(targetY);
        }

        private CieXyz ComputeColorReference(string colorName, double stimulusPercent)
        {
            double level = stimulusPercent / 100.0;
            double eotfLevel = ApplyEotf(level);

            double r = 0, g = 0, b = 0;
            string lower = colorName.ToLowerInvariant();
            if (lower.Contains("red")) { r = eotfLevel; }
            else if (lower.Contains("green")) { g = eotfLevel; }
            else if (lower.Contains("blue")) { b = eotfLevel; }
            else if (lower.Contains("cyan")) { g = eotfLevel; b = eotfLevel; }
            else if (lower.Contains("magenta")) { r = eotfLevel; b = eotfLevel; }
            else if (lower.Contains("yellow")) { r = eotfLevel; g = eotfLevel; }
            else if (lower.Contains("white")) { r = eotfLevel; g = eotfLevel; b = eotfLevel; }

            var xyz = TargetColorSpace.RgbToXyz(r, g, b);
            // Scale to measured peak white
            return xyz * PeakWhiteLuminance;
        }

        private double ApplyEotf(double signal)
        {
            return TargetEotf switch
            {
                EotfType.Gamma => Eotf.PowerLaw(signal, TargetGamma),
                EotfType.Srgb => Eotf.Srgb(signal),
                EotfType.Bt1886 => Eotf.Bt1886(signal, 1.0, 0.0), // normalized
                EotfType.PQ => Eotf.Pq(signal) / 10000.0, // normalize to [0,1]
                EotfType.Hlg => Eotf.Hlg(signal),
                EotfType.LStar => Eotf.LStar(signal),
                _ => Eotf.PowerLaw(signal, TargetGamma)
            };
        }

        private (double R, double G, double B) ComputeRgbError(CieXyz measured, CieXyz reference)
        {
            var (mr, mg, mb) = TargetColorSpace.XyzToRgb(measured);
            var (rr, rg, rb) = TargetColorSpace.XyzToRgb(reference);

            // Normalize both to reference
            double denomR = Math.Abs(rr) > 1e-10 ? rr : 1.0;
            double denomG = Math.Abs(rg) > 1e-10 ? rg : 1.0;
            double denomB = Math.Abs(rb) > 1e-10 ? rb : 1.0;

            return ((mr - rr) / denomR, (mg - rg) / denomG, (mb - rb) / denomB);
        }
    }
}
