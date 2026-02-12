namespace PickleCalLG.AutoCal
{
    /// <summary>
    /// Result of an auto-calibration phase.
    /// </summary>
    public sealed class AutoCalResult
    {
        public AutoCalResult(string phaseName, double finalDeltaE, int iterations, string summary)
        {
            PhaseName = phaseName;
            FinalDeltaE = finalDeltaE;
            Iterations = iterations;
            Summary = summary;
        }

        public string PhaseName { get; }
        public double FinalDeltaE { get; }
        public int Iterations { get; }
        public string Summary { get; }

        public override string ToString() =>
            $"{PhaseName}: ΔE₂₀₀₀={FinalDeltaE:F2} ({Summary})";
    }
}
