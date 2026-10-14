using WaterCoolerCLI.Common;

namespace WaterCoolerCLI.Handlers;

public static class SystemInfoHandler
{
    private const string CpuBaseDir = "/sys/devices/system/cpu";
    private const string CpuFreqFile = "cpufreq/scaling_cur_freq";
    private const string CpuPolicyFreqFile = "/sys/devices/system/cpu/cpufreq/policy0/scaling_cur_freq";
    private const string PowercapBaseDir = "/sys/class/powercap";
    private const string PackageZoneNamePrefix = "package-";
    private const string HwmonBaseDir = "/sys/class/hwmon";
    private const string AmdEnergyDriverName = "amd_energy";
    private const string SocketEnergyLabelPrefix = "Esocket";
    private const string ProcCpuInfo = "/proc/cpuinfo";
    private const string ProcStat = "/proc/stat";
    private const string ModelNamePrefix = "model name";
    private const string LogTag = "SystemInfoHandler";

    private const int FallbackFrequencyMHz = 3600;
    private const int FallbackPowerWatts = 0;

    // Energy state for power calculation (delta between readings)
    private static long _lastEnergyUj;
    private static long _lastTimestampTicks;
    private static string _energyPath;
    private static string _energyRangePath;
    private static long _lastUser;
    private static long _lastNice;
    private static long _lastSys;
    private static long _lastIdle;
    private static long _lastIo;
    private static long _lastIrq;
    private static long _lastSoft;
    private static bool _cpuUsageInitialized;

    /// <summary>
    /// Reads the CPU model name from /proc/cpuinfo (call once at startup).
    /// </summary>
    public static string GetCpuName()
    {
        try
        {
            foreach (var line in File.ReadLines(ProcCpuInfo))
            {
                if (!line.StartsWith(ModelNamePrefix, StringComparison.Ordinal))
                    continue;

                int colonIndex = line.IndexOf(':');
                if (colonIndex >= 0)
                {
                    return line.Substring(colonIndex + 1).Trim();
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.Error(LogTag, "GetCpuName: " + ex.Message);
        }

        return "Unknown CPU";
    }

    /// <summary>
    /// Reads the current CPU frequency in MHz using the same policy source as TuringMonitor.
    /// </summary>
    public static int GetCpuFrequencyMHz()
    {
        try
        {
            if (File.Exists(CpuPolicyFreqFile))
            {
                var policyContent = File.ReadAllText(CpuPolicyFreqFile).Trim();
                if (long.TryParse(policyContent, out long policyKhz) && policyKhz > 0)
                {
                    return (int)(policyKhz / 1000);
                }
            }

            long maxKhz = 0;

            foreach (var cpuDir in Directory.GetDirectories(CpuBaseDir, "cpu*"))
            {
                // Filter: only directories like cpu0, cpu1... (skip cpufreq, cpuidle, etc.)
                var dirName = Path.GetFileName(cpuDir);
                if (dirName.Length <= 3 || !char.IsDigit(dirName[3]))
                    continue;

                var freqPath = Path.Combine(cpuDir, CpuFreqFile);
                if (!File.Exists(freqPath))
                    continue;

                var content = File.ReadAllText(freqPath).Trim();
                if (long.TryParse(content, out long khz) && khz > maxKhz)
                {
                    maxKhz = khz;
                }
            }

            if (maxKhz > 0)
            {
                return (int)(maxKhz / 1000);
            }
        }
        catch (Exception ex)
        {
            LogUtil.Error(LogTag, "GetCpuFrequencyMHz: " + ex.Message);
        }

        return FallbackFrequencyMHz;
    }

    public static int GetCpuUsage()
    {
        try
        {
            var cpuLine = File.ReadLines(ProcStat).FirstOrDefault(line => line.StartsWith("cpu ", StringComparison.Ordinal));
            if (cpuLine == null)
            {
                return 0;
            }

            var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
            {
                return 0;
            }

            long user = long.Parse(parts[1]);
            long nice = long.Parse(parts[2]);
            long sys = long.Parse(parts[3]);
            long idle = long.Parse(parts[4]);
            long io = long.Parse(parts[5]);
            long irq = long.Parse(parts[6]);
            long soft = long.Parse(parts[7]);

            if (!_cpuUsageInitialized)
            {
                UpdateCpuUsageBaseline(user, nice, sys, idle, io, irq, soft);
                return 0;
            }

            long totalTime = user + nice + sys + idle + io + irq + soft;
            long previousTotalTime = _lastUser + _lastNice + _lastSys + _lastIdle + _lastIo + _lastIrq + _lastSoft;
            long totalDiff = totalTime - previousTotalTime;
            long idleDiff = idle + io - (_lastIdle + _lastIo);

            UpdateCpuUsageBaseline(user, nice, sys, idle, io, irq, soft);

            if (totalDiff <= 0)
            {
                return 0;
            }

            return (int)Math.Clamp(Math.Round((double)(totalDiff - idleDiff) / totalDiff * 100), 0, 100);
        }
        catch (Exception ex)
        {
            LogUtil.Error(LogTag, "GetCpuUsage: " + ex.Message);
            return 0;
        }
    }

    private static void UpdateCpuUsageBaseline(long user, long nice, long sys, long idle, long io, long irq, long soft)
    {
        _lastUser = user;
        _lastNice = nice;
        _lastSys = sys;
        _lastIdle = idle;
        _lastIo = io;
        _lastIrq = irq;
        _lastSoft = soft;
        _cpuUsageInitialized = true;
    }

    /// <summary>
    /// Reads CPU package power in Watts from a Linux energy counter.
    /// Supports the generic powercap package-zone export and the AMD amd_energy HWMON export.
    /// Calculates power as delta(energy) / delta(time) between successive calls.
    /// The first reading, a source change, and an uncorrectable counter reset return 0.
    /// </summary>
    public static int GetCpuPowerWatts()
    {
        try
        {
            if (!TryFindEnergyCounter(out string energyPath, out string energyRangePath))
            {
                ResetPowerBaseline();
                return FallbackPowerWatts;
            }

            var content = File.ReadAllText(energyPath).Trim();
            if (!long.TryParse(content, out long energyUj))
            {
                return FallbackPowerWatts;
            }

            long nowTicks = Environment.TickCount64;

            // First reading or source change: initialize state.
            if (_lastTimestampTicks == 0 || !string.Equals(_energyPath, energyPath, StringComparison.Ordinal))
            {
                _lastEnergyUj = energyUj;
                _lastTimestampTicks = nowTicks;
                _energyPath = energyPath;
                _energyRangePath = energyRangePath;
                return FallbackPowerWatts;
            }

            long deltaTimeMs = nowTicks - _lastTimestampTicks;
            if (deltaTimeMs <= 0)
            {
                return FallbackPowerWatts;
            }

            long deltaEnergyUj = energyUj - _lastEnergyUj;

            // Correct a wrap only when the provider supplies its counter range.
            if (deltaEnergyUj < 0)
            {
                if (_energyRangePath == null || !long.TryParse(File.ReadAllText(_energyRangePath).Trim(), out long maxEnergy) || maxEnergy <= 0)
                {
                    ResetPowerBaseline(energyPath, energyRangePath, energyUj, nowTicks);
                    return FallbackPowerWatts;
                }

                deltaEnergyUj += maxEnergy;
            }

            if (deltaEnergyUj < 0)
            {
                ResetPowerBaseline(energyPath, energyRangePath, energyUj, nowTicks);
                return FallbackPowerWatts;
            }

            // Power (W) = deltaEnergy (µJ) / deltaTime (ms) / 1000.
            double watts = (double)deltaEnergyUj / (deltaTimeMs * 1000.0);

            _lastEnergyUj = energyUj;
            _lastTimestampTicks = nowTicks;

            return (int)Math.Round(watts);
        }
        catch (Exception ex)
        {
            LogUtil.Error(LogTag, "GetCpuPowerWatts: " + ex.Message);
            return FallbackPowerWatts;
        }
    }

    private static bool TryFindEnergyCounter(out string energyPath, out string energyRangePath)
    {
        energyPath = null;
        energyRangePath = null;

        if (Directory.Exists(PowercapBaseDir))
        {
            foreach (var zoneDir in Directory.GetDirectories(PowercapBaseDir))
            {
                var namePath = Path.Combine(zoneDir, "name");
                var energyCandidate = Path.Combine(zoneDir, "energy_uj");
                if (!File.Exists(namePath) || !File.Exists(energyCandidate) ||
                    !File.ReadAllText(namePath).Trim().StartsWith(PackageZoneNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                energyPath = energyCandidate;
                var rangeCandidate = Path.Combine(zoneDir, "max_energy_range_uj");
                energyRangePath = File.Exists(rangeCandidate) ? rangeCandidate : null;
                return true;
            }
        }

        if (!Directory.Exists(HwmonBaseDir))
        {
            return false;
        }

        foreach (var hwmonDir in Directory.GetDirectories(HwmonBaseDir))
        {
            var namePath = Path.Combine(hwmonDir, "name");
            if (!File.Exists(namePath) || !string.Equals(File.ReadAllText(namePath).Trim(), AmdEnergyDriverName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var inputPath in Directory.GetFiles(hwmonDir, "energy*_input"))
            {
                var labelPath = Path.Combine(hwmonDir, Path.GetFileNameWithoutExtension(inputPath) + "_label");
                if (!File.Exists(labelPath) || !File.ReadAllText(labelPath).Trim().StartsWith(SocketEnergyLabelPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                energyPath = inputPath;
                return true;
            }
        }

        return false;
    }

    private static void ResetPowerBaseline()
    {
        _lastEnergyUj = 0;
        _lastTimestampTicks = 0;
        _energyPath = null;
        _energyRangePath = null;
    }

    private static void ResetPowerBaseline(string energyPath, string energyRangePath, long energyUj, long timestampTicks)
    {
        _lastEnergyUj = energyUj;
        _lastTimestampTicks = timestampTicks;
        _energyPath = energyPath;
        _energyRangePath = energyRangePath;
    }
}
