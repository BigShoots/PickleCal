namespace PickleCalLG.Meters
{
    public sealed class MeterCalibrationResult
    {
        private MeterCalibrationResult(bool success, string? message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string? Message { get; }

        public static MeterCalibrationResult Ok() => new MeterCalibrationResult(true, null);
        public static MeterCalibrationResult Error(string message) => new MeterCalibrationResult(false, message);
    }
}
