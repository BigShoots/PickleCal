namespace PickleCalLG.Meters
{
    /// <summary>
    /// Target EOTF (Electro-Optical Transfer Function) type for calibration.
    /// </summary>
    public enum EotfType
    {
        /// <summary>Pure power law gamma (e.g. 2.2, 2.4).</summary>
        Gamma,

        /// <summary>sRGB IEC 61966-2-1.</summary>
        Srgb,

        /// <summary>BT.1886 (absolute γ=2.4 with black offset).</summary>
        Bt1886,

        /// <summary>SMPTE ST.2084 Perceptual Quantizer (HDR-10).</summary>
        PQ,

        /// <summary>BT.2100 Hybrid Log-Gamma (HLG).</summary>
        Hlg,

        /// <summary>CIE L* perceptual lightness curve.</summary>
        LStar
    }
}
