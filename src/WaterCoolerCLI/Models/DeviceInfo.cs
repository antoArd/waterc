using WaterCoolerCLI.Invoke;

namespace WaterCoolerCLI.Models
{

    public class DeviceInfo
    {
        public int VID { get; set; }

        public int PID { get; set; }

        public string ModelName { get; set; }

        public int DeviceType { get; set; }

        public HidDriver HidDriver { get; set; }

        public GDriverInfo gDriverInfo { get; set; }
    }

}
