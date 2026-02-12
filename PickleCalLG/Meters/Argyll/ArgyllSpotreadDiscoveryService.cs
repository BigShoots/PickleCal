using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Argyll
{
    /// <summary>
    /// Discovers ArgyllCMS-based meters (spotread).
    /// 
    /// Detection flow:
    /// 1. Scan USB bus for connected colorimeters (VID/PID matching)
    /// 2. Look for spotread.exe in standard locations
    /// 3. If device found but spotread missing → auto-download ArgyllCMS
    /// 4. Verify USB driver compatibility (needs WinUSB/libusb, not HID)
    /// </summary>
    public sealed class ArgyllSpotreadDiscoveryService : IMeterDiscoveryService
    {
        private readonly SpotreadLocator _locator;
        private readonly ArgyllCmsManager _argyllManager;
        private string? _resolvedSpotreadPath;

        /// <summary>
        /// Raised during discovery to report status (e.g. download progress).
        /// </summary>
        public event Action<string>? StatusChanged;

        /// <summary>
        /// List of USB colorimeters that were detected during the last discovery.
        /// Empty if no hardware found. Populated even if spotread is unavailable.
        /// </summary>
        public IReadOnlyList<UsbMeterDetector.DetectedColorimeter> DetectedHardware { get; private set; }
            = Array.Empty<UsbMeterDetector.DetectedColorimeter>();

        /// <summary>
        /// If discovery found a USB device but it has the wrong driver, this contains
        /// a user-friendly message explaining how to fix it.
        /// </summary>
        public string? DriverIssueMessage { get; private set; }

        public ArgyllSpotreadDiscoveryService() : this(new SpotreadLocator())
        {
        }

        public ArgyllSpotreadDiscoveryService(SpotreadLocator locator)
        {
            _locator = locator;
            _argyllManager = new ArgyllCmsManager();
            _argyllManager.ProgressChanged += msg => StatusChanged?.Invoke(msg);
        }

        public async Task<IReadOnlyList<MeterDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DriverIssueMessage = null;

            // Step 1: Detect USB colorimeter hardware
            StatusChanged?.Invoke("Scanning for USB colorimeters...");
            DetectedHardware = UsbMeterDetector.Detect();

            if (DetectedHardware.Count > 0)
            {
                var hw = DetectedHardware[0];
                Debug.WriteLine($"[ArgyllDiscovery] Found USB device: {hw}");
                StatusChanged?.Invoke($"Found: {hw.Name}");

                // Check driver compatibility
                if (!hw.HasArgyllCompatibleDriver)
                {
                    DriverIssueMessage =
                        $"Your {hw.Name} was detected on USB, but it's using the \"{hw.CurrentDriver}\" driver.\n\n" +
                        "ArgyllCMS requires the WinUSB driver to communicate with the meter.\n\n" +
                        "To fix this:\n" +
                        "  1. Download Zadig from https://zadig.akeo.ie/\n" +
                        "  2. Run Zadig, select your colorimeter from the device list\n" +
                        "  3. Select \"WinUSB\" as the target driver\n" +
                        "  4. Click \"Replace Driver\" or \"Install Driver\"\n" +
                        "  5. Restart PickleCal and scan again\n\n" +
                        "Alternatively, click \"Install WinUSB Driver\" if available.";

                    Debug.WriteLine($"[ArgyllDiscovery] Driver issue: {hw.CurrentDriver} (need WinUSB)");
                }
            }
            else
            {
                Debug.WriteLine("[ArgyllDiscovery] No USB colorimeters detected");
            }

            // Step 2: Look for spotread.exe
            StatusChanged?.Invoke("Looking for ArgyllCMS (spotread)...");
            _resolvedSpotreadPath = await _locator.FindAsync(cancellationToken);

            // Step 3: If not found and we have USB hardware, auto-download ArgyllCMS
            if (_resolvedSpotreadPath == null && DetectedHardware.Count > 0)
            {
                StatusChanged?.Invoke("ArgyllCMS not found — downloading automatically...");
                try
                {
                    _resolvedSpotreadPath = await _argyllManager.EnsureAvailableAsync(cancellationToken);
                    StatusChanged?.Invoke("ArgyllCMS downloaded successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ArgyllDiscovery] Auto-download failed: {ex.Message}");
                    StatusChanged?.Invoke($"ArgyllCMS download failed: {ex.Message}");
                }
            }
            else if (_resolvedSpotreadPath == null && DetectedHardware.Count == 0)
            {
                // Also try auto-download even without detected hardware
                // (the user might have driver issues preventing USB detection)
                var cached = ArgyllCmsManager.GetCachedSpotreadPath();
                if (cached != null)
                {
                    _resolvedSpotreadPath = cached;
                }
                else
                {
                    // Try download anyway — user asked for it
                    try
                    {
                        StatusChanged?.Invoke("No colorimeter detected. Downloading ArgyllCMS to be ready...");
                        _resolvedSpotreadPath = await _argyllManager.EnsureAvailableAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ArgyllDiscovery] Auto-download failed: {ex.Message}");
                        StatusChanged?.Invoke($"ArgyllCMS download failed: {ex.Message}");
                    }
                }
            }

            if (_resolvedSpotreadPath == null)
            {
                StatusChanged?.Invoke("No meter software available");
                return Array.Empty<MeterDescriptor>();
            }

            // Step 4: Build descriptors — one per detected USB device, or a generic one
            var descriptors = new List<MeterDescriptor>();

            if (DetectedHardware.Count > 0)
            {
                foreach (var hw in DetectedHardware)
                {
                    descriptors.Add(new MeterDescriptor(
                        id: $"argyll.spotread.{hw.Vid:X4}.{hw.Pid:X4}",
                        displayName: hw.Name,
                        capabilities: MeterCapabilities.SupportsDisplay | MeterCapabilities.SupportsAmbient | MeterCapabilities.SupportsAutoCalibration,
                        providerId: ArgyllSpotreadMeter.ProviderId));
                }
            }
            else
            {
                // No USB device detected but spotread is available — show generic entry
                descriptors.Add(new MeterDescriptor(
                    id: "argyll.spotread",
                    displayName: "ArgyllCMS Spotread (no colorimeter detected — connect device and rescan)",
                    capabilities: MeterCapabilities.SupportsDisplay | MeterCapabilities.SupportsAmbient | MeterCapabilities.SupportsAutoCalibration,
                    providerId: ArgyllSpotreadMeter.ProviderId));
            }

            var status = DetectedHardware.Count > 0
                ? $"Ready: {DetectedHardware[0].Name}"
                : "ArgyllCMS ready (waiting for colorimeter)";
            StatusChanged?.Invoke(status);

            return descriptors;
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
