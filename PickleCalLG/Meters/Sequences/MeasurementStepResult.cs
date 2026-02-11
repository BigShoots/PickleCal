namespace PickleCalLG.Meters.Sequences
{
    public sealed class MeasurementStepResult
    {
        public MeasurementStepResult(MeasurementStep step, MeterMeasurementResult? result)
        {
            Step = step;
            Result = result;
        }

        public MeasurementStep Step { get; }
        public MeterMeasurementResult? Result { get; }

        public bool Success => Result != null && Result.Success;
    }
}
