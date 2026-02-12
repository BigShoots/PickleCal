using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PickleCalLG.ColorScience;
using PickleCalLG.Meters;
using PickleCalLG.Meters.Sequences;

namespace PickleCalLG.AutoCal
{
    /// <summary>
    /// Automated calibration engine for TVs that support remote white balance and CMS control.
    /// Orchestrates measure→adjust→re-measure loops for white balance and CMS.
    /// Supports SDR and HDR workflows using Android PGen patterns.
    /// </summary>
    public sealed class AutoCalEngine
    {
        private readonly ITvController _tv;
        private readonly MeterManager _meter;
        private readonly Func<byte, byte, byte, CancellationToken, Task> _setPattern;
        private readonly Action<string> _log;

        /// <summary>Maximum iterations per adjustment point before giving up.</summary>
        public int MaxIterations { get; set; } = 20;

        /// <summary>ΔE2000 target threshold — stop adjusting when below this.</summary>
        public double DeltaETarget { get; set; } = 1.0;

        /// <summary>Settle delay (ms) after setting a pattern before measuring.</summary>
        public int SettleDelayMs { get; set; } = 2000;

        /// <summary>Target color space for reference computation.</summary>
        public ColorSpace TargetColorSpace { get; set; } = ColorSpace.Rec709;

        /// <summary>Target gamma for grayscale reference.</summary>
        public double TargetGamma { get; set; } = 2.2;

        /// <summary>Measured peak white luminance (cd/m²). Set after measuring 100% white.</summary>
        public double PeakWhiteLuminance { get; set; } = 100.0;

        /// <summary>Measured black luminance (cd/m²).</summary>
        public double BlackLuminance { get; set; } = 0.0;

        public event Action<AutoCalProgress>? ProgressChanged;

        public AutoCalEngine(
            ITvController tv,
            MeterManager meter,
            Func<byte, byte, byte, CancellationToken, Task> setPattern,
            Action<string> log)
        {
            _tv = tv ?? throw new ArgumentNullException(nameof(tv));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _setPattern = setPattern ?? throw new ArgumentNullException(nameof(setPattern));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ────────────────────────────────────────
        //  White Balance Calibration (2-Point)
        // ────────────────────────────────────────

        /// <summary>
        /// Runs 2-point white balance calibration.
        /// Adjusts gain (for highlights) and offset (for shadows) to bring
        /// white and near-black points to target chromaticity.
        /// </summary>
        public async Task<AutoCalResult> Run2PointWhiteBalanceAsync(CancellationToken token)
        {
            _log("Starting 2-point white balance calibration...");
            ReportProgress("2pt WB", "Initializing", 0, MaxIterations);

            // Reset WB first
            await _tv.ResetWhiteBalanceAsync();
            await Task.Delay(500, token);

            // Measure baseline white (100%)
            var whiteMeas = await MeasurePatternAsync(255, 255, 255, token);
            PeakWhiteLuminance = whiteMeas.Y;
            _log($"Peak white: {whiteMeas.Y:F2} cd/m²");

            // Measure baseline black (0%)
            var blackMeas = await MeasurePatternAsync(0, 0, 0, token);
            BlackLuminance = blackMeas.Y;
            _log($"Black level: {blackMeas.Y:F4} cd/m²");

            int rGain = 0, gGain = 0, bGain = 0;
            int rOffset = 0, gOffset = 0, bOffset = 0;

            var targetXy = TargetColorSpace.WhitePoint;

            // Iterate: adjust gains for 80% white, offsets for 20% gray
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                token.ThrowIfCancellationRequested();
                ReportProgress("2pt WB", $"Iteration {iter + 1}", iter, MaxIterations);

                // --- Gain pass (measure 80% white) ---
                var highMeas = await MeasurePatternAsync(204, 204, 204, token);
                var highXy = highMeas.ToCieXy();
                double highDe = ComputeWbDeltaE(highMeas);

                if (highDe > DeltaETarget)
                {
                    var (dR, dG, dB) = ComputeWbCorrection(highXy, targetXy, 0.5);
                    rGain = Clamp(rGain + dR, -50, 50);
                    gGain = Clamp(gGain + dG, -50, 50);
                    bGain = Clamp(bGain + dB, -50, 50);

                    await _tv.SetWhiteBalance2ptAsync(rGain, gGain, bGain, rOffset, gOffset, bOffset);
                    await Task.Delay(500, token);
                    _log($"  Gain iter {iter + 1}: RGB({rGain},{gGain},{bGain}) ΔE={highDe:F2}");
                }

                // --- Offset pass (measure 20% gray) ---
                var lowMeas = await MeasurePatternAsync(51, 51, 51, token);
                var lowXy = lowMeas.ToCieXy();
                double lowDe = ComputeWbDeltaE(lowMeas);

                if (lowDe > DeltaETarget)
                {
                    var (dR, dG, dB) = ComputeWbCorrection(lowXy, targetXy, 0.3);
                    rOffset = Clamp(rOffset + dR, -50, 50);
                    gOffset = Clamp(gOffset + dG, -50, 50);
                    bOffset = Clamp(bOffset + dB, -50, 50);

                    await _tv.SetWhiteBalance2ptAsync(rGain, gGain, bGain, rOffset, gOffset, bOffset);
                    await Task.Delay(500, token);
                    _log($"  Offset iter {iter + 1}: RGB({rOffset},{gOffset},{bOffset}) ΔE={lowDe:F2}");
                }

                // Check convergence
                if (highDe <= DeltaETarget && lowDe <= DeltaETarget)
                {
                    _log($"2pt WB converged at iteration {iter + 1} (high ΔE={highDe:F2}, low ΔE={lowDe:F2})");
                    break;
                }
            }

            // Final verification
            var finalWhite = await MeasurePatternAsync(255, 255, 255, token);
            double finalDe = ComputeWbDeltaE(finalWhite);
            _log($"2pt WB complete. Final white ΔE={finalDe:F2}, Gain=({rGain},{gGain},{bGain}), Offset=({rOffset},{gOffset},{bOffset})");

            ReportProgress("2pt WB", "Complete", MaxIterations, MaxIterations);
            return new AutoCalResult("2-Point White Balance", finalDe, MaxIterations,
                $"Gain R={rGain} G={gGain} B={bGain}, Offset R={rOffset} G={gOffset} B={bOffset}");
        }

        // ────────────────────────────────────────
        //  White Balance Calibration (20-Point)
        // ────────────────────────────────────────

        /// <summary>
        /// Runs 20-point white balance calibration.
        /// Adjusts each 5% IRE step independently (0–100% in 5% increments = 21 points, indices 0–20).
        /// </summary>
        public async Task<AutoCalResult> Run20PointWhiteBalanceAsync(CancellationToken token)
        {
            _log("Starting 20-point white balance calibration...");

            // Measure baseline white and black
            var whiteMeas = await MeasurePatternAsync(255, 255, 255, token);
            PeakWhiteLuminance = whiteMeas.Y;
            var blackMeas = await MeasurePatternAsync(0, 0, 0, token);
            BlackLuminance = blackMeas.Y;

            var targetXy = TargetColorSpace.WhitePoint;
            int totalPoints = 21; // 0%,5%,10%,...100%
            double worstDe = 0;

            for (int ptIdx = 0; ptIdx < totalPoints; ptIdx++)
            {
                token.ThrowIfCancellationRequested();
                int ire = ptIdx * 5;
                byte level = (byte)Math.Round(ire / 100.0 * 255);
                ReportProgress("20pt WB", $"Point {ptIdx + 1}/21 ({ire}% IRE)", ptIdx, totalPoints);

                int rAdj = 0, gAdj = 0, bAdj = 0;

                for (int iter = 0; iter < MaxIterations; iter++)
                {
                    token.ThrowIfCancellationRequested();

                    var meas = await MeasurePatternAsync(level, level, level, token);
                    var xy = meas.ToCieXy();
                    double de = ComputeWbDeltaE(meas);

                    if (de <= DeltaETarget)
                    {
                        _log($"  {ire}% converged at iter {iter + 1}: ΔE={de:F2}");
                        break;
                    }

                    var (dR, dG, dB) = ComputeWbCorrection(xy, targetXy, 0.4);
                    rAdj = Clamp(rAdj + dR, -50, 50);
                    gAdj = Clamp(gAdj + dG, -50, 50);
                    bAdj = Clamp(bAdj + dB, -50, 50);

                    await _tv.SetWhiteBalance20ptPointAsync(ptIdx, rAdj, gAdj, bAdj);
                    await Task.Delay(300, token);

                    if (iter == MaxIterations - 1)
                        _log($"  {ire}% max iterations reached: ΔE={de:F2}");

                    if (de > worstDe) worstDe = de;
                }
            }

            _log($"20pt WB complete. Worst ΔE={worstDe:F2}");
            ReportProgress("20pt WB", "Complete", totalPoints, totalPoints);
            return new AutoCalResult("20-Point White Balance", worstDe, MaxIterations, $"21 points adjusted");
        }

        // ────────────────────────────────────────
        //  CMS (Color Management System) Calibration
        // ────────────────────────────────────────

        /// <summary>
        /// Adjusts CMS Hue/Saturation/Luminance for each primary and secondary color
        /// to match target color space chromaticities.
        /// </summary>
        public async Task<AutoCalResult> RunCmsCalibrationAsync(CancellationToken token)
        {
            _log("Starting CMS calibration...");

            // Reset CMS first
            await _tv.ResetCmsAsync();
            await Task.Delay(500, token);

            var colors = new[]
            {
                ("Red", 255, 0, 0),
                ("Green", 0, 255, 0),
                ("Blue", 0, 0, 255),
                ("Cyan", 0, 255, 255),
                ("Magenta", 255, 0, 255),
                ("Yellow", 255, 255, 0)
            };

            double worstDe = 0;
            int colorIdx = 0;

            foreach (var (name, r, g, b) in colors)
            {
                token.ThrowIfCancellationRequested();
                ReportProgress("CMS", $"{name}", colorIdx, colors.Length);

                // Compute reference XYZ for this color
                double rLinear = Eotf.PowerLaw(r / 255.0, TargetGamma);
                double gLinear = Eotf.PowerLaw(g / 255.0, TargetGamma);
                double bLinear = Eotf.PowerLaw(b / 255.0, TargetGamma);
                var refXyz = TargetColorSpace.RgbToXyz(rLinear, gLinear, bLinear);
                var refXy = refXyz.ToCieXy();

                int hueAdj = 0, satAdj = 0, lumAdj = 0;

                for (int iter = 0; iter < MaxIterations; iter++)
                {
                    token.ThrowIfCancellationRequested();

                    var meas = await MeasurePatternAsync((byte)r, (byte)g, (byte)b, token);
                    var measXy = meas.ToCieXy();

                    // Compute ΔE in Lab
                    var whiteXyz = TargetColorSpace.WhitePoint.ToXyz(PeakWhiteLuminance);
                    var measLab = meas.ToLab(whiteXyz);
                    var refLab = (refXyz * PeakWhiteLuminance).ToLab(whiteXyz);
                    double de = DeltaE.CIE2000(refLab, measLab);

                    if (de <= DeltaETarget)
                    {
                        _log($"  {name} converged at iter {iter + 1}: ΔE={de:F2}");
                        break;
                    }

                    // Hue: rotate to match target hue angle
                    var measLch = measLab.ToLch();
                    var refLch = refLab.ToLch();
                    double hueDiff = refLch.H - measLch.H;
                    if (hueDiff > 180) hueDiff -= 360;
                    if (hueDiff < -180) hueDiff += 360;
                    hueAdj = Clamp(hueAdj + (int)Math.Round(hueDiff * 0.3), -50, 50);

                    // Saturation: adjust chroma
                    double chromaRatio = refLch.C > 1 ? measLch.C / refLch.C : 1;
                    if (Math.Abs(chromaRatio - 1.0) > 0.02)
                    {
                        int satDelta = (int)Math.Round((1.0 - chromaRatio) * 10);
                        satAdj = Clamp(satAdj + satDelta, -50, 50);
                    }

                    // Luminance: adjust brightness
                    if (refLab.L > 1)
                    {
                        double lumRatio = measLab.L / refLab.L;
                        if (Math.Abs(lumRatio - 1.0) > 0.02)
                        {
                            int lumDelta = (int)Math.Round((1.0 - lumRatio) * 8);
                            lumAdj = Clamp(lumAdj + lumDelta, -50, 50);
                        }
                    }

                    await _tv.SetCmsColorAsync(name, hueAdj, satAdj, lumAdj);
                    await Task.Delay(300, token);

                    _log($"  {name} iter {iter + 1}: H={hueAdj} S={satAdj} L={lumAdj} ΔE={de:F2}");

                    if (de > worstDe) worstDe = de;

                    if (iter == MaxIterations - 1)
                        _log($"  {name} max iterations reached: ΔE={de:F2}");
                }

                colorIdx++;
            }

            _log($"CMS calibration complete. Worst ΔE={worstDe:F2}");
            ReportProgress("CMS", "Complete", colors.Length, colors.Length);
            return new AutoCalResult("CMS Calibration", worstDe, MaxIterations, "6 colors adjusted");
        }

        // ────────────────────────────────────────
        //  Full AutoCal (WB + CMS)
        // ────────────────────────────────────────

        /// <summary>
        /// Runs the complete auto-calibration:
        /// 1. Reset all settings
        /// 2. 20-point white balance
        /// 3. CMS color management
        /// 4. Verification measurement
        /// </summary>
        public async Task<List<AutoCalResult>> RunFullAutoCalAsync(CancellationToken token)
        {
            var results = new List<AutoCalResult>();

            _log("═══ Starting Full AutoCal ═══");

            // Step 1: Reset
            _log("Resetting white balance and CMS...");
            await _tv.ResetWhiteBalanceAsync();
            await _tv.ResetCmsAsync();
            await Task.Delay(1000, token);

            // Step 2: 20-point WB
            var wbResult = await Run20PointWhiteBalanceAsync(token);
            results.Add(wbResult);

            // Step 3: CMS
            var cmsResult = await RunCmsCalibrationAsync(token);
            results.Add(cmsResult);

            // Step 4: Verification
            _log("Running verification measurements...");
            ReportProgress("Verify", "Final check", 0, 1);
            var finalWhite = await MeasurePatternAsync(255, 255, 255, token);
            double finalWbDe = ComputeWbDeltaE(finalWhite);
            _log($"Final white ΔE={finalWbDe:F2}");

            results.Add(new AutoCalResult("Verification", finalWbDe, 1, "Final white balance check"));

            _log("═══ Full AutoCal Complete ═══");
            ReportProgress("Done", "AutoCal complete", 1, 1);

            return results;
        }

        // ────────────────────────────────────────
        //  Internal helpers
        // ────────────────────────────────────────

        private async Task<CieXyz> MeasurePatternAsync(byte r, byte g, byte b, CancellationToken token)
        {
            await _setPattern(r, g, b, token);
            await Task.Delay(SettleDelayMs, token);

            var request = new MeterMeasureRequest(MeterMeasurementMode.Display, TimeSpan.Zero, 0, false);
            var result = await _meter.MeasureAsync(request, token);

            if (result == null || !result.Success || result.Reading == null)
                throw new InvalidOperationException("Measurement failed during AutoCal");

            return new CieXyz(result.Reading.X, result.Reading.Y, result.Reading.Z);
        }

        private double ComputeWbDeltaE(CieXyz measured)
        {
            // Reference = white point at measured luminance
            var refXyz = TargetColorSpace.WhitePoint.ToXyz(measured.Y);
            var whiteRef = TargetColorSpace.WhitePoint.ToXyz(PeakWhiteLuminance);
            var measLab = measured.ToLab(whiteRef);
            var refLab = refXyz.ToLab(whiteRef);
            return DeltaE.CIE2000(refLab, measLab);
        }

        private static (int dR, int dG, int dB) ComputeWbCorrection(CieXy measured, CieXy target, double gain)
        {
            // Convert xy to approximate RGB correction direction
            // When x is too high (reddish), reduce red or boost blue/green
            // When y is too high (greenish), reduce green or boost red/blue
            double dx = target.X - measured.X;
            double dy = target.Y - measured.Y;

            // Approximate xy→RGB mapping for WB corrections
            // Red primary is at high x, low y
            // Green primary is at low x, high y
            // Blue primary is at low x, low y
            double scale = 100.0 * gain;

            // Simplified correction based on CIE xy deviation
            int dR = (int)Math.Round((dx * 3.0 - dy * 1.0) * scale);
            int dG = (int)Math.Round((-dx * 1.0 + dy * 3.0) * scale);
            int dB = (int)Math.Round((-dx * 2.0 - dy * 2.0) * scale);

            // Normalize so the dominant correction isn't too aggressive
            int maxAbs = Math.Max(Math.Abs(dR), Math.Max(Math.Abs(dG), Math.Abs(dB)));
            if (maxAbs > 5)
            {
                double norm = 5.0 / maxAbs;
                dR = (int)Math.Round(dR * norm);
                dG = (int)Math.Round(dG * norm);
                dB = (int)Math.Round(dB * norm);
            }

            return (dR, dG, dB);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void ReportProgress(string phase, string status, int current, int total)
        {
            ProgressChanged?.Invoke(new AutoCalProgress(phase, status, current, total));
        }
    }
}
