using System;

namespace PickleCalLG.Meters
{
    public sealed class MeterConnectOptions
    {
        public TimeSpan WarmupTime { get; init; } = TimeSpan.FromSeconds(2);
        public MeterMeasurementMode PreferredMode { get; init; } = MeterMeasurementMode.Display;
        public bool UseHighResolution { get; init; }
        public string? DisplayType { get; init; }
        public string? SpectrumPreset { get; init; }
    }
}
