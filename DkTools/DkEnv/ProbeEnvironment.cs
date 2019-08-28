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
			System.Threading.ThreadPool.QueueUserWorkItem(state =>
			{
				Reload(null);
			});
		}

		internal static void OnSettingsSaved()
		{
			_probeExtensions = null;
		}

		public static bool Initialized
		{
			get { return _appSettings.Initialized; }
		}
		#endregion

		#region PSelect
		private static ProbeAppSettings _appSettings = new ProbeAppSettings();

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
					appSettings.AppNames = LoadAppNames(env);
					appSettings.SourceDirs = LoadSourceDirs(currentApp);
					appSettings.IncludeDirs = LoadIncludeDirs(currentApp, appSettings);
					appSettings.LibDirs = LoadLibDirs(currentApp, appSettings);
					appSettings.ExeDirs = LoadExeDirs(currentApp);
					appSettings.ObjectDir = currentApp.ObjectPath;
					appSettings.TempDir = currentApp.TempPath;
					appSettings.ReportDir = currentApp.ListingsPath;
					appSettings.DataDir = currentApp.DataPath;
					appSettings.LogDir = currentApp.LogPath;
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

		public static void Reload(string appName)
		{
			var appSettings = ReloadCurrentApp(appName);
			ReloadTableList();
			ClearFileLists();

			_appSettings = appSettings;
			// TODO: notify that refreshes are required
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

		public static IEnumerable<string> SourceDirs
		{
			get { return _appSettings.SourceDirs; }
		}

		public static string ObjectDir
		{
			get { return _appSettings.ObjectDir; }
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

		public static IEnumerable<string> ExeDirs
		{
			get { return _appSettings.ExeDirs; }
		}

		public static string TempDir
		{
			get { return _appSettings.TempDir; }
		}

		public static string ReportDir
		{
			get { return _appSettings.ReportDir; }
		}

		public static string DataDir
		{
			get { return _appSettings.DataDir; }
		}

		public static string LogDir
		{
			get { return _appSettings.LogDir; }
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

		public static IEnumerable<string> LibDirs
		{
			get { return _appSettings.LibDirs; }
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

		public static IEnumerable<string> IncludeDirs
		{
			get { return _appSettings.IncludeDirs; }
		}

		/// <summary>
		/// Gets a list of all source and include directories.
		/// No effort is taken to remove duplicates.
		/// </summary>
		public static IEnumerable<string> SourceIncludeDirs
		{
			get
			{
				foreach (var dir in SourceDirs) yield return dir;
				foreach (var dir in IncludeDirs) yield return dir;
			}
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

		public static IEnumerable<string> AppNames
		{
			get { return _appSettings.AppNames; }
		}

		private const int k_defaultPort = 5001;

		public static int SamPort
		{
			get
			{
				var portString = GetRegString("DB1SocketNumber", "");
				if (string.IsNullOrEmpty(portString)) return k_defaultPort;

				int port;
				if (!int.TryParse(portString, out port)) return k_defaultPort;
				return port;
			}
		}

		public static string CurrentApp
		{
			get { return _appSettings.AppName; }
			set
			{
				try
				{
					if (_appSettings.AppName != value)
					{
						var appName = value;
						System.Threading.ThreadPool.QueueUserWorkItem(state =>
						{
							Reload(appName);
							AppChanged?.Invoke(null, EventArgs.Empty);
							TryUpdateDefaultCurrentApp();
						});
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error when setting current Probe app.");
				}
			}
		}

		private static void TryUpdateDefaultCurrentApp()
		{
			if (string.IsNullOrEmpty(_appSettings.AppName)) return;

			// Read the current value from the registry in read-only mode, to see if it needs updating.
			using (var key = Registry.LocalMachine.OpenSubKey(Constants.WbdkRegKey, false))
			{
				var value = Convert.ToString(key.GetValue("CurrentConfig", string.Empty));
				if (value == _appSettings.AppName)
				{
					// No update required.
					return;
				}
			}

			// Try to update the registry.
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(Constants.WbdkRegKey, true))
				{
					key.SetValue("CurrentConfig", _appSettings.AppName);
				}
			}
			catch (System.Security.SecurityException ex)
			{
				var options = ProbeToolsPackage.Instance.ErrorSuppressionOptions;
				if (!options.DkAppChangeAdminFailure)
				{
					var msg = "The system-wide default DK application can't be changed because access was denied. To resolve this problem, run Visual Studio as an administrator.";
					var dlg = new ErrorDialog(msg, ex.ToString())
					{
						ShowUserSuppress = true,
						Owner = System.Windows.Application.Current.MainWindow
					};
					dlg.ShowDialog();

					if (options.DkAppChangeAdminFailure != dlg.UserSuppress)
					{
						options.DkAppChangeAdminFailure = dlg.UserSuppress;
						options.SaveSettingsToStorage();
					}
				}
			}
		}

		public static string PlatformPath
		{
			get { return _appSettings.PlatformPath; }
		}
		#endregion

		#region Table List
		private static void ReloadTableList()
		{
			try
			{
				Log.Write(LogLevel.Info, "Loading dictionary...");
				var startTime = DateTime.Now;

				DkDict.Dict.Load();

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
		private static HashSet<string> _probeExtensions;
		private static List<string> _sourceFiles;
		private static List<string> _includeFiles;
		private static List<string> _sourceAndIncludeFiles;

		private static void ClearFileLists()
		{
			_sourceFiles = null;
			_includeFiles = null;
			_sourceAndIncludeFiles = null;
		}

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

		public static string GetRelativePathName(string pathName)
		{
			if (string.IsNullOrEmpty(pathName)) return "";

			string fullFileName = Path.GetFullPath(pathName);

			foreach (string sourceDir in SourceDirs)
			{
				if (fullFileName.Length > sourceDir.Length + 1 &&
					fullFileName.StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase))
				{
					string relPathName = fullFileName.Substring(sourceDir.Length);
					if (relPathName.StartsWith("\\")) relPathName = relPathName.Substring(1);
					return relPathName;
				}
			}

			return "";
		}

		public static string FindBaseFile(string pathName)
		{
			if (string.IsNullOrEmpty(pathName)) return "";

			string relPathName = GetRelativePathName(Path.GetFullPath(pathName));
			if (string.IsNullOrEmpty(relPathName)) return "";
			if (relPathName.EndsWith("&")) relPathName = relPathName.Substring(0, relPathName.Length - 1);

			foreach (string dir in SourceDirs)
			{
				string testPathName = Path.Combine(dir, relPathName);
				if (File.Exists(testPathName)) return testPathName;
			}

			return "";
		}

		public static IEnumerable<string> FindLocalFiles(string pathName, bool includeBaseFile)
		{
			List<string> files = new List<string>();

			if (string.IsNullOrEmpty(pathName)) return files;

			string relPathName = GetRelativePathName(Path.GetFullPath(pathName));
			if (string.IsNullOrEmpty(relPathName)) return files;
			if (relPathName.EndsWith("&")) relPathName = relPathName.Substring(0, relPathName.Length - 1);

			foreach (string dir in SourceDirs)
			{
				string testPathName = Path.Combine(dir, relPathName);
				if (includeBaseFile && File.Exists(testPathName)) files.Add(testPathName);

				testPathName += "&";
				if (File.Exists(testPathName)) files.Add(testPathName);
			}

			return files;
		}

		public static bool FileExistsInApp(string pathName)
		{
			return !string.IsNullOrEmpty(GetRelativePathName(Path.GetFullPath(pathName)));
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
			var fileExt = Path.GetExtension(pathName);
			return _probeExtensions.Contains(fileExt.ToLower());
		}

		public static IEnumerable<string> GetAllSourceFiles()
		{
			if (_sourceFiles == null)
			{
				_sourceFiles = new List<string>();
				foreach (var dir in SourceDirs)
				{
					if (string.IsNullOrWhiteSpace(dir)) continue;
					_sourceFiles.AddRange(GetAllSourceFiles_ProcessDir(dir));
				}
			}
			return _sourceFiles;
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

		public static IEnumerable<string> GetAllIncludeFiles()
		{
			if (_includeFiles == null)
			{
				_includeFiles = new List<string>();
				foreach (var dir in IncludeDirs)
				{
					try
					{
						if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;
						_includeFiles.AddRange(GetAllIncludeFiles_ProcessDir(dir));
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Exception when scanning for include files in directory [{0}]", dir);
					}
				}
			}
			return _includeFiles;
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

		public static IEnumerable<string> GetAllSourceIncludeFiles()
		{
			if (_sourceAndIncludeFiles == null)
			{
				_sourceAndIncludeFiles = new List<string>();

				foreach (var fileName in GetAllSourceFiles())
				{
					if (!_sourceAndIncludeFiles.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase)))
					{
						_sourceAndIncludeFiles.Add(fileName);
					}
				}

				foreach (var fileName in GetAllIncludeFiles())
				{
					if (!_sourceAndIncludeFiles.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase)))
					{
						_sourceAndIncludeFiles.Add(fileName);
					}
				}
			}

			return _sourceAndIncludeFiles;
		}

		public static IEnumerable<string> GetAllIncludeFilesForDir(string path)
		{
			var list = new List<string>();

			list.AddRange(GetAllIncludeFiles());

			foreach (var fileName in (from f in GetAllSourceFiles() where f.StartsWith(path, StringComparison.OrdinalIgnoreCase) select f))
			{
				if (!list.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(fileName);
				}
			}

			return list;
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

		#region Environment Variables

		public static void MergeEnvironmentVariables(System.Collections.Specialized.StringDictionary vars)
		{
			if (!_appSettings.Initialized) return;

			var merger = new DkEnv.DkEnvVarMerger();
			var mergedVars = merger.CreateMergedVarList(_appSettings);

			foreach (var v in mergedVars) vars[v.Name] = v.Value;
		}

        class EnvVars : PROBEENVSRVRLib.IPEnvironmentVars
        {
            private List<EnvVar> _vars = new List<EnvVar>();

            private class EnvVar
            {
                public string name;
                public string value;
            }

            public int Count
            {
                get { return _vars.Count; }
            }

            public string GetItemName(int index)
            {
                return _vars[index - 1].name;
            }

            public string GetVariable(string name)
            {
                foreach (var v in _vars)
                {
                    if (v.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return v.value;
                }

                return null;
            }

            public bool IsExists(string name)
            {
                foreach (var v in _vars)
                {
                    if (v.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
                }

                return false;
            }

            public void SetVariable(string name, string varvalue)
            {
                foreach (var v in _vars)
                {
                    if (v.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        v.value = varvalue;
                        return;
                    }
                }

                _vars.Add(new EnvVar { name = name, value = varvalue });
            }

            public bool TryGetVariable(string name, out string varvalue)
            {
                foreach (var v in _vars)
                {
                    if (v.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        varvalue = v.value;
                        return true;
                    }
                }

                varvalue = null;
                return false;
            }

            public void ResetToCurrent()
            {
                _vars.Clear();

                var envVars = Environment.GetEnvironmentVariables();
                foreach (var ev in envVars.Keys)
                {
                    var value = envVars[ev];
                    _vars.Add(new EnvVar { name = ev.ToString(), value = value.ToString() });
                }
            }

            public void DumpToConsole()
            {
                foreach (var v in _vars)
                {
                    Console.WriteLine(string.Concat(v.name, "=", v.value));
                }
            }
        }
		#endregion

		#region DK Registry
		private const string k_configPath = @"SOFTWARE\Fincentric\WBDK\Configurations\";
		//private const string k_configPathWow64 = @"SOFTWARE\Wow6432Node\Fincentric\WBDK\Configurations";

		public static string GetRegString(string name, string defaultValue)
		{
			if (!_appSettings.Initialized) return defaultValue;

			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(k_configPath + _appSettings.AppName, false))
			{
				if (key == null) return defaultValue;

				var obj = key.GetValue(name, defaultValue);
				if (obj == null) return defaultValue;
				return obj.ToString();
			}
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
