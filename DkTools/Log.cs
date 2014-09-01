﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DkTools
{
	internal enum LogLevel
	{
		Debug,
		Info,
		Warning,
		Error
	}

	internal static class Log
	{
		private static bool _initialized;
		private static StreamWriter _writer;
		private static StringBuilder _sb = new StringBuilder();
		private static object _lock = new object();
		private static LogLevel _level;

		public static void Initialize()
		{
			try
			{
#if DEBUG
				_level = LogLevel.Debug;
#else
				_level = LogLevel.Info;
#endif

				lock (_lock)
				{
					var logFileName = Path.Combine(ProbeToolsPackage.LogDir, string.Format(Constants.LogFileNameFormat, DateTime.Now));
					_writer = new StreamWriter(logFileName);
					_initialized = true;

					Write(LogLevel.Info, "Created log file: {0}", logFileName);
				}

				PurgeOldLogFiles();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				_initialized = false;
			}
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
				Debug.WriteLine(ex);
			}
		}

		private static void PurgeOldLogFiles()
		{
			try
			{
				var purgeDate = DateTime.Now.Subtract(TimeSpan.FromDays(Constants.LogFilePurgeDays));
				var removeList = new List<string>();

				foreach (var logFileName in Directory.GetFiles(ProbeToolsPackage.LogDir))
				{
					try
					{
						var fileInfo = new FileInfo(logFileName);
						if (fileInfo.LastWriteTime < purgeDate) removeList.Add(logFileName);
					}
					catch (Exception ex)
					{
						WriteEx(ex, "Error when examining log file for purge: {0}", logFileName);
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
						WriteEx(ex, "Error when deleting old log file: {0}", logFileName);
					}
				}
			}
			catch (Exception ex)
			{
				WriteEx(ex, "Error when purging old log files.");
			}
		}

		public static void Write(LogLevel level, string message)
		{
			try
			{
				if (level < _level) return;

				lock (_lock)
				{
					if (!_initialized) Initialize();

					_sb.Clear();
					_sb.Append("[");
					_sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
					_sb.Append("] (");
					_sb.Append(level.ToString());
					_sb.Append(") ");
					_sb.Append(message);
					var msg = _sb.ToString();
#if DEBUG
					Debug.WriteLine(msg);
#endif
					if (_writer != null) _writer.WriteLine(msg);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
		}

		public static void Write(LogLevel level, string format, params object[] args)
		{
			if (level < _level) return;

			Write(level, string.Format(format, args));
		}

		public static void WriteEx(Exception ex, string message)
		{
			Write(LogLevel.Error, string.Concat(message, "\r\n", ex));
		}

		public static void WriteEx(Exception ex, string format, params object[] args)
		{
			WriteEx(ex, string.Format(format, args));
		}

		public static void WriteEx(Exception ex)
		{
			Write(LogLevel.Error, ex.ToString());
		}

		public static void WriteDebug(string message)
		{
			if (_level > LogLevel.Debug) return;

			Write(LogLevel.Debug, message);
		}

		public static void WriteDebug(string format, params object[] args)
		{
			if (_level > LogLevel.Debug) return;

			Write(LogLevel.Debug, string.Format(format, args));
		}
	}
}
