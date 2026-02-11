using System;

namespace PickleCalLG.Meters
{
    public sealed class MeterDescriptor
    {
        public MeterDescriptor(string id, string displayName, MeterCapabilities capabilities, string providerId)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Capabilities = capabilities;
            ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        }

        public string Id { get; }
        public string DisplayName { get; }
        public MeterCapabilities Capabilities { get; }
        public string ProviderId { get; }
    }
}
