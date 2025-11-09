using WaterCoolerCLI.Common;
using WaterCoolerCLI.Invoke;
using WaterCoolerCLI.Models;

namespace WaterCoolerCLI.Api
{
    public class DeviceApi
    {
        // Constants for commands
        private const byte CommandPrefix = 153;
        private const byte GetDeviceModelCommandCode = 222;
        private const byte GetModeCommandCode = 221;
        private const byte GetRPMCommandCode = 218;
        private const byte GetCustomCurveCommandCode = 217;
        private const byte SetModeCommandCode = 229;
        private const byte SetCurveCommandCode = 230;
        private const byte SetFanQtyCommandCode = 200;
        private const byte GetFanQtyCommandCode = 199;
        private const byte SaveCommandCode = 182;

        private readonly static byte[] GetDeviceModelCommand = [CommandPrefix, GetDeviceModelCommandCode, 0, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] GetModeCommand = [CommandPrefix, GetModeCommandCode, 0, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] GetRPMCommand = [CommandPrefix, GetRPMCommandCode, 0, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] GetCustomCurveCommand = [CommandPrefix, GetCustomCurveCommandCode, 0, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] SetModeCommand = [CommandPrefix, SetModeCommandCode, 0, 0, 0];
        private readonly static byte[] SetCurveCommand = [CommandPrefix, SetCurveCommandCode, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] SetFanQtyCommand = [CommandPrefix, SetFanQtyCommandCode, 0];
        private readonly static byte[] GetFanQtyCommand = [CommandPrefix, GetFanQtyCommandCode, 0, 0, 0, 0, 0, 0];
        private readonly static byte[] SaveCommand = [CommandPrefix, SaveCommandCode, 0, 0, 0, 0, 0, 0, 0];

        private readonly static byte[] Buffer = new byte[256];

        // Constants for model names
        private const string ModelX240 = "GP-AORUS WATERFORCE X 240";
        private const string ModelX280 = "GP-AORUS WATERFORCE X 280";
        private const string ModelX360 = "GP-AORUS WATERFORCE X 360";
        private const string ModelXII240 = "GP-AORUS WATERFORCE X II 240";
        private const string ModelXII360I = "GP-AORUS WATERFORCEX II 360I";
        private const string ModelXII360 = "GP-AORUS WATERFORCE X II 360";

        // Constants for PIDs
        private const int PidX240_280_360 = 31309;
        private const int PidXII240_360_360I = 31326;
        private const int PidX280_360 = 31313;

        // Constants for speeds
        private const int DefaultFanMinSpeed = 1000;
        private const int DefaultFanMaxSpeed = 2500;
        private const int FanMinSpeedX280 = 1000;
        private const int FanMaxSpeedX280 = 2800;
        private const int FanMinSpeedX240_280_360 = 900;
        private const int FanMaxSpeedX240_280_360 = 2500;
        private const int FanMinSpeedX280_360 = 950;
        private const int FanMaxSpeedX280_360 = 2500;
        private const int MaxSpeedValue = 3200;

        // Constants for temperatures
        private const int Temp0 = 0;
        private const int Temp30 = 30;
        private const int Temp50 = 50;
        private const int Temp65 = 65;

        // Constants for array indices
        private const int ModelTypeIndex = 2;
        private const int FanModeIndex = 2;
        private const int PumpModeIndex = 3;
        private const int FanQtyIndex = 2;
        private const int FanSpeedStartIndex = 2;
        private const int PumpSpeedStartIndex = 5;
        private const int CurveDataStartIndex = 4;
        private const int CurveDataStride = 3;
        private const int TempIndex = 0;
        private const int SpeedHighIndex = 1;
        private const int SpeedLowIndex = 2;

        // Constants for log messages
        private const string LogPrefix = "ucCooler.DeviceApi";
        private const string GetDeviceModelFailMessage = "GetDeviceModelName fail";
        private const string GetFanPumpModeFailMessage = "GetFanPumpMode fail";
        private const string SetSpeedModeSendFailMessage = "SetSpeedMode Send fail";
        private const string SetSpeedModeFailMessage = "SetSpeedMode fail";
        private const string GetSpeedCurveCustomizedFailMessage = "GetSpeedCurve SpeedMode.Customized fail";
        private const string GetSpeedCurveFailMessage = "GetSpeedCurve fail";
        private const string GetFanPumpSpeedFailMessage = "GetFanPumpSpeed fail";
        private const string SetSpeedCurveSendFailMessage = "SetSpeedCurve Send fail";
        private const string SetFanQtySendFailMessage = "SetFanQty Send fail";
        private const string SetFanQtyFailMessage = "SetFanQty fail";
        private const string GetFanQtyFailMessage = "GetFanQty fail";
        private const string SaveSendFailMessage = "SetSpeedCurve Send fail";
        private const string SaveFailMessage = "Save fail";

        private const string GetSpeedCurveFailPrefix = "GetSpeedCurve fail:";
        private const string SetSpeedCurveFailPrefix = "SetSpeedCurve fail:";
        private const string SaveFailPrefix = "Save fail:";

        private const int SpecialModeValue = 3;
        private const int DefaultModeValue = 0;

        #region Device Information

        /// <summary>
        /// Retrieves the device model name based on the device PID and device info.
        /// </summary>
        public static bool GetDeviceModelName(DevicePId devicePId, HidDriver hidDriver, ref string sModelName)
        {
            ClearBuffer();

            bool result = CoolerApi.Receive(hidDriver, GetDeviceModelCommand, Buffer);
            switch (devicePId)
            {
                case DevicePId.DEVICE_X240_280_360:
                    switch (Buffer[ModelTypeIndex])
                    {
                        case 0:
                            sModelName = ModelX240;
                            break;
                        case 1:
                            sModelName = ModelX280;
                            break;
                        case 2:
                            sModelName = ModelX360;
                            break;
                    }
                    break;
                case DevicePId.DEVICE_XII240_360_360I:
                    switch (Buffer[ModelTypeIndex])
                    {
                        case 0:
                            sModelName = ModelXII240;
                            break;
                        case 1:
                            sModelName = ModelXII360I;
                            break;
                        case 2:
                            sModelName = ModelXII360;
                            break;
                    }
                    break;
            }
            return result;
        }

        /// <summary>
        /// Gets the fan speed limits for the specified PID.
        /// </summary>
        public static void GetFanSpeedLimit(int nPID, out int nFanMinSpeed, out int nFanMaxSpeed)
        {
            nFanMinSpeed = DefaultFanMinSpeed;
            nFanMaxSpeed = DefaultFanMaxSpeed;
            switch (nPID)
            {
                case PidXII240_360_360I:
                    nFanMinSpeed = FanMinSpeedX280;
                    nFanMaxSpeed = FanMaxSpeedX280;
                    break;
                case PidX240_280_360:
                    nFanMinSpeed = FanMinSpeedX240_280_360;
                    nFanMaxSpeed = FanMaxSpeedX240_280_360;
                    break;
                case PidX280_360:
                    nFanMinSpeed = FanMinSpeedX280_360;
                    nFanMaxSpeed = FanMaxSpeedX280_360;
                    break;
            }
        }

        /// <summary>
        /// Gets the maximum speed.
        /// </summary>
        public static int MaxSpeed()
        {
            return MaxSpeedValue;
        }

        /// <summary>
        /// Sets the fan quantity.
        /// </summary>
        public static bool SetFanQty(HidDriver hidDriver, byte nFanQty)
        {
            try
            {
                SetFanQtyCommand[2] = nFanQty;
                if (!CoolerApi.Send(hidDriver, SetFanQtyCommand))
                {
                    LogUtil.Error(LogPrefix, SetFanQtySendFailMessage);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, SetFanQtyFailMessage + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the fan quantity.
        /// </summary>
        public static bool GetFanQty(HidDriver hidDriver, ref int nFanQty)
        {
            try
            {
                ClearBuffer();

                byte[] szCommand = GetFanQtyCommand;

                if (!CoolerApi.Receive(hidDriver, GetFanQtyCommand, Buffer))
                {
                    LogUtil.Error(LogPrefix, GetFanQtyFailMessage);
                    return false;
                }
                nFanQty = Buffer[FanQtyIndex];
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, GetFanQtyFailMessage + ex.Message);
                return false;
            }
        }

        #endregion

        #region Mode Management

        /// <summary>
        /// Retrieves the current fan and pump modes.
        /// </summary>
        public static bool GetMode(HidDriver hidDriver, ref SpeedMode fanMode, ref SpeedMode pumMode)
        {
            try
            {
                ClearBuffer();

                bool flag = CoolerApi.Receive(hidDriver, GetModeCommand, Buffer);
                if (!flag)
                {
                    LogUtil.Error(LogPrefix, GetFanPumpModeFailMessage);
                    return false;
                }
                int num = Buffer[FanModeIndex];
                int num2 = Buffer[PumpModeIndex];
                if (num == SpecialModeValue)
                {
                    num = DefaultModeValue;
                }
                if (num2 == SpecialModeValue)
                {
                    num2 = DefaultModeValue;
                }
                fanMode = (SpeedMode)num;
                pumMode = (SpeedMode)num2;
                return flag;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, GetFanPumpModeFailMessage + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the speed mode for the specified speed type.
        /// </summary>
        public static bool SetSpeedMode(HidDriver hidDriver, SpeedType speedType, SpeedMode speedMode)
        {
            try
            {
                SetModeCommand[2] = (byte)speedType;
                SetModeCommand[3] = (byte)speedMode;

                if (!CoolerApi.Send(hidDriver, SetModeCommand))
                {
                    LogUtil.Error(LogPrefix, SetSpeedModeSendFailMessage);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(LogPrefix, SetSpeedModeFailMessage + ex.Message);
                return false;
            }
        }

        #endregion

        #region Speed and Curve Management

        /// <summary>
        /// Retrieves the speed curve for the device and mode.
        /// </summary>
        public static bool GetSpeedCurve(DeviceInfo deviceInfo, SpeedMode speedMode, ref SpeedCurve speedCurve)
        {
            try
            {
                if (speedMode == SpeedMode.Customized)
                {
                    if (!GetSpeedCurve(deviceInfo.HidDriver, ref speedCurve))
                    {
                        LogUtil.Error("ucCooler.DeviceApi", "GetSpeedCurve SpeedMode.Customized fail");
                        return false;
                    }
                    return true;
                }
                switch (speedCurve.SpeedType)
                {
                    case SpeedType.Fan:
                        {
                            Span<int> array2 = stackalloc int[3];
                            if (deviceInfo.PID == 31309)
                            {
                                array2 = ((!deviceInfo.ModelName.Contains("240")) ? FanApi.dcX280_360.FirstOrDefault((KeyValuePair<int, int[]> el) => el.Key == (int)speedMode).Value : FanApi.dcX240.FirstOrDefault((KeyValuePair<int, int[]> el) => el.Key == (int)speedMode).Value);
                            }
                            if (deviceInfo.PID == 31326)
                            {
                                array2 = FanApi.dcXII240_360_360I.FirstOrDefault((KeyValuePair<int, int[]> el) => el.Key == (int)speedMode).Value;
                            }
                            speedCurve.TemperatureSpeeds[0].Temperature = Temp0;
                            speedCurve.TemperatureSpeeds[0].Speed = array2[0];
                            speedCurve.TemperatureSpeeds[1].Temperature = Temp30;
                            speedCurve.TemperatureSpeeds[1].Speed = array2[1];
                            speedCurve.TemperatureSpeeds[2].Temperature = Temp50;
                            speedCurve.TemperatureSpeeds[2].Speed = array2[2];
                            speedCurve.TemperatureSpeeds[3].Temperature = Temp65;
                            speedCurve.TemperatureSpeeds[3].Speed = array2[3];
                            break;
                        }
                    case SpeedType.Pump:
                        {
                            Span<int> array = stackalloc int[3];
                            if (deviceInfo.PID == 31309)
                            {
                                array = PumpApi.dcX240_280_360.FirstOrDefault((KeyValuePair<int, int[]> el) => el.Key == (int)speedMode).Value;
                            }
                            if (deviceInfo.PID == 31326)
                            {
                                array = PumpApi.dcXII240_360_360I.FirstOrDefault((KeyValuePair<int, int[]> el) => el.Key == (int)speedMode).Value;
                            }
                            speedCurve.TemperatureSpeeds[0].Temperature = Temp0;
                            speedCurve.TemperatureSpeeds[0].Speed = array[0];
                            speedCurve.TemperatureSpeeds[1].Temperature = Temp30;
                            speedCurve.TemperatureSpeeds[1].Speed = array[1];
                            speedCurve.TemperatureSpeeds[2].Temperature = Temp50;
                            speedCurve.TemperatureSpeeds[2].Speed = array[2];
                            speedCurve.TemperatureSpeeds[3].Temperature = Temp65;
                            speedCurve.TemperatureSpeeds[3].Speed = array[3];
                            break;
                        }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error("ucCooler.DeviceApi", "GetSpeedCurve fail:" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the customized speed curve from the device.
        /// </summary>
        private static bool GetSpeedCurve(HidDriver hidDriver, ref SpeedCurve speedCurve)
        {
            try
            {
                ClearBuffer();
                GetCustomCurveCommand[2] = (byte)speedCurve.SpeedType;

                bool flag = CoolerApi.Receive(hidDriver, GetCustomCurveCommand, Buffer);
                if (!flag)
                {
                    LogUtil.Error("ucCooler.DeviceApi", "GetSpeedCurve fail");
                    return false;
                }
                for (int i = 0; i < 4; i++)
                {
                    speedCurve.TemperatureSpeeds[i].Temperature = Buffer[4 + i * 3];
                    speedCurve.TemperatureSpeeds[i].Speed = (Buffer[5 + i * 3] << 8) | Buffer[6 + i * 3];
                }
                return flag;
            }
            catch (Exception ex)
            {
                LogUtil.Error("ucCooler.DeviceApi", "GetSpeedCurve fail:" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the current RPM speeds for fan and pump.
        /// </summary>
        public static bool GetRPMSpeed(HidDriver hidDriver, ref int nFanSpeed, ref int nPumpSpeed)
        {
            try
            {
                ClearBuffer();

                bool flag = CoolerApi.Receive(hidDriver, GetRPMCommand, Buffer);
                if (!flag)
                {
                    LogUtil.Error("ucCooler.DeviceApi", "GetFanPumpSpeed fail");
                    return false;
                }
                nFanSpeed = Buffer[2] | (Buffer[3] << 8) | (Buffer[4] << 16);
                nPumpSpeed = Buffer[5] | (Buffer[6] << 8) | (Buffer[7] << 16);
                return flag;
            }
            catch (Exception ex)
            {
                LogUtil.Error("ucCooler.DeviceApi", GetSpeedCurveFailPrefix + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the speed curve on the device.
        /// </summary>
        public static bool SetSpeedCurve(HidDriver hidDriver, SpeedCurve speedCurve)
        {
            try
            {
                SetCurveCommand[3] = (byte)speedCurve.SpeedType;

                for (int i = 0; i < 4; i++)
                {
                    SetCurveCommand[4 + i * 3] = (byte)speedCurve.TemperatureSpeeds[i].Temperature;
                    SetCurveCommand[5 + i * 3] = (byte)(speedCurve.TemperatureSpeeds[i].Speed >> 8);
                    SetCurveCommand[6 + i * 3] = (byte)(speedCurve.TemperatureSpeeds[i].Speed & 0xFF);
                }
                if (!CoolerApi.Send(hidDriver, SetCurveCommand))
                {
                    LogUtil.Error("ucCooler.DeviceApi", "SetSpeedCurve Send fail");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error("ucCooler.DeviceApi", SetSpeedCurveFailPrefix + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Saves the current settings to the device.
        /// </summary>
        public static bool Save(HidDriver hidDriver)
        {
            try
            {
                if (!CoolerApi.Send(hidDriver, SaveCommand))
                {
                    LogUtil.Error("ucCooler.DeviceApi", "SetSpeedCurve Send fail");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error("ucCooler.DeviceApi", SaveFailPrefix + ex.Message);
                return false;
            }
        }

        private static void ClearBuffer()
        {
            Buffer.AsSpan().Clear();
        }

        #endregion
    }
}
