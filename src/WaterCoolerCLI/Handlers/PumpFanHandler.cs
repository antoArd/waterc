using WaterCoolerCLI.Api;
using WaterCoolerCLI.Models;

public static class PumpFanHandler
{
    // Constants for strings
    private const string FanSpeedFormat = "Fan: {0} RPM | ";
    private const string PumpSpeedFormat = "Pump: {0} RPM";
    private const string FailedGetSpeeds = "Failed to get speeds.";
    private const string FanPumpNA = "Fan Speed: N/A\nPump Speed: N/A";
    private const string ModeFormat = "{0} Mode: {1}";
    private const string FailedGetModeFormat = "Failed to get {0} mode.";
    private const string InvalidModeMessage = "Invalid mode. Available modes: Balanced, Customized, Default, FixedRPM, Turbo, Performance, Quiet, ZeroRPM";
    private const string ModeSetFormat = "{0} mode set to {1}.";
    private const string FailedSetModeFormat = "Failed to set {0} mode.";
    private const string CurveHeaderFormat = "{0} Curve:";
    private const string TempSpeedFormat = "{0} °C: {1} RPM";
    private const string FailedGetCurveFormat = "Failed to get {0} curve.";
    private const string InvalidPointsMessage = "Invalid points. Need exactly 4 temp:speed pairs (e.g., 0:1000,30:1500,50:2000,65:2500).";
    private const string FailedSetModeToCustomizedFormat = "Failed to set {0} mode to customized.";
    private const string FailedSetCurveFormat = "Failed to set {0} curve.";
    private const string CurveSetAndSavedFormat = "{0} curve set and saved.";
    private const string FailedGetModes = "Failed to get modes.";

    // Constants for numbers
    private const int NumPoints = 4;
    private const int DefaultSpeed = 0;

    /// <summary>
    /// Gets the current fan mode.
    /// </summary>
    public static void GetFanMode(DeviceInfo device)
    {
        GetMode(device, SpeedType.Fan);
    }

    /// <summary>
    /// Sets the fan mode.
    /// </summary>
    public static void SetFanMode(DeviceInfo device, string modeStr)
    {
        SetMode(device, SpeedType.Fan, modeStr);
    }

    /// <summary>
    /// Gets the current pump mode.
    /// </summary>
    public static void GetPumpMode(DeviceInfo device)
    {
        GetMode(device, SpeedType.Pump);
    }

    /// <summary>
    /// Sets the pump mode.
    /// </summary>
    public static void SetPumpMode(DeviceInfo device, string modeStr)
    {
        SetMode(device, SpeedType.Pump, modeStr);
    }

    /// <summary>
    /// Gets the current speeds of fan and pump.
    /// </summary>
    public static void GetSpeeds(DeviceInfo device)
    {
        int fanSpeed = DefaultSpeed;
        int pumpSpeed = DefaultSpeed;
        if (DeviceApi.GetRPMSpeed(device.HidDriver, ref fanSpeed, ref pumpSpeed))
        {
            Console.WriteLine(string.Format(FanSpeedFormat, fanSpeed));
            Console.Write(string.Format(PumpSpeedFormat, pumpSpeed));
        }
        else
        {
            Console.WriteLine(FailedGetSpeeds);
        }
    }

    /// <summary>
    /// Gets the current fan curve.
    /// </summary>
    public static void GetFanCurve(DeviceInfo device)
    {
        GetCurve(device, SpeedType.Fan);
    }

    /// <summary>
    /// Gets the current pump curve.
    /// </summary>
    public static void GetPumpCurve(DeviceInfo device)
    {
        GetCurve(device, SpeedType.Pump);
    }

    /// <summary>
    /// Sets the fan curve.
    /// </summary>
    public static void SetFanCurve(DeviceInfo device, string pointsStr)
    {
        SetCurve(device, SpeedType.Fan, pointsStr);
    }

    /// <summary>
    /// Sets the pump curve.
    /// </summary>
    public static void SetPumpCurve(DeviceInfo device, string pointsStr)
    {
        SetCurve(device, SpeedType.Pump, pointsStr);
    }

    /// <summary>
    /// Prints the speed metrics for monitoring.
    /// </summary>
    public static void PrintSpeedMetrics(DeviceInfo device)
    {
        // Fan and Pump Speeds
        int fanSpeed = DefaultSpeed;
        int pumpSpeed = DefaultSpeed;
        if (DeviceApi.GetRPMSpeed(device.HidDriver, ref fanSpeed, ref pumpSpeed))
        {
            Console.Write(string.Format(FanSpeedFormat, fanSpeed));
            Console.Write(string.Format(PumpSpeedFormat, pumpSpeed));
        }
        else
        {
            Console.WriteLine(FanPumpNA);
        }
    }

    private static void GetMode(DeviceInfo device, SpeedType speedType)
    {
        SpeedMode fanMode = SpeedMode.Balanced;
        SpeedMode pumpMode = SpeedMode.Balanced;
        if (DeviceApi.GetMode(device.HidDriver, ref fanMode, ref pumpMode))
        {
            SpeedMode mode = speedType == SpeedType.Fan ? fanMode : pumpMode;
            Console.WriteLine(string.Format(ModeFormat, speedType, mode));
        }
        else
        {
            Console.WriteLine(string.Format(FailedGetModeFormat, speedType.ToString().ToLower()));
        }
    }

    private static void SetMode(DeviceInfo device, SpeedType speedType, string modeStr)
    {
        if (!Enum.TryParse<SpeedMode>(modeStr, true, out SpeedMode mode))
        {
            Console.WriteLine(InvalidModeMessage);
            return;
        }

        if (DeviceApi.SetSpeedMode(device.HidDriver, speedType, mode))
        {
            DeviceApi.Save(device.HidDriver);
            Console.WriteLine(string.Format(ModeSetFormat, speedType, mode));
        }
        else
        {
            Console.WriteLine(string.Format(FailedSetModeFormat, speedType.ToString().ToLower()));
        }
    }

    private static void GetCurve(DeviceInfo device, SpeedType speedType)
    {
        SpeedMode fanMode = SpeedMode.Balanced;
        SpeedMode pumpMode = SpeedMode.Balanced;
        if (!DeviceApi.GetMode(device.HidDriver, ref fanMode, ref pumpMode))
        {
            Console.WriteLine(FailedGetModes);
            return;
        }
        SpeedMode mode = speedType == SpeedType.Fan ? fanMode : pumpMode;
        SpeedCurve speedCurve = new SpeedCurve(speedType);
        if (!DeviceApi.GetSpeedCurve(device, mode, ref speedCurve))
        {
            Console.WriteLine(string.Format(FailedGetCurveFormat, speedType.ToString().ToLower()));
            return;
        }
        Console.WriteLine(string.Format(CurveHeaderFormat, speedType));
        foreach (var ts in speedCurve.TemperatureSpeeds)
        {
            Console.WriteLine(string.Format(TempSpeedFormat, ts.Temperature, ts.Speed));
        }
    }

    private static void SetCurve(DeviceInfo device, SpeedType speedType, string pointsStr)
    {
        var points = ParsePoints(pointsStr);
        if (points == null || points.Length != NumPoints)
        {
            Console.WriteLine(InvalidPointsMessage);
            return;
        }
        SpeedCurve speedCurve = new SpeedCurve(speedType);
        for (int i = 0; i < NumPoints; i++)
        {
            speedCurve.TemperatureSpeeds[i].Temperature = points[i].Item1;
            speedCurve.TemperatureSpeeds[i].Speed = points[i].Item2;
        }
        // Set mode to customized
        if (!DeviceApi.SetSpeedMode(device.HidDriver, speedType, SpeedMode.Customized))
        {
            Console.WriteLine(string.Format(FailedSetModeToCustomizedFormat, speedType.ToString().ToLower()));
            return;
        }
        if (!DeviceApi.SetSpeedCurve(device.HidDriver, speedCurve))
        {
            Console.WriteLine(string.Format(FailedSetCurveFormat, speedType.ToString().ToLower()));
            return;
        }
        DeviceApi.Save(device.HidDriver);
        Console.WriteLine(string.Format(CurveSetAndSavedFormat, speedType));
    }

    private static (int, int)[] ParsePoints(string str)
    {
        var parts = str.Split(',');
        if (parts.Length != NumPoints) return null;
        var result = new (int, int)[NumPoints];
        for (int i = 0; i < NumPoints; i++)
        {
            var sub = parts[i].Split(':');
            if (sub.Length != 2 || !int.TryParse(sub[0], out int temp) || !int.TryParse(sub[1], out int speed))
                return null;
            result[i] = (temp, speed);
        }
        return result;
    }

    public static List<(int temp, int speed)> GetFanCurvePoints(DeviceInfo device)
    {
        return GetCurvePoints(device, SpeedType.Fan);
    }

    public static List<(int temp, int speed)> GetPumpCurvePoints(DeviceInfo device)
    {
        return GetCurvePoints(device, SpeedType.Pump);
    }

    private static List<(int temp, int speed)> GetCurvePoints(DeviceInfo device, SpeedType speedType)
    {
        SpeedMode fanMode = SpeedMode.Balanced;
        SpeedMode pumpMode = SpeedMode.Balanced;
        if (!DeviceApi.GetMode(device.HidDriver, ref fanMode, ref pumpMode))
        {
            return new List<(int, int)>();
        }
        SpeedMode mode = speedType == SpeedType.Fan ? fanMode : pumpMode;
        SpeedCurve speedCurve = new SpeedCurve(speedType);
        if (!DeviceApi.GetSpeedCurve(device, mode, ref speedCurve))
        {
            return new List<(int, int)>();
        }
        var points = new List<(int temp, int speed)>();
        foreach (var ts in speedCurve.TemperatureSpeeds)
        {
            points.Add((ts.Temperature, ts.Speed));
        }
        return points;
    }
}