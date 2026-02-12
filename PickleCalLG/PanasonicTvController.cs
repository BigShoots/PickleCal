using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PickleCalLG
{
    /// <summary>
    /// Panasonic Viera TV controller using the Viera HTTP control API.
    /// Panasonic TVs (2018+) expose calibration endpoints via their local
    /// HTTP interface for white balance and CMS adjustments.
    ///
    /// Prerequisites:
    ///   - Enable "Network Remote Control" in TV settings → Network
    ///   - TV and PC must be on the same local network
    /// </summary>
    public sealed class PanasonicTvController : ITvController
    {
        private const string TAG = "PanasonicTvController";
        private const int API_PORT = 55000;
        private const int TIMEOUT_MS = 10000;

        private readonly HttpClient _http;
        private readonly string _tvIp;

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public TvBrand Brand => TvBrand.Panasonic;
        public string TvIp => _tvIp;
        public bool IsConnected { get; private set; }
        public bool IsPaired => IsConnected;

        public PanasonicTvController(string tvIp)
        {
            _tvIp = tvIp;
            _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            Log("Connecting to Panasonic TV...");
            OnStatusChange?.Invoke($"Connecting to Panasonic TV at {_tvIp}...");

            try
            {
                // Panasonic Viera uses a SOAP-like XML API — query device info
                var response = await _http.GetAsync($"http://{_tvIp}:{API_PORT}/nrc/sdd_0.xml");
                response.EnsureSuccessStatusCode();
                var xml = await response.Content.ReadAsStringAsync();

                // Parse device name from UPnP service description
                string modelName = "Panasonic TV";
                try
                {
                    var doc = XDocument.Parse(xml);
                    XNamespace ns = "urn:schemas-upnp-org:device-1-0";
                    modelName = doc.Root?.Element(ns + "device")?.Element(ns + "modelName")?.Value
                        ?? "Panasonic TV";
                }
                catch { }

                Log($"Connected to {modelName}");

                IsConnected = true;
                OnStatusChange?.Invoke($"Connected to {modelName} — ready for calibration");
            }
            catch (HttpRequestException ex)
            {
                Log($"Connection failed: {ex.Message}");
                OnStatusChange?.Invoke(
                    $"Connection failed — ensure Network Remote Control is enabled. Error: {ex.Message}");
                throw;
            }
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            OnDisconnect?.Invoke();
            return Task.CompletedTask;
        }

        // ── Picture Settings ──

        public async Task SetPictureModeAsync(string mode)
        {
            await SendCalibrationCommandAsync("picture_mode", new JObject { ["mode"] = mode });
            OnStatusChange?.Invoke($"Picture mode set to '{mode}'");
        }

        public async Task SetBacklightAsync(int value)
        {
            await SendCalibrationCommandAsync("backlight", new JObject { ["value"] = value });
        }

        public async Task SetContrastAsync(int value)
        {
            await SendCalibrationCommandAsync("contrast", new JObject { ["value"] = value });
        }

        public async Task SetBrightnessAsync(int value)
        {
            await SendCalibrationCommandAsync("brightness", new JObject { ["value"] = value });
        }

        public async Task SetColorAsync(int value)
        {
            await SendCalibrationCommandAsync("colour", new JObject { ["value"] = value });
        }

        public async Task SetSharpnessAsync(int value)
        {
            await SendCalibrationCommandAsync("sharpness", new JObject { ["value"] = value });
        }

        public async Task SetColorGamutAsync(string gamut)
        {
            await SendCalibrationCommandAsync("colour_gamut", new JObject { ["gamut"] = gamut });
        }

        public async Task SetGammaAsync(string gamma)
        {
            await SendCalibrationCommandAsync("gamma", new JObject { ["value"] = gamma });
        }

        public async Task SetColorTemperatureAsync(string temp)
        {
            await SendCalibrationCommandAsync("colour_temperature", new JObject { ["value"] = temp });
        }

        public async Task DisableProcessingAsync()
        {
            await SendCalibrationCommandAsync("processing", new JObject
            {
                ["intelligentFrameCreation"] = "off",
                ["noiseReduction"] = "off",
                ["mpegRemaster"] = "off",
                ["resolutionRemaster"] = "off",
                ["dynamicRange"] = "off"
            });
            OnStatusChange?.Invoke("Image processing disabled");
        }

        public Task ReadPictureSettingsAsync()
        {
            OnStatusChange?.Invoke("Reading picture settings is not yet supported for Panasonic TVs");
            return Task.CompletedTask;
        }

        // ── White Balance ──

        public async Task SetWhiteBalance2ptAsync(int redGain, int greenGain, int blueGain,
            int redOffset, int greenOffset, int blueOffset)
        {
            var payload = new JObject
            {
                ["method"] = "2point",
                ["rGain"] = redGain,
                ["gGain"] = greenGain,
                ["bGain"] = blueGain,
                ["rOffset"] = redOffset,
                ["gOffset"] = greenOffset,
                ["bOffset"] = blueOffset
            };
            await SendCalibrationCommandAsync("white_balance", payload);
            Log($"2pt WB set: RG={redGain} GG={greenGain} BG={blueGain} RO={redOffset} GO={greenOffset} BO={blueOffset}");
        }

        public async Task SetWhiteBalance20ptPointAsync(int index, int red, int green, int blue)
        {
            var payload = new JObject
            {
                ["method"] = "multipoint",
                ["index"] = index,
                ["ire"] = index * 5,
                ["red"] = red,
                ["green"] = green,
                ["blue"] = blue
            };
            await SendCalibrationCommandAsync("white_balance_point", payload);
            Log($"20pt WB[{index}]: R={red} G={green} B={blue}");
        }

        public async Task ResetWhiteBalanceAsync()
        {
            await SendCalibrationCommandAsync("white_balance_reset", new JObject());
            Log("White balance reset");
        }

        // ── CMS ──

        public async Task SetCmsColorAsync(string color, int hue, int saturation, int luminance)
        {
            var payload = new JObject
            {
                ["colour"] = color,
                ["hue"] = hue,
                ["saturation"] = saturation,
                ["luminance"] = luminance
            };
            await SendCalibrationCommandAsync("cms", payload);
            Log($"CMS {color}: H={hue} S={saturation} L={luminance}");
        }

        public async Task ResetCmsAsync()
        {
            await SendCalibrationCommandAsync("cms_reset", new JObject());
            Log("CMS reset");
        }

        // ── Utility ──

        public Task ShowToastAsync(string message)
        {
            OnStatusChange?.Invoke($"Toast: {message}");
            return Task.CompletedTask;
        }

        // ── HTTP Helpers ──

        /// <summary>
        /// Sends a calibration command to the Panasonic TV's local HTTP API.
        /// The TV exposes /cal/ endpoints when in professional calibration mode.
        /// </summary>
        private async Task SendCalibrationCommandAsync(string command, JObject payload)
        {
            var url = $"http://{_tvIp}:{API_PORT}/cal/{command}";
            var content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        private void Log(string msg)
        {
            Console.WriteLine($"[{TAG}] {msg}");
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
