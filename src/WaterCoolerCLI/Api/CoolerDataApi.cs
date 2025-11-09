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
        private readonly static byte[] SendCpuNameCommand = [CommandPrefix, SendCpuNameCommandCode, 0];
        private readonly static byte[] SendCoolerDataCommand = [CommandPrefix, SendCoolerDataCommandCode, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] Buffer = new byte[256];

        public static void SendCpuName(HidDriver hidDriver, string sCpuName)
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
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error("CoolerDataApi", "SendCpuName fail:" + ex.Message);
            }
        }

        public static void SendCoolerData(HidDriver hidDriver, CoolerData coolerData)
        {
            try
            {
                SendCoolerDataCommand[2] = (byte)coolerData.CpuVendor;
                SendCoolerDataCommand[3] = (byte)coolerData.CpuTemperature;
                SendCoolerDataCommand[4] = (byte)coolerData.CpuThreadCount;
                SendCoolerDataCommand[5] = (byte)(coolerData.CpuFrequency / 1000);
                SendCoolerDataCommand[6] = (byte)(coolerData.CpuFrequency / 100 % 10);
                SendCoolerDataCommand[7] = (byte)coolerData.CpuCoreCount;
                SendCoolerDataCommand[8] = (byte)coolerData.VRAMTemperature;
                SendCoolerDataCommand[9] = (byte)coolerData.LiqiudTemperature;
                SendCoolerDataCommand[10] = (byte)coolerData.CpuUsage;
                SendCoolerDataCommand[11] = (byte)(coolerData.CpuPower % 256);
                SendCoolerDataCommand[12] = (byte)(coolerData.CpuPower / 256);

                if (!CoolerApi.Send(hidDriver, SendCoolerDataCommand))
                {
                    LogUtil.Error("CoolerDataApi", "SendCoolerData fail");
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error("CoolerDataApi", "SendCoolerData fail:" + ex.Message);
            }
        }

        private static void ClearBuffer()
        {
            Buffer.AsSpan().Clear();
        }
    }
}
