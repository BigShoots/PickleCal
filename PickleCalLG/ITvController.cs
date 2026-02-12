using System;
using System.Threading.Tasks;

namespace PickleCalLG
{
    /// <summary>
    /// Abstraction for TV remote control — allows different TV brands to be
    /// controlled through a common interface for calibration purposes.
    /// </summary>
    public interface ITvController : IDisposable
    {
        /// <summary>The TV brand this controller handles.</summary>
        TvBrand Brand { get; }

        /// <summary>The TV's IP address.</summary>
        string TvIp { get; }

        /// <summary>Whether the controller is connected to the TV.</summary>
        bool IsConnected { get; }

        /// <summary>Whether the TV has been paired/authenticated.</summary>
        bool IsPaired { get; }

        /// <summary>Fired when the connection status changes.</summary>
        event Action<string>? OnStatusChange;

        /// <summary>Fired when the connection is lost.</summary>
        event Action? OnDisconnect;

        // ── Connection ──

        /// <summary>Connect to the TV.</summary>
        Task ConnectAsync();

        /// <summary>Disconnect from the TV.</summary>
        Task DisconnectAsync();

        // ── Picture Settings ──

        /// <summary>Set the picture mode (e.g., cinema, game, expert1).</summary>
        Task SetPictureModeAsync(string mode);

        /// <summary>Set the backlight level (0-100).</summary>
        Task SetBacklightAsync(int value);

        /// <summary>Set the contrast level (0-100).</summary>
        Task SetContrastAsync(int value);

        /// <summary>Set the brightness level (0-100).</summary>
        Task SetBrightnessAsync(int value);

        /// <summary>Set the color level (0-100).</summary>
        Task SetColorAsync(int value);

        /// <summary>Set the sharpness level (0-100).</summary>
        Task SetSharpnessAsync(int value);

        /// <summary>Set the color gamut.</summary>
        Task SetColorGamutAsync(string gamut);

        /// <summary>Set the gamma preset.</summary>
        Task SetGammaAsync(string gamma);

        /// <summary>Set the color temperature preset.</summary>
        Task SetColorTemperatureAsync(string temp);

        /// <summary>Disable image processing (dynamic contrast, NR, motion, etc.).</summary>
        Task DisableProcessingAsync();

        /// <summary>Read the current picture settings.</summary>
        Task ReadPictureSettingsAsync();

        // ── White Balance ──

        /// <summary>Set 2-point white balance (gain and offset for R/G/B).</summary>
        Task SetWhiteBalance2ptAsync(int redGain, int greenGain, int blueGain,
            int redOffset, int greenOffset, int blueOffset);

        /// <summary>Set a single 20-point white balance point (index 0-20, each at 5% IRE step).</summary>
        Task SetWhiteBalance20ptPointAsync(int index, int red, int green, int blue);

        /// <summary>Reset white balance to factory defaults.</summary>
        Task ResetWhiteBalanceAsync();

        // ── Color Management System ──

        /// <summary>Set CMS adjustments for a specific color (Hue, Saturation, Luminance).</summary>
        Task SetCmsColorAsync(string color, int hue, int saturation, int luminance);

        /// <summary>Reset CMS to factory defaults.</summary>
        Task ResetCmsAsync();

        // ── Utility ──

        /// <summary>Display a toast/notification on the TV screen.</summary>
        Task ShowToastAsync(string message);
    }
}
