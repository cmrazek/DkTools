using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeModel;
using Microsoft.Win32;

namespace DkTools
{
	internal static class DkEnvironment
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
		private static DkAppSettings _currentApp = new DkAppSettings();

		public static DkAppSettings CurrentAppSettings
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

		private static DkAppSettings ReloadCurrentApp(string appName = "")
		{
			Log.Write(LogLevel.Info, "Loading application settings...");
			var startTime = DateTime.Now;

			var appSettings = new DkAppSettings();

			if (string.IsNullOrEmpty(appName)) appName = GetDefaultAppName();
			if (string.IsNullOrEmpty(appName))
			{
				Log.Warning("No current app found.");
				appSettings.Initialized = true;
				return appSettings;
			}

			Log.Info("Current App: {0}", appName);
			var appKey = GetReadOnlyAppKey(appName);
			if (appKey == null)
			{
				Log.Warning("Registry key for current app not found.");
				appSettings.Initialized = true;
				return appSettings;
			}

			var rootPath = appKey.GetString("RootPath");

			appSettings.AppName = appName;
			appSettings.Initialized = true;
			appSettings.PlatformPath = WbdkPlatformFolder;
			appSettings.AllAppNames = GetAllAppNames();
			appSettings.SourceDirs = appKey.LoadWbdkMultiPath("SourcePaths", rootPath);
			foreach (var dir in appSettings.SourceDirs) Log.Info("Source Dir: {0}", dir);
			appSettings.IncludeDirs = appKey.LoadWbdkMultiPath("IncludePaths", rootPath)
				.Concat(new string[] { string.IsNullOrEmpty(appSettings.PlatformPath)
					? string.Empty
					: Path.Combine(appSettings.PlatformPath, "include") })
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToArray();
			foreach (var dir in appSettings.IncludeDirs) Log.Info("Include Dir: {0}", dir);
			appSettings.LibDirs = appKey.LoadWbdkMultiPath("LibPaths", rootPath);
			foreach (var dir in appSettings.LibDirs) Log.Info("Lib Dir: {0}", dir);
			appSettings.ExeDirs = appKey.LoadWbdkMultiPath("ExecutablePaths", rootPath);
			foreach (var dir in appSettings.ExeDirs) Log.Info("Executable Dir: {0}", dir);
			appSettings.ObjectDir = appKey.LoadWbdkPath("ObjectPath", rootPath);
			Log.Info("Object Dir: {0}", appSettings.ObjectDir);
			appSettings.TempDir = appKey.LoadWbdkPath("DiagPath", rootPath);
			Log.Info("Temp Dir: {0}", appSettings.TempDir);
			appSettings.ReportDir = appKey.LoadWbdkPath("ListingPath", rootPath);
			Log.Info("Report Dir: {0}", appSettings.ReportDir);
			appSettings.DataDir = appKey.LoadWbdkPath("DataPath", rootPath);
			Log.Info("Data Dir: {0}", appSettings.DataDir);
			appSettings.LogDir = appKey.LoadWbdkPath("LogPath", rootPath);
			Log.Info("Log Dir: {0}", appSettings.LogDir);
			appSettings.SourceFiles = LoadSourceFiles(appSettings);
			appSettings.IncludeFiles = LoadIncludeFiles(appSettings);
			appSettings.SourceAndIncludeFiles = appSettings.SourceFiles.Concat(appSettings.IncludeFiles.Where(i => !appSettings.SourceFiles.Contains(i))).ToArray();

			appSettings.Dict = new DkDict.Dict();
			appSettings.Dict.Load(appSettings);

			appSettings.Repo = new GlobalData.AppRepo(appSettings);

			appSettings.CreateFileSystemWatcher();

			var elapsed = DateTime.Now.Subtract(startTime);
			Log.Write(LogLevel.Info, "Application settings reloaded (elapsed: {0})", elapsed);
			return appSettings;
		}

		public static void Reload(string appName, bool updateDefaultApp)
		{
			var appSettings = ReloadCurrentApp(appName);
			if (appSettings.Initialized)
			{
				ReloadTableList(appSettings);

				CurrentAppSettings = appSettings;

				IncludeFileCache.OnAppChanged();
				AppChanged?.Invoke(null, EventArgs.Empty);
				ProbeToolsPackage.Instance.FireRefreshAllDocuments();

				if (updateDefaultApp)
				{
					CurrentAppSettings.TryUpdateDefaultCurrentApp();
				}
			}
		}

		private const string BaseKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK";
		private const string BaseKey32 = @"SOFTWARE\Fincentric\WBDK";
		private const string CurrentConfigName = "CurrentConfig";

		private const string ConfigurationsKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK\Configurations";
		private const string ConfigurationsKey32 = @"SOFTWARE\Fincentric\WBDK\Configurations";

		private const string AppKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK\Configurations\{0}";
		private const string AppKey32 = @"SOFTWARE\Fincentric\WBDK\Configurations\{0}";

		private static string GetDefaultAppName()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(BaseKey64, writable: false))
			{
				var value = key?.GetValue(CurrentConfigName);
				if (value != null) return value.ToString();
			}

			using (var key = Registry.LocalMachine.OpenSubKey(BaseKey32, writable: false))
			{
				var value = key?.GetValue(CurrentConfigName);
				if (value != null) return value.ToString();
			}

			return null;
		}

		private static string[] GetAllAppNames()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(ConfigurationsKey64, writable: false))
			{
				if (key != null) return key.GetSubKeyNames();
			}

			using (var key = Registry.LocalMachine.OpenSubKey(ConfigurationsKey32, writable: false))
			{
				if (key != null) return key.GetSubKeyNames();
			}

			return StringUtil.EmptyStringArray;
		}

		private static RegistryKey GetReadOnlyAppKey(string appName)
		{
			if (string.IsNullOrEmpty(appName)) return null;

			var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey64, appName), writable: false);
			if (key != null) return key;

			key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey32, appName), writable: false);
			if (key != null) return key;

			return null;
		}

		private static string _wbdkPlatformVersion = null;
		private static string _wbdkPlatformFolder = null;

		private static void GetWbdkPlatformInfo()
		{
			// FEC.exe will be located in the platform folder, and the version number
			// on that file is the same as the WBDK platform version.
			try
			{
				foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(';').Select(x => x.Trim()))
				{
					var fileName = Path.Combine(path, "fec.exe");
					if (File.Exists(fileName))
					{
						Log.Debug("Located FEC.exe: {0}", fileName);
						_wbdkPlatformVersion = FileVersionInfo.GetVersionInfo(fileName)?.FileVersion ?? string.Empty;
						_wbdkPlatformFolder = path;
						Log.Debug("WBDK Platform Directory: {0}", _wbdkPlatformFolder);
						Log.Debug("WBDK Platform Version: {0}", _wbdkPlatformVersion);
						return;
					}
				}

				throw new FileNotFoundException("FEC.exe could not be found.");
			}
			catch (Exception ex)
			{
				Log.Warning("Failed to get WBDK platform info from FEC.exe: {0}", ex);
				if (_wbdkPlatformFolder == null) _wbdkPlatformFolder = string.Empty;
				if (_wbdkPlatformVersion == null) _wbdkPlatformVersion = string.Empty;
			}
		}

		public static string WbdkPlatformVersion
		{
			get
			{
				if (_wbdkPlatformVersion == null) GetWbdkPlatformInfo();
				return _wbdkPlatformVersion;
			}
		}

		public static string WbdkPlatformFolder
		{
			get
			{
				if (_wbdkPlatformFolder == null) GetWbdkPlatformInfo();
				return _wbdkPlatformFolder;
			}
		}
		#endregion

		#region Table List
		private static void ReloadTableList(DkAppSettings appSettings)
		{
			try
			{
				Log.Write(LogLevel.Info, "Loading dictionary...");
				var startTime = DateTime.Now;

				appSettings.Dict.Load(appSettings);

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

		private static string[] LoadSourceFiles(DkAppSettings appSettings)
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

		private static string[] LoadIncludeFiles(DkAppSettings appSettings)
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
