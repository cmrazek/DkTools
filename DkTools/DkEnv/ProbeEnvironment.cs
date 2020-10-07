using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DkTools
{
	internal static class ProbeEnvironment
	{
		#region Events
		public static event EventHandler AppChanged;
		#endregion

		#region Construction
		public static void Initialize()
		{
			Reload(null, false);
		}

		internal static void OnSettingsSaved()
		{
		}
		#endregion

		#region PSelect
		private static ProbeAppSettings _currentApp = new ProbeAppSettings();

		public static ProbeAppSettings CurrentAppSettings
		{
			get { return _currentApp; }
			set
			{
				if (_currentApp != value)
				{
					if (_currentApp != null) _currentApp.Deactivate();
					_currentApp = value;
				}
			}
		}

		private static ProbeAppSettings ReloadCurrentApp(string appName = "")
		{
			Log.Write(LogLevel.Info, "Loading application settings...");
			var startTime = DateTime.Now;

			var appSettings = new ProbeAppSettings();

			PROBEENVSRVRLib.ProbeEnv env = null;
			PROBEENVSRVRLib.ProbeEnvApp currentApp = null;
			try
			{
				env = new PROBEENVSRVRLib.ProbeEnv();
				if (!string.IsNullOrEmpty(appName)) currentApp = env.FindApp(appName);
				if (currentApp == null) currentApp = env.FindApp(env.DefaultAppName);
				if (currentApp == null)
				{
					Debug.WriteLine("No current app found.");
					appSettings.Initialized = true;
				}
				else
				{
					Debug.WriteLine("Current App: " + currentApp.Name);
					appSettings.AppName = currentApp.Name;
					appSettings.Initialized = true;
					appSettings.PlatformPath = (env as PROBEENVSRVRLib.IProbeEnvPlatform).Folder;
					appSettings.AllAppNames = LoadAppNames(env);
					appSettings.SourceDirs = LoadSourceDirs(currentApp);
					appSettings.IncludeDirs = LoadIncludeDirs(currentApp, appSettings);
					appSettings.LibDirs = LoadLibDirs(currentApp, appSettings);
					appSettings.ExeDirs = LoadExeDirs(currentApp);
					appSettings.ObjectDir = currentApp.ObjectPath;
					appSettings.TempDir = currentApp.TempPath;
					appSettings.ReportDir = currentApp.ListingsPath;
					appSettings.DataDir = currentApp.DataPath;
					appSettings.LogDir = currentApp.LogPath;
					appSettings.SourceFiles = LoadSourceFiles(appSettings);
					appSettings.IncludeFiles = LoadIncludeFiles(appSettings);
					appSettings.SourceAndIncludeFiles = appSettings.SourceFiles.Concat(appSettings.IncludeFiles.Where(i => !appSettings.SourceFiles.Contains(i))).ToArray();
					appSettings.CreateFileSystemWatcher();
				}

				var elapsed = DateTime.Now.Subtract(startTime);
				Log.Write(LogLevel.Info, "Application settings reloaded (elapsed: {0})", elapsed);
				return appSettings;
			}
			finally
			{
				if (currentApp != null)
				{
					Marshal.ReleaseComObject(currentApp);
					currentApp = null;
				}
				if (env != null)
				{
					Marshal.ReleaseComObject(env);
					env = null;
				}
			}
		}

		public static void Reload(string appName, bool updateDefaultApp)
		{
			var appSettings = ReloadCurrentApp(appName);
			if (appSettings.Initialized)
			{
				ReloadTableList(appSettings);

				CurrentAppSettings = appSettings;

				AppChanged?.Invoke(null, EventArgs.Empty);
				ProbeToolsPackage.Instance.FireRefreshAllDocuments();

				if (updateDefaultApp)
				{
					CurrentAppSettings.TryUpdateDefaultCurrentApp();
				}
			}
		}

		private static string[] LoadSourceDirs(PROBEENVSRVRLib.ProbeEnvApp currentApp)
		{
			if (currentApp == null) throw new ArgumentNullException(nameof(currentApp));

			var sourceDirs = new List<string>();
			for (int i = 1, ii = currentApp.NumSourcePath; i <= ii; i++)
			{
				try
				{
					var path = currentApp.SourcePath[i];
					if (string.IsNullOrWhiteSpace(path))
					{
						Log.Warning("PROBE environment has returned a blank source path in slot {0}.", i);
					}
					else if (!Directory.Exists(path))
					{
						Log.Warning("Source directory [{0}] does not exist.", path);
					}
					else
					{
						sourceDirs.Add(path);
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when attempting to retrieve source dir in slot{0}", i);
				}

			}
			return sourceDirs.ToArray();
		}

		private static string[] LoadExeDirs(PROBEENVSRVRLib.ProbeEnvApp currentApp)
		{
			if (currentApp == null) throw new ArgumentNullException(nameof(currentApp));

			var exeDirs = new List<string>();
			for (int i = 1, ii = currentApp.NumExePath; i <= ii; i++)
			{
				try
				{
					var path = currentApp.ExePath[i];
					if (string.IsNullOrWhiteSpace(path))
					{
						Log.Warning("PROBE has returned a blank exe path in slot {0}", i);
					}
					else if (!Directory.Exists(path))
					{
						Log.Warning("Exe directory [{0}] does not exist.", path);
					}
					else
					{
						exeDirs.Add(path);
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when attempting to retrieve exe path in slot {0}", i);
				}
			}

			return exeDirs.ToArray();
		}
		
		private static string[] LoadLibDirs(PROBEENVSRVRLib.ProbeEnvApp currentApp, ProbeAppSettings appSettings)
		{
			var list = new List<string>();

			for (int i = 1, ii = currentApp.NumLibraryPath; i <= ii; i++)
			{
				try
				{
					var path = currentApp.LibraryPath[i];
					if (string.IsNullOrWhiteSpace(path))
					{
						Log.Warning("PROBE returned blank lib path in slot {0}", i);
					}
					else if (!Directory.Exists(path))
					{
						Log.Warning("Lib directory [{0}] does not exist.", path);
					}
					else
					{
						list.Add(path);
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when attempting to retrieve lib path in slot {0}", i);
				}

			}
			if (!string.IsNullOrEmpty(appSettings.PlatformPath)) list.Add(appSettings.PlatformPath);

			return list.ToArray();
		}

		private static string[] LoadIncludeDirs(PROBEENVSRVRLib.ProbeEnvApp currentApp, ProbeAppSettings appSettings)
		{
			var list = new List<string>();

			for (int i = 1, ii = currentApp.NumIncludePath; i <= ii; i++)
			{
				try
				{
					var path = currentApp.IncludePath[i];
					if (string.IsNullOrWhiteSpace(path))
					{
						Log.Warning("PROBE has returned blank include path in slot {0}", i);
					}
					else if (!Directory.Exists(path))
					{
						Log.Warning("Lib directory [{0}] does not exist.", path);
					}
					else
					{
						list.Add(path);
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when attempting to retrieve include path slot {0}", i);
				}
			}

			if (!string.IsNullOrEmpty(appSettings.PlatformPath))
			{
				var includePath = Path.Combine(appSettings.PlatformPath, "include");
				if (Directory.Exists(includePath)) list.Add(includePath);
			}

			return list.ToArray();
		}

		private static string[] LoadAppNames(PROBEENVSRVRLib.ProbeEnv env)
		{
			var list = new List<string>();
			var e = env.EnumApps();
			// One-based array
			for (int i = 1, ii = e.Count; i <= ii; i++)
			{
				list.Add(e.Element(i).Name);
			}
			return list.ToArray();
		}
		#endregion

		#region Table List
		private static void ReloadTableList(ProbeAppSettings appSettings)
		{
			try
			{
				Log.Write(LogLevel.Info, "Loading dictionary...");
				var startTime = DateTime.Now;

				DkDict.Dict.Load(appSettings);

				var elapsed = DateTime.Now.Subtract(startTime);
				Log.Write(LogLevel.Info, "Successfully loaded dictionary (elapsed: {0})", elapsed);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when reloading DK table list.");
			}
		}
		#endregion

		#region File Paths
		public static string LocateFileInPath(string fileName)
		{
			foreach (string path in Environment.GetEnvironmentVariable("path").Split(';'))
			{
				try
				{
					if (Directory.Exists(path.Trim()))
					{
						string fullPath = Path.Combine(path.Trim(), fileName);
						if (File.Exists(fullPath)) return Path.GetFullPath(fullPath);
					}
				}
				catch (Exception)
				{ }
			}
			return "";
		}

		/// <summary>
		/// Determines if a file has an extension considered to be a probe file.
		/// </summary>
		/// <param name="pathName">The path name of the file to test.</param>
		/// <returns>True if the file extension appears to be a type containing Probe code or table definition;
		/// Otherwise false.</returns>
		public static bool IsProbeFile(string pathName)
		{
			// Special exception for dictionary files.
			switch (Path.GetFileName(pathName).ToLower())
			{
				case "dict":
				case "dict&":
					return true;
			}

			// Search the file extension list.
			var fileExt = Path.GetExtension(pathName).TrimStart('.');
			return Constants.ProbeExtensions.Contains(fileExt.ToLower());
		}

		private static string[] LoadSourceFiles(ProbeAppSettings appSettings)
		{
			var sourceFiles = new List<string>();
			foreach (var dir in appSettings.SourceDirs)
			{
				if (string.IsNullOrWhiteSpace(dir)) continue;
				sourceFiles.AddRange(GetAllSourceFiles_ProcessDir(dir));
			}

			return sourceFiles.ToArray();
		}

		private static IEnumerable<string> GetAllSourceFiles_ProcessDir(string dir)
		{
			foreach (var fileName in Directory.GetFiles(dir))
			{
				yield return fileName;
			}

			foreach (var subDir in Directory.GetDirectories(dir))
			{
				foreach (var fileName in GetAllSourceFiles_ProcessDir(subDir))
				{
					yield return fileName;
				}
			}
		}

		private static string[] LoadIncludeFiles(ProbeAppSettings appSettings)
		{
			var includeFiles = new List<string>();

			foreach (var dir in appSettings.IncludeDirs)
			{
				try
				{
					if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;
					includeFiles.AddRange(GetAllIncludeFiles_ProcessDir(dir));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when scanning for include files in directory [{0}]", dir);
				}
			}

			return includeFiles.ToArray();
		}

		private static IEnumerable<string> GetAllIncludeFiles_ProcessDir(string dir)
		{
			var files = new List<string>();

			foreach (var fileName in Directory.GetFiles(dir))
			{
				files.Add(fileName);
			}

			foreach (var subDir in Directory.GetDirectories(dir))
			{
				try
				{
					foreach (var fileName in GetAllIncludeFiles_ProcessDir(subDir))
					{
						files.Add(fileName);
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when scanning for include files in subdirectory [{0}]", subDir);
				}
			}

			return files;
		}
		#endregion

		#region Probe Language
		public static string StringEscape(string str)
		{
			var sb = new StringBuilder(str.Length);

			foreach (var ch in str)
			{
				switch (ch)
				{
					case '\t':
						sb.Append(@"\t");
						break;
					case '\r':
						sb.Append(@"\r");
						break;
					case '\n':
						sb.Append(@"\n");
						break;
					case '\\':
						sb.Append(@"\\");
						break;
					case '\"':
						sb.Append(@"\""");
						break;
					case '\'':
						sb.Append(@"\'");
						break;
					default:
						sb.Append(ch);
						break;
				}
			}

			return sb.ToString();
		}

		public static bool IsValidFunctionName(string str)
		{
			if (string.IsNullOrEmpty(str)) return false;

			bool firstCh = true;
			foreach (var ch in str)
			{
				if (firstCh)
				{
					if (!Char.IsLetter(ch) && ch != '_') return false;
					firstCh = false;
				}
				else
				{
					if (!Char.IsLetterOrDigit(ch) && ch != '_') return false;
				}
			}

			return true;
		}

		public static bool IsValidFileName(string str)
		{
			if (string.IsNullOrEmpty(str)) return false;

			var badPathChars = Path.GetInvalidPathChars();

			foreach (var ch in str)
			{
				if (badPathChars.Contains(ch) || Char.IsWhiteSpace(ch)) return false;
			}

			return true;
		}

		public static bool IsValidTableName(string name)
		{
			return name.IsWord() && name.Length <= 8;
		}

		public static bool IsValidFieldName(string name)
		{
			return name.IsWord();
		}
		#endregion

		#region Tags
		private static readonly Regex _rxNextFormTag = new Regex(@"^probeformgroup:nextform\d+");
		public static bool IsValidTagName(string name)
		{
			switch (name)
			{
				case "accesstype":
				case "checkbox":
				case "cols":
				case "defaultenumcontrolstyle":
				case "disabledborder":
				case "easyview":
				case "formatstring":
				case "hideModalMenus":
				case "probeform:col":
				case "probeform:expressentry":
				case "probeform:nobuttonbar":
				case "probeform:row":
				case "probeform:SelectedHighLight":
				case "probeform:ShowChildForm":
				case "probeform:tabkeycapture":
				case "probeformgroup:folder":
				case "probeformgroup:folderorder":
				case "probeformgroup:homeform":
				case "probeformgroup:LogicalCascadeClearParent":
				case "probeformgroup:nextform":
				case "probeformgroup:stayloaded":
				case "probeformgroup:tooln":
				case "probegroupmenu:alltables":
				case "rows":
				case "scrollbars":
				case "wordwrap":
					return true;

				default:
					if (_rxNextFormTag.IsMatch(name)) return true;
					return false;
			}
		}
		#endregion
	}
}
