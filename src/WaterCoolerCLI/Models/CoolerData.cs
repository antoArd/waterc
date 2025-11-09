namespace WaterCoolerCLI.Models
{
    public class CoolerData
    {
        public string CpuName { get; set; }

        public byte CpuVendor { get; set; }
        public byte CpuTemperature { get; set; }
        public byte CpuFrequency { get; set; }
        public byte CpuThreadCount { get; set; }
        public byte CpuCoreCount { get; set; }
        public byte CpuUsage { get; set; }
        public byte CpuPower { get; set; }
        public byte VRAMTemperature { get; set; }
        public byte LiqiudTemperature => 0;
    }
}
