using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PickleCalLG
{
    /// <summary>
    /// PGenerator protocol server for HCFR/PickleCal calibration.
    /// Acts as a PGenerator for calibration software that doesn't support Resolve protocol.
    /// </summary>
    public class PGenServer : IDisposable
    {
        private const string TAG = "PGenServer";
        private const int UDP_PORT = 1977;
        private const int TCP_PORT = 85;
        private const int SCREEN_WIDTH = 3840;
        private const int SCREEN_HEIGHT = 2160;

        private TcpListener _tcpListener = null!;
        private UdpClient _udpClient = null!;
        private Thread? _udpThread = null;
        private Thread? _tcpThread = null;
        private volatile bool _running = false;
        private volatile bool _udpRunning = false;
        private TcpClient? _client = null;

        public event Action<string>? OnStatusChange;
        public event Action<string>? OnPatternChange;

        public int Port => TCP_PORT;

        public PGenServer()
        {
            _tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
            _udpClient = new UdpClient(UDP_PORT);
        }

        public async Task StartAsync()
        {
            await Task.Yield();
            _running = true;
            _udpRunning = true;

            // Start UDP discovery
            _udpThread = new Thread(new ThreadStart(UdpDiscoveryLoop));
            _udpThread.IsBackground = true;
            _udpThread.Start();

            // Start TCP server
            _tcpListener.Start();
            OnStatusChange?.Invoke($"PGen: Waiting on port {TCP_PORT}...");

            while (_running)
            {
                try
                {
                    _tcpThread = new Thread(new ParameterizedThreadStart(TcpClientLoop));
                    _tcpThread.IsBackground = true;
                    _tcpThread.Start(_tcpListener.AcceptTcpClient());
                }
                catch (SocketException)
                {
                    if (_running) throw;
                }
            }
        }

        private void UdpDiscoveryLoop()
        {
            byte[] buffer = new byte[1024];
            while (_udpRunning)
            {
                try
                {
                    IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
                    var result = _udpClient.ReceiveAsync().Result;
                    string message = Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length);

                    if (message == "Who is a PGenerator")
                    {
                        string response = "This is picklecal-lg-pgen :)";
                        _udpClient.Send(Encoding.UTF8.GetBytes(response), response.Length, result.RemoteEndPoint);
                        OnStatusChange?.Invoke($"Sent discovery response to {result.RemoteEndPoint}");
                    }
                }
                catch (Exception ex)
                {
                    if (_udpRunning) OnStatusChange?.Invoke($"UDP error: {ex.Message}");
                }
            }
        }

        private void TcpClientLoop(object? clientObj)
        {
            if (clientObj is not TcpClient client)
            {
                OnStatusChange?.Invoke("PGen: Invalid TCP client object");
                return;
            }
            _client = client;
            OnStatusChange?.Invoke("PGen: Client connected");

            NetworkStream stream = client.GetStream();
            BinaryReader reader = new BinaryReader(stream);
            BinaryWriter writer = new BinaryWriter(stream);

            try
            {
                while (_running && client.Connected)
                {
                    string? message = ReadPGenMessage(reader);
                    if (message == null) break;

                    OnStatusChange?.Invoke($"Received: {message}");
                    string? response = ProcessCommand(message);
                    if (response != null)
                    {
                        SendPGenResponse(writer, response);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_running) OnStatusChange?.Invoke($"TCP error: {ex.Message}");
            }
            finally
            {
                client.Close();
                _client = null;
                OnStatusChange?.Invoke("PGen: Client disconnected");
            }
        }

        private string? ProcessCommand(string command)
        {
            switch (command)
            {
                case "CMD:GET_RESOLUTION":
                    return $"OK:{SCREEN_WIDTH}x{SCREEN_HEIGHT}";
                case "CMD:GET_GPU_MEMORY":
                    return "OK:192";
                case "CMD:GET_VERSION":
                    return "OK:1.0.0";
                case "CMD:READY":
                    return "OK:READY";
                case "CMD:STOP":
                    return "OK:STOPPED";
                default:
                    if (command.StartsWith("RGB=RECTANGLE"))
                    {
                        HandleRectangleCommand(command);
                        return "OK:PATTERN";
                    }
                    if (command.StartsWith("RGB=FULLFIELD"))
                    {
                        HandleFullFieldCommand(command);
                        return "OK:PATTERN";
                    }
                    if (command.StartsWith("RGB=WINDOW"))
                    {
                        HandleWindowCommand(command);
                        return "OK:PATTERN";
                    }
                    return "ERR:UNKNOWN_COMMAND";
            }
        }

        private void HandleRectangleCommand(string command)
        {
            try
            {
                // RGB=RECTANGLE;width,height,depth,red,green,blue,bgred,bggreen,bgblue;...
                string content = command.Substring(command.IndexOf('=') + 1);
                string[] parts = content.Split(';');
                
                if (parts.Length >= 2)
                {
                    int width = int.Parse(parts[0]);
                    int height = int.Parse(parts[1]);
                    // Extract colors: red,green,blue for pattern and background
                    int r = 255, g = 255, b = 255;
                    int bgR = 0, bgG = 0, bgB = 0;
                    
                    if (parts.Length >= 5)
                    {
                        r = int.Parse(parts[3]);
                        g = int.Parse(parts[4]);
                        b = int.Parse(parts[5]);
                    }
                    if (parts.Length >= 8)
                    {
                        bgR = int.Parse(parts[6]);
                        bgG = int.Parse(parts[7]);
                        bgB = int.Parse(parts[8]);
                    }

                    // Store pattern for Android app to render
                    OnPatternChange?.Invoke(JsonConvert.SerializeObject(new
                    {
                        type = "rectangle",
                        x = 0,
                        y = 0,
                        width = width,
                        height = height,
                        color = new { r, g, b },
                        background = new { r = bgR, g = bgG, b = bgB }
                    }));
                }
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Rectangle command error: {ex.Message}");
            }
        }

        private void HandleFullFieldCommand(string command)
        {
            try
            {
                // RGB=FULLFIELD;depth;red;green;blue
                string content = command.Substring(command.IndexOf('=') + 1);
                string[] parts = content.Split(';');
                
                if (parts.Length >= 4)
                {
                    int r = int.Parse(parts[2]);
                    int g = int.Parse(parts[3]);
                    int b = int.Parse(parts[4]);

                    OnPatternChange?.Invoke(JsonConvert.SerializeObject(new
                    {
                        type = "fullfield",
                        color = new { r, g, b }
                    }));
                }
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Fullfield command error: {ex.Message}");
            }
        }

        private void HandleWindowCommand(string command)
        {
            try
            {
                // RGB=WINDOW;percent;depth;red;green;blue;bgred;bggreen;bgblue
                string content = command.Substring(command.IndexOf('=') + 1);
                string[] parts = content.Split(';');
                
                if (parts.Length >= 5)
                {
                    float percent = float.Parse(parts[1]);
                    int r = int.Parse(parts[3]);
                    int g = int.Parse(parts[4]);
                    int b = int.Parse(parts[5]);
                    int bgR = 0, bgG = 0, bgB = 0;
                    
                    if (parts.Length >= 8)
                    {
                        bgR = int.Parse(parts[6]);
                        bgG = int.Parse(parts[7]);
                        bgB = int.Parse(parts[8]);
                    }

                    OnPatternChange?.Invoke(JsonConvert.SerializeObject(new
                    {
                        type = "window",
                        percent = percent,
                        color = new { r, g, b },
                        background = new { r = bgR, g = bgG, b = bgB }
                    }));
                }
            }
            catch (Exception ex)
            {
                OnStatusChange?.Invoke($"Window command error: {ex.Message}");
            }
        }

        private string? ReadPGenMessage(BinaryReader reader)
        {
            List<byte> buffer = new List<byte>();
            int prevByte = -1;

            while (true)
            {
                int b = reader.ReadByte();
                if (b == -1) return null;

                buffer.Add((byte)b);

                if (prevByte == 0x02 && b == 0x0D)
                {
                    return Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count - 2);
                }
                prevByte = b;
            }
        }

        private void SendPGenResponse(BinaryWriter writer, string response)
        {
            byte[] data = Encoding.UTF8.GetBytes(response);
            byte[] final = new byte[data.Length + 1];
            Array.Copy(data, final, data.Length);
            final[data.Length] = 0;
            writer.Write(final);
            writer.Flush();
        }

        public void EmitFullField(byte red, byte green, byte blue)
        {
            string command = $"RGB=FULLFIELD;8;{red};{green};{blue}";
            HandleFullFieldCommand(command);
            OnStatusChange?.Invoke($"PGen pattern: full field {red},{green},{blue}");
        }

        public void EmitWindow(double percent, byte red, byte green, byte blue, byte backgroundRed, byte backgroundGreen, byte backgroundBlue)
        {
            percent = Math.Clamp(percent, 0d, 100d);
            string formattedPercent = percent.ToString(CultureInfo.InvariantCulture);
            string command = $"RGB=WINDOW;{formattedPercent};8;{red};{green};{blue};{backgroundRed};{backgroundGreen};{backgroundBlue}";
            HandleWindowCommand(command);
            OnStatusChange?.Invoke($"PGen pattern: window {formattedPercent}% {red},{green},{blue}");
        }

        public async Task StopAsync()
        {
            _running = false;
            _udpRunning = false;
            _tcpListener.Stop();
            _client?.Close();

            if (_udpThread != null) await Task.Run(() => _udpThread.Join(1000));
            if (_tcpThread != null) await Task.Run(() => _tcpThread.Join(1000));
        }

        public void Dispose()
        {
            _udpClient?.Close();
            _tcpListener?.Stop();
        }
    }
}