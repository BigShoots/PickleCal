using PickleCalLG.Meters;

namespace PickleCalLG.Meters.Argyll
{
    public sealed class SpotreadRunParameters
    {
        public MeterMeasurementMode Mode { get; init; } = MeterMeasurementMode.Display;
        public bool HighResolution { get; init; }
        public bool UseAveraging { get; init; }
        public bool SkipInitialCalibration { get; init; } = true;
        public string? DisplayType { get; init; }
        public string? CorrectionFile { get; init; }
        public string? SpectralSampleFile { get; init; }
        public string? ExtraArguments { get; init; }
    }
}
