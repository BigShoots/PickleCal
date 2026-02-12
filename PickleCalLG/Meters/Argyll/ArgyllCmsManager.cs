using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG.Meters.Argyll
{
    /// <summary>
    /// Manages the lifecycle of ArgyllCMS binaries.
    /// If spotread.exe is not found on the system, this class can download
    /// and extract ArgyllCMS to a local cache directory under %LOCALAPPDATA%\PickleCal.
    /// </summary>
    public sealed class ArgyllCmsManager
    {
        /// <summary>
        /// ArgyllCMS download URL — Windows 64-bit executables.
        /// This is the standard distribution from the ArgyllCMS website.
        /// </summary>
        private const string ARGYLL_DOWNLOAD_URL =
            "https://www.argyllcms.com/Argyll_V3.2.0_win64_exe.zip";

        /// <summary>Fallback mirror in case primary URL fails.</summary>
        private const string ARGYLL_DOWNLOAD_URL_FALLBACK =
            "https://www.argyllcms.com/Argyll_V3.1.0_win64_exe.zip";

        private static readonly string CacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PickleCal", "ArgyllCMS");

        private static readonly string SpotreadPath = Path.Combine(CacheRoot, "bin", "spotread.exe");

        /// <summary>
        /// Event raised to report download/extraction progress.
        /// </summary>
        public event Action<string>? ProgressChanged;

        /// <summary>
        /// Returns the path to the cached spotread.exe, or null if it hasn't been downloaded yet.
        /// </summary>
        public static string? GetCachedSpotreadPath()
        {
            return File.Exists(SpotreadPath) ? SpotreadPath : null;
        }

        /// <summary>
        /// Returns the cache directory where ArgyllCMS binaries are stored.
        /// </summary>
        public static string GetCacheDirectory() => Path.Combine(CacheRoot, "bin");

        /// <summary>
        /// Download and extract ArgyllCMS if not already cached.
        /// Returns the path to spotread.exe.
        /// </summary>
        public async Task<string> EnsureAvailableAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(SpotreadPath))
            {
                ReportProgress("ArgyllCMS already available");
                return SpotreadPath;
            }

            ReportProgress("ArgyllCMS not found — downloading...");

            // Create cache directory
            Directory.CreateDirectory(CacheRoot);
            var zipPath = Path.Combine(CacheRoot, "argyllcms.zip");

            // Download
            await DownloadAsync(zipPath, cancellationToken);

            // Extract
            ReportProgress("Extracting ArgyllCMS...");
            ExtractArgyll(zipPath, CacheRoot);

            // Clean up zip
            try { File.Delete(zipPath); } catch { }

            // Verify spotread exists
            if (!File.Exists(SpotreadPath))
            {
                // The zip might have a top-level directory like "Argyll_V3.2.0"
                // Try to find spotread.exe recursively and move it into our expected layout
                var found = Directory.GetFiles(CacheRoot, "spotread.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (found != null)
                {
                    var foundBinDir = Path.GetDirectoryName(found)!;
                    var targetBinDir = Path.Combine(CacheRoot, "bin");

                    if (!foundBinDir.Equals(targetBinDir, StringComparison.OrdinalIgnoreCase))
                    {
                        // Move all exe/dll files from the found bin dir to our expected bin dir
                        Directory.CreateDirectory(targetBinDir);
                        foreach (var file in Directory.GetFiles(foundBinDir))
                        {
                            var dest = Path.Combine(targetBinDir, Path.GetFileName(file));
                            if (!File.Exists(dest))
                                File.Move(file, dest);
                        }
                    }
                }
            }

            if (!File.Exists(SpotreadPath))
            {
                throw new FileNotFoundException(
                    "Failed to locate spotread.exe after extraction. " +
                    "Please install ArgyllCMS manually from https://www.argyllcms.com/");
            }

            ReportProgress("ArgyllCMS ready");
            return SpotreadPath;
        }

        private async Task DownloadAsync(string destPath, CancellationToken cancellationToken)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

            Exception? firstError = null;

            foreach (var url in new[] { ARGYLL_DOWNLOAD_URL, ARGYLL_DOWNLOAD_URL_FALLBACK })
            {
                try
                {
                    ReportProgress($"Downloading from {url}...");

                    using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1;
                    using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    int lastPercent = -1;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            int percent = (int)(totalRead * 100 / totalBytes);
                            if (percent != lastPercent && percent % 10 == 0)
                            {
                                lastPercent = percent;
                                ReportProgress($"Downloading ArgyllCMS... {percent}%");
                            }
                        }
                    }

                    ReportProgress("Download complete");
                    return; // Success
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    firstError ??= ex;
                    Debug.WriteLine($"[ArgyllCmsManager] Download failed from {url}: {ex.Message}");
                }
            }

            throw new InvalidOperationException(
                $"Failed to download ArgyllCMS: {firstError?.Message ?? "unknown error"}. " +
                "Please install ArgyllCMS manually from https://www.argyllcms.com/",
                firstError);
        }

        private void ExtractArgyll(string zipPath, string destDir)
        {
            try
            {
                ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to extract ArgyllCMS: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Attempt to install the WinUSB driver for a connected colorimeter.
        /// On Windows 10+, WinUSB is built into the OS — we just need to bind it
        /// to the specific USB device using pnputil or devcon.
        /// </summary>
        /// <remarks>
        /// This requires admin elevation. Returns true if successful.
        /// </remarks>
        public static async Task<bool> TryInstallWinUsbDriverAsync(string deviceInstanceId)
        {
            try
            {
                // Use pnputil to add a generic WinUSB driver for the device
                // This is the modern Windows way — no external tools needed
                var startInfo = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"/add-driver \"{GetWinUsbInfPath()}\" /install",
                    UseShellExecute = true,
                    Verb = "runas", // Request elevation
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var proc = Process.Start(startInfo);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ArgyllCmsManager] Driver install failed: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Creates a minimal WinUSB INF file for the given USB VID:PID in the cache directory.
        /// </summary>
        public static string CreateWinUsbInf(ushort vid, ushort pid, string deviceName)
        {
            var infDir = Path.Combine(CacheRoot, "driver");
            Directory.CreateDirectory(infDir);
            var infPath = Path.Combine(infDir, $"colorimeter_{vid:X4}_{pid:X4}.inf");

            if (File.Exists(infPath)) return infPath;

            var inf = $@"; PickleCal WinUSB driver for {deviceName}
; VID={vid:X4} PID={pid:X4}
[Version]
Signature   = ""$Windows NT$""
Class       = USBDevice
ClassGUID   = {{88BAE032-5A81-49f0-BC3D-A4FF138216D6}}
Provider    = %ProviderName%
DriverVer   = 01/01/2025,1.0.0.0
CatalogFile = colorimeter.cat

[Manufacturer]
%MfgName% = DeviceList, NTamd64

[DeviceList.NTamd64]
%DeviceName% = USB_Install, USB\VID_{vid:X4}&PID_{pid:X4}

[USB_Install]
Include = winusb.inf
Needs   = WINUSB.NT

[USB_Install.Services]
Include    = winusb.inf
AddService = WinUSB,0x00000002,WinUSB_ServiceInstall

[WinUSB_ServiceInstall]
DisplayName   = %ServiceName%
ServiceType   = 1
StartType     = 3
ErrorControl  = 1
ServiceBinary = %12%\WinUSB.sys

[USB_Install.HW]
AddReg = Dev_AddReg

[Dev_AddReg]
HKR,,DeviceInterfaceGUIDs,0x10000,""{{12345678-1234-1234-1234-123456789ABC}}""

[Strings]
ProviderName = ""PickleCal""
MfgName      = ""Colorimeter""
DeviceName   = ""{deviceName}""
ServiceName  = ""WinUSB""
";
            File.WriteAllText(infPath, inf);
            return infPath;
        }

        private static string GetWinUsbInfPath()
        {
            var infDir = Path.Combine(CacheRoot, "driver");
            var infFiles = Directory.Exists(infDir) ? Directory.GetFiles(infDir, "*.inf") : Array.Empty<string>();
            return infFiles.Length > 0 ? infFiles[0] : "";
        }

        private void ReportProgress(string status)
        {
            ProgressChanged?.Invoke(status);
            Debug.WriteLine($"[ArgyllCmsManager] {status}");
        }
    }
}
