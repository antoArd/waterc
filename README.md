# WaterCoolerCLI

## Overview

WaterCoolerCLI is a command-line interface (CLI) tool designed to control and monitor AORUS water coolers on Linux systems. Official AORUS software does not support Linux, so this tool provides essential functionality for managing fan and pump modes, curves, speeds, and real-time monitoring. It was developed as a reverse-engineered solution based on the AORUS cooler library to fill this gap.

**Important Note:** This project is a quick proof-of-concept built in a couple of hours of spare time due to my busy work schedule. The code is not perfect, has rough edges, and could benefit from significant improvements, such as refactoring, better error handling, and possibly porting to a lower-level language like C or Rust for better performance and stability. Contributions are welcome!

The majority of the core logic was reverse-engineered from the proprietary AORUS cooler library. Use at your own risk, as interacting with hardware can potentially cause damage if misused.

## Features

- **Fan and Pump Control:**
  - Get/set fan mode (e.g., Balanced, Turbo, Quiet, Custom).
  - Get/set pump mode (similar options).
  - Get current fan and pump speeds.
  - Get/set custom fan and pump curves (temperature:speed points, e.g., `0:1000,30:1500`).

- **Monitoring:**
  - Real-time monitoring of speeds and CPU temperature (press 'q' to stop).
  - Service mode to continuously send CPU temperature telemetry to the cooler for dynamic adjustment (intended for background daemon use).

- **Visualization:**
  - Plot current fan and pump curves in a simple ASCII graph.

- **Interactive CLI:**
  - Tab completion for commands and options.
  - Command history with up/down arrows.
  - Help system with `help` command.

- **Supported Devices:**
  - Automatically detects common AORUS devices (VID:1044 PID:7A51, VID:1044 PID:7A4D, VID:0414 PID:7A5E).
  - Extendable for other compatible HID-based coolers.

## Requirements

- **OS:** Linux (tested on latest Fedora and Ubuntu/Debian derivatives; requires HID access).
- **Runtime:** .NET 9 SDK (for building).
- **Hardware Access:** 
  - Run as root for mode/curve changes (requires HID raw access via `libhidapi` or equivalent).
  - CPU temperature reading requires `lm-sensors` or OpenHardwareMonitor-like access (uses `CpuTempHandler` for AMD/Intel support).
- **Dependencies:** 
  - HID libraries (e.g., `hidapi` for Linux).
  - No external NuGet packages beyond standard .NET (self-contained build).

For CPU temperature reading in service mode, ensure your kernel supports it (e.g., via `/sys/class/thermal` or WMI equivalents, but adapted for Linux).

## Building

This project targets .NET 9 and should be built as a native AOT (Ahead-of-Time), self-contained, and trimmed executable for optimal performance and minimal size on Linux.

### Prerequisites
- Install .NET 9 SDK: [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0).
- On Linux, ensure build essentials: `sudo apt install build-essential libhidapi-dev` on Ubuntu/Debian, or `sudo dnf install gcc-c++ hidapi-devel` on Fedora (or equivalent for your distro).

### Build Commands
Navigate to the project directory (`WaterCoolerCLI/`) and run:

```bash
# Clean previous builds
dotnet clean

# Publish as native AOT self-contained trimmed binary for Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true -p:PublishTrimmed=true -o ./publish

# The executable will be at ./publish/WaterCoolerCLI
```

This produces a standalone `WaterCoolerCLI` binary (no .NET runtime needed). The trimmed AOT build reduces size and startup time, ideal for daemon use.

- **Customization:** 
  - For ARM64: Use `-r linux-arm64`.
  - Disable trimming if issues arise: Remove `-p:PublishTrimmed=true`.
  - Verify build: `./publish/WaterCoolerCLI --help` (run as root for full access).

## Usage

Copy the built binary to a system path (e.g., `/usr/local/bin/WaterCoolerCLI`) and make it executable: `chmod +x WaterCoolerCLI`.

### Interactive Mode
Run without arguments for an interactive shell:

```bash
sudo ./WaterCoolerCLI
```

- Type `help` for a list of commands.
- Examples:
  - `get-fan-mode` – View current fan mode.
  - `set-fan-mode Turbo` – Set fan to Turbo (requires root).
  - `set-fan-curve "0:1000,30:1500,50:2000,65:2500"` – Set custom fan curve (requires root).
  - `monitor 1000` – Start monitoring every 1 second (press 'q' to quit).
  - `plot-curves` – Display ASCII plot of curves.
  - `quit` – Exit.

**Example Output (Connection and Plotting Curves):**

```
Connected to device: GP-AORUS WATERFORCE X 240 (VID: 1044, PID: 7A4D)
Device initialized. Type 'help' for commands.
> plot-curves
Fan and Pump Curves Plot:
Legend: * Fan curve, + Pump curve, O Fan points, o Pump points, x Intersection

3200 ||                                                                    x
     ||                                                                   x
     ||                                                                  x
     ||                                                                 x
2526 ||                                                                +*
     ||                                                               +*
     ||                                                              +*
     ||                                                             + *
1852 ||                                                  +++++++++++o*
     ||+++++++++++++++++++++++++++++++++++++++++++++++++o           *
     |x                                                         ****O
     ||                                                     ****
1178 ||                                                 ****
     ||                                             ****
     ||                                         ****
     ||                                     ****
 505 ||                        ************O
     ||            ************
     ||************
   0 |O---------------------------------------------------------------------
      0°           11°           22°           33°           44°         55°

Current Fan Curve Points:
   0°C:    0 RPM
  30°C:  637 RPM
  50°C: 1578 RPM
  55°C: 3200 RPM

Current Pump Curve Points:
   0°C: 1600 RPM
  40°C: 1800 RPM
  50°C: 2000 RPM
  55°C: 3200 RPM
```

**Note:** Changing modes/curves requires root privileges due to HID device access. Monitoring/viewing can run as a regular user.

### Single Command Mode
Execute a single command and exit:

```bash
sudo ./WaterCoolerCLI get-speeds
./WaterCoolerCLI get-fan-mode  # Non-modifying commands don't need sudo
```

### Service Mode (Daemon for Telemetry)
The `service` command sends CPU temperature to the cooler at regular intervals, enabling dynamic curve-based adjustments. This is designed for background use via systemd.

1. **Test Manually:**
   ```bash
   sudo ./WaterCoolerCLI service  # Send temps at default interval (press 'q' to stop)
   ```

2. **Systemd Integration:**
   Create a systemd service file `/etc/systemd/system/watercooler.service`:

   ```ini
   [Unit]
   Description=AORUS Water Cooler Telemetry Service
   After=network.target

   [Service]
   Type=simple
   User=root
   ExecStart=/usr/local/bin/WaterCoolerCLI service
   Restart=always
   RestartSec=5

   [Install]
   WantedBy=multi-user.target
   ```

   - Adjust `ExecStart` path and add interval if needed (500ms default).
   - Enable and start: 
     ```bash
     sudo systemctl daemon-reload
     sudo systemctl enable watercooler.service
     sudo systemctl start watercooler.service
     sudo systemctl status watercooler.service  # Check logs
     ```

   - Stop: `sudo systemctl stop watercooler.service`.
   - Logs: `journalctl -u watercooler.service -f`.

**Permissions Note:** The service runs as root for HID write access. Ensure your cooler is detected (check `lsusb` for VID:1044 or similar).

## Troubleshooting

- **Device Not Found:** Run `lsusb` to verify cooler VID/PID. Add custom VID/PID via command-line args if needed (future enhancement).
- **HID Access Denied:** Ensure udev rules allow HID access (e.g., add rule for your device's VID/PID).
- **CPU Temp Errors:** Install `lm-sensors` (e.g., `sudo apt install lm-sensors` on Ubuntu/Debian, `sudo dnf install lm-sensors` on Fedora) and run `sensors` to verify detection. The tool uses Linux sysfs for reading.
- **Build Issues:** If AOT trimming breaks HID interop, build without trimming first.
- **Compatibility:** Tested with specific AORUS models; may need tweaks for others.

## Contributing

Feel free to fork, improve, and submit pull requests! Potential enhancements:
- Better cross-distro support.
- GUI wrapper.
- Port to C/Rust for native Linux integration.
- More device support.
- Improved logging and error handling.

## License

This project is open-source. GPL 3.0

## Author

Developed by Antonio Ardolino in spare time. Reverse-engineered from AORUS libraries for personal use and community benefit.

---

*Disclaimer: This tool interacts directly with hardware. The author is not responsible for any damage to your cooler, PC, or data. Always test in a safe environment. Code quality is basic due to time constraints – improvements appreciated!*
