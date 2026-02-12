using System;
using System.Threading;
using System.Threading.Tasks;
using PickleCalLG.Meters;

namespace PickleCalLG.Meters.Argyll
{
    public sealed class ArgyllSpotreadMeter : IMeterDevice
    {
        public const string ProviderId = "PickleCalLG.Meters.ArgyllSpotread";

        private readonly SpotreadProcessRunner _runner;
        private MeterConnectOptions? _connectOptions;
        private MeterMeasurementState _state = MeterMeasurementState.Disconnected;
        private bool _disposed;

        public ArgyllSpotreadMeter(SpotreadProcessRunner runner)
        {
            _runner = runner;
        }

        public string Id => "argyll.spotread";
        public string DisplayName => "ArgyllCMS Spotread";
        public MeterCapabilities Capabilities => MeterCapabilities.SupportsDisplay | MeterCapabilities.SupportsAmbient | MeterCapabilities.SupportsAutoCalibration;
        public MeterMeasurementState State => _state;

        public async Task ConnectAsync(MeterConnectOptions options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _connectOptions = options;
            _state = MeterMeasurementState.Idle;
            await Task.CompletedTask;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _connectOptions = null;
            _state = MeterMeasurementState.Disconnected;
            await Task.CompletedTask;
        }

        public async Task<MeterMeasurementResult> MeasureAsync(MeterMeasureRequest request, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _state = MeterMeasurementState.Measuring;
            try
            {
                var parameters = BuildParameters(request, skipCalibration: true);
                var measurement = await _runner.RunMeasurementAsync(parameters, cancellationToken);
                return MeterMeasurementResult.Ok(measurement.ToMeterReading(request.TargetIre));
            }
            catch (Exception ex)
            {
                _state = MeterMeasurementState.Faulted;
                return MeterMeasurementResult.Error(ex.Message);
            }
            finally
            {
                if (_state == MeterMeasurementState.Measuring)
                {
                    _state = MeterMeasurementState.Idle;
                }
            }
        }

        public async Task<MeterCalibrationResult> CalibrateAsync(MeterCalibrationRequest request, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _state = MeterMeasurementState.Calibrating;
            try
            {
                var parameters = BuildParameters(new MeterMeasureRequest(request.Mode, TimeSpan.Zero, 100d, false), skipCalibration: false);
                await _runner.RunMeasurementAsync(parameters, cancellationToken);
                return MeterCalibrationResult.Ok();
            }
            catch (Exception ex)
            {
                _state = MeterMeasurementState.Faulted;
                return MeterCalibrationResult.Error(ex.Message);
            }
            finally
            {
                if (_state == MeterMeasurementState.Calibrating)
                {
                    _state = MeterMeasurementState.Idle;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _disposed = true;
            await Task.CompletedTask;
        }

        private SpotreadRunParameters BuildParameters(MeterMeasureRequest request, bool skipCalibration)
        {
            var options = _connectOptions ?? new MeterConnectOptions();
            return new SpotreadRunParameters
            {
                Mode = request.Mode,
                HighResolution = options.UseHighResolution,
                UseAveraging = request.UseAveraging,
                SkipInitialCalibration = skipCalibration,
                DisplayType = options.DisplayType,
                CorrectionFile = options.SpectrumPreset,
                SpectralSampleFile = null
            };
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ArgyllSpotreadMeter));
            }
        }
    }
}
