using WaterCoolerCLI.Common;

public static class CpuTempHandler
{
    const string hwmonDir = "/sys/class/hwmon";
    const string StartInfoFileName = "/bin/sh";
    const string StartInfoArguments = "-c \"lsmod | grep k10temp\"";

    public static bool CanReadCpuTemperature
    {
        get
        {
            try
            {
                // Check if k10temp module is loaded (optional, for diagnostics)
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = StartInfoFileName,
                        Arguments = StartInfoArguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string lsmodOutput = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (!lsmodOutput.Contains("k10temp"))
                {
                    return false;
                }

                if (!Directory.Exists(hwmonDir))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }

    public static (double? PackageTempC, string PackageLabel) GetCpuTemperature()
    {
        var hwmonDirs = Directory.GetDirectories(hwmonDir, "hwmon*");
        double? packageTempC = null;
        string packageLabel = null;

        try
        {
            foreach (var hwmonDirPath in hwmonDirs)
            {
                // Look for k10temp device (usually hwmon0 or similar)
                try
                {
                    var nameFiles = Directory.GetFiles(hwmonDirPath, "name");
                    if (!nameFiles.Any(f => File.ReadAllText(f).Trim() == "k10temp")) continue;
                }
                catch(Exception ex)
                {
                    LogUtil.Error(nameof(CpuTempHandler),ex.Message);
                }

                try
                {
                    var tempInputFiles = Directory.GetFiles(hwmonDirPath, "temp*_input");
                    foreach (var inputFile in tempInputFiles)
                    {
                        try
                        {
                            string inputName = Path.GetFileNameWithoutExtension(inputFile);
                            string labelFile = Path.Combine(Path.GetDirectoryName(inputFile)!, inputName.Replace("_input", "_label"));

                            if (File.Exists(labelFile))
                            {
                                string label = File.ReadAllText(labelFile).Trim();
                                string tempStr = File.ReadAllText(inputFile).Trim();

                                if (int.TryParse(tempStr, out int tempMilli))
                                {
                                    double tempC = tempMilli / 1000.0;

                                    // Prioritize Tctl for package temp
                                    if (label == "Tctl" && !packageTempC.HasValue)
                                    {
                                        packageTempC = tempC;
                                        packageLabel = label;
                                    }
                                    // Fallback to Tdie if no Tctl
                                    else if (label == "Tdie" && !packageTempC.HasValue)
                                    {
                                        packageTempC = tempC;
                                        packageLabel = label;
                                    }
                                }
                            }
                            else
                            {
                                LogUtil.Error(nameof(CpuTempHandler),$"No label found {inputName} {labelFile}");
                            }
                        }
                        catch(Exception ex)
                        {
                            LogUtil.Error(nameof(CpuTempHandler),ex.Message);
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogUtil.Error(nameof(CpuTempHandler),ex.Message);
                }
            }
        }
        catch(Exception ex)
        {
            LogUtil.Error(nameof(CpuTempHandler),ex.Message);
        }

        return (packageTempC, packageLabel);
    }
}
