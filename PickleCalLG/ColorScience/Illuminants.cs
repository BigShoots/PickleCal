namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// Standard illuminant reference white points in CIE xy chromaticity.
    /// </summary>
    public static class Illuminants
    {
        // CIE standard illuminant D65 (6504 K daylight)
        public static readonly CieXy D65 = new(0.3127, 0.3290);

        // CIE standard illuminant D50 (5000 K horizon daylight)
        public static readonly CieXy D50 = new(0.3457, 0.3585);

        // CIE standard illuminant D93 (9300 K blue-ish)
        public static readonly CieXy D93 = new(0.2848, 0.2932);

        // CIE illuminant E (equal-energy white)
        public static readonly CieXy E = new(1.0 / 3.0, 1.0 / 3.0);
    }
}
