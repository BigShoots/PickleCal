using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters
{
    public sealed class MeterManager : IAsyncDisposable
    {
        private readonly IMeterDiscoveryService _discoveryService;
        private readonly List<MeterDescriptor> _knownMeters = new();
        private IMeterDevice? _activeMeter;
        private MeterDescriptor? _activeDescriptor;

        public MeterManager(IMeterDiscoveryService discoveryService)
        {
            _discoveryService = discoveryService;
        }

        public event Action<MeterStateChangedEventArgs>? MeterStateChanged;
        public event Action<MeterMeasurementResult>? MeasurementAvailable;

        public IReadOnlyList<MeterDescriptor> KnownMeters => new ReadOnlyCollection<MeterDescriptor>(_knownMeters);
        public IMeterDevice? ActiveMeter => _activeMeter;

        public async Task RefreshMetersAsync(CancellationToken cancellationToken)
        {
            var meters = await _discoveryService.DiscoverAsync(cancellationToken);
            _knownMeters.Clear();
            _knownMeters.AddRange(meters);
            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(null, MeterMeasurementState.Disconnected));
        }

        public async Task<bool> SelectMeterAsync(string meterId, CancellationToken cancellationToken)
        {
            if (_activeMeter != null)
            {
                await _activeMeter.DisconnectAsync(cancellationToken);
                await _activeMeter.DisposeAsync();
                _activeMeter = null;
                _activeDescriptor = null;
            }

            var descriptor = _knownMeters.FirstOrDefault(m => string.Equals(m.Id, meterId, StringComparison.OrdinalIgnoreCase));
            if (descriptor == null)
            {
                return false;
            }

            _activeMeter = _discoveryService.Create(descriptor);
            _activeDescriptor = descriptor;
            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(descriptor, MeterMeasurementState.Disconnected));
            return true;
        }

        public async Task<bool> ConnectActiveMeterAsync(MeterConnectOptions options, CancellationToken cancellationToken)
        {
            if (_activeMeter == null)
            {
                return false;
            }

            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(CurrentDescriptor, MeterMeasurementState.Calibrating));
            await _activeMeter.ConnectAsync(options, cancellationToken);
            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(CurrentDescriptor, MeterMeasurementState.Idle));
            return true;
        }

        public async Task<MeterMeasurementResult?> MeasureAsync(MeterMeasureRequest request, CancellationToken cancellationToken)
        {
            if (_activeMeter == null)
            {
                return null;
            }

            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(CurrentDescriptor, MeterMeasurementState.Measuring));
            var result = await _activeMeter.MeasureAsync(request, cancellationToken);
            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(CurrentDescriptor, MeterMeasurementState.Idle));
            MeasurementAvailable?.Invoke(result);
            return result;
        }

        public async Task<MeterCalibrationResult?> CalibrateAsync(MeterCalibrationRequest request, CancellationToken cancellationToken)
        {
            if (_activeMeter == null)
            {
                return null;
            }

            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(CurrentDescriptor, MeterMeasurementState.Calibrating));
            var result = await _activeMeter.CalibrateAsync(request, cancellationToken);
            var state = result.Success ? MeterMeasurementState.Idle : MeterMeasurementState.Faulted;
            MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(CurrentDescriptor, state));
            return result;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (_activeMeter != null)
            {
                try
                {
                    await _activeMeter.DisconnectAsync(cancellationToken);
                }
                finally
                {
                    await _activeMeter.DisposeAsync();
                    _activeMeter = null;
                    _activeDescriptor = null;
                    MeterStateChanged?.Invoke(new MeterStateChangedEventArgs(null, MeterMeasurementState.Disconnected));
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_activeMeter != null)
            {
                await _activeMeter.DisposeAsync();
                _activeMeter = null;
                _activeDescriptor = null;
            }
        }

        private MeterDescriptor? CurrentDescriptor => _activeDescriptor;
    }

    public sealed class MeterStateChangedEventArgs : EventArgs
    {
        public MeterStateChangedEventArgs(MeterDescriptor? descriptor, MeterMeasurementState state)
        {
            Descriptor = descriptor;
            State = state;
        }

        public MeterDescriptor? Descriptor { get; }
        public MeterMeasurementState State { get; }
    }
}
