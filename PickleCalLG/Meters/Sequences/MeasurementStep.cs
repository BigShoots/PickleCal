using System;

namespace PickleCalLG.Meters.Sequences
{
    public sealed class MeasurementStep
    {
        public MeasurementStep(
            string name,
            double targetIre,
            MeterMeasurementMode mode,
            TimeSpan integrationTime,
            bool useAveraging,
            string? patternDescription = null,
            MeasurementCategory category = MeasurementCategory.Grayscale,
            byte patternR = 0,
            byte patternG = 0,
            byte patternB = 0)
        {
            if (targetIre < 0 || targetIre > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(targetIre), "Target IRE must be 0-100.");
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            TargetIre = targetIre;
            Mode = mode;
            IntegrationTime = integrationTime;
            UseAveraging = useAveraging;
            PatternDescription = patternDescription;
            Category = category;
            PatternR = patternR;
            PatternG = patternG;
            PatternB = patternB;
        }

        public string Name { get; }
        public double TargetIre { get; }
        public MeterMeasurementMode Mode { get; }
        public TimeSpan IntegrationTime { get; }
        public bool UseAveraging { get; }
        public string? PatternDescription { get; }
        public MeasurementCategory Category { get; }

        /// <summary>Explicit pattern RGB values (when not derived from name/IRE).</summary>
        public byte PatternR { get; }
        public byte PatternG { get; }
        public byte PatternB { get; }

        public override string ToString() => $"{Name} ({TargetIre:F0} IRE)";
    }
}
