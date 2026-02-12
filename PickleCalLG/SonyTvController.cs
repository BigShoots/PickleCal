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
    /// Sony Bravia TV controller using the Bravia Professional Display REST API.
    /// Sony TVs (2018+ with Android TV / Google TV) expose JSON-RPC calibration
    /// endpoints for white balance, CMS, and picture settings.
    ///
    /// Prerequisites:
    ///   - Enable "Remote device / IP control" in TV settings → Network → Home Network
    ///   - Set Authentication to "Normal" or "Pre-Shared Key" mode
    ///   - If using PSK, set the key in TV settings and pass it to the constructor
    /// </summary>
    public sealed class SonyTvController : ITvController
    {
        private const string TAG = "SonyTvController";
        private const int API_PORT = 80;
        private const int TIMEOUT_MS = 10000;

        private readonly HttpClient _http;
        private readonly string _tvIp;
        private readonly string _psk;
        private int _requestId;

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public TvBrand Brand => TvBrand.Sony;
        public string TvIp => _tvIp;
        public bool IsConnected { get; private set; }
        public bool IsPaired => IsConnected;

        /// <param name="tvIp">TV IP address.</param>
        /// <param name="preSharedKey">Pre-Shared Key configured on the TV (default "0000").</param>
        public SonyTvController(string tvIp, string preSharedKey = "0000")
        {
            _tvIp = tvIp;
            _psk = preSharedKey;
            _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS) };
            _http.DefaultRequestHeaders.Add("X-Auth-PSK", _psk);
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            Log("Connecting to Sony Bravia TV...");
            OnStatusChange?.Invoke($"Connecting to Sony TV at {_tvIp}...");

            try
            {
                // Query system info to verify connectivity
                var result = await SendJsonRpcAsync("system", "getSystemInformation", new JArray());
                var model = result?["model"]?.ToString() ?? "Sony TV";
                var product = result?["product"]?.ToString() ?? "Bravia";

                Log($"Connected to {product} {model}");

                IsConnected = true;
                OnStatusChange?.Invoke($"Connected to {product} {model} — ready for calibration");
            }
            catch (HttpRequestException ex)
            {
                Log($"Connection failed: {ex.Message}");
                OnStatusChange?.Invoke(
                    $"Connection failed — ensure IP control is enabled. Error: {ex.Message}");
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
            // Sony uses "setPictureQualitySettings" with scene = current
            await SetPictureSettingAsync("pictureMode", mode);
            OnStatusChange?.Invoke($"Picture mode set to '{mode}'");
        }

        public async Task SetBacklightAsync(int value)
        {
            await SetPictureSettingAsync("backlight", value.ToString());
        }

        public async Task SetContrastAsync(int value)
        {
            await SetPictureSettingAsync("contrast", value.ToString());
        }

        public async Task SetBrightnessAsync(int value)
        {
            await SetPictureSettingAsync("brightness", value.ToString());
        }

        public async Task SetColorAsync(int value)
        {
            await SetPictureSettingAsync("color", value.ToString());
        }

        public async Task SetSharpnessAsync(int value)
        {
            await SetPictureSettingAsync("sharpness", value.ToString());
        }

        public async Task SetColorGamutAsync(string gamut)
        {
            // Sony: "colorSpace" setting with values like "sRGB", "BT.2020", etc.
            await SetPictureSettingAsync("colorSpace", gamut);
        }

        public async Task SetGammaAsync(string gamma)
        {
            await SetPictureSettingAsync("gamma", gamma);
        }

        public async Task SetColorTemperatureAsync(string temp)
        {
            await SetPictureSettingAsync("colorTemperature", temp);
        }

        public async Task DisableProcessingAsync()
        {
            // Disable processing features known on Sony/Bravia
            await SetPictureSettingAsync("motionFlow", "off");
            await SetPictureSettingAsync("realityCreation", "off");
            await SetPictureSettingAsync("noiseReduction", "off");
            await SetPictureSettingAsync("digitalNoiseReduction", "off");
            await SetPictureSettingAsync("smoothGradation", "off");
            await SetPictureSettingAsync("liveColour", "off");
            OnStatusChange?.Invoke("Image processing disabled");
        }

        public Task ReadPictureSettingsAsync()
        {
            OnStatusChange?.Invoke("Reading picture settings is not yet supported for Sony TVs");
            return Task.CompletedTask;
        }

        // ── White Balance ──

        public async Task SetWhiteBalance2ptAsync(int redGain, int greenGain, int blueGain,
            int redOffset, int greenOffset, int blueOffset)
        {
            // Sony Bravia professional calibration: white balance via JSON-RPC
            var settings = new JArray
            {
                WbSetting("whiteBalanceRedGain", redGain),
                WbSetting("whiteBalanceGreenGain", greenGain),
                WbSetting("whiteBalanceBlueGain", blueGain),
                WbSetting("whiteBalanceRedOffset", redOffset),
                WbSetting("whiteBalanceGreenOffset", greenOffset),
                WbSetting("whiteBalanceBlueOffset", blueOffset)
            };

            var outerParams = new JArray
            {
                new JObject { ["settings"] = settings }
            };
            await SendJsonRpcAsync("videoScreen", "setWhiteBalanceSettings", outerParams);
            Log($"2pt WB set: RG={redGain} GG={greenGain} BG={blueGain} RO={redOffset} GO={greenOffset} BO={blueOffset}");
        }

        public async Task SetWhiteBalance20ptPointAsync(int index, int red, int green, int blue)
        {
            // Sony 20-point: each IRE point (index*5% from 0 to 100)
            var settings = new JArray
            {
                new JObject
                {
                    ["index"] = index,
                    ["ire"] = index * 5,
                    ["red"] = red,
                    ["green"] = green,
                    ["blue"] = blue
                }
            };

            var outerParams = new JArray
            {
                new JObject
                {
                    ["mode"] = "20point",
                    ["points"] = settings
                }
            };
            await SendJsonRpcAsync("videoScreen", "setWhiteBalanceMultipoint", outerParams);
            Log($"20pt WB[{index}]: R={red} G={green} B={blue}");
        }

        public async Task ResetWhiteBalanceAsync()
        {
            await SendJsonRpcAsync("videoScreen", "resetWhiteBalance", new JArray());
            Log("White balance reset");
        }

        // ── CMS ──

        public async Task SetCmsColorAsync(string color, int hue, int saturation, int luminance)
        {
            var settings = new JArray
            {
                new JObject
                {
                    ["target"] = color.ToLowerInvariant(),
                    ["hue"] = hue,
                    ["saturation"] = saturation,
                    ["value"] = luminance   // Sony uses "value" for luminance
                }
            };

            var outerParams = new JArray
            {
                new JObject { ["settings"] = settings }
            };
            await SendJsonRpcAsync("videoScreen", "setColorAdjustment", outerParams);
            Log($"CMS {color}: H={hue} S={saturation} L={luminance}");
        }

        public async Task ResetCmsAsync()
        {
            await SendJsonRpcAsync("videoScreen", "resetColorAdjustment", new JArray());
            Log("CMS reset");
        }

        // ── Utility ──

        public Task ShowToastAsync(string message)
        {
            OnStatusChange?.Invoke($"Toast: {message}");
            return Task.CompletedTask;
        }

        // ── Sony JSON-RPC Helpers ──

        private async Task<JObject?> SendJsonRpcAsync(string service, string method, JArray parameters)
        {
            var url = $"http://{_tvIp}/sony/{service}";
            int id = Interlocked.Increment(ref _requestId);

            var request = new JObject
            {
                ["method"] = method,
                ["id"] = id,
                ["params"] = parameters,
                ["version"] = "1.0"
            };

            var content = new StringContent(request.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var body = JObject.Parse(await response.Content.ReadAsStringAsync());

            // Check for JSON-RPC error
            if (body["error"] != null)
            {
                var errCode = body["error"]?[0]?.ToString() ?? "?";
                var errMsg = body["error"]?[1]?.ToString() ?? "Unknown error";
                throw new InvalidOperationException($"Sony API error {errCode}: {errMsg}");
            }

            // Return first result object if present
            return body["result"]?[0] as JObject;
        }

        private async Task SetPictureSettingAsync(string target, string value)
        {
            var settings = new JArray
            {
                new JObject
                {
                    ["target"] = target,
                    ["value"] = value
                }
            };

            var outerParams = new JArray
            {
                new JObject { ["settings"] = settings }
            };
            await SendJsonRpcAsync("videoScreen", "setPictureQualitySettings", outerParams);
        }

        private static JObject WbSetting(string target, int value)
        {
            return new JObject
            {
                ["target"] = target,
                ["value"] = value.ToString()
            };
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
