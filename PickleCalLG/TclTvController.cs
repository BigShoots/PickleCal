using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG
{
    /// <summary>
    /// TCL TV controller using the TCL calibration REST API.
    /// TCL TVs with the Aurora engine (2020+) expose local HTTP calibration
    /// endpoints including 3D LUT and per-channel white balance / CMS controls.
    ///
    /// Prerequisites:
    ///   - TV must be on the same network as the PC
    ///   - Some models require entering a calibration/service mode first
    /// </summary>
    public sealed class TclTvController : ITvController
    {
        private const string TAG = "TclTvController";
        private const int API_PORT = 8080;
        private const int TIMEOUT_MS = 10000;

        private readonly HttpClient _http;
        private readonly string _tvIp;

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public TvBrand Brand => TvBrand.TCL;
        public string TvIp => _tvIp;
        public bool IsConnected { get; private set; }
        public bool IsPaired => IsConnected;

        public TclTvController(string tvIp)
        {
            _tvIp = tvIp;
            _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            Log("Connecting to TCL TV...");
            OnStatusChange?.Invoke($"Connecting to TCL TV at {_tvIp}...");

            try
            {
                // Query device info
                var response = await _http.GetAsync($"http://{_tvIp}:{API_PORT}/api/device/info");
                response.EnsureSuccessStatusCode();
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                var model = json["model"]?.ToString() ?? "TCL TV";

                // Enter calibration mode
                await EnterCalibrationModeAsync();

                Log($"Connected to {model}");
                IsConnected = true;
                OnStatusChange?.Invoke($"Connected to TCL {model} — ready for calibration");
            }
            catch (HttpRequestException ex)
            {
                Log($"Connection failed: {ex.Message}");
                OnStatusChange?.Invoke(
                    $"Connection failed — ensure TV is on and networked. Error: {ex.Message}");
                throw;
            }
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            OnDisconnect?.Invoke();
            return Task.CompletedTask;
        }

        private async Task EnterCalibrationModeAsync()
        {
            try
            {
                await PostApiAsync("/api/calibration/enter", new JObject { ["mode"] = "professional" });
                Log("Calibration mode enabled");
            }
            catch (Exception ex)
            {
                Log($"Could not enter calibration mode (may already be active): {ex.Message}");
            }
        }

        // ── Picture Settings ──

        public async Task SetPictureModeAsync(string mode)
        {
            await PostApiAsync("/api/picture/mode", new JObject { ["mode"] = mode });
            OnStatusChange?.Invoke($"Picture mode set to '{mode}'");
        }

        public async Task SetBacklightAsync(int value)
        {
            await PostApiAsync("/api/picture/backlight", new JObject { ["value"] = value });
        }

        public async Task SetContrastAsync(int value)
        {
            await PostApiAsync("/api/picture/contrast", new JObject { ["value"] = value });
        }

        public async Task SetBrightnessAsync(int value)
        {
            await PostApiAsync("/api/picture/brightness", new JObject { ["value"] = value });
        }

        public async Task SetColorAsync(int value)
        {
            await PostApiAsync("/api/picture/color", new JObject { ["value"] = value });
        }

        public async Task SetSharpnessAsync(int value)
        {
            await PostApiAsync("/api/picture/sharpness", new JObject { ["value"] = value });
        }

        public async Task SetColorGamutAsync(string gamut)
        {
            await PostApiAsync("/api/picture/colorgamut", new JObject { ["gamut"] = gamut });
        }

        public async Task SetGammaAsync(string gamma)
        {
            await PostApiAsync("/api/picture/gamma", new JObject { ["gamma"] = gamma });
        }

        public async Task SetColorTemperatureAsync(string temp)
        {
            await PostApiAsync("/api/picture/colortemperature", new JObject { ["temperature"] = temp });
        }

        public async Task DisableProcessingAsync()
        {
            var payload = new JObject
            {
                ["noiseReduction"] = "off",
                ["motionSmoothing"] = "off",
                ["dynamicContrast"] = "off",
                ["microDimming"] = "off"
            };
            await PostApiAsync("/api/picture/processing", payload);
            OnStatusChange?.Invoke("Image processing disabled");
        }

        public Task ReadPictureSettingsAsync()
        {
            OnStatusChange?.Invoke("Reading picture settings is not yet supported for TCL TVs");
            return Task.CompletedTask;
        }

        // ── White Balance ──

        public async Task SetWhiteBalance2ptAsync(int redGain, int greenGain, int blueGain,
            int redOffset, int greenOffset, int blueOffset)
        {
            var payload = new JObject
            {
                ["mode"] = "2point",
                ["redGain"] = redGain,
                ["greenGain"] = greenGain,
                ["blueGain"] = blueGain,
                ["redOffset"] = redOffset,
                ["greenOffset"] = greenOffset,
                ["blueOffset"] = blueOffset
            };
            await PostApiAsync("/api/calibration/whitebalance", payload);
            Log($"2pt WB set: RG={redGain} GG={greenGain} BG={blueGain} RO={redOffset} GO={greenOffset} BO={blueOffset}");
        }

        public async Task SetWhiteBalance20ptPointAsync(int index, int red, int green, int blue)
        {
            var payload = new JObject
            {
                ["mode"] = "20point",
                ["index"] = index,
                ["ire"] = index * 5,
                ["red"] = red,
                ["green"] = green,
                ["blue"] = blue
            };
            await PostApiAsync("/api/calibration/whitebalance/point", payload);
            Log($"20pt WB[{index}]: R={red} G={green} B={blue}");
        }

        public async Task ResetWhiteBalanceAsync()
        {
            await PostApiAsync("/api/calibration/whitebalance/reset", new JObject());
            Log("White balance reset");
        }

        // ── CMS ──

        public async Task SetCmsColorAsync(string color, int hue, int saturation, int luminance)
        {
            var payload = new JObject
            {
                ["color"] = color,
                ["hue"] = hue,
                ["saturation"] = saturation,
                ["luminance"] = luminance
            };
            await PostApiAsync("/api/calibration/cms", payload);
            Log($"CMS {color}: H={hue} S={saturation} L={luminance}");
        }

        public async Task ResetCmsAsync()
        {
            await PostApiAsync("/api/calibration/cms/reset", new JObject());
            Log("CMS reset");
        }

        // ── Utility ──

        public Task ShowToastAsync(string message)
        {
            OnStatusChange?.Invoke($"Toast: {message}");
            return Task.CompletedTask;
        }

        // ── HTTP Helpers ──

        private async Task PostApiAsync(string endpoint, JObject payload)
        {
            var url = $"http://{_tvIp}:{API_PORT}{endpoint}";
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
