using System.Threading.Channels;

namespace WaterCoolerCLI.Common
{
    public static class LogUtil
    {
        private static readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();
        private static readonly Task _loggingTask;

        private const string ErrorFormat = "[ERROR] [{0}] {1}";
        private const string InfoFormat = "[INFO] [{0}] {1}";

        static LogUtil()
        {
            _loggingTask = Task.Run(async () =>
            {
                await foreach (var message in _logChannel.Reader.ReadAllAsync())
                {
                    Console.WriteLine(message);
                }
            });
        }

        public static void Error(string tag, string message)
        {
            string formattedMessage = string.Format(ErrorFormat, tag, message);
            _logChannel.Writer.TryWrite(formattedMessage);
        }

        public static void Info(string tag, string message)
        {
            string formattedMessage = string.Format(InfoFormat, tag, message);
            _logChannel.Writer.TryWrite(formattedMessage);
        }
    }
}