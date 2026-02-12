using System;

namespace PickleCalLG.Meters
{
    public sealed class MeterCalibrationRequest
    {
        public MeterCalibrationRequest(MeterMeasurementMode mode, bool useAutoCalibration)
        {
            Mode = mode;
            UseAutoCalibration = useAutoCalibration;
        }

        public MeterMeasurementMode Mode { get; }
        public bool UseAutoCalibration { get; }
    }
}
