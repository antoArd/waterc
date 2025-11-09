using HidSharp;

namespace WaterCoolerCLI.Invoke
{
    public class HidDriver
    {
        // Constants
        private const string WriteByteExceptionMessage = "WriteByte Exception: {0}";
        private const string ReadByteExceptionMessage = "ReadByte Exception: {0}";
        private const int ReadTimeoutMs = 1000;
        private const int SpecialAddress = -1;
        private const int ReportDataOffset = 1;

        private readonly HidStream _hidStream;
        private readonly HidDevice _hidDevice;

        public int OutputReportLen => _hidDevice.GetMaxOutputReportLength();
        public int InputReportLen => _hidDevice.GetMaxInputReportLength();

        public HidDriver(HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
            _hidStream = hidDevice.Open();
            _hidStream.ReadTimeout = ReadTimeoutMs;
        }

        /// <summary>
        /// Writes bytes to the HID device.
        /// </summary>
        public int WriteByte(ReadOnlySpan<byte> buffer, int nAddress)
        {
            try
            {
                if (nAddress == SpecialAddress)
                {
                    _hidStream.Write(buffer);
                }
                else
                {
                    Span<byte> report = stackalloc byte[_hidDevice.GetMaxFeatureReportLength()];
                    report[0] = (byte)nAddress;
                    buffer.CopyTo(report.Slice(ReportDataOffset));
                    _hidStream.SetFeature(report.ToArray());
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format(WriteByteExceptionMessage, ex.Message));
                return -1;
            }
        }

        /// <summary>
        /// Reads bytes from the HID device.
        /// </summary>
        public int ReadByte(Span<byte> buffer, ref int nLength, int nAddress, bool isMCU = false)
        {
            try
            {
                if (nAddress == SpecialAddress)
                {
                    nLength = _hidStream.Read(buffer);
                }
                else
                {
                    byte[] report = new byte[_hidDevice.GetMaxFeatureReportLength()];
                    report[0] = (byte)nAddress;
                    _hidStream.GetFeature(report);

                    int len = Math.Min(nLength, report.Length - ReportDataOffset);
                    var tempSpan = report.AsSpan(ReportDataOffset, len);
                    tempSpan.CopyTo(buffer);
                    nLength = len;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format(ReadByteExceptionMessage, ex.Message));
                return -1;
            }
        }
    }
}
