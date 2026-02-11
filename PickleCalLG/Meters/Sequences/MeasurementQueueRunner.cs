using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Sequences
{
    public sealed class MeasurementQueueRunner
    {
        private readonly MeterManager _meterManager;
        private readonly Func<MeasurementStep, CancellationToken, Task>? _patternHandler;

        public MeasurementQueueRunner(MeterManager meterManager, Func<MeasurementStep, CancellationToken, Task>? patternHandler = null)
        {
            _meterManager = meterManager ?? throw new ArgumentNullException(nameof(meterManager));
            _patternHandler = patternHandler;
        }

        public event Action<MeasurementSequence>? SequenceStarted;
        public event Action<MeasurementStep>? StepStarted;
        public event Action<MeasurementStep, MeterMeasurementResult?>? StepCompleted;
        public event Action<IReadOnlyList<MeasurementStepResult>>? SequenceCompleted;
        public event Action<MeasurementStep, Exception>? StepFailed;

        public async Task<IReadOnlyList<MeasurementStepResult>> RunAsync(MeasurementSequence sequence, CancellationToken cancellationToken)
        {
            SequenceStarted?.Invoke(sequence);
            var results = new List<MeasurementStepResult>();

            foreach (var step in sequence.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                StepStarted?.Invoke(step);

                try
                {
                    if (_patternHandler != null)
                    {
                        await _patternHandler(step, cancellationToken).ConfigureAwait(false);
                    }

                    var request = new MeterMeasureRequest(
                        step.Mode,
                        step.IntegrationTime,
                        step.TargetIre,
                        step.UseAveraging);

                    var result = await _meterManager.MeasureAsync(request, cancellationToken).ConfigureAwait(false);
                    var wrapped = new MeasurementStepResult(step, result);
                    results.Add(wrapped);
                    StepCompleted?.Invoke(step, result);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    StepFailed?.Invoke(step, ex);
                    var failureResult = MeterMeasurementResult.Error(ex.Message);
                    results.Add(new MeasurementStepResult(step, failureResult));
                    StepCompleted?.Invoke(step, failureResult);
                }
            }

            SequenceCompleted?.Invoke(results);
            return results;
        }
    }
}
