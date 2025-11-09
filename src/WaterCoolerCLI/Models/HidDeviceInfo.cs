using System.Runtime.InteropServices;

namespace WaterCoolerCLI.Models
{
    public struct HidDeviceInfo
    {
        public uint uVID;

        public uint uPID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public char[] szShortName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public char[] szModelName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public char[] szVersion;

        public DeviceType eType;

        public uint uFeature;

        public string sModelName { get; set; }

        public string sShortName { get; set; }

        public string sVersion { get; set; }

        public void Alloc()
        {
            uPID = (uVID = 0u);
            szShortName = new char[200];
            szModelName = new char[200];
            szVersion = new char[200];
            eType = DeviceType.DEVICE_TYPE_KEYBOARD;
        }
    }
}
