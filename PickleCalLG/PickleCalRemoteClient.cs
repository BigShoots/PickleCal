using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PickleCalLG
{
    /// <summary>
    /// Client for the PickleCal Remote Control protocol.
    /// Connects to PickleGen Android app running in Easy mode.
    /// Provides full control: patterns, HDR/SDR switching, metadata, device status.
    /// </summary>
    public sealed class PickleCalRemoteClient : IAsyncDisposable
    {
        public const int DEFAULT_PORT = 5742;
        private const int CONNECT_TIMEOUT_MS = 10000;
        private const int READ_TIMEOUT_MS = 5000;

        private TcpClient? _tcp;
        private StreamWriter? _writer;
        private StreamReader? _reader;
        private CancellationTokenSource? _listenCts;
        private Task? _listenTask;
        private volatile bool _connected;

        public event Action<string>? OnStatusChange;
        public event Action<JObject>? OnEvent;
        public event Action? OnDisconnected;

        public bool IsConnected => _connected;
        public string? DeviceName { get; private set; }
        public string? RemoteAddress { get; private set; }
        public string? ProtocolVersion { get; private set; }

        // ---------- Connection ----------

        /// <summary>Discover PickleGen devices using UDP broadcast on port 5743.</summary>
        public async Task<(IPAddress Address, string Name)[]> DiscoverAsync(CancellationToken ct = default)
        {
            var results = new System.Collections.Generic.List<(IPAddress, string)>();
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            var msg = Encoding.UTF8.GetBytes("PICKLECAL_DISCOVER");
            var broadcast = new IPEndPoint(IPAddress.Broadcast, DEFAULT_PORT + 1);

            OnStatusChange?.Invoke("Searching for PickleGen devices...");

            try
            {
                await udp.SendAsync(msg, msg.Length, broadcast);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(3000);

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var task = udp.ReceiveAsync();
                        var completed = await Task.WhenAny(task, Task.Delay(3000, cts.Token));
                        if (completed != task) break;
                        var result = await task;
                        string response = Encoding.UTF8.GetString(result.Buffer);
                        results.Add((result.RemoteEndPoint.Address, response));
                    }
                    catch { break; }
                }
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Discovery error: {ex.Message}");
            }

            OnStatusChange?.Invoke($"Found {results.Count} PickleGen device(s)");
            return results.ToArray();
        }

        /// <summary>Connect to a PickleGen device in Easy mode.</summary>
        public async Task ConnectAsync(string ipAddress, int port = DEFAULT_PORT, CancellationToken ct = default)
        {
            if (_connected) await DisconnectAsync();

            OnStatusChange?.Invoke($"Connecting to PickleGen at {ipAddress}:{port}...");

            _tcp = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(CONNECT_TIMEOUT_MS);

            try
            {
                await _tcp.ConnectAsync(IPAddress.Parse(ipAddress), port, cts.Token);
                var stream = _tcp.GetStream();
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                _reader = new StreamReader(stream, Encoding.UTF8);
                RemoteAddress = ipAddress;

                // Read hello message
                var helloLine = await _reader.ReadLineAsync();
                if (helloLine != null)
                {
                    var hello = JObject.Parse(helloLine);
                    DeviceName = hello["device"]?.ToString() ?? "PickleGen";
                    ProtocolVersion = hello["version"]?.ToString() ?? "unknown";
                }

                _connected = true;

                // Start background listener for events
                _listenCts = new CancellationTokenSource();
                _listenTask = ListenForEventsAsync(_listenCts.Token);

                OnStatusChange?.Invoke($"Connected to PickleGen ({DeviceName}) at {ipAddress}");
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Connection failed: {ex.Message}");
                _tcp?.Dispose();
                _tcp = null;
                throw;
            }
        }

        /// <summary>Disconnect from the device.</summary>
        public async Task DisconnectAsync()
        {
            if (!_connected) return;
            _connected = false;

            try
            {
                await SendCommandAsync("disconnect");
            }
            catch { }

            _listenCts?.Cancel();
            try { if (_listenTask != null) await _listenTask; } catch { }

            _writer?.Dispose();
            _reader?.Dispose();
            _tcp?.Dispose();
            _writer = null;
            _reader = null;
            _tcp = null;

            OnStatusChange?.Invoke("Disconnected from PickleGen");
            OnDisconnected?.Invoke();
        }

        // ---------- Pattern Commands ----------

        /// <summary>Send a full-field pattern.</summary>
        public Task<JObject?> SendFullFieldAsync(byte r, byte g, byte b)
        {
            return SendCommandAsync("pattern_fullfield", new { r = (int)r, g = (int)g, b = (int)b });
        }

        /// <summary>Send a window pattern.</summary>
        public Task<JObject?> SendWindowAsync(float windowPercent, byte r, byte g, byte b,
            byte bgR = 0, byte bgG = 0, byte bgB = 0)
        {
            return SendCommandAsync("pattern_window", new
            {
                r = (int)r, g = (int)g, b = (int)b,
                bgR = (int)bgR, bgG = (int)bgG, bgB = (int)bgB,
                windowPercent
            });
        }

        /// <summary>Send black screen.</summary>
        public Task<JObject?> SendBlackAsync() => SendCommandAsync("pattern_black");

        /// <summary>Send white screen.</summary>
        public Task<JObject?> SendWhiteAsync() => SendCommandAsync("pattern_white");

        /// <summary>Clear all patterns.</summary>
        public Task<JObject?> ClearPatternAsync() => SendCommandAsync("pattern_clear");

        // ---------- Mode Control ----------

        /// <summary>Switch HDR/SDR mode and bit depth on the Android device.</summary>
        public Task<JObject?> SetModeAsync(bool hdr, int bitDepth = 8)
        {
            return SendCommandAsync("set_mode", new { hdr, bitDepth });
        }

        /// <summary>Set HDR metadata on the Android device.</summary>
        public Task<JObject?> SetHdrMetadataAsync(int maxCLL, int maxFALL, int maxDML)
        {
            return SendCommandAsync("set_hdr_metadata", new { maxCLL, maxFALL, maxDML });
        }

        /// <summary>Get current device status.</summary>
        public Task<JObject?> GetStatusAsync() => SendCommandAsync("get_status");

        /// <summary>Ping the device.</summary>
        public Task<JObject?> PingAsync() => SendCommandAsync("ping");

        // ---------- Convenience ----------

        /// <summary>Configure the device for SDR calibration.</summary>
        public async Task SetupForSdrCalibrationAsync()
        {
            await SetModeAsync(hdr: false, bitDepth: 8);
            OnStatusChange?.Invoke("PickleGen configured for SDR calibration");
        }

        /// <summary>Configure the device for HDR10 calibration.</summary>
        public async Task SetupForHdrCalibrationAsync(int maxCLL = 1000, int maxFALL = 400, int maxDML = 1000)
        {
            await SetModeAsync(hdr: true, bitDepth: 10);
            await SetHdrMetadataAsync(maxCLL, maxFALL, maxDML);
            OnStatusChange?.Invoke("PickleGen configured for HDR10 calibration");
        }

        // ---------- Transport ----------

        private readonly SemaphoreSlim _sendLock = new(1, 1);

        private async Task<JObject?> SendCommandAsync(string cmd, object? parameters = null)
        {
            if (!_connected || _writer == null)
            {
                OnStatusChange?.Invoke("Not connected to PickleGen");
                return null;
            }

            await _sendLock.WaitAsync();
            try
            {
                var json = new JObject { ["cmd"] = cmd };
                if (parameters != null)
                {
                    var paramsObj = JObject.FromObject(parameters);
                    foreach (var prop in paramsObj.Properties())
                    {
                        json[prop.Name] = prop.Value;
                    }
                }

                string line = json.ToString(Formatting.None);
                await _writer.WriteLineAsync(line);

                // Response is read by the listener loop — for now we fire-and-forget
                // and rely on events for responses
                return json;
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Send error: {ex.Message}");
                HandleDisconnect();
                return null;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ListenForEventsAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _reader != null)
                {
                    var line = await _reader.ReadLineAsync();
                    if (line == null)
                    {
                        HandleDisconnect();
                        break;
                    }

                    try
                    {
                        var json = JObject.Parse(line);
                        OnEvent?.Invoke(json);
                    }
                    catch
                    {
                        // Ignore malformed responses
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { HandleDisconnect(); }
            catch (ObjectDisposedException) { }
        }

        private void HandleDisconnect()
        {
            if (!_connected) return;
            _connected = false;
            OnStatusChange?.Invoke("PickleGen connection lost");
            OnDisconnected?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
            _sendLock.Dispose();
        }
    }
}
