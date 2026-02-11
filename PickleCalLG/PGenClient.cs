using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PickleCalLG
{
    /// <summary>
    /// Client that discovers and drives a remote PGenerator (Android PickleGen app)
    /// over the network. Uses UDP broadcast for discovery and TCP for pattern commands.
    /// This is the bridge between the Windows calibration app and the Android pattern generator.
    /// </summary>
    public sealed class PGenClient : IAsyncDisposable
    {
        private const int UDP_DISCOVERY_PORT = 1977;
        private const int TCP_PORT = 85;
        private const string DISCOVERY_MESSAGE = "Who is a PGenerator";
        private const int DISCOVERY_TIMEOUT_MS = 5000;
        private const int CONNECT_TIMEOUT_MS = 5000;

        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private IPEndPoint? _remoteEndpoint;
        private volatile bool _connected;

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public bool IsConnected => _connected;
        public string? RemoteAddress => _remoteEndpoint?.Address.ToString();
        public int? RemotePort => _remoteEndpoint?.Port;
        public string? DeviceName { get; private set; }

        /// <summary>
        /// Discover PGenerator devices on the local network via UDP broadcast.
        /// Returns a list of (IP, device name) tuples.
        /// </summary>
        public async Task<(IPAddress Address, string Name)[]> DiscoverAsync(CancellationToken cancellationToken = default)
        {
            var results = new System.Collections.Generic.List<(IPAddress, string)>();
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            udp.Client.ReceiveTimeout = DISCOVERY_TIMEOUT_MS;

            var message = Encoding.UTF8.GetBytes(DISCOVERY_MESSAGE);
            var broadcast = new IPEndPoint(IPAddress.Broadcast, UDP_DISCOVERY_PORT);

            OnStatusChange?.Invoke("Searching for PGenerator devices...");

            try
            {
                await udp.SendAsync(message, message.Length, broadcast);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(DISCOVERY_TIMEOUT_MS);

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var receiveTask = udp.ReceiveAsync();
                        var completedTask = await Task.WhenAny(receiveTask, Task.Delay(DISCOVERY_TIMEOUT_MS, cts.Token));
                        if (completedTask != receiveTask) break;

                        var result = await receiveTask;
                        string response = Encoding.UTF8.GetString(result.Buffer);
                        results.Add((result.RemoteEndPoint.Address, response));
                        OnStatusChange?.Invoke($"Found PGenerator: {result.RemoteEndPoint.Address} - {response}");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Discovery error: {ex.Message}");
            }

            OnStatusChange?.Invoke($"Discovery complete: {results.Count} device(s) found");
            return results.ToArray();
        }

        /// <summary>Connect to a PGenerator at the given IP address.</summary>
        public async Task ConnectAsync(IPAddress address, CancellationToken cancellationToken = default)
        {
            if (_connected)
            {
                await DisconnectAsync();
            }

            OnStatusChange?.Invoke($"Connecting to PGenerator at {address}:{TCP_PORT}...");

            try
            {
                _tcpClient = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(CONNECT_TIMEOUT_MS);

                await _tcpClient.ConnectAsync(address, TCP_PORT, cts.Token);
                _stream = _tcpClient.GetStream();
                _remoteEndpoint = new IPEndPoint(address, TCP_PORT);
                _connected = true;

                // Query device info
                var version = await SendCommandAsync("CMD:GET_VERSION");
                var resolution = await SendCommandAsync("CMD:GET_RESOLUTION");
                DeviceName = version ?? "Unknown PGenerator";

                OnStatusChange?.Invoke($"Connected to PGenerator at {address} ({DeviceName}, {resolution})");
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Connection failed: {ex.Message}");
                _connected = false;
                _tcpClient?.Dispose();
                _tcpClient = null;
                _stream = null;
                throw;
            }
        }

        /// <summary>Connect to a PGenerator at the given IP string.</summary>
        public Task ConnectAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            if (!IPAddress.TryParse(ipAddress, out var address))
                throw new ArgumentException($"Invalid IP address: {ipAddress}");
            return ConnectAsync(address, cancellationToken);
        }

        /// <summary>Send a full-field pattern to the remote PGenerator.</summary>
        public async Task SendFullFieldAsync(byte red, byte green, byte blue, int bitDepth = 8)
        {
            string cmd = $"RGB=FULLFIELD;{bitDepth};{red};{green};{blue}";
            await SendCommandAsync(cmd);
            OnStatusChange?.Invoke($"PGen remote: fullfield R={red} G={green} B={blue}");
        }

        /// <summary>Send a window pattern to the remote PGenerator.</summary>
        public async Task SendWindowAsync(double percent, byte red, byte green, byte blue,
            byte bgRed = 0, byte bgGreen = 0, byte bgBlue = 0, int bitDepth = 8)
        {
            string pct = percent.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cmd = $"RGB=WINDOW;{pct};{bitDepth};{red};{green};{blue};{bgRed};{bgGreen};{bgBlue}";
            await SendCommandAsync(cmd);
            OnStatusChange?.Invoke($"PGen remote: window {pct}% R={red} G={green} B={blue}");
        }

        /// <summary>Send a ready command.</summary>
        public Task SendReadyAsync() => SendCommandAsync("CMD:READY");

        /// <summary>Send a stop command.</summary>
        public Task SendStopAsync() => SendCommandAsync("CMD:STOP");

        /// <summary>Send a raw command and return the response.</summary>
        public async Task<string?> SendCommandAsync(string command)
        {
            if (!_connected || _stream == null)
            {
                OnStatusChange?.Invoke("PGen client: not connected");
                return null;
            }

            try
            {
                // PGenerator protocol: message + 0x02 + 0x0D terminator
                byte[] cmdBytes = Encoding.UTF8.GetBytes(command);
                byte[] packet = new byte[cmdBytes.Length + 2];
                Array.Copy(cmdBytes, packet, cmdBytes.Length);
                packet[cmdBytes.Length] = 0x02;
                packet[cmdBytes.Length + 1] = 0x0D;

                await _stream.WriteAsync(packet);
                await _stream.FlushAsync();

                // Read response (null-terminated string)
                var buffer = new byte[4096];
                int bytesRead = await _stream.ReadAsync(buffer);
                if (bytesRead <= 0)
                {
                    HandleDisconnect();
                    return null;
                }

                // Find null terminator
                int end = Array.IndexOf(buffer, (byte)0, 0, bytesRead);
                if (end < 0) end = bytesRead;

                return Encoding.UTF8.GetString(buffer, 0, end);
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"PGen command error: {ex.Message}");
                HandleDisconnect();
                return null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connected)
            {
                try { await SendStopAsync(); } catch { }
            }

            _connected = false;
            _stream?.Dispose();
            _stream = null;
            _tcpClient?.Dispose();
            _tcpClient = null;
            _remoteEndpoint = null;
            DeviceName = null;
            OnStatusChange?.Invoke("PGen client: disconnected");
            OnDisconnect?.Invoke();
        }

        private void HandleDisconnect()
        {
            _connected = false;
            OnStatusChange?.Invoke("PGen client: connection lost");
            OnDisconnect?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}
