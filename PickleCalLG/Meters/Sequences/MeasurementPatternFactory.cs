using System;
using System.Collections.Generic;

namespace PickleCalLG.Meters.Sequences
{
    public static class MeasurementPatternFactory
    {
        private static readonly Dictionary<string, (byte r, byte g, byte b)> ColorLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "white", (255, 255, 255) },
            { "black", (0, 0, 0) },
            { "red", (255, 0, 0) },
            { "green", (0, 255, 0) },
            { "blue", (0, 0, 255) },
            { "cyan", (0, 255, 255) },
            { "magenta", (255, 0, 255) },
            { "yellow", (255, 255, 0) }
        };

        public static bool TryCreate(MeasurementStep step, out PatternInstruction instruction)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));

            // If the step has explicit RGB values, use them directly
            if (step.PatternR != 0 || step.PatternG != 0 || step.PatternB != 0)
            {
                string desc = step.PatternDescription ?? step.Name;
                instruction = PatternInstruction.FullField(desc, step.PatternR, step.PatternG, step.PatternB);
                return true;
            }

            string descriptor = (step.PatternDescription ?? step.Name ?? string.Empty).Trim();
            if (descriptor.Length == 0)
            {
                instruction = PatternInstruction.None;
                return false;
            }

            if (TryCreateForGray(step, descriptor, out instruction))
            {
                return true;
            }

            if (TryCreateForNamedColor(descriptor, out instruction))
            {
                return true;
            }

            instruction = PatternInstruction.None;
            return false;
        }

        private static bool TryCreateForGray(MeasurementStep step, string descriptor, out PatternInstruction instruction)
        {
            instruction = PatternInstruction.None;

            if (!descriptor.Contains("gray", StringComparison.OrdinalIgnoreCase) &&
                !descriptor.Contains("grey", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            byte level = FromIre(step.TargetIre);
            instruction = PatternInstruction.FullField(descriptor, level, level, level);
            return true;
        }

        private static bool TryCreateForNamedColor(string descriptor, out PatternInstruction instruction)
        {
            instruction = PatternInstruction.None;

            foreach (var entry in ColorLookup)
            {
                if (descriptor.StartsWith(entry.Key, StringComparison.OrdinalIgnoreCase))
                {
                    var (r, g, b) = entry.Value;
                    instruction = PatternInstruction.FullField(descriptor, r, g, b);
                    return true;
                }
            }

            return false;
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
