namespace DK.Diagnostics
{
    public class DkToolsLogger : ILogger
    {
        public LogLevel Level { get => Log.Level; set => Log.Level = value; }

        public void Write(LogLevel level, string message) => Log.Write(level, message);

        public void Write(LogLevel level, string format, params object[] args) => Log.Write(level, format, args);
    }
}
