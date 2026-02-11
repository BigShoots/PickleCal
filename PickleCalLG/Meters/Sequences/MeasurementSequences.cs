using System;
using System.Collections.Generic;

namespace PickleCalLG.Meters.Sequences
{
    public static class MeasurementSequences
    {
        // ---------- Grayscale ----------

        public static MeasurementSequence Grayscale10Point(bool useAveraging = true)
        {
            return GrayscaleNPoint(10, useAveraging);
        }

        public static MeasurementSequence Grayscale21Point(bool useAveraging = true)
        {
            return GrayscaleNPoint(21, useAveraging);
        }

        /// <summary>Configurable N-point grayscale from 0% to 100% IRE.</summary>
        public static MeasurementSequence GrayscaleNPoint(int count, bool useAveraging = true)
        {
            if (count < 2) count = 2;
            var steps = new List<MeasurementStep>();
            for (int i = 0; i < count; i++)
            {
                double ire = (i * 100.0) / (count - 1);
                ire = Math.Round(ire, 1);
                byte level = FromIre(ire);
                steps.Add(new MeasurementStep(
                    name: $"Gray {ire:F0}",
                    targetIre: ire,
                    mode: MeterMeasurementMode.Display,
                    integrationTime: TimeSpan.Zero,
                    useAveraging: useAveraging,
                    patternDescription: $"Gray {ire:F0}%",
                    category: MeasurementCategory.Grayscale,
                    patternR: level, patternG: level, patternB: level));
            }
            return new MeasurementSequence($"{count}-Point Grayscale", steps);
        }

        // ---------- Near-Black (0–10 IRE in 1% steps) ----------

        public static MeasurementSequence NearBlack(bool useAveraging = true)
        {
            var steps = new List<MeasurementStep>();
            for (int ire = 0; ire <= 10; ire++)
            {
                byte level = FromIre(ire);
                steps.Add(new MeasurementStep(
                    name: $"Near-Black {ire}",
                    targetIre: ire,
                    mode: MeterMeasurementMode.Display,
                    integrationTime: TimeSpan.Zero,
                    useAveraging: useAveraging,
                    patternDescription: $"Gray {ire}%",
                    category: MeasurementCategory.NearBlack,
                    patternR: level, patternG: level, patternB: level));
            }
            return new MeasurementSequence("Near-Black", steps);
        }

        // ---------- Near-White (90–100 IRE in 1% steps) ----------

        public static MeasurementSequence NearWhite(bool useAveraging = true)
        {
            var steps = new List<MeasurementStep>();
            for (int ire = 90; ire <= 100; ire++)
            {
                byte level = FromIre(ire);
                steps.Add(new MeasurementStep(
                    name: $"Near-White {ire}",
                    targetIre: ire,
                    mode: MeterMeasurementMode.Display,
                    integrationTime: TimeSpan.Zero,
                    useAveraging: useAveraging,
                    patternDescription: $"Gray {ire}%",
                    category: MeasurementCategory.NearWhite,
                    patternR: level, patternG: level, patternB: level));
            }
            return new MeasurementSequence("Near-White", steps);
        }

        // ---------- Primaries & Secondaries ----------

        public static MeasurementSequence PrimarySecondarySweep(bool useAveraging = false)
        {
            var steps = new List<MeasurementStep>();

            // White reference
            steps.Add(ColorStep("White", 100, 255, 255, 255, MeasurementCategory.Primary, useAveraging));

            // Primaries at 75% and 100%
            foreach (var level in new[] { 75, 100 })
            {
                byte v = FromIre(level);
                steps.Add(ColorStep($"Red {level}%", level, v, 0, 0, MeasurementCategory.Primary, useAveraging));
                steps.Add(ColorStep($"Green {level}%", level, 0, v, 0, MeasurementCategory.Primary, useAveraging));
                steps.Add(ColorStep($"Blue {level}%", level, 0, 0, v, MeasurementCategory.Primary, useAveraging));
            }

            // Secondaries at 75% and 100%
            foreach (var level in new[] { 75, 100 })
            {
                byte v = FromIre(level);
                steps.Add(ColorStep($"Cyan {level}%", level, 0, v, v, MeasurementCategory.Secondary, useAveraging));
                steps.Add(ColorStep($"Magenta {level}%", level, v, 0, v, MeasurementCategory.Secondary, useAveraging));
                steps.Add(ColorStep($"Yellow {level}%", level, v, v, 0, MeasurementCategory.Secondary, useAveraging));
            }

            return new MeasurementSequence("Primary & Secondary Sweep", steps);
        }

        // ---------- Saturation Sweeps ----------

        /// <summary>Saturation sweep for a single color at configurable steps (0–100%).</summary>
        public static MeasurementSequence SaturationSweep(string colorName, int steps = 5, bool useAveraging = false)
        {
            var list = new List<MeasurementStep>();
            for (int i = 1; i <= steps; i++)
            {
                double satPercent = (i * 100.0) / steps;
                byte sat = FromIre(satPercent);
                byte bg = (byte)(255 - sat); // desaturation complement

                byte r = 0, g = 0, b = 0;
                switch (colorName.ToLowerInvariant())
                {
                    case "red": r = 255; g = bg; b = bg; break;
                    case "green": r = bg; g = 255; b = bg; break;
                    case "blue": r = bg; g = bg; b = 255; break;
                    case "cyan": r = 0; g = sat; b = sat; break;
                    case "magenta": r = sat; g = 0; b = sat; break;
                    case "yellow": r = sat; g = sat; b = 0; break;
                }

                list.Add(new MeasurementStep(
                    name: $"{colorName} Sat {satPercent:F0}%",
                    targetIre: satPercent,
                    mode: MeterMeasurementMode.Display,
                    integrationTime: TimeSpan.Zero,
                    useAveraging: useAveraging,
                    patternDescription: $"{colorName} Saturation {satPercent:F0}%",
                    category: MeasurementCategory.Saturation,
                    patternR: r, patternG: g, patternB: b));
            }
            return new MeasurementSequence($"{colorName} Saturation Sweep", list);
        }

        /// <summary>Full saturation sweep for all 6 colors (HCFR-style).</summary>
        public static MeasurementSequence FullSaturationSweep(int stepsPerColor = 5, bool useAveraging = false)
        {
            var all = new List<MeasurementStep>();
            foreach (var color in new[] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" })
            {
                var seq = SaturationSweep(color, stepsPerColor, useAveraging);
                all.AddRange(seq.Steps);
            }
            return new MeasurementSequence("Full Saturation Sweep", all);
        }

        // ---------- Contrast Ratio ----------

        public static MeasurementSequence ContrastRatio(bool useAveraging = true)
        {
            var steps = new List<MeasurementStep>
            {
                new MeasurementStep("White (100%)", 100, MeterMeasurementMode.Display,
                    TimeSpan.Zero, useAveraging, "White 100%", MeasurementCategory.ContrastRatio,
                    255, 255, 255),
                new MeasurementStep("Black (0%)", 0, MeterMeasurementMode.Display,
                    TimeSpan.Zero, useAveraging, "Black 0%", MeasurementCategory.ContrastRatio,
                    0, 0, 0)
            };
            return new MeasurementSequence("Contrast Ratio", steps);
        }

        // ---------- Measure Everything ----------

        /// <summary>Combined full measurement run (grayscale + primaries + secondaries + near-black + near-white + contrast).</summary>
        public static MeasurementSequence MeasureEverything(int grayscalePoints = 21, bool useAveraging = true)
        {
            var all = new List<MeasurementStep>();
            all.AddRange(ContrastRatio(useAveraging).Steps);
            all.AddRange(GrayscaleNPoint(grayscalePoints, useAveraging).Steps);
            all.AddRange(NearBlack(useAveraging).Steps);
            all.AddRange(NearWhite(useAveraging).Steps);
            all.AddRange(PrimarySecondarySweep(useAveraging).Steps);
            all.AddRange(FullSaturationSweep(5, useAveraging).Steps);
            return new MeasurementSequence("Measure Everything", all);
        }

        // ---------- Helpers ----------

        private static MeasurementStep ColorStep(string name, int ire, byte r, byte g, byte b,
            MeasurementCategory category, bool useAveraging)
        {
            return new MeasurementStep(
                name: name,
                targetIre: ire,
                mode: MeterMeasurementMode.Display,
                integrationTime: TimeSpan.Zero,
                useAveraging: useAveraging,
                patternDescription: name,
                category: category,
                patternR: r, patternG: g, patternB: b);
        }

        private static byte FromIre(double ire)
        {
            double clamped = Math.Clamp(ire, 0d, 100d);
            double scaled = clamped / 100d * 255d;
            int rounded = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
            return (byte)Math.Clamp(rounded, 0, 255);
        }
    }
}
