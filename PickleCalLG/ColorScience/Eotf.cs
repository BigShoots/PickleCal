using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// Electro-Optical Transfer Function (EOTF) models used in calibration.
    /// Maps normalized signal level [0,1] to linear light [0,1] (SDR) or absolute nits (HDR).
    /// </summary>
    public static class Eotf
    {
        /// <summary>Pure power-law gamma (e.g. 2.2, 2.4).</summary>
        public static double PowerLaw(double signal, double gamma)
        {
            if (signal <= 0) return 0;
            return Math.Pow(signal, gamma);
        }

        /// <summary>sRGB EOTF (IEC 61966-2-1).</summary>
        public static double Srgb(double signal)
        {
            if (signal <= 0.04045)
                return signal / 12.92;
            return Math.Pow((signal + 0.055) / 1.055, 2.4);
        }

        /// <summary>Inverse sRGB (linear → sRGB signal).</summary>
        public static double SrgbInverse(double linear)
        {
            if (linear <= 0.0031308)
                return 12.92 * linear;
            return 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
        }

        /// <summary>
        /// BT.1886 EOTF (absolute or relative).
        /// Parameters: Lw = peak white luminance (cd/m²), Lb = black level luminance.
        /// </summary>
        public static double Bt1886(double signal, double Lw = 100.0, double Lb = 0.0)
        {
            double gamma = 2.4;
            double a = Math.Pow(Math.Pow(Lw, 1.0 / gamma) - Math.Pow(Lb, 1.0 / gamma), gamma);
            double b = Math.Pow(Lb, 1.0 / gamma) / (Math.Pow(Lw, 1.0 / gamma) - Math.Pow(Lb, 1.0 / gamma));
            return a * Math.Pow(Math.Max(signal + b, 0), gamma);
        }

        /// <summary>
        /// SMPTE ST.2084 (PQ) EOTF — returns absolute luminance in cd/m² (0–10000 nits).
        /// </summary>
        public static double Pq(double signal)
        {
            const double m1 = 2610.0 / 16384.0;
            const double m2 = 2523.0 / 4096.0 * 128.0;
            const double c1 = 3424.0 / 4096.0;
            const double c2 = 2413.0 / 4096.0 * 32.0;
            const double c3 = 2392.0 / 4096.0 * 32.0;

            if (signal <= 0) return 0;

            double Np = Math.Pow(signal, 1.0 / m2);
            double numerator = Math.Max(Np - c1, 0);
            double denominator = c2 - c3 * Np;
            if (denominator <= 0) return 0;

            return 10000.0 * Math.Pow(numerator / denominator, 1.0 / m1);
        }

        /// <summary>Inverse PQ (luminance cd/m² → PQ signal).</summary>
        public static double PqInverse(double luminance)
        {
            const double m1 = 2610.0 / 16384.0;
            const double m2 = 2523.0 / 4096.0 * 128.0;
            const double c1 = 3424.0 / 4096.0;
            const double c2 = 2413.0 / 4096.0 * 32.0;
            const double c3 = 2392.0 / 4096.0 * 32.0;

            if (luminance <= 0) return 0;
            double Y = luminance / 10000.0;
            double Ym1 = Math.Pow(Y, m1);
            return Math.Pow((c1 + c2 * Ym1) / (1.0 + c3 * Ym1), m2);
        }

        /// <summary>
        /// HLG (Hybrid Log-Gamma) OETF — BT.2100 scene-referred.
        /// </summary>
        public static double Hlg(double signal)
        {
            const double a = 0.17883277;
            const double b = 1.0 - 4.0 * a;
            const double c = 0.5 - a * Math.E; // ln(4a) ≈ ...

            if (signal <= 0) return 0;
            if (signal <= 0.5)
                return signal * signal / 3.0;
            return (Math.Exp((signal - c) / a) + b) / 12.0;
        }

        /// <summary>CIE L* perceptual lightness EOTF.</summary>
        public static double LStar(double signal)
        {
            // L* = 116 * f(Y/Yn) - 16 => Y/Yn = f^-1((L*+16)/116)
            // Re-purpose: treat signal as normalized L* → linear
            double L = signal * 100.0;
            double fy = (L + 16.0) / 116.0;
            if (fy > 6.0 / 29.0)
                return fy * fy * fy;
            return 3.0 * (6.0 / 29.0) * (6.0 / 29.0) * (fy - 4.0 / 29.0);
        }

        /// <summary>
        /// Compute effective gamma at a given signal level from measured luminance.
        /// effectiveGamma = log(measuredLinear / peakLinear) / log(signal)
        /// </summary>
        public static double EffectiveGamma(double signal, double measuredLuminance, double peakLuminance, double blackLuminance = 0)
        {
            if (signal <= 0 || signal >= 1.0 || peakLuminance <= blackLuminance)
                return double.NaN;

            double normalizedMeasured = (measuredLuminance - blackLuminance) / (peakLuminance - blackLuminance);
            if (normalizedMeasured <= 0)
                return double.NaN;

            return Math.Log(normalizedMeasured) / Math.Log(signal);
        }
    }
}
