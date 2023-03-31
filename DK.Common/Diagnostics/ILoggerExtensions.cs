using System;

namespace DK.Diagnostics
{
    public static class ILoggerExtensions
    {
		public static void Debug(this ILogger log, string message)
		{
			if (log.Level > LogLevel.Debug) return;

			log.Write(LogLevel.Debug, message);
		}

		public static void Debug(this ILogger log, string format, params object[] args)
		{
			if (log.Level > LogLevel.Debug) return;

			log.Write(LogLevel.Debug, string.Format(format, args));
		}

		public static void Debug(this ILogger log, Exception ex)
		{
			if (log.Level > LogLevel.Debug) return;

			log.Write(LogLevel.Debug, ex.ToString());
		}

		public static void Info(this ILogger log, string message)
		{
			if (log.Level > LogLevel.Info) return;

			log.Write(LogLevel.Info, message);
		}

		public static void Info(this ILogger log, string format, params object[] args)
		{
			if (log.Level > LogLevel.Info) return;

			log.Write(LogLevel.Info, string.Format(format, args));
		}

		public static void Info(this ILogger log, Exception ex)
		{
			if (log.Level > LogLevel.Info) return;

			log.Write(LogLevel.Info, ex.ToString());
		}

		public static void Warning(this ILogger log, string message)
		{
			if (log.Level > LogLevel.Warning) return;

			log.Write(LogLevel.Warning, message);
		}

		public static void Warning(this ILogger log, string format, params object[] args)
		{
			if (log.Level > LogLevel.Warning) return;

			log.Write(LogLevel.Warning, string.Format(format, args));
		}

		public static void Warning(this ILogger log, Exception ex)
		{
			if (log.Level > LogLevel.Warning) return;

			log.Write(LogLevel.Warning, ex.ToString());
		}

		public static void Warning(this ILogger log, Exception ex, string message)
		{
			if (log.Level > LogLevel.Warning) return;

			log.Write(LogLevel.Warning, string.Concat(message, "\r\n", ex));
		}

		public static void Warning(this ILogger log, Exception ex, string format, params object[] args)
		{
			if (log.Level > LogLevel.Warning) return;

			log.Warning(ex, string.Format(format, args));
		}

		public static void Error(this ILogger log, string message)
		{
			log.Write(LogLevel.Error, message);
		}

		public static void Error(this ILogger log, string format, params object[] args)
		{
			log.Write(LogLevel.Error, string.Format(format, args));
		}

        public static void Error(this ILogger log, Exception ex)
		{
			log.Write(LogLevel.Error, ex.ToString());
		}

		public static void Error(this ILogger log, Exception ex, string message)
		{
			log.Write(LogLevel.Error, string.Concat(message, "\r\n", ex));
		}

		public static void Error(this ILogger log, Exception ex, string format, params object[] args)
		{
			log.Error(ex, string.Format(format, args));
		}
    }
}
