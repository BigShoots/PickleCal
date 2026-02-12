namespace PickleCalLG.AutoCal
{
    /// <summary>
    /// Progress update from the AutoCal engine.
    /// </summary>
    public sealed class AutoCalProgress
    {
        public AutoCalProgress(string phase, string status, int currentStep, int totalSteps)
        {
            Phase = phase;
            Status = status;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
        }

        public string Phase { get; }
        public string Status { get; }
        public int CurrentStep { get; }
        public int TotalSteps { get; }

        public double ProgressPercent => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100.0 : 0;

        public override string ToString() =>
            $"[{Phase}] {Status} ({CurrentStep}/{TotalSteps})";
    }
}
