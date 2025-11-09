using HidSharp;
using WaterCoolerCLI.Common;
using WaterCoolerCLI.Invoke;
using WaterCoolerCLI.Models;

namespace WaterCoolerCLI.Api
{
    public class CoolerApi
    {
        private const string LogPrefix = "ucCooler";
        private const string GetDeviceListFailMessage = "GetDeviceList fail:";
        private const string SendFailMessage = "Send fail:";
        private const string ReceiveFailMessage = "Receive fail:";

        /// <summary>
        /// Connects to the device using the provided driver and device info.
        /// </summary>
        public static bool Connect(int nVID, int nPID, ref GDriverInfo driverInfo, ref HidDriver hidDriver)
        {
            try
            {
                driverInfo = new GDriverInfo((uint)nVID, (uint)nPID, (nPID == 31313) ? 65282u : 0u);
                var device = DeviceList.Local.GetHidDevices((int)driverInfo.uVID, (int)driverInfo.uPID).FirstOrDefault();

                if (device is not null)
                {
                    hidDriver = new HidDriver(device);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, GetDeviceListFailMessage + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        public static bool Send(HidDriver hidDriver, ReadOnlySpan<byte> szCommand)
        {
            try
            {
                int outputReportLen = hidDriver.OutputReportLen;
                Span<byte> array = stackalloc byte[outputReportLen];
                szCommand.CopyTo(array);

                return hidDriver.WriteByte(array, -1) == 0;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, SendFailMessage + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Receives data from the device in response to a command.
        /// </summary>
        public static bool Receive(HidDriver hidDriver, ReadOnlySpan<byte> szCommand, Span<byte> szRecv, int nRecvCheckLength = 1)
        {
            try
            {
                int outputReportLen = hidDriver.OutputReportLen;
                Span<byte> array = stackalloc byte[outputReportLen];
                szCommand.CopyTo(array);

                bool result = hidDriver.WriteByte(array, -1) == 0;

                if (szRecv.Length == 0)
                {
                    return result;
                }

                int nLength = 256;
                int num = szRecv.Length / nLength;
                Span<byte> array2 = stackalloc byte[nLength];

                for (int i = 0; i < num; i++)
                {
                    array2.Clear();
                    int num2 = hidDriver.ReadByte(array2, ref nLength, -1);
                    bool flag = true;
                    for (int j = 0; j <= nRecvCheckLength; j++)
                    {
                        if (num2 == 0 && array2[j] != szCommand[j])
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        i--;
                    }
                    else
                    {
                        array2.CopyTo(szRecv.Slice(i * nLength, nLength));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, ReceiveFailMessage + ex.Message);
                return false;
            }
        }
    }
}
