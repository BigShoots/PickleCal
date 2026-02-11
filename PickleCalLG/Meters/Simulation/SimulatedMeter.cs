using System;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Simulation
{
    public sealed class SimulatedMeter : IMeterDevice
    {
        private MeterMeasurementState _state = MeterMeasurementState.Disconnected;
        private DateTime _lastTimestamp = DateTime.UtcNow;
        private int _sequence;

        public string Id => "simulated.meter";
        public string DisplayName => "Simulated Meter";
        public MeterCapabilities Capabilities => MeterCapabilities.SupportsDisplay | MeterCapabilities.SupportsAmbient;
        public MeterMeasurementState State => _state;

        public ValueTask DisposeAsync()
        {
            _state = MeterMeasurementState.Disconnected;
            return ValueTask.CompletedTask;
        }

        public Task ConnectAsync(MeterConnectOptions options, CancellationToken cancellationToken)
        {
            _state = MeterMeasurementState.Idle;
            _sequence = 0;
            _lastTimestamp = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            _state = MeterMeasurementState.Disconnected;
            return Task.CompletedTask;
        }

        public async Task<MeterCalibrationResult> CalibrateAsync(MeterCalibrationRequest request, CancellationToken cancellationToken)
        {
            _state = MeterMeasurementState.Calibrating;
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            _state = MeterMeasurementState.Idle;
            return MeterCalibrationResult.Ok();
        }

        public async Task<MeterMeasurementResult> MeasureAsync(MeterMeasureRequest request, CancellationToken cancellationToken)
        {
            _state = MeterMeasurementState.Measuring;
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

            double phase = _sequence / 24.0;
            double luminance = 30 + 20 * Math.Sin(phase * Math.PI * 2);
            double x = 0.3127 + 0.01 * Math.Cos(phase * Math.PI * 2);
            double y = 0.3290 + 0.01 * Math.Sin(phase * Math.PI * 2);
            double X = luminance * x / y;
            double Y = luminance;
            double Z = luminance * (1 - x - y) / y;

            var reading = new MeterReading(
                timestamp: DateTime.UtcNow,
                x: X,
                y: Y,
                z: Z,
                luminance: luminance,
                correlatedColorTemperatureK: 6500 + 200 * Math.Sin(phase * Math.PI * 2),
                deltaE2000: Math.Abs(Math.Sin(phase * Math.PI * 2)) * 0.5,
                targetIre: request.TargetIre);

            _sequence++;
            _lastTimestamp = reading.Timestamp;
            _state = MeterMeasurementState.Idle;
            return MeterMeasurementResult.Ok(reading);
        }
    }
}
