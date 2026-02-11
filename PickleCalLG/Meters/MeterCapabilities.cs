using System;

namespace PickleCalLG.Meters
{
    [Flags]
    public enum MeterCapabilities
    {
        None = 0,
        SupportsAutoCalibration = 1 << 0,
        ReportsSpectralData = 1 << 1,
        SupportsAmbient = 1 << 2,
        SupportsContact = 1 << 3,
        SupportsDisplay = 1 << 4,
        SupportsSync = 1 << 5,
        SupportsLuminanceOnly = 1 << 6
    }
}
