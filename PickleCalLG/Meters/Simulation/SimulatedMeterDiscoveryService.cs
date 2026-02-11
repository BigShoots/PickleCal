using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Simulation
{
    public sealed class SimulatedMeterDiscoveryService : IMeterDiscoveryService
    {
        public const string ProviderId = "PickleCalLG.Meters.Simulated";
        private static readonly MeterDescriptor Descriptor = new(
            id: "simulated.meter",
            displayName: "Simulated Meter",
            capabilities: MeterCapabilities.SupportsDisplay | MeterCapabilities.SupportsAmbient,
            providerId: ProviderId);

        public Task<IReadOnlyList<MeterDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<MeterDescriptor>>(new[] { Descriptor });
        }

        public IMeterDevice Create(MeterDescriptor descriptor)
        {
            if (!string.Equals(descriptor.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Unknown provider id", nameof(descriptor));
            }

            return new SimulatedMeter();
        }
    }
}
