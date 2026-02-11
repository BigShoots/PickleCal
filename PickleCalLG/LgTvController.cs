using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG
{
    /// <summary>
    /// LG TV SSAP (Simple Service Access Protocol) WebSocket controller.
    /// Controls LG webOS TVs for calibration purposes.
    /// </summary>
    public class LgTvController : ITvController
    {
        private const string TAG = "LgTvController";
        private const string WS_URL_SECURE = "wss://{0}:3001";
        private const string WS_URL_INSECURE = "ws://{0}:3000";
        private const int CONNECT_TIMEOUT_MS = 10000;
        private const int REQUEST_TIMEOUT_MS = 5000;

        private ClientWebSocket _webSocket = null!;
        private string _tvIp = "";
        private bool _useSecure = true;
        private string? _clientKey = null;
        private int _messageId = 1;
        private readonly object _lock = new();

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public TvBrand Brand => TvBrand.LG;
        public string TvIp => _tvIp;
        public bool IsConnected { get; private set; }
        public bool IsPaired { get; private set; }

        public LgTvController(string tvIp, bool useSecure = true)
        {
            _tvIp = tvIp;
            _useSecure = useSecure;
            _webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync()
        {
            if (IsConnected)
            {
                Log("Already connected");
                return;
            }

            string url = _useSecure ? string.Format(WS_URL_SECURE, _tvIp) : string.Format(WS_URL_INSECURE, _tvIp);
            Log($"Connecting to {url}");

            OnStatusChange?.Invoke($"Connecting to {_tvIp}...");

            // Configure WebSocket
            _webSocket.Options.SetRequestHeader("User-Agent", "pgendroid-lg/1.0");
            _webSocket.Options.SetRequestHeader("Origin", "http://pgendroid-lg");
            
            if (_useSecure)
            {
                // Note: ServerCertificateCustomValidationCallback not available in .NET 8.0
                // SSL/TLS validation will still occur using system certificates
            }

            try
            {
                await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                IsConnected = true;
                Log("WebSocket connected");
                OnStatusChange?.Invoke("Connected, registering...");
                await SendRegistrationAsync();
            }
            catch (Exception ex)
            {
                Log($"Connection failed: {ex.Message}");
                OnStatusChange?.Invoke($"Connection failed: {ex.Message}");
                HandleDisconnect();
            }
        }

        private async Task SendRegistrationAsync()
        {
            var manifest = new JObject
            {
                ["manifestVersion"] = 1,
                ["appVersion"] = "1.0.0",
                ["signed"] = new JObject
                {
                    ["created"] = "20240101",
                    ["appId"] = "com.pgendroid.lg",
                    ["vendorId"] = "com.pgendroid",
                    ["localizedAppNames"] = new JObject { [""] = "PickleCal LG" },
                    ["localizedVendorNames"] = new JObject { [""] = "PickleCal LG" },
                    ["permissions"] = new JArray
                    {
                        "CONTROL_AUDIO", "CONTROL_DISPLAY", "CONTROL_INPUT_JOYSTICK",
                        "CONTROL_INPUT_MEDIA_PLAYBACK", "CONTROL_INPUT_MEDIA_RECORDING",
                        "CONTROL_INPUT_TEXT", "CONTROL_INPUT_TV", "CONTROL_MOUSE_AND_KEYBOARD",
                        "CONTROL_POWER", "READ_APP_STATUS", "READ_CURRENT_CHANNEL",
                        "READ_INPUT_DEVICE_LIST", "READ_NETWORK_STATE", "READ_RUNNING_APPS",
                        "READ_TV_CHANNEL_LIST", "READ_TV_CURRENT_TIME", "WRITE_NOTIFICATION_TOAST",
                        "READ_INSTALLED_APPS", "CONTROL_TV_SCREEN"
                    },
                    ["serial"] = "picklecal-lg-001"
                },
                ["permissions"] = new JArray
                {
                    "CONTROL_AUDIO", "CONTROL_DISPLAY", "CONTROL_INPUT_JOYSTICK",
                    "CONTROL_INPUT_MEDIA_PLAYBACK", "CONTROL_INPUT_MEDIA_RECORDING",
                    "CONTROL_INPUT_TEXT", "CONTROL_INPUT_TV", "CONTROL_MOUSE_AND_KEYBOARD",
                    "CONTROL_POWER", "READ_APP_STATUS", "READ_CURRENT_CHANNEL",
                    "READ_INPUT_DEVICE_LIST", "READ_NETWORK_STATE", "READ_RUNNING_APPS",
                    "READ_TV_CHANNEL_LIST", "READ_TV_CURRENT_TIME", "WRITE_NOTIFICATION_TOAST",
                    "READ_INSTALLED_APPS", "CONTROL_TV_SCREEN"
                },
                ["signatures"] = new JArray
                {
                    new JObject
                    {
                        ["signatureVersion"] = 1,
                        ["signature"] = "eyJhbGdvcml0aG0iOiJSU0EtU0hBMjU2Iiwia2V5SWQiOiJ0ZXN0LXNpZ25pbmctY2VydCIsInNpZ25hdHVyZVZlcnNpb24iOjF9.hrVRgjCwXVvE2OOSpDZ58hR+59aFNwYDyjQgKk3auukd7pcegmE2CzPCa0bJ0ZsRAcKkCTJrWo5iDzNhMBWRyaMOv5zWSrthlf7G128qvIlpMT0YNY+n/FaOHE73uLrS/g7swl3/qH/BGFG2Hu4RlL48eb3lLKqTt2xKHdCs6Cd4RMfJPYnzgvI4BNrFUKsjkcu+WD4OO2A27Pq1n50cMchmcaXadJhGrOqH5YmHdOCj5NSHzJYrsW0HPlpuAx/ECMeIZYDh6RMqaFM2DXzdKX9NmmyqzJ3o/0lkk/N97gfVRLW5hA29yeAwaCViZNCP8iC9aO0q9fQojoa7NQnAtw=="
                    }
                }
            };

            var payload = new JObject
            {
                ["forcePairing"] = false,
                ["pairingType"] = "PROMPT",
                ["manifest"] = manifest
            };

            if (!string.IsNullOrEmpty(_clientKey))
            {
                payload["client-key"] = _clientKey;
            }

            var message = new JObject
            {
                ["type"] = "register",
                ["id"] = "register_0",
                ["payload"] = payload
            };

            await SendJsonAsync(message);
        }

        public async Task<string> SendRequestAsync(string uri, JObject? payload = null)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected");

            int id = Interlocked.Increment(ref _messageId);
            var message = new
            {
                type = "request",
                id = $"msg_{id}",
                uri = uri,
                payload = payload
            };

            var json = JsonConvert.SerializeObject(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, CancellationToken.None);
            return message.id.ToString();
        }

        public async Task<JObject?> SendRequestSyncAsync(string uri, JObject? payload = null, int timeoutMs = 5000)
        {
            var tcs = new TaskCompletionSource<JObject?>();
            string messageId = await SendRequestAsync(uri, payload);

            var timer = new Timer(_ =>
            {
                tcs.TrySetResult(null);
            }, null, timeoutMs, Timeout.Infinite);

            // TODO: Store callback by messageId
            return await tcs.Task;
        }

        public async Task SetSystemSettingsAsync(string category, JObject settings)
        {
            var payload = new JObject
            {
                ["category"] = category,
                ["settings"] = settings
            };
            await SendRequestAsync("ssap://com.webos.service.settings/setSystemSettings", payload);
        }

        public async Task DisableProcessingAsync()
        {
            var settings = new JObject
            {
                ["dynamicContrast"] = "off",
                ["hdrDynamicToneMapping"] = "off",
                ["sharpness"] = "0",
                ["noiseReduction"] = "off",
                ["mpegNoiseReduction"] = "off",
                ["smoothGradation"] = "off",
                ["realCinema"] = "off"
            };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetPictureModeAsync(string mode)
        {
            var settings = new JObject { ["pictureMode"] = mode };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetBacklightAsync(int value)
        {
            var settings = new JObject { ["backlight"] = value.ToString() };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetContrastAsync(int value)
        {
            var settings = new JObject { ["contrast"] = value.ToString() };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetBrightnessAsync(int value)
        {
            var settings = new JObject { ["brightness"] = value.ToString() };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetColorAsync(int value)
        {
            var settings = new JObject { ["color"] = value.ToString() };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetSharpnessAsync(int value)
        {
            var settings = new JObject { ["sharpness"] = value.ToString() };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetColorGamutAsync(string gamut)
        {
            var settings = new JObject { ["colorGamut"] = gamut };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetGammaAsync(string gamma)
        {
            var settings = new JObject { ["gamma"] = gamma };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetColorTemperatureAsync(string temp)
        {
            var settings = new JObject { ["colorTemperature"] = temp };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetWhiteBalance2ptAsync(int redGain, int greenGain, int blueGain, 
            int redOffset, int greenOffset, int blueOffset)
        {
            var settings = new JObject
            {
                ["whiteBalanceRedGain"] = redGain.ToString(),
                ["whiteBalanceGreenGain"] = greenGain.ToString(),
                ["whiteBalanceBlueGain"] = blueGain.ToString(),
                ["whiteBalanceRedOffset"] = redOffset.ToString(),
                ["whiteBalanceGreenOffset"] = greenOffset.ToString(),
                ["whiteBalanceBlueOffset"] = blueOffset.ToString(),
                ["whiteBalanceMethod"] = "2",
                ["whiteBalanceColorTemperature"] = "warm50"
            };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetWhiteBalance20ptPointAsync(int index, int red, int green, int blue)
        {
            var settings = new JObject
            {
                ["whiteBalanceMethod"] = "20",
                ["whiteBalancePoint"] = index.ToString(),
                ["whiteBalanceColorTemperature"] = "warm50",
                ["whiteBalanceIre"] = (index * 5).ToString(),
                ["whiteBalanceRed"] = red.ToString(),
                ["whiteBalanceGreen"] = green.ToString(),
                ["whiteBalanceBlue"] = blue.ToString()
            };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task SetCmsColorAsync(string color, int hue, int saturation, int luminance)
        {
            var settings = new JObject
            {
                ["colorManagement" + color + "Hue"] = hue.ToString(),
                ["colorManagement" + color + "Saturation"] = saturation.ToString(),
                ["colorManagement" + color + "Luminance"] = luminance.ToString()
            };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task ResetCmsAsync()
        {
            var cmsColors = new[] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" };
            var settings = new JObject();
            foreach (var color in cmsColors)
            {
                settings["colorManagement" + color + "Hue"] = "0";
                settings["colorManagement" + color + "Saturation"] = "0";
                settings["colorManagement" + color + "Luminance"] = "0";
            }
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task ResetWhiteBalanceAsync()
        {
            var settings = new JObject
            {
                ["whiteBalanceRedGain"] = "0",
                ["whiteBalanceGreenGain"] = "0",
                ["whiteBalanceBlueGain"] = "0",
                ["whiteBalanceRedOffset"] = "0",
                ["whiteBalanceGreenOffset"] = "0",
                ["whiteBalanceBlueOffset"] = "0"
            };
            await SetSystemSettingsAsync("picture", settings);
        }

        public async Task ReadPictureSettingsAsync()
        {
            var keys = new JArray
            {
                "pictureMode", "backlight", "contrast", "brightness", "color",
                "sharpness", "colorGamut", "gamma", "colorTemperature",
                "dynamicContrast", "hdrDynamicToneMapping", "blackLevel",
                "whiteBalanceRedGain", "whiteBalanceGreenGain", "whiteBalanceBlueGain",
                "whiteBalanceRedOffset", "whiteBalanceGreenOffset", "whiteBalanceBlueOffset"
            };

            var payload = new JObject
            {
                ["category"] = "picture",
                ["keys"] = keys
            };
            await SendRequestAsync("ssap://com.webos.service.settings/getSystemSettings", payload);
        }

        public async Task ShowToastAsync(string message)
        {
            var payload = new JObject { ["message"] = message };
            await SendRequestAsync("ssap://com.webos.service.system.notifications/createToast", payload);
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
            }
            HandleDisconnect();
        }

        private void HandleDisconnect()
        {
            IsConnected = false;
            IsPaired = false;
            OnDisconnect?.Invoke();
        }

        private void Log(string msg)
        {
            Console.WriteLine($"[{TAG}] {msg}");
        }

        private async Task SendJsonAsync(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            await _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }
    }
}