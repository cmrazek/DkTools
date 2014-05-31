using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DkTools
{
	static class Log
	{
		private static bool _sourceExists;

		private static void CheckSource()
		{
			if (!_sourceExists)
			{
				try
				{
					if (!EventLog.SourceExists(Constants.EventLogSource))
					{
						EventLog.CreateEventSource(Constants.EventLogSource, "Application");
					}
					_sourceExists = true;
				}
				catch (Exception ex)
				{
					_sourceExists = true;	// Likely permission error.  Set to true with the anticipation that the source was already created manually.

#if DEBUG
					Debug.WriteLine(string.Concat("Exception when checking event log source: ", ex));
#endif
				}
			}
			
		}

		public static void Write(EventLogEntryType type, string message)
		{
			try
			{
#if DEBUG
				Debug.WriteLine(message);
#endif
				CheckSource();
				EventLog.WriteEntry(Constants.EventLogSource, message, type);
			}
			catch (Exception ex)
			{
#if DEBUG
				Debug.WriteLine(ex.ToString());
#endif
			}
		}

		public static void WriteEx(Exception ex, string message)
		{
			try
			{
				var text = string.Concat(message, "\r\n", ex);
#if DEBUG
				Debug.WriteLine(text);
#endif
				CheckSource();
				EventLog.WriteEntry(Constants.EventLogSource, text, EventLogEntryType.Error);
			}
			catch (Exception ex2)
			{
#if DEBUG
				Debug.WriteLine(ex2.ToString());
#endif
			}
		}

		public static void WriteEx(Exception ex)
		{
			try
			{
#if DEBUG
				Debug.WriteLine(ex.ToString());
#endif
				CheckSource();
				EventLog.WriteEntry(Constants.EventLogSource, ex.ToString(), EventLogEntryType.Error);
			}
			catch (Exception ex2)
			{
#if DEBUG
				Debug.WriteLine(ex2.ToString());
#endif
			}
		}

		public static void WriteDebug(string message)
		{
#if DEBUG
			Debug.WriteLine(message);
#endif
		}

		public static void WriteDebug(string format, params object[] args)
		{
#if DEBUG
			Debug.WriteLine(string.Format(format, args));
#endif
		}
	}
}
