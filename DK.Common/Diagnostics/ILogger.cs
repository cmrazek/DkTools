namespace DK.Diagnostics
{
    public interface ILogger
    {
        LogLevel Level { get; set; }

        void Write(LogLevel level, string message);

        void Write(LogLevel level, string format, params object[] args);
    }
}
