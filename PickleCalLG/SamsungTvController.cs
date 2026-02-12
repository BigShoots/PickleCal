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
    /// Samsung TV controller using the SmartThings-compatible REST calibration API.
    /// Samsung TVs (2019+) expose a local HTTP calibration endpoint for white balance
    /// and CMS adjustments when placed into calibration/service mode.
    /// </summary>
    public sealed class SamsungTvController : ITvController
    {
        private const string TAG = "SamsungTvController";
        private const int API_PORT = 8001;
        private const int CAL_PORT = 8080;
        private const int TIMEOUT_MS = 10000;

        private readonly HttpClient _http;
        private readonly string _tvIp;
        private string _deviceId = "";

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public TvBrand Brand => TvBrand.Samsung;
        public string TvIp => _tvIp;
        public bool IsConnected { get; private set; }
        public bool IsPaired => IsConnected;

        public SamsungTvController(string tvIp)
        {
            _tvIp = tvIp;
            _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            Log("Connecting to Samsung TV...");
            OnStatusChange?.Invoke($"Connecting to Samsung TV at {_tvIp}...");

            try
            {
                // Samsung TVs expose device info at port 8001
                var response = await _http.GetAsync($"http://{_tvIp}:{API_PORT}/api/v2/");
                response.EnsureSuccessStatusCode();
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                _deviceId = json["device"]?["id"]?.ToString() ?? "samsung-tv";

                var name = json["device"]?["name"]?.ToString() ?? "Samsung TV";
                Log($"Connected to {name} (ID: {_deviceId})");

                // Attempt to enter calibration mode
                await EnterCalibrationModeAsync();

                IsConnected = true;
                OnStatusChange?.Invoke($"Connected to {name} — ready for calibration");
            }
            catch (HttpRequestException ex)
            {
                Log($"Connection failed: {ex.Message}");
                OnStatusChange?.Invoke($"Connection failed — ensure TV is on and API is enabled. Error: {ex.Message}");
                throw;
            }
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            OnDisconnect?.Invoke();
            return Task.CompletedTask;
        }

        // ── Calibration Mode ──

        private async Task EnterCalibrationModeAsync()
        {
            try
            {
                var payload = new JObject { ["mode"] = "calibration" };
                await PostCalibrationAsync("/calibration/mode", payload);
                Log("Calibration mode enabled");
            }
            catch (Exception ex)
            {
                Log($"Could not enable calibration mode (may already be active): {ex.Message}");
            }
        }

        // ── Picture Settings ──

        public async Task SetPictureModeAsync(string mode)
        {
            await PostCalibrationAsync("/picture/mode", new JObject { ["mode"] = mode });
            OnStatusChange?.Invoke($"Picture mode set to '{mode}'");
        }

        public async Task SetBacklightAsync(int value)
        {
            await PostCalibrationAsync("/picture/backlight", new JObject { ["value"] = value });
        }

        public async Task SetContrastAsync(int value)
        {
            await PostCalibrationAsync("/picture/contrast", new JObject { ["value"] = value });
        }

        public async Task SetBrightnessAsync(int value)
        {
            await PostCalibrationAsync("/picture/brightness", new JObject { ["value"] = value });
        }

        public async Task SetColorAsync(int value)
        {
            await PostCalibrationAsync("/picture/color", new JObject { ["value"] = value });
        }

        public async Task SetSharpnessAsync(int value)
        {
            await PostCalibrationAsync("/picture/sharpness", new JObject { ["value"] = value });
        }

        public async Task SetColorGamutAsync(string gamut)
        {
            await PostCalibrationAsync("/picture/colorgamut", new JObject { ["gamut"] = gamut });
        }

        public async Task SetGammaAsync(string gamma)
        {
            await PostCalibrationAsync("/picture/gamma", new JObject { ["gamma"] = gamma });
        }

        public async Task SetColorTemperatureAsync(string temp)
        {
            await PostCalibrationAsync("/picture/colortemperature", new JObject { ["temperature"] = temp });
        }

        public async Task DisableProcessingAsync()
        {
            var payload = new JObject
            {
                ["dynamicContrast"] = "off",
                ["motionSmoothing"] = "off",
                ["noiseReduction"] = "off",
                ["digitalCleanView"] = "off",
                ["contrastEnhancer"] = "off",
                ["hdrPlus"] = "off"
            };
            await PostCalibrationAsync("/picture/processing", payload);
            OnStatusChange?.Invoke("Image processing disabled");
        }

        public Task ReadPictureSettingsAsync()
        {
            OnStatusChange?.Invoke("Reading picture settings is not yet supported for Samsung TVs");
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
            await PostCalibrationAsync("/calibration/whitebalance", payload);
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
            await PostCalibrationAsync("/calibration/whitebalance/point", payload);
            Log($"20pt WB[{index}]: R={red} G={green} B={blue}");
        }

        public async Task ResetWhiteBalanceAsync()
        {
            await PostCalibrationAsync("/calibration/whitebalance/reset", new JObject());
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
            await PostCalibrationAsync("/calibration/cms", payload);
            Log($"CMS {color}: H={hue} S={saturation} L={luminance}");
        }

        public async Task ResetCmsAsync()
        {
            await PostCalibrationAsync("/calibration/cms/reset", new JObject());
            Log("CMS reset");
        }

        // ── Utility ──

        public Task ShowToastAsync(string message)
        {
            OnStatusChange?.Invoke($"Toast: {message}");
            return Task.CompletedTask;
        }

        // ── HTTP Helpers ──

        private async Task PostCalibrationAsync(string endpoint, JObject payload)
        {
            var url = $"http://{_tvIp}:{CAL_PORT}{endpoint}";
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
