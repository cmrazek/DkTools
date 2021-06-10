using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DK.Diagnostics
{
	public enum LogLevel
	{
		Debug,
		Info,
		Warning,
		Error
	}

	public static class Log
	{
		private static StreamWriter _writer;
		private static StringBuilder _sb = new StringBuilder();
		private static object _lock = new object();
		private static string _logDir;
		private static string _logFileNameFormat;

#if DEBUG
		private static LogLevel _level = LogLevel.Debug;
#else
		private static LogLevel _level = LogLevel.Info;
#endif

		private const int PurgeDays = 7;

		public static void Initialize(string logDir, string logFileNameFormat)
		{
			try
			{
				_logDir = logDir ?? throw new ArgumentNullException(nameof(logDir));
				_logFileNameFormat = logFileNameFormat ?? throw new ArgumentNullException(nameof(logFileNameFormat));

				lock (_lock)
				{
					CheckStream();
				}

				PurgeOldLogFiles();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
			}
		}

		public static LogLevel Level
		{
			get
			{
				lock (_lock)
				{
					return _level;
				}
			}
			set
			{
				lock (_lock)
				{
					if (_level != value)
					{
						_level = value;
						Log.Info("Log level changed to {0}.", _level);
					}
				}
			}
		}

		public static bool CheckStream()
		{
			if (_writer == null)
			{
				if (string.IsNullOrEmpty(_logDir) || string.IsNullOrEmpty(_logFileNameFormat)) return false;

				var logFileName = Path.Combine(_logDir, string.Format(_logFileNameFormat, DateTime.Now));
				_writer = new StreamWriter(logFileName);

				Write(LogLevel.Info, "Created log file: {0}", logFileName);
			}

			return true;
		}

		public static void Close()
		{
			try
			{
				lock (_lock)
				{
					if (_writer != null)
					{
						Write(LogLevel.Info, "Closing log file.");
						_writer.Close();
						_writer = null;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex);
			}
		}

		private static void PurgeOldLogFiles()
		{
			try
			{
				if (string.IsNullOrEmpty(_logDir) || string.IsNullOrEmpty(_logFileNameFormat)) return;

				var purgeDate = DateTime.Now.Subtract(TimeSpan.FromDays(PurgeDays));
				var removeList = new List<string>();

				foreach (var logFileName in Directory.GetFiles(_logDir))
				{
					try
					{
						var fileInfo = new FileInfo(logFileName);
						if (fileInfo.LastWriteTime < purgeDate) removeList.Add(logFileName);
					}
					catch (Exception ex)
					{
						Error(ex, "Error when examining log file for purge: {0}", logFileName);
					}
				}

				foreach (var logFileName in removeList)
				{
					try
					{
						var attribs = File.GetAttributes(logFileName);
						if ((attribs & FileAttributes.ReadOnly) != 0) File.SetAttributes(logFileName, attribs & ~FileAttributes.ReadOnly);

						File.Delete(logFileName);
					}
					catch (Exception ex)
					{
						Error(ex, "Error when deleting old log file: {0}", logFileName);
					}
				}
			}
			catch (Exception ex)
			{
				Error(ex, "Error when purging old log files.");
			}
		}

		public static void Write(LogLevel level, string message)
		{
			try
			{
				if (level < _level) return;

				lock (_lock)
				{
					_sb.Clear();
					_sb.Append("[");
					_sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
					_sb.Append("] (");
					_sb.Append(level.ToString());
					_sb.Append(") {");
					_sb.Append(System.Threading.Thread.CurrentThread.ManagedThreadId);
					_sb.Append("} ");
					_sb.Append(message);
					var msg = _sb.ToString();
#if DEBUG
					System.Diagnostics.Debug.WriteLine(msg);
#endif
					if (!CheckStream()) return;
					if (_writer != null) _writer.WriteLine(msg);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				try
				{
					if (_writer != null)
					{
						_writer.Close();
						_writer = null;
					}
				}
				catch (Exception)
				{ }
			}
		}

		public static void Write(LogLevel level, string format, params object[] args)
		{
			if (level < _level) return;

			Write(level, string.Format(format, args));
		}

		public static void Error(Exception ex)
		{
			Write(LogLevel.Error, ex.ToString());
		}

		public static void Error(Exception ex, string message)
		{
			Write(LogLevel.Error, string.Concat(message, "\r\n", ex));
		}

		public static void Error(Exception ex, string format, params object[] args)
		{
			Error(ex, string.Format(format, args));
		}

		public static void WriteEx(Exception ex)
		{
			Write(LogLevel.Error, ex.ToString());
		}

		public static void Debug(string message)
		{
			if (_level > LogLevel.Debug) return;

			Write(LogLevel.Debug, message);
		}

		public static void Debug(string format, params object[] args)
		{
			if (_level > LogLevel.Debug) return;

			Write(LogLevel.Debug, string.Format(format, args));
		}

		public static void Debug(Exception ex)
		{
			Write(LogLevel.Debug, ex.ToString());
		}

		public static void Info(string message)
		{
			if (_level > LogLevel.Info) return;

			Write(LogLevel.Info, message);
		}

		public static void Info(string format, params object[] args)
		{
			if (_level > LogLevel.Info) return;

			Write(LogLevel.Info, string.Format(format, args));
		}

		public static void Info(Exception ex)
		{
			Write(LogLevel.Info, ex.ToString());
		}

		public static void Warning(string message)
		{
			if (_level > LogLevel.Warning) return;

			Write(LogLevel.Warning, message);
		}

		public static void Warning(string format, params object[] args)
		{
			if (_level > LogLevel.Warning) return;

			Write(LogLevel.Warning, string.Format(format, args));
		}

		public static void Warning(Exception ex)
		{
			Write(LogLevel.Warning, ex.ToString());
		}

		public static void Error(string message)
		{
			Write(LogLevel.Error, message);
		}

		public static void Error(string format, params object[] args)
		{
			Write(LogLevel.Error, string.Format(format, args));
		}
	}
}
