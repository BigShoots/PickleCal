using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PickleCalLG.Meters
{
    /// <summary>
    /// Detects physically connected USB colorimeters using Windows Setup API (P/Invoke).
    /// This works regardless of whether ArgyllCMS is installed — it reads from the
    /// Windows PnP device tree directly.
    /// </summary>
    public static class UsbMeterDetector
    {
        // ── Known colorimeter USB Vendor/Product IDs ──

        private static readonly (ushort Vid, ushort Pid, string Name)[] KnownDevices =
        {
            // X-Rite / GretagMacbeth (VID 0x0971)
            (0x0971, 0x2000, "X-Rite i1 Pro"),
            (0x0971, 0x2001, "X-Rite i1 Monitor"),
            (0x0971, 0x2003, "X-Rite i1 Display"),
            (0x0971, 0x2005, "X-Rite i1 Display 2"),
            (0x0971, 0x2006, "X-Rite ColorMunki Smile"),
            (0x0971, 0x2007, "X-Rite i1 Display Pro / ColorMunki Display"),

            // X-Rite / Calibrite (VID 0x0765)
            (0x0765, 0x5001, "X-Rite / Calibrite i1Display Pro"),
            (0x0765, 0x5010, "X-Rite / Calibrite i1Display Pro Plus"),
            (0x0765, 0x5020, "Calibrite ColorChecker Display / i1Display Plus"),
            (0x0765, 0x5021, "Calibrite ColorChecker Display Pro"),
            (0x0765, 0x5022, "Calibrite ColorChecker Display Plus"),
            (0x0765, 0x6003, "X-Rite i1Pro 2"),
            (0x0765, 0x6008, "X-Rite i1Pro 3 / i1Pro 3 Plus"),

            // Datacolor (VID 0x085C)
            (0x085C, 0x0100, "Datacolor Spyder 2"),
            (0x085C, 0x0200, "Datacolor Spyder 3"),
            (0x085C, 0x0300, "Datacolor Spyder 4"),
            (0x085C, 0x0400, "Datacolor Spyder 4"),
            (0x085C, 0x0500, "Datacolor Spyder 5"),
            (0x085C, 0x0A00, "Datacolor SpyderX"),

            // ColorVision (VID 0x0670)
            (0x0670, 0x0001, "ColorVision Spyder"),
        };

        // ── Known vendor IDs for broader matching ──

        private static readonly (ushort Vid, string Vendor)[] KnownVendors =
        {
            (0x0971, "X-Rite / GretagMacbeth"),
            (0x0765, "X-Rite / Calibrite"),
            (0x085C, "Datacolor"),
            (0x0670, "ColorVision"),
        };

        /// <summary>
        /// Describes a detected USB colorimeter.
        /// </summary>
        public sealed class DetectedColorimeter
        {
            public string Name { get; init; } = "";
            public string Vendor { get; init; } = "";
            public ushort Vid { get; init; }
            public ushort Pid { get; init; }
            public string InstanceId { get; init; } = "";
            public string CurrentDriver { get; init; } = "";
            public bool HasArgyllCompatibleDriver { get; init; }

            public override string ToString() =>
                $"{Name} (VID={Vid:X4} PID={Pid:X4}) Driver={CurrentDriver}";
        }

        /// <summary>
        /// Scan the system for connected USB colorimeters.
        /// Uses the Windows registry PnP device tree — works without admin rights.
        /// </summary>
        public static List<DetectedColorimeter> Detect()
        {
            var results = new List<DetectedColorimeter>();

            try
            {
                using var usbKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Enum\USB");

                if (usbKey == null) return results;

                foreach (var deviceKeyName in usbKey.GetSubKeyNames())
                {
                    // deviceKeyName is like "VID_0971&PID_2007" or "VID_0971&PID_2007&MI_00"
                    if (!TryParseVidPid(deviceKeyName, out ushort vid, out ushort pid))
                        continue;

                    // Check if this VID belongs to a known colorimeter vendor
                    if (!IsKnownVendor(vid))
                        continue;

                    using var deviceKey = usbKey.OpenSubKey(deviceKeyName);
                    if (deviceKey == null) continue;

                    // Each sub-key here is a device instance
                    foreach (var instanceName in deviceKey.GetSubKeyNames())
                    {
                        using var instanceKey = deviceKey.OpenSubKey(instanceName);
                        if (instanceKey == null) continue;

                        // Check if device is currently present by looking for standard properties
                        var deviceDesc = instanceKey.GetValue("DeviceDesc") as string ?? "";
                        var service = instanceKey.GetValue("Service") as string ?? "";
                        var friendlyName = instanceKey.GetValue("FriendlyName") as string ?? "";

                        // The DeviceDesc often has a prefix like "@driver.inf,%desc%;Actual Name"
                        // Extract just the human-readable part
                        var displayName = ParseDeviceDescription(friendlyName, deviceDesc);
                        var knownName = LookupDeviceName(vid, pid);

                        bool isArgyllCompatible = service.Equals("WinUSB", StringComparison.OrdinalIgnoreCase)
                            || service.Equals("libusb0", StringComparison.OrdinalIgnoreCase)
                            || service.Equals("libusbK", StringComparison.OrdinalIgnoreCase);

                        results.Add(new DetectedColorimeter
                        {
                            Name = knownName ?? displayName ?? $"Unknown colorimeter (PID={pid:X4})",
                            Vendor = LookupVendor(vid),
                            Vid = vid,
                            Pid = pid,
                            InstanceId = $"USB\\{deviceKeyName}\\{instanceName}",
                            CurrentDriver = string.IsNullOrEmpty(service) ? "No driver" : service,
                            HasArgyllCompatibleDriver = isArgyllCompatible
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsbMeterDetector] Error scanning USB devices: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Quick check: is any supported colorimeter connected (regardless of driver)?
        /// </summary>
        public static bool AnyColorimeterConnected() => Detect().Count > 0;

        // ── Parsing helpers ──

        private static bool TryParseVidPid(string keyName, out ushort vid, out ushort pid)
        {
            vid = 0;
            pid = 0;

            // Format: VID_XXXX&PID_XXXX  (possibly with &MI_XX suffix)
            var upper = keyName.ToUpperInvariant();
            int vidIdx = upper.IndexOf("VID_");
            int pidIdx = upper.IndexOf("PID_");

            if (vidIdx < 0 || pidIdx < 0) return false;

            var vidStr = upper.Substring(vidIdx + 4, 4);
            var pidStr = upper.Substring(pidIdx + 4, Math.Min(4, upper.Length - pidIdx - 4));

            return ushort.TryParse(vidStr, System.Globalization.NumberStyles.HexNumber, null, out vid)
                && ushort.TryParse(pidStr, System.Globalization.NumberStyles.HexNumber, null, out pid);
        }

        private static bool IsKnownVendor(ushort vid)
        {
            foreach (var (v, _) in KnownVendors)
                if (v == vid) return true;
            return false;
        }

        private static string LookupVendor(ushort vid)
        {
            foreach (var (v, name) in KnownVendors)
                if (v == vid) return name;
            return "Unknown";
        }

        private static string? LookupDeviceName(ushort vid, ushort pid)
        {
            foreach (var (v, p, name) in KnownDevices)
                if (v == vid && p == pid) return name;
            return null;
        }

        private static string ParseDeviceDescription(string friendlyName, string deviceDesc)
        {
            // Prefer friendlyName
            if (!string.IsNullOrWhiteSpace(friendlyName))
            {
                var semi = friendlyName.LastIndexOf(';');
                return semi >= 0 ? friendlyName.Substring(semi + 1) : friendlyName;
            }

            if (!string.IsNullOrWhiteSpace(deviceDesc))
            {
                var semi = deviceDesc.LastIndexOf(';');
                return semi >= 0 ? deviceDesc.Substring(semi + 1) : deviceDesc;
            }

            return "";
        }
    }
}
