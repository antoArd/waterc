using WaterCoolerCLI.Common;
using WaterCoolerCLI.Invoke;
using WaterCoolerCLI.Models;

namespace WaterCoolerCLI.Api
{
    public class CoolerDataApi
    {
        private const byte CommandPrefix = 153;
        private const byte SendCpuNameCommandCode = 225;
        private const byte SendCoolerDataCommandCode = 224;
        private static readonly byte[] SendCpuNameCommand = [CommandPrefix, SendCpuNameCommandCode, 0];

        private static readonly byte[] SendCoolerDataCommand =
        [
            CommandPrefix,
            SendCoolerDataCommandCode,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ];
        private static readonly byte[] Buffer = new byte[256];

        public static bool SendCpuName(HidDriver hidDriver, string sCpuName)
        {
            try
            {
                ClearBuffer();

                SendCpuNameCommand[2] = (byte)sCpuName.Length;

                Array.Copy(SendCpuNameCommand, Buffer, SendCpuNameCommand.Length);
                for (int i = 0; i < sCpuName.Length; i++)
                {
                    Buffer[3 + i] = (byte)sCpuName[i];
                }
                if (!CoolerApi.Send(hidDriver, Buffer))
                {
                    LogUtil.Error("CoolerDataApi", "SendCpuName fail");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error("CoolerDataApi", "SendCpuName fail:" + ex.Message);
                return false;
            }
            return true;
        }

        public static bool SendCoolerData(HidDriver hidDriver, CoolerData coolerData)
        {
            try
            {
                SendCoolerDataCommand[2] = 0;
                SendCoolerDataCommand[3] = 0;
                SendCoolerDataCommand[4] = (byte)coolerData.CpuTemperature;
                SendCoolerDataCommand[5] = 0;
                // The LCD displays CPU frequency with one decimal place.
                // Round at the wire-format boundary instead of truncating 2999 MHz to 2.9 GHz.
                int frequencyTenths = (int)Math.Round(
                    coolerData.CpuFrequency / 100.0,
                    MidpointRounding.AwayFromZero);
                SendCoolerDataCommand[6] = (byte)(frequencyTenths / 10);
                SendCoolerDataCommand[7] = (byte)(frequencyTenths % 10);
                SendCoolerDataCommand[8] = 0;
                SendCoolerDataCommand[9] = 0;
                SendCoolerDataCommand[10] = 0;
                SendCoolerDataCommand[11] = (byte)coolerData.CpuUsage;
                SendCoolerDataCommand[12] = (byte)(coolerData.CpuPower / 256);
                SendCoolerDataCommand[13] = (byte)(coolerData.CpuPower % 256);
                if (!CoolerApi.Send(hidDriver, SendCoolerDataCommand))
                {
                    LogUtil.Error("CoolerDataApi", "SendCoolerData fail");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error("CoolerDataApi", "SendCoolerData fail:" + ex.Message);
                return false;
            }
            return true;
        }

        private static void ClearBuffer()
        {
            Buffer.AsSpan().Clear();
        }
    }
}
