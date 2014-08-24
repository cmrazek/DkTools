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
#if DEBUG
				catch (Exception ex)
				{
					_sourceExists = true;	// Likely permission error.  Set to true with the anticipation that the source was already created manually.
					Debug.WriteLine(string.Concat("Exception when checking event log source: ", ex));
				}
#else
				catch (Exception)
				{
					_sourceExists = true;	// Likely permission error.  Set to true with the anticipation that the source was already created manually.
				}
#endif
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
#if DEBUG
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
#else
			catch (Exception)
			{ }
#endif
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
#if DEBUG
			catch (Exception ex2)
			{
				Debug.WriteLine(ex2.ToString());
			}
#else
			catch (Exception)
			{ }
#endif
		}

		public static void WriteEx(Exception ex, string format, params object[] args)
		{
			WriteEx(ex, string.Format(format, args));
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
#if DEBUG
			catch (Exception ex2)
			{
				Debug.WriteLine(ex2.ToString());
			}
#else
			catch (Exception)
			{ }
#endif
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
