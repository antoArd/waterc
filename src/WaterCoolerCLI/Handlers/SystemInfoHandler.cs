using WaterCoolerCLI.Common;

namespace WaterCoolerCLI.Handlers;

public static class SystemInfoHandler
{
    private const string CpuBaseDir = "/sys/devices/system/cpu";
    private const string CpuFreqFile = "cpufreq/scaling_cur_freq";
    private const string RaplEnergyFile = "/sys/class/powercap/intel-rapl:0/energy_uj";
    private const string ProcCpuInfo = "/proc/cpuinfo";
    private const string ModelNamePrefix = "model name";
    private const string LogTag = "SystemInfoHandler";

    private const int FallbackFrequencyMHz = 3600;
    private const int FallbackPowerWatts = 0;

    // RAPL state for power calculation (delta between readings)
    private static long _lastEnergyUj;
    private static long _lastTimestampTicks;

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
    /// Reads the average CPU frequency across all cores in MHz.
    /// Enumerates /sys/devices/system/cpu/cpuN/cpufreq/scaling_cur_freq for each core.
    /// </summary>
    public static int GetAverageCpuFrequencyMHz()
    {
        try
        {
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
            LogUtil.Error(LogTag, "GetAverageCpuFrequencyMHz: " + ex.Message);
        }

        return FallbackFrequencyMHz;
    }

    /// <summary>
    /// Reads CPU package power in Watts using RAPL energy counter.
    /// Calculates power as delta(energy) / delta(time) between successive calls.
    /// First call initializes state and returns 0.
    /// </summary>
    public static int GetCpuPowerWatts()
    {
        try
        {
            var content = File.ReadAllText(RaplEnergyFile).Trim();
            if (!long.TryParse(content, out long energyUj))
            {
                return FallbackPowerWatts;
            }

            long nowTicks = Environment.TickCount64;

            // First reading: initialize state
            if (_lastTimestampTicks == 0)
            {
                _lastEnergyUj = energyUj;
                _lastTimestampTicks = nowTicks;
                return FallbackPowerWatts;
            }

            long deltaTimeMs = nowTicks - _lastTimestampTicks;
            if (deltaTimeMs <= 0)
            {
                return FallbackPowerWatts;
            }

            long deltaEnergyUj = energyUj - _lastEnergyUj;

            // Handle RAPL counter overflow
            if (deltaEnergyUj < 0)
            {
                // Read max range for overflow correction
                try
                {
                    var maxContent = File.ReadAllText("/sys/class/powercap/intel-rapl:0/max_energy_range_uj").Trim();
                    if (long.TryParse(maxContent, out long maxEnergy))
                    {
                        deltaEnergyUj += maxEnergy;
                    }
                    else
                    {
                        _lastEnergyUj = energyUj;
                        _lastTimestampTicks = nowTicks;
                        return FallbackPowerWatts;
                    }
                }
                catch
                {
                    _lastEnergyUj = energyUj;
                    _lastTimestampTicks = nowTicks;
                    return FallbackPowerWatts;
                }
            }

            // Power (W) = deltaEnergy (µJ) / deltaTime (ms) / 1000
            // = deltaEnergy / (deltaTime * 1000)
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
}
