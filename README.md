# PickleGen

PickleGen is a cross-platform calibration tool for LG TVs, featuring:

- **Android App**: Control LG webOS TV picture settings via WiFi
- **Windows App**: Full-featured calibration interface with LG TV integration

## Features

### Android App (PickleCal-lg)
- Connect to LG webOS TVs via WiFi
- Adjust picture modes, backlight, contrast, brightness, color, sharpness
- Control color gamut, gamma, and color temperature
- 2-point and 20-point white balance adjustment
- CMS (Color Management System) adjustments
- Disable video processing for calibration

### Windows App (PickleCal)
- Connect to LG webOS TVs via WebSocket
- Full picture settings control
- HCFR/PickleCal PGen protocol support for calibration software compatibility
- Windows Forms UI with tabbed interface

## Requirements

### Android
- Android 5.0+ (API 21+)
- LG webOS TV on same WiFi network

### Windows
- Windows 10/11 (64-bit)
- .NET 8.0 SDK

## Building

### Android
```bash
./gradlew assembleDebug
```

### Windows
```bash
dotnet build
```

## Usage

1. Connect to your LG TV's WiFi network
2. Launch PickleGen on your device
3. Enter your TV's IP address
4. Pair with the TV (enter PIN displayed on screen)
5. Use the interface to adjust picture settings

## License

MIT License