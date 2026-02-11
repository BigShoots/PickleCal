using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters
{
    public interface IMeterDiscoveryService
    {
        Task<IReadOnlyList<MeterDescriptor>> DiscoverAsync(CancellationToken cancellationToken);
        IMeterDevice Create(MeterDescriptor descriptor);
    }
}
