using System;

namespace PickleCalLG.Meters
{
    public sealed class MeterMeasureRequest
    {
        public MeterMeasureRequest(MeterMeasurementMode mode, TimeSpan integrationTime, double targetIre, bool useAveraging)
        {
            Mode = mode;
            IntegrationTime = integrationTime;
            TargetIre = targetIre;
            UseAveraging = useAveraging;
        }

        public MeterMeasurementMode Mode { get; }
        public TimeSpan IntegrationTime { get; }
        public double TargetIre { get; }
        public bool UseAveraging { get; }
    }
}
