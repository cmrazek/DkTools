using DK.Diagnostics;
using DK.Repository;
using DK.Schema;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IO = System.IO;

namespace DK.AppEnvironment
{
	public static class DkEnvironment
	{
		#region PSelect
		public static DkAppSettings LoadAppSettings(DkAppContext app, string appName)
		{
			var appSettings = ReloadCurrentApp(app, appName);
			if (appSettings.Initialized)
			{
				ReloadTableList(appSettings, app.Log);
			}

			return appSettings;
		}

		private static DkAppSettings ReloadCurrentApp(DkAppContext app, string appName)
		{
			app.Log.Write(LogLevel.Info, "Loading application settings...");
			var startTime = DateTime.Now;

			var appSettings = new DkAppSettings(app);

			appSettings.PlatformPath = GetWbdkPlatformFolder(app.FileSystem, app.Log);
			appSettings.AllAppNames = GetAllAppNames();

			if (string.IsNullOrEmpty(appName)) appName = GetDefaultAppName();
			if (string.IsNullOrEmpty(appName))
			{
				app.Log.Warning("No current app found.");
				appSettings.Initialized = true;
				return appSettings;
			}

			app.Log.Info("Current App: {0}", appName);
			var appKey = GetReadOnlyAppKey(appName);
			if (appKey == null)
			{
				app.Log.Warning("Registry key for current app not found.");
				appSettings.Initialized = true;
				return appSettings;
			}

			var rootPath = appKey.GetString("RootPath");

			appSettings.AppName = appName;
			appSettings.Initialized = true;
			appSettings.SourceDirs = appKey.LoadWbdkMultiPath("SourcePaths", rootPath);
			foreach (var dir in appSettings.SourceDirs) app.Log.Info("Source Dir: {0}", dir);
			appSettings.IncludeDirs = appKey.LoadWbdkMultiPath("IncludePaths", rootPath)
				.Concat(new string[] { string.IsNullOrEmpty(appSettings.PlatformPath)
					? string.Empty
					: app.FileSystem.CombinePath(appSettings.PlatformPath, "include") })
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToArray();
			foreach (var dir in appSettings.IncludeDirs) app.Log.Info("Include Dir: {0}", dir);
			appSettings.LibDirs = appKey.LoadWbdkMultiPath("LibPaths", rootPath);
			foreach (var dir in appSettings.LibDirs) app.Log.Info("Lib Dir: {0}", dir);
			appSettings.ExeDirs = appKey.LoadWbdkMultiPath("ExecutablePaths", rootPath);
			foreach (var dir in appSettings.ExeDirs) app.Log.Info("Executable Dir: {0}", dir);
			appSettings.ObjectDir = appKey.LoadWbdkPath("ObjectPath", rootPath);
			app.Log.Info("Object Dir: {0}", appSettings.ObjectDir);
			appSettings.TempDir = appKey.LoadWbdkPath("DiagPath", rootPath);
			app.Log.Info("Temp Dir: {0}", appSettings.TempDir);
			appSettings.ReportDir = appKey.LoadWbdkPath("ListingPath", rootPath);
			app.Log.Info("Report Dir: {0}", appSettings.ReportDir);
			appSettings.DataDir = appKey.LoadWbdkPath("DataPath", rootPath);
			app.Log.Info("Data Dir: {0}", appSettings.DataDir);
			appSettings.LogDir = appKey.LoadWbdkPath("LogPath", rootPath);
			app.Log.Info("Log Dir: {0}", appSettings.LogDir);
			appSettings.SourceFiles = LoadSourceFiles(appSettings);
			appSettings.IncludeFiles = LoadIncludeFiles(appSettings);
			appSettings.SourceAndIncludeFiles = appSettings.SourceFiles.Concat(appSettings.IncludeFiles.Where(i => !appSettings.SourceFiles.Contains(i))).ToArray();

			appSettings.Dict = new Dict();
			appSettings.Dict.Load(appSettings);

			appSettings.Repo = new AppRepo(appSettings);

			appSettings.CreateFileSystemWatcher();

			var elapsed = DateTime.Now.Subtract(startTime);
			app.Log.Write(LogLevel.Info, "Application settings reloaded (elapsed: {0})", elapsed);
			return appSettings;
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

			return StringHelper.EmptyStringArray;
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

		private static string _wbdkPlatformVersionText = null;
		private static Version _wbdkPlatformVersion = new Version(1, 0);
		private static string _wbdkPlatformFolder = null;

		public static readonly Version DK10Version = new Version(10, 0);

		private static void GetWbdkPlatformInfo(IFileSystem fs, ILogger log)
		{
			// FEC.exe will be located in the platform folder, and the version number
			// on that file is the same as the WBDK platform version.
			try
			{
				foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(';').Select(x => x.Trim()))
				{
					var fileName = fs.CombinePath(path, "fec.exe");
					if (fs.FileExists(fileName))
					{
						log.Debug("Located FEC.exe: {0}", fileName);
						_wbdkPlatformVersionText = FileVersionInfo.GetVersionInfo(fileName)?.FileVersion ?? string.Empty;
						if (!Version.TryParse(_wbdkPlatformVersionText, out _wbdkPlatformVersion)) _wbdkPlatformVersion = new Version(1, 0);
						_wbdkPlatformFolder = path;
						log.Debug("WBDK Platform Directory: {0}", _wbdkPlatformFolder);
						log.Debug("WBDK Platform Version: {0}", _wbdkPlatformVersionText);
						return;
					}
				}

				throw new IO.FileNotFoundException("FEC.exe could not be found.");
			}
			catch (Exception ex)
			{
				log.Warning("Failed to get WBDK platform info from FEC.exe: {0}", ex);
				if (_wbdkPlatformFolder == null) _wbdkPlatformFolder = string.Empty;
				if (_wbdkPlatformVersionText == null) _wbdkPlatformVersionText = string.Empty;
			}
		}

		public static string GetWbdkPlatformVersionText(IFileSystem fs, ILogger log)
		{
			if (_wbdkPlatformVersionText == null) GetWbdkPlatformInfo(fs, log);
			return _wbdkPlatformVersionText;
		}

		public static Version GetWbdkPlatformVersion(IFileSystem fs, ILogger log)
        {
			if (_wbdkPlatformVersion == null) GetWbdkPlatformInfo(fs, log);
			return _wbdkPlatformVersion;
        }

		public static string GetWbdkPlatformFolder(IFileSystem fs, ILogger log)
		{
			if (_wbdkPlatformFolder == null) GetWbdkPlatformInfo(fs, log);
			return _wbdkPlatformFolder;
		}
		#endregion

		#region Table List
		private static void ReloadTableList(DkAppSettings appSettings, ILogger log)
		{
			try
			{
				log.Write(LogLevel.Info, "Loading dictionary...");
				var startTime = DateTime.Now;

				appSettings.Dict.Load(appSettings);

				var elapsed = DateTime.Now.Subtract(startTime);
				log.Write(LogLevel.Info, "Successfully loaded dictionary (elapsed: {0})", elapsed);
			}
			catch (Exception ex)
			{
				log.Error(ex, "Exception when reloading DK table list.");
			}
		}
		#endregion

		#region File Paths
		public static string LocateFileInPath(string fileName, IFileSystem fs)
		{
			foreach (string path in Environment.GetEnvironmentVariable("path").Split(';'))
			{
				try
				{
					if (fs.DirectoryExists(path.Trim()))
					{
						string fullPath = fs.CombinePath(path.Trim(), fileName);
						if (fs.FileExists(fullPath)) return fs.GetFullPath(fullPath);
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
		public static bool IsProbeFile(string pathName, IFileSystem fs)
		{
			// Special exception for dictionary files.
			switch (fs.GetFileName(pathName).ToLower())
			{
				case "dict":
				case "dict&":
					return true;
			}

			// Search the file extension list.
			var fileExt = fs.GetExtension(pathName).TrimStart('.');
			return Constants.ProbeExtensions.Contains(fileExt.ToLower());
		}

		public static string[] LoadSourceFiles(DkAppSettings appSettings)
		{
			var sourceFiles = new List<string>();
			foreach (var dir in appSettings.SourceDirs)
			{
				if (string.IsNullOrWhiteSpace(dir) || !appSettings.FileSystem.DirectoryExists(dir)) continue;
				sourceFiles.AddRange(GetAllSourceFiles_ProcessDir(dir, appSettings.FileSystem));
			}

			return sourceFiles.ToArray();
		}

		private static IEnumerable<string> GetAllSourceFiles_ProcessDir(string dir, IFileSystem fs)
		{
			foreach (var fileName in fs.GetFilesInDirectory(dir))
			{
				yield return fileName;
			}

			foreach (var subDir in fs.GetDirectoriesInDirectory(dir))
			{
				foreach (var fileName in GetAllSourceFiles_ProcessDir(subDir, fs))
				{
					yield return fileName;
				}
			}
		}

		public static string[] LoadIncludeFiles(DkAppSettings appSettings)
		{
			var includeFiles = new List<string>();

			foreach (var dir in appSettings.IncludeDirs)
			{
				try
				{
					if (string.IsNullOrWhiteSpace(dir) || !appSettings.FileSystem.DirectoryExists(dir)) continue;
					includeFiles.AddRange(GetAllIncludeFiles_ProcessDir(dir, appSettings));
				}
				catch (Exception ex)
				{
					appSettings.Log.Error(ex, "Exception when scanning for include files in directory [{0}]", dir);
				}
			}

			return includeFiles.ToArray();
		}

		private static IEnumerable<string> GetAllIncludeFiles_ProcessDir(string dir, DkAppSettings appSettings)
		{
			var files = new List<string>();

			foreach (var pathName in appSettings.FileSystem.GetFilesInDirectory(dir))
			{
				files.Add(pathName);
			}

			foreach (var subDir in appSettings.FileSystem.GetDirectoriesInDirectory(dir))
			{
				try
				{
					foreach (var fileName in GetAllIncludeFiles_ProcessDir(subDir, appSettings))
					{
						files.Add(fileName);
					}
				}
				catch (Exception ex)
				{
					appSettings.Log.Error(ex, "Exception when scanning for include files in subdirectory [{0}]", subDir);
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

		public static bool IsValidFileName(string str, IFileSystem fs)
		{
			if (string.IsNullOrEmpty(str)) return false;

			var badPathChars = fs.GetInvalidPathChars();

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
