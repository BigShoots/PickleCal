using System;
using System.Threading.Tasks;

namespace PickleCalLG
{
    /// <summary>
    /// Generic TV controller for non-LG brands.
    /// Provides a placeholder connection (no actual network communication).
    /// Automated calibration operations are not supported — measurement-only workflow.
    /// </summary>
    public sealed class GenericTvController : ITvController
    {
        private readonly TvBrand _brand;
        private readonly string _tvIp;

        public event Action<string>? OnStatusChange;
        public event Action? OnDisconnect;

        public TvBrand Brand => _brand;
        public string TvIp => _tvIp;
        public bool IsConnected { get; private set; }
        public bool IsPaired => IsConnected;

        public GenericTvController(TvBrand brand, string tvIp)
        {
            _brand = brand;
            _tvIp = tvIp;
        }

        public Task ConnectAsync()
        {
            // Generic TVs don't have a network control API.
            // We mark as "connected" to indicate the user has identified their TV.
            IsConnected = true;
            OnStatusChange?.Invoke($"{_brand} TV registered at {_tvIp} — manual control required");
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            OnDisconnect?.Invoke();
            return Task.CompletedTask;
        }

        // ── Picture Settings (not supported — manual control) ──

        public Task SetPictureModeAsync(string mode)
        {
            OnStatusChange?.Invoke($"Set picture mode to '{mode}' on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetBacklightAsync(int value)
        {
            OnStatusChange?.Invoke($"Set backlight to {value} on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetContrastAsync(int value)
        {
            OnStatusChange?.Invoke($"Set contrast to {value} on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetBrightnessAsync(int value)
        {
            OnStatusChange?.Invoke($"Set brightness to {value} on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetColorAsync(int value)
        {
            OnStatusChange?.Invoke($"Set color to {value} on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetSharpnessAsync(int value)
        {
            OnStatusChange?.Invoke($"Set sharpness to {value} on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetColorGamutAsync(string gamut)
        {
            OnStatusChange?.Invoke($"Set color gamut to '{gamut}' on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetGammaAsync(string gamma)
        {
            OnStatusChange?.Invoke($"Set gamma to '{gamma}' on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task SetColorTemperatureAsync(string temp)
        {
            OnStatusChange?.Invoke($"Set color temperature to '{temp}' on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task DisableProcessingAsync()
        {
            OnStatusChange?.Invoke($"Disable all processing features on your {_brand} TV remote");
            return Task.CompletedTask;
        }

        public Task ReadPictureSettingsAsync()
        {
            OnStatusChange?.Invoke($"Reading picture settings is not supported for {_brand} TVs");
            return Task.CompletedTask;
        }

        // ── White Balance (not supported — throws for automated calibration guard) ──

        public Task SetWhiteBalance2ptAsync(int redGain, int greenGain, int blueGain,
            int redOffset, int greenOffset, int blueOffset)
            => throw new NotSupportedException($"Automated white balance is not supported for {_brand} TVs");

        public Task SetWhiteBalance20ptPointAsync(int index, int red, int green, int blue)
            => throw new NotSupportedException($"Automated 20-point white balance is not supported for {_brand} TVs");

        public Task ResetWhiteBalanceAsync()
            => throw new NotSupportedException($"Automated white balance reset is not supported for {_brand} TVs");

        // ── CMS (not supported) ──

        public Task SetCmsColorAsync(string color, int hue, int saturation, int luminance)
            => throw new NotSupportedException($"Automated CMS is not supported for {_brand} TVs");

        public Task ResetCmsAsync()
            => throw new NotSupportedException($"Automated CMS reset is not supported for {_brand} TVs");

        // ── Utility ──

        public Task ShowToastAsync(string message)
        {
            OnStatusChange?.Invoke($"Toast: {message}");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Nothing to dispose for generic TVs
        }
    }
}
