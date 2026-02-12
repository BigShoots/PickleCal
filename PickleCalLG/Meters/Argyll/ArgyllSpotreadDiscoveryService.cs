using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Argyll
{
    public sealed class ArgyllSpotreadDiscoveryService : IMeterDiscoveryService
    {
        private readonly SpotreadLocator _locator;

        public ArgyllSpotreadDiscoveryService() : this(new SpotreadLocator())
        {
        }

        public ArgyllSpotreadDiscoveryService(SpotreadLocator locator)
        {
            _locator = locator;
        }

        public async Task<IReadOnlyList<MeterDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var executablePath = await _locator.FindAsync(cancellationToken);
            if (executablePath == null)
            {
                return Array.Empty<MeterDescriptor>();
            }

            var descriptor = new MeterDescriptor(
                id: "argyll.spotread",
                displayName: "ArgyllCMS Spotread",
                capabilities: MeterCapabilities.SupportsDisplay | MeterCapabilities.SupportsAmbient | MeterCapabilities.SupportsAutoCalibration,
                providerId: ArgyllSpotreadMeter.ProviderId);
            return new[] { descriptor };
        }

        public IMeterDevice Create(MeterDescriptor descriptor)
        {
            if (descriptor.ProviderId != ArgyllSpotreadMeter.ProviderId)
            {
                throw new ArgumentException("Unknown provider id", nameof(descriptor));
            }

            return new ArgyllSpotreadMeter(new SpotreadProcessRunner(_locator));
        }
    }
}
