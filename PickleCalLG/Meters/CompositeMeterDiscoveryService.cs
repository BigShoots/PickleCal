using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters
{
    public sealed class CompositeMeterDiscoveryService : IMeterDiscoveryService
    {
        private readonly IReadOnlyList<IMeterDiscoveryService> _services;
        private readonly Dictionary<string, IMeterDiscoveryService> _descriptorMap = new(StringComparer.OrdinalIgnoreCase);

        public CompositeMeterDiscoveryService(params IMeterDiscoveryService[] services)
        {
            if (services == null || services.Length == 0)
            {
                throw new ArgumentException("At least one discovery service must be provided", nameof(services));
            }

            _services = services;
        }

        public async Task<IReadOnlyList<MeterDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
        {
            var all = new List<MeterDescriptor>();
            _descriptorMap.Clear();

            foreach (var service in _services)
            {
                var discovered = await service.DiscoverAsync(cancellationToken);
                foreach (var descriptor in discovered)
                {
                    all.Add(descriptor);
                    _descriptorMap[descriptor.Id] = service;
                }
            }

            // Deduplicate by id to avoid duplicates across services.
            return all.GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IMeterDevice Create(MeterDescriptor descriptor)
        {
            if (_descriptorMap.TryGetValue(descriptor.Id, out var service))
            {
                return service.Create(descriptor);
            }

            throw new ArgumentException($"Unknown meter descriptor id '{descriptor.Id}'", nameof(descriptor));
        }
    }
}
