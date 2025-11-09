namespace WaterCoolerCLI.Models
{
    public struct GDriverInfo
    {
        public uint uVID;

        public uint uPID;

        public uint uReportId;

        public GDriverInfo(uint vId, uint pId, uint reportId)
        {
            uVID = vId;
            uPID = pId;
            uReportId = reportId;
        }
    }
}
