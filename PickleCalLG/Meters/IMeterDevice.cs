using System;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters
{
    public interface IMeterDevice : IAsyncDisposable
    {
        string Id { get; }
        string DisplayName { get; }
        MeterCapabilities Capabilities { get; }
        MeterMeasurementState State { get; }

        Task ConnectAsync(MeterConnectOptions options, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        Task<MeterMeasurementResult> MeasureAsync(MeterMeasureRequest request, CancellationToken cancellationToken);
        Task<MeterCalibrationResult> CalibrateAsync(MeterCalibrationRequest request, CancellationToken cancellationToken);
    }
}
