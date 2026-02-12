using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PickleCalLG.Meters.Sequences
{
    public sealed class MeasurementSequence
    {
        public MeasurementSequence(string name, IEnumerable<MeasurementStep> steps)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Steps = new ReadOnlyCollection<MeasurementStep>(new List<MeasurementStep>(steps ?? throw new ArgumentNullException(nameof(steps))));
            if (Steps.Count == 0)
            {
                throw new ArgumentException("Sequence must contain at least one step.", nameof(steps));
            }
        }

        public string Name { get; }
        public IReadOnlyList<MeasurementStep> Steps { get; }

        public override string ToString() => $"{Name} ({Steps.Count} steps)";
    }
}
