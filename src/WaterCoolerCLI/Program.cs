using System.Text;
using WaterCoolerCLI.Api;
using WaterCoolerCLI.Common;
using WaterCoolerCLI.Models;
using WaterCoolerCLI.Invoke;

public class Program
{
    private static readonly string[] AvailableCommands = {
        "help", "get-fan-mode", "set-fan-mode", "get-pump-mode", "set-pump-mode",
        "get-speeds", "get-fan-curve", "get-pump-curve", "set-fan-curve",
        "set-pump-curve", "plot-curves", "monitor", "service", "clear", "quit", "exit"
    };

    private static readonly Dictionary<string, string[]> CommandOptions = new()
    {
        ["set-fan-mode"] = new[] { "Balanced", "Customized", "Default", "FixedRPM", "Turbo", "Performance", "Quiet", "ZeroRPM" },
        ["set-pump-mode"] = new[] { "Balanced", "Customized", "Default", "FixedRPM", "Turbo", "Performance", "Quiet", "ZeroRPM" },
        ["monitor"] = new[] { "2000" },
        ["service"] = new[] { "500" }
    };

    private static List<string> commandHistory = new();
    private static int historyIndex = -1;

    // Constants for strings
    private const string InitializationMessage = "Device initialized. Type 'help' for commands.";
    private const string DeviceConnectedMessage = "Connected to device: {0} (VID: {1:X4}, PID: {2:X4})";
    private const string FailedToConnectMessage = "Failed to connect to the specified device.";
    private const string InvalidVidPidFormatMessage = "Invalid VID or PID format. Please provide hexadecimal values.";
    private const string NoDevicesFoundMessage = "No supported devices found.";
    private const string UsageSetFanMode = "Usage: set-fan-mode <mode>";
    private const string UsageSetPumpMode = "Usage: set-pump-mode <mode>";
    private const string UsageSetFanCurve = "Usage: set-fan-curve <points> (e.g., 0:1000,30:1500,50:2000,65:2500)";
    private const string UsageSetPumpCurve = "Usage: set-pump-curve <points> (e.g., 0:1000,30:1500,50:2000,65:2500)";
    private const string InvalidIntervalMessage = "Invalid interval. Using default 2000 milliseconds.";
    private const string UnknownCommandMessage = "Unknown command. Type 'help' for available commands.";
    private const string AnErrorOccurredMessage = "An error occurred: {0}";
    private const string HelpAvailableCommands = "Available commands:";
    private const string HelpHelp = "  help                            - Show this help";
    private const string HelpClear = "  clear                           - Clear the console";
    private const string HelpQuitExit = "  quit/exit                       - Exit the program";
    private const string HelpFanCommands = "Fan commands:";
    private const string HelpGetFanMode = "  get-fan-mode                    - Get current fan mode";
    private const string HelpSetFanMode = "  set-fan-mode <mode>             - Set fan mode (Balanced, Customized, Default, FixedRPM, Turbo, Performance, Quiet, ZeroRPM)";
    private const string HelpGetFanCurve = "  get-fan-curve                   - Get current fan curve";
    private const string HelpSetFanCurve = "  set-fan-curve <points>          - Set fan curve (temp:speed,temp:speed,... e.g., 0:1000,30:1500,50:2000,65:2500)";
    private const string HelpPumpCommands = "Pump commands:";
    private const string HelpGetPumpMode = "  get-pump-mode                   - Get current pump mode";
    private const string HelpSetPumpMode = "  set-pump-mode <mode>            - Set pump mode (Balanced, Customized, Default, FixedRPM, Turbo, Performance, Quiet, ZeroRPM)";
    private const string HelpGetPumpCurve = "  get-pump-curve                  - Get current pump curve";
    private const string HelpSetPumpCurve = "  set-pump-curve <points>         - Set pump curve (temp:speed,temp:speed,... e.g., 0:1000,30:1500,50:2000,65:2500)";
    private const string HelpMonitoringCommands = "Monitoring commands:";
    private const string HelpServiceCommands = "Service commands:";
    private const string HelpGetSpeeds = "  get-speeds                      - Get current fan and pump speeds";
    private const string HelpMonitor = "  monitor [milliseconds]          - Start real-time monitoring of metrics (press 'q' to stop), default refresh 2000 milliseconds";
    private const string HelpService = "  service [milliseconds]          - Start service mode, sends telemetry to cooler (press 'q' to stop), default refresh 500 milliseconds";
    private const string HelpMiscCommands = "Misc commands:";
    private const string HelpMiscClear = "  clear                           - Clear the console screen";
    private const string HelpPlotCurves = "  plot-curves                     - Plot current fan and pump curves graphically";
    private const string MonitoringStartingMessage = "Starting real-time monitoring... Press 'q' to stop.\n";
    private const string MonitoringStoppedMessage = "Monitoring stopped.";
    private const string ServiceStartingMessage = "Starting service... Press 'q' to stop.\n";
    private const string ServiceStoppedMessage = "Service stopped.";
    private const string TimeFormat = "HH:mm:ss";
    private const string TimePrefix = "[{0}] ";
    private const string CpuTempFormat = " CPU Temp: {0}" + DegreeCelsius + " | ";
    private const string Prompt = "> ";
    private const string QuitCommand = "quit";
    private const string ExitCommand = "exit";
    private const string ClearCommand = "clear";
    private const string VidPrefix = "VID_";
    private const string PidPrefix = "PID_";
    private const string FailedToRetrieveCurveDataMessage = "Failed to retrieve curve data for plotting.";
    private const string InvalidCurveDataMessage = "Invalid curve data for plotting.";
    private const string FanPumpCurvesPlotTitle = "Fan and Pump Curves Plot:";
    private const string PlotLegend = "Legend: * Fan curve, + Pump curve, O Fan points, o Pump points, x Intersection";
    private const string CurrentFanCurvePointsTitle = "\nCurrent Fan Curve Points:";
    private const string CurrentPumpCurvePointsTitle = "\nCurrent Pump Curve Points:";
    private const string DegreeCelsius = "°C";
    private const string RPMUnit = " RPM";
    private const string GetDeviceModelNameFailMessage = "GetDeviceModelName fail";

    // Constants for numbers
    private const int DefaultMonitorIntervalMs = 2000;
    private const int DefaultServiceIntervalMs = 500;
    private const int HistoryLimit = 100;
    private const int CommandWidth = 30;
    private const int PromptLength = 2;
    private const int SleepDelayMs = 100;
    private const int SpecialPid = 31313;
    private const uint SpecialReportId = 65282u;
    private const uint DefaultReportId = 0u;
    private const int VidPidHexLength = 4;
    private const int PlotHeight = 20;
    private const int PlotWidth = 70;
    private const int PlotLabelWidth = 6;
    private const int NumXTicks = 6;
    private const int YLabelInterval = 4;
    private const int ShortLabelLength = 3;

    // Constants for arrays
    private static readonly string[] DefaultDevices = { "VID_1044&PID_7A51", "VID_1044&PID_7A4D", "VID_0414&PID_7A5E" };

    public static async Task Main(string[] args)
    {
        DeviceInfo deviceInfo = null;

        try
        {
            if (!TryConnect(args, ref deviceInfo))
            {
                Console.WriteLine(FailedToConnectMessage);
                return;
            }

            // If connected, initialize and start command loop or execute single command
            if (args.Length > 0)
            {
                // Execute single command from arguments and exit
                string[] commandArgs = args.ToArray();
                await ExecuteCommand(deviceInfo, commandArgs, isArg: true);
            }
            else
            {
                Console.WriteLine(InitializationMessage);

                // Start command loop
                while (true)
                {
                    Console.Write(Prompt);
                    string input = ReadLineWithTabCompletion(AvailableCommands);
                    if (string.IsNullOrEmpty(input))
                        continue;

                    string trimmedInput = input.Trim();
                    if (trimmedInput.Length == 0)
                        continue;

                    if (trimmedInput.ToLower() != QuitCommand && trimmedInput.ToLower() != ExitCommand)
                    {
                        AddToCommandHistory(input);
                    }

                    string lowerInput = trimmedInput.ToLower();
                    string[] parts = lowerInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    await ExecuteCommand(deviceInfo, parts);
                    if (parts[0] == QuitCommand || parts[0] == ExitCommand)
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(AnErrorOccurredMessage, ex.Message));
        }
    }

    /// <summary>
    /// Attempts to connect to a device based on command line arguments.
    /// </summary>
    private static bool TryConnect(string[] args, ref DeviceInfo deviceInfo)
    {
        bool connected = false;
        int vid = 0;
        int pid = 0;

        // Try default devices
        foreach (string device in DefaultDevices)
        {
            uint v = uint.Parse(device.AsSpan(device.IndexOf(VidPrefix) + VidPrefix.Length, VidPidHexLength), System.Globalization.NumberStyles.HexNumber);
            uint p = uint.Parse(device.AsSpan(device.IndexOf(PidPrefix) + PidPrefix.Length, VidPidHexLength), System.Globalization.NumberStyles.HexNumber);
            vid = (int)v;
            pid = (int)p;
            connected = ConnectDevice(vid, pid, ref deviceInfo);
            if (connected)
            {
                Console.WriteLine(string.Format(DeviceConnectedMessage, deviceInfo.ModelName, vid, pid));
                break;
            }
        }
        if (!connected)
        {
            Console.WriteLine(NoDevicesFoundMessage);
            return false;
        }

        return connected;
    }

    /// <summary>
    /// Executes a command based on the provided arguments.
    /// </summary>
    private static async Task ExecuteCommand(DeviceInfo device, string[] parts, bool isArg = false)
    {
        if (parts.Length == 0)
            return;

        string command = parts[0];

        switch (command)
        {
            case "help":
                PrintHelp();
                break;
            case "get-fan-mode":
                PumpFanHandler.GetFanMode(device);
                break;
            case "set-fan-mode":
                if (parts.Length < 2)
                {
                    Console.WriteLine(UsageSetFanMode);
                    break;
                }
                PumpFanHandler.SetFanMode(device, parts[1]);
                break;
            case "get-pump-mode":
                PumpFanHandler.GetPumpMode(device);
                break;
            case "set-pump-mode":
                if (parts.Length < 2)
                {
                    Console.WriteLine(UsageSetPumpMode);
                    break;
                }
                PumpFanHandler.SetPumpMode(device, parts[1]);
                break;
            case "get-speeds":
                PumpFanHandler.GetSpeeds(device);
                break;
            case "get-fan-curve":
                PumpFanHandler.GetFanCurve(device);
                break;
            case "get-pump-curve":
                PumpFanHandler.GetPumpCurve(device);
                break;
            case "set-fan-curve":
                if (parts.Length < 2)
                {
                    Console.WriteLine(UsageSetFanCurve);
                    break;
                }
                PumpFanHandler.SetFanCurve(device, parts[1]);
                break;
            case "set-pump-curve":
                if (parts.Length < 2)
                {
                    Console.WriteLine(UsageSetPumpCurve);
                    break;
                }
                PumpFanHandler.SetPumpCurve(device, parts[1]);
                break;
            case "plot-curves":
                PlotCurves(device);
                break;
            case "monitor":
                int monitorIntervalMs = DefaultMonitorIntervalMs; // default 2 seconds
                if (parts.Length > 1)
                {
                    if (int.TryParse(parts[1], out int ms) && ms > 0)
                    {
                        monitorIntervalMs = ms;
                    }
                    else
                    {
                        Console.WriteLine(InvalidIntervalMessage);
                    }
                }
                await StartMonitoring(device, monitorIntervalMs, isArg);
                break;
            case "service":
                int serviceIntervalMs = DefaultServiceIntervalMs; // default 500 millisenconds
                if (parts.Length > 1)
                {
                    if (int.TryParse(parts[1], out int ms) && ms > 0)
                    {
                        serviceIntervalMs = ms;
                    }
                    else
                    {
                        Console.WriteLine(InvalidIntervalMessage);
                    }
                }
                await StartService(device, serviceIntervalMs, isArg);
                break;
            case "clear":
                Console.Clear();
                break;
            case "quit":
            case "exit":
                // In command execution, just return (will exit if single command)
                break;
            default:
                Console.WriteLine(UnknownCommandMessage);
                break;
        }
    }

    /// <summary>
    /// Prints the help information for available commands.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine(HelpAvailableCommands);
        Console.WriteLine(HelpHelp);
        Console.WriteLine(HelpClear);
        Console.WriteLine(HelpQuitExit);
        Console.WriteLine();
        Console.WriteLine(HelpFanCommands);
        Console.WriteLine(HelpGetFanMode);
        Console.WriteLine(HelpSetFanMode);
        Console.WriteLine(HelpGetFanCurve);
        Console.WriteLine(HelpSetFanCurve);
        Console.WriteLine();
        Console.WriteLine(HelpPumpCommands);
        Console.WriteLine(HelpGetPumpMode);
        Console.WriteLine(HelpSetPumpMode);
        Console.WriteLine(HelpGetPumpCurve);
        Console.WriteLine(HelpSetPumpCurve);
        Console.WriteLine();
        Console.WriteLine(HelpMonitoringCommands);
        Console.WriteLine(HelpGetSpeeds);
        Console.WriteLine(HelpMonitor);
        Console.WriteLine(HelpPlotCurves);
        Console.WriteLine();
        Console.WriteLine(HelpServiceCommands);
        Console.WriteLine(HelpService);
        Console.WriteLine();
        Console.WriteLine(HelpMiscCommands);
        Console.WriteLine(HelpMiscClear);
    }

    /// <summary>
    /// Connects to a specific device and populates the device info.
    /// </summary>
    private static bool ConnectDevice(int vid, int pid, ref DeviceInfo deviceInfo)
    {
        var gDriverInfo = new GDriverInfo((uint)vid, (uint)pid, (pid == SpecialPid) ? SpecialReportId : DefaultReportId);

        HidDriver hidDriver = null;

        if (!CoolerApi.Connect(vid, pid, ref gDriverInfo, ref hidDriver))
        {
            return false;
        }

        string curDeviceModelName = string.Empty;
        if (!DeviceApi.GetDeviceModelName((DevicePId)pid, hidDriver, ref curDeviceModelName))
        {
            LogUtil.Error("Program", GetDeviceModelNameFailMessage);
            return false;
        }

        deviceInfo ??= new DeviceInfo();

        // Populate deviceInfo
        deviceInfo.VID = vid;
        deviceInfo.PID = pid;
        deviceInfo.DeviceType = 0;
        deviceInfo.ModelName = curDeviceModelName;
        deviceInfo.HidDriver = hidDriver;
        deviceInfo.gDriverInfo = gDriverInfo;

        return true;
    }

    /// <summary>
    /// Reads a line from the console with tab completion support.
    /// </summary>
    private static string ReadLineWithTabCompletion(string[] commands)
    {
        StringBuilder input = new StringBuilder();
        int cursor = 0;
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input.ToString();
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (cursor > 0)
                {
                    input.Remove(cursor - 1, 1);
                    cursor--;
                    RedrawInput(input.ToString(), cursor);
                }
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                if (cursor > 0)
                {
                    cursor--;
                    Console.CursorLeft--;
                }
            }
            else if (key.Key == ConsoleKey.RightArrow)
            {
                if (cursor < input.Length)
                {
                    cursor++;
                    Console.CursorLeft++;
                }
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                if (commandHistory.Count > 0)
                {
                    historyIndex = Math.Min(historyIndex + 1, commandHistory.Count - 1);
                    string hist = commandHistory[commandHistory.Count - 1 - historyIndex];
                    RedrawInput(hist, hist.Length);
                    input = new StringBuilder(hist);
                    cursor = hist.Length;
                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (historyIndex > 0)
                {
                    historyIndex--;
                    string hist = commandHistory[commandHistory.Count - 1 - historyIndex];
                    RedrawInput(hist, hist.Length);
                    input = new StringBuilder(hist);
                    cursor = hist.Length;
                }
                else
                {
                    historyIndex = -1;
                    RedrawInput("", 0);
                    input = new StringBuilder();
                    cursor = 0;
                }
            }
            else if (key.Key == ConsoleKey.Tab)
            {
                string prefix = input.ToString().Substring(0, cursor);
                var parts = prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && CommandOptions.ContainsKey(parts[0].ToLower()))
                {
                    string cmd = parts[0].ToLower();
                    string argPrefix = prefix.Length > parts[0].Length + 1 ? prefix.Substring(parts[0].Length + 1) : "";
                    var opts = CommandOptions[cmd];
                    var matches = opts.Where(o => o.StartsWith(argPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matches.Length == 1)
                    {
                        string completion = matches[0].Substring(argPrefix.Length);
                        input.Insert(cursor, completion);
                        Console.Write(completion);
                        cursor += completion.Length;
                    }
                    else if (matches.Length > 1)
                    {
                        string common = GetCommonPrefix(matches);
                        if (common.Length > argPrefix.Length)
                        {
                            string add = common.Substring(argPrefix.Length);
                            input.Insert(cursor, add);
                            Console.Write(add);
                            cursor += add.Length;
                        }
                        else
                        {
                            Console.Beep();
                        }
                    }
                }
                else
                {
                    // original command completion
                    var matches = commands.Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matches.Length == 1)
                    {
                        string completion = matches[0].Substring(prefix.Length);
                        input.Insert(cursor, completion);
                        Console.Write(completion);
                        cursor += completion.Length;
                    }
                    else if (matches.Length > 1)
                    {
                        string commonPrefix = GetCommonPrefix(matches);
                        if (commonPrefix.Length > prefix.Length)
                        {
                            string add = commonPrefix.Substring(prefix.Length);
                            input.Insert(cursor, add);
                            Console.Write(add);
                            cursor += add.Length;
                        }
                        else
                        {
                            Console.Beep();
                        }
                    }
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                input.Insert(cursor, key.KeyChar);
                Console.Write(key.KeyChar);
                cursor++;
                if (cursor < input.Length)
                {
                    string rest = input.ToString().Substring(cursor);
                    Console.Write(rest);
                    Console.CursorLeft -= rest.Length;
                }
            }
        }
    }

    /// <summary>
    /// Redraws the input line on the console.
    /// </summary>
    private static void RedrawInput(string line, int cursor)
    {
        int promptLength = PromptLength;
        Console.CursorLeft = 0;
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.CursorLeft = 0;
        Console.Write(Prompt + line);
        Console.CursorLeft = promptLength + cursor;
    }

    /// <summary>
    /// Gets the common prefix of an array of strings.
    /// </summary>
    private static string GetCommonPrefix(string[] strings)
    {
        if (strings.Length == 0) return "";
        string prefix = strings[0];
        for (int i = 1; i < strings.Length; i++)
        {
            while (!strings[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = prefix.Substring(0, prefix.Length - 1);
                if (prefix == "") return "";
            }
        }
        return prefix;
    }

    /// <summary>
    /// Starts real-time monitoring of device metrics.
    /// </summary>
    private static async Task StartMonitoring(DeviceInfo device, int intervalMs, bool isArg = false)
    {
        if (!isArg)
        {
            Console.WriteLine(MonitoringStartingMessage);
        }

        using (var cts = new CancellationTokenSource())
        {
            var token = cts.Token;

            // Start monitoring task
            var monitoringTask = Task.Run(async () =>
            {
                var canReadCpuTemperature = CpuTempHandler.CanReadCpuTemperature;

                while (!token.IsCancellationRequested)
                {
                    Console.Write(string.Format(TimePrefix, DateTime.Now.ToString(TimeFormat)));

                    if (canReadCpuTemperature)
                    {
                        Console.Write(CpuTempFormat, CpuTempHandler.GetCpuTemperature());
                    }

                    PumpFanHandler.PrintSpeedMetrics(device);
                    Console.WriteLine(); // Blank line for readability

                    try
                    {
                        await Task.Delay(intervalMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when canceled
                        break;
                    }
                }
            }, token);

            // Monitor for user input to stop
            while (!cts.IsCancellationRequested)
            {
                if (!isArg && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        cts.Cancel();
                        break;
                    }
                }
                await Task.Delay(SleepDelayMs); // Small delay to avoid busy-waiting
            }

            // Wait for monitoring task to complete
            monitoringTask.Wait();
            Console.WriteLine(MonitoringStoppedMessage);
        }
    }

    /// <summary>
    /// Starts service.
    /// </summary>
    private static async Task StartService(DeviceInfo device, int intervalMs, bool isArg = false)
    {
        if (!isArg)
        {
            Console.WriteLine(ServiceStartingMessage);
        }
        if (!CpuTempHandler.CanReadCpuTemperature)
        {
            LogUtil.Error("Program", "Error reading CPU temperature: Unable to read the temperature sensors!");
            Console.WriteLine(ServiceStoppedMessage);
            return;
        }

        //TODO get cpu name
        CoolerDataApi.SendCpuName(device.HidDriver, "AMD Ryzen 9 7950X3D");

        using (var cts = new CancellationTokenSource())
        {
            var token = cts.Token;

            // Start service task
            var ServiceTask = Task.Run(async () =>
            {
                var coolerData = new CoolerData();

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var temp = CpuTempHandler.GetCpuTemperature();
                        if (temp.PackageTempC is null) { break; }
                        ;
                        coolerData.CpuTemperature = (byte)Math.Clamp(Math.Round(temp.PackageTempC.Value), 0, 255);

                        CoolerDataApi.SendCoolerData(device.HidDriver, coolerData);
                        await Task.Delay(intervalMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when canceled
                        break;
                    }
                }
            }, token);

            // Service for user input to stop
            while (!cts.IsCancellationRequested)
            {
                if (!isArg && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        cts.Cancel();
                        break;
                    }
                }
                await Task.Delay(SleepDelayMs); // Small delay to avoid busy-waiting
            }

            // Wait for monitoring task to complete
            ServiceTask.Wait();
            Console.WriteLine(ServiceStoppedMessage);
        }
    }

    /// <summary>
    /// Adds a command to the history list.
    /// </summary>
    private static void AddToCommandHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || commandHistory.Contains(command))
            return;

        commandHistory.Add(command);
        // Limit history size
        if (commandHistory.Count > HistoryLimit)
        {
            commandHistory.RemoveAt(0);
        }
        historyIndex = -1; // Reset index
    }

    private static void PlotCurves(DeviceInfo device)
    {
        var fanPoints = PumpFanHandler.GetFanCurvePoints(device);
        var pumpPoints = PumpFanHandler.GetPumpCurvePoints(device);

        if (fanPoints.Count == 0 || pumpPoints.Count == 0)
        {
            Console.WriteLine(FailedToRetrieveCurveDataMessage);
            return;
        }

        // Find min/max
        var temps = fanPoints.Select(p => p.temp).Concat(pumpPoints.Select(p => p.temp)).Distinct().OrderBy(t => t).ToList();
        int minTemp = temps.Min();
        int maxTemp = temps.Max();
        var speeds = fanPoints.Select(p => p.speed).Concat(pumpPoints.Select(p => p.speed)).ToList();
        int minSpeed = speeds.Min();
        int maxSpeed = speeds.Max();

        if (maxTemp == minTemp || maxSpeed == minSpeed)
        {
            Console.WriteLine(InvalidCurveDataMessage);
            return;
        }

        char[,] graph = new char[PlotHeight, PlotWidth];
        for (int y = 0; y < PlotHeight; y++)
            for (int x = 0; x < PlotWidth; x++)
                graph[y, x] = ' ';

        // Axes
        for (int y = 0; y < PlotHeight; y++) graph[y, 0] = '|';
        for (int x = 0; x < PlotWidth; x++) graph[PlotHeight - 1, x] = '-';
        graph[PlotHeight - 1, 0] = '+';

        double tempScale = (double)(PlotWidth - 1) / (maxTemp - minTemp);
        double speedScale = (double)(PlotHeight - 1) / (maxSpeed - minSpeed);

        // Plot fan curve lines
        char fanLineChar = '*';
        for (int i = 0; i < fanPoints.Count - 1; i++)
        {
            var p1 = fanPoints[i];
            var p2 = fanPoints[i + 1];
            int x1 = Math.Max(0, Math.Min(PlotWidth - 1, (int)((p1.temp - minTemp) * tempScale)));
            int y1 = Math.Max(0, Math.Min(PlotHeight - 1, PlotHeight - 1 - (int)((p1.speed - minSpeed) * speedScale)));
            int x2 = Math.Max(0, Math.Min(PlotWidth - 1, (int)((p2.temp - minTemp) * tempScale)));
            int y2 = Math.Max(0, Math.Min(PlotHeight - 1, PlotHeight - 1 - (int)((p2.speed - minSpeed) * speedScale)));

            int dx = x2 - x1;
            int dy = y2 - y1;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps == 0) continue;
            double xInc = (double)dx / steps;
            double yInc = (double)dy / steps;
            double cx = x1;
            double cy = y1;
            for (int s = 0; s <= steps; s++)
            {
                int ix = Math.Max(0, Math.Min(PlotWidth - 1, (int)cx));
                int iy = Math.Max(0, Math.Min(PlotHeight - 1, (int)cy));
                if (graph[iy, ix] == ' ') graph[iy, ix] = fanLineChar;
                cx += xInc;
                cy += yInc;
            }
        }

        // Plot pump curve lines
        char pumpLineChar = '+';
        for (int i = 0; i < pumpPoints.Count - 1; i++)
        {
            var p1 = pumpPoints[i];
            var p2 = pumpPoints[i + 1];
            int x1 = Math.Max(0, Math.Min(PlotWidth - 1, (int)((p1.temp - minTemp) * tempScale)));
            int y1 = Math.Max(0, Math.Min(PlotHeight - 1, PlotHeight - 1 - (int)((p1.speed - minSpeed) * speedScale)));
            int x2 = Math.Max(0, Math.Min(PlotWidth - 1, (int)((p2.temp - minTemp) * tempScale)));
            int y2 = Math.Max(0, Math.Min(PlotHeight - 1, PlotHeight - 1 - (int)((p2.speed - minSpeed) * speedScale)));

            int dx = x2 - x1;
            int dy = y2 - y1;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps == 0) continue;
            double xInc = (double)dx / steps;
            double yInc = (double)dy / steps;
            double cx = x1;
            double cy = y1;
            for (int s = 0; s <= steps; s++)
            {
                int ix = Math.Max(0, Math.Min(PlotWidth - 1, (int)cx));
                int iy = Math.Max(0, Math.Min(PlotHeight - 1, (int)cy));
                if (graph[iy, ix] == ' ')
                    graph[iy, ix] = pumpLineChar;
                else if (graph[iy, ix] == fanLineChar)
                    graph[iy, ix] = 'x'; // intersection
                cx += xInc;
                cy += yInc;
            }
        }

        // Plot points
        foreach (var p in fanPoints)
        {
            int x = Math.Max(0, Math.Min(PlotWidth - 1, (int)((p.temp - minTemp) * tempScale)));
            int y = Math.Max(0, Math.Min(PlotHeight - 1, PlotHeight - 1 - (int)((p.speed - minSpeed) * speedScale)));
            graph[y, x] = 'O';
        }
        foreach (var p in pumpPoints)
        {
            int x = Math.Max(0, Math.Min(PlotWidth - 1, (int)((p.temp - minTemp) * tempScale)));
            int y = Math.Max(0, Math.Min(PlotHeight - 1, PlotHeight - 1 - (int)((p.speed - minSpeed) * speedScale)));
            if (graph[y, x] == ' ' || graph[y, x] == pumpLineChar)
                graph[y, x] = 'o';
            else
                graph[y, x] = 'x';
        }

        // Print header and legend
        Console.WriteLine(FanPumpCurvesPlotTitle);
        Console.WriteLine(PlotLegend);
        Console.WriteLine(); // Blank line for separation

        // Print graph rows with y-axis scale labels (RPM: max at top (y=0), 0 at bottom (y=height-1))
        for (int y = 0; y < PlotHeight; y++)
        {
            // Calculate precise RPM for this row: max at top, min at bottom
            double fractionFromTop = (double)y / (PlotHeight - 1);
            double labelSpeed = maxSpeed - fractionFromTop * (maxSpeed - minSpeed);

            string yLabel = "     |"; // Default: 5 spaces + "|"
            if (y % YLabelInterval == 0 || y == PlotHeight - 1) // Every YLabelInterval rows + bottom
            {
                yLabel = $"{(int)labelSpeed,4} |"; // Right-align 4 digits + " |"
            }
            Console.Write(yLabel);
            for (int x = 0; x < PlotWidth; x++)
            {
                Console.Write(graph[y, x]);
            }
            Console.WriteLine();
        }

        // Print bottom axis line
        Console.WriteLine(new string(' ', PlotLabelWidth) + new string('-', PlotWidth));

        // Print x-axis scale with 6 ticks (Temp: min to max, e.g., 0,13,26,39,52,65)
        Console.WriteLine(); // New line
        var xAxisBuilder = new StringBuilder(new string(' ', PlotLabelWidth)); // Aligned start
        double xTickStep = (double)(maxTemp - minTemp) / (NumXTicks - 1);
        for (int i = 0; i < NumXTicks; i++)
        {
            int tickTemp = minTemp + (int)(i * xTickStep);
            int pos = Math.Max(0, Math.Min(PlotWidth - ShortLabelLength, (int)((tickTemp - minTemp) * tempScale))); // Column, reserve 3 for label
            int currentLen = xAxisBuilder.Length - PlotLabelWidth;
            int padsNeeded = Math.Max(0, pos - currentLen);
            xAxisBuilder.Append(new string(' ', padsNeeded));

            // Fixed 3-char label: e.g., "0° ", "13° ", "26° ", "39° ", "52° ", "65° "
            string shortLabel = $"{tickTemp}° ";
            if (shortLabel.Length > ShortLabelLength) shortLabel = shortLabel.Substring(0, ShortLabelLength); // Trim if needed
            xAxisBuilder.Append(shortLabel);
        }
        // Pad to full width
        while (xAxisBuilder.Length < PlotLabelWidth + PlotWidth)
            xAxisBuilder.Append(' ');
        // Trim exactly
        string xAxisLine = xAxisBuilder.ToString().Substring(0, PlotLabelWidth + PlotWidth);
        Console.WriteLine(xAxisLine);

        // Print current curve values below
        Console.WriteLine(CurrentFanCurvePointsTitle);
        foreach (var p in fanPoints.OrderBy(p => p.temp))
        {
            Console.WriteLine($"  {p.temp,2}{DegreeCelsius}: {p.speed,4}{RPMUnit}");
        }
        Console.WriteLine(CurrentPumpCurvePointsTitle);
        foreach (var p in pumpPoints.OrderBy(p => p.temp))
        {
            Console.WriteLine($"  {p.temp,2}{DegreeCelsius}: {p.speed,4}{RPMUnit}");
        }
    }
}
