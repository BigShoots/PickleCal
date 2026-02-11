using System;

namespace PickleCalLG.Meters
{
    public sealed class MeterMeasurementResult
    {
        private MeterMeasurementResult(bool success, MeterReading? reading, string? message)
        {
            Success = success;
            Reading = reading;
            Message = message;
        }

        public bool Success { get; }
        public MeterReading? Reading { get; }
        public string? Message { get; }

        public static MeterMeasurementResult Ok(MeterReading reading) => new MeterMeasurementResult(true, reading, null);
        public static MeterMeasurementResult Error(string message) => new MeterMeasurementResult(false, null, message);

        public MeterMeasurementResult WithMessage(string? message) => new MeterMeasurementResult(Success, Reading, message);
    }
}
