using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
			Reload(false);
		}

		internal static void OnSettingsSaved()
		{
			_probeExtensions = null;
		}

		public static bool Initialized
		{
			get { return _currentApp != null; }
		}
		#endregion

		#region PSelect
		private static PROBEENVSRVRLib.ProbeEnv _env;
		private static PROBEENVSRVRLib.ProbeEnvApp _currentApp;
		private static string[] _sourceDirs;
		private static string[] _includeDirs;
		private static string[] _libDirs;
		private static string[] _exeDirs;
		private static string _platformPath;

		public static void ReloadCurrentApp(string appName = "")
		{
			_sourceDirs = null;
			_includeDirs = null;
			_libDirs = null;
			_exeDirs = null;

			_env = new PROBEENVSRVRLib.ProbeEnv();

			_currentApp = null;
			if (!string.IsNullOrEmpty(appName)) _currentApp = _env.FindApp(appName);
			if (_currentApp == null) _currentApp = _env.FindApp(_env.DefaultAppName);
			if (_currentApp == null) Debug.WriteLine("No current app found.");
			else Debug.WriteLine("Current App: " + _currentApp.Name);

			var platform = _env as PROBEENVSRVRLib.IProbeEnvPlatform;
			_platformPath = platform.Folder;
			Debug.WriteLine("Platform Version: " + platform.Version);
			Debug.WriteLine("Platform Folder: " + platform.Folder);

			var customData = _currentApp as PROBEENVSRVRLib.IProbeEnvAppCustomDat;
			for (int v = 1, vv = customData.Count; v <= vv; v++)
			{
				var name = customData.GetVariableName(v);
				var value = customData.GetValue(v);
				Debug.WriteLine("  " + name + "=" + value);
			}
		}

		public static void Reload(bool keepCurrentApp)
		{
			var appName = "";
			if (keepCurrentApp) appName = _currentApp != null ? _currentApp.Name : "";
			ReloadCurrentApp(appName);
			ReloadTableList();
			ClearFileLists();
		}

		public static IEnumerable<string> SourceDirs
		{
			get
			{
				if (_sourceDirs == null)
				{
					if (_currentApp != null)
					{
						var sourceDirs = new List<string>();
						for (int i = 1, ii = _currentApp.NumSourcePath; i <= ii; i++)
						{
							try
							{
								var path = _currentApp.SourcePath[i];
								if (string.IsNullOrWhiteSpace(path))
								{
									Log.Write(LogLevel.Warning, "PROBE environment has returned a blank source path in slot {0}.", i);
								}
								else
								{
									sourceDirs.Add(path);
								}
							}
							catch (Exception ex)
							{
								Log.WriteEx(ex, "Exception when attempting to retrieve source dir in slot{0}", i);
							}
							
						}
						_sourceDirs = sourceDirs.ToArray();
					}
					else
					{
						_sourceDirs = new string[0];
					}
				}
				return _sourceDirs;
			}
		}

		public static string ObjectDir
		{
			get
			{
				if (_currentApp != null) return _currentApp.ObjectPath;
				return string.Empty;
			}
		}

		public static IEnumerable<string> ExeDirs
		{
			get
			{
				if (_exeDirs == null)
				{
					if (_currentApp != null)
					{
						var exeDirs = new List<string>();
						for (int i = 1, ii = _currentApp.NumExePath; i <= ii; i++)
						{
							try
							{
								var path = _currentApp.ExePath[i];
								if (string.IsNullOrWhiteSpace(path))
								{
									Log.Write(LogLevel.Warning, "PROBE has returned a blank exe path in slot {0}", i);
								}
								else
								{
									exeDirs.Add(path);
								}
							}
							catch (Exception ex)
							{
								Log.WriteEx(ex, "Exception when attempting to retrieve exe path in slot {0}", i);
							}
						}

						_exeDirs = exeDirs.ToArray();
					}
					else
					{
						_exeDirs = new string[0];
					}
				}
				return _exeDirs;
			}
		}

		public static string TempDir
		{
			get
			{
				if (_currentApp != null) return _currentApp.TempPath;
				return string.Empty;
			}
		}

		public static string ReportDir
		{
			get
			{
				if (_currentApp != null) return _currentApp.ListingsPath;
				return string.Empty;
			}
		}

		public static string DataDir
		{
			get
			{
				if (_currentApp != null) return _currentApp.DataPath;
				return string.Empty;
			}
		}

		public static string LogDir
		{
			get
			{
				if (_currentApp != null) return _currentApp.LogPath;
				return string.Empty;
			}
		}

		public static IEnumerable<string> LibDirs
		{
			get
			{
				if (_libDirs == null)
				{
					if (_currentApp != null)
					{
						var list = new List<string>();

						for (int i = 1, ii = _currentApp.NumLibraryPath; i <= ii; i++)
						{
							try
							{
								var path = _currentApp.LibraryPath[i];
								if (string.IsNullOrWhiteSpace(path))
								{
									Log.Write(LogLevel.Warning, "PROBE returned blank lib path in slot {0}", i);
								}
								else
								{
									list.Add(path);
								}
							}
							catch (Exception ex)
							{
								Log.WriteEx(ex, "Exception when attempting to retrieve lib path in slot {0}", i);
							}
							
						}
						if (!string.IsNullOrEmpty(_platformPath)) list.Add(_platformPath);

						_libDirs = list.ToArray();
					}
					else
					{
						_libDirs = new string[0];
					}
				}
				return _libDirs;
			}
		}

		public static IEnumerable<string> IncludeDirs
		{
			get
			{
				if (_includeDirs == null)
				{
					if (_currentApp != null)
					{
						var list = new List<string>();

						for (int i = 1, ii = _currentApp.NumIncludePath; i <= ii; i++)
						{
							try
							{
								var path = _currentApp.IncludePath[i];
								if (string.IsNullOrWhiteSpace(path))
								{
									Log.Write(LogLevel.Warning, "PROBE has returned blank include path in slot {0}", i);
								}
								else
								{
									list.Add(path);
								}
							}
							catch (Exception ex)
							{
								Log.WriteEx(ex, "Exception when attempting to retrieve include path slot {0}", i);
							}
						}

						if (!string.IsNullOrEmpty(_platformPath))
						{
							var includePath = Path.Combine(_platformPath, "include");
							if (Directory.Exists(includePath)) list.Add(includePath);
						}

						_includeDirs = list.ToArray();
					}
					else
					{
						_includeDirs = new string[0];
					}
				}
				return _includeDirs;
			}
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

		public static IEnumerable<string> AppNames
		{
			get
			{
				if (_env != null)
				{
					var e = _env.EnumApps();
					// One-based array
					for (int i = 1, ii = e.Count; i <= ii; i++) yield return e.Element(i).Name;
				}
			}
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
			get
			{
				if (_currentApp != null) return _currentApp.Name;
				return string.Empty;
			}
			set
			{
				try
				{
					var oldCurrentApp = _currentApp;

					if (_currentApp == null || _currentApp.Name != value)
					{
						ReloadCurrentApp(value);

						if (_currentApp != oldCurrentApp)
						{
							EventHandler ev = AppChanged;
							if (ev != null) ev(null, EventArgs.Empty);
						}

						TryUpdateDefaultCurrentApp();
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex, "Error when setting current Probe app.");
				}
			}
		}

		private static void TryUpdateDefaultCurrentApp()
		{
			// Read the current value from the registry in read-only mode, to see if it needs updating.
			using (var key = Registry.LocalMachine.OpenSubKey(Constants.WbdkRegKey, false))
			{
				var value = Convert.ToString(key.GetValue("CurrentConfig", string.Empty));
				if (value == _currentApp.Name)
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
					key.SetValue("CurrentConfig", _currentApp.Name);
				}
			}
			catch (System.Security.SecurityException ex)
			{
				var options = ProbeToolsPackage.Instance.ErrorSuppressionOptions;
				if (!options.DkAppChangeAdminFailure)
				{
					var msg = "The system-wide default DK application can't be changed because access was denied. To resolve this problem, run Visual Studio as an administrator.";
					var dlg = new ErrorDialog(msg, ex.ToString());
					dlg.ShowUserSuppress = true;
					dlg.Owner = System.Windows.Application.Current.MainWindow;
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
			get { return _platformPath; }
		}
		#endregion

		#region Table List
		private static Dictionary<string, Dict.Table> _tables = new Dictionary<string, Dict.Table>();
		private static Dictionary<string, Dict.StringDef> _stringDefs = new Dictionary<string, Dict.StringDef>();
		private static Dictionary<string, Dict.TypeDef> _typeDefs = new Dictionary<string, Dict.TypeDef>();
		private static Dictionary<string, Dict.InterfaceType> _intTypes = new Dictionary<string, Dict.InterfaceType>();

		public static Dict.Dict CreateDictionary(string appName)
		{
			return new Dict.Dict(appName);
		}

		private static void ReloadTableList()
		{
			try
			{
				Log.Write(LogLevel.Info, "Loading dictionary...");
				var startTime = DateTime.Now;

				_tables.Clear();
				_stringDefs.Clear();
				_typeDefs.Clear();
				_intTypes.Clear();

				if (_currentApp != null)
				{
					using (var dict = CreateDictionary(_currentApp.Name))
					{
						foreach (var repoTable in dict.Tables)
						{
							var table = new Dict.Table(repoTable);
							_tables[table.Name] = table;
						}

						foreach (var repoSD in dict.StringDefines)
						{
							var stringDef = new Dict.StringDef(repoSD);
							_stringDefs[stringDef.Name] = stringDef;
						}

						foreach (var repoTD in dict.TypeDefines)
						{
							var typeDef = new Dict.TypeDef(repoTD);
							_typeDefs[typeDef.Name] = typeDef;
						}

						foreach (var repoRel in dict.Relationships)
						{
							var parent = repoRel.Parent;

							Dict.Table table;
							if (_tables.TryGetValue(repoRel.Parent.Name, out table))
							{
								var relind = new Dict.RelInd(table, repoRel);
								table.AddRelInd(relind);
							}
							else
							{
								Log.WriteDebug("Could not find parent table for relationship '{0}'.", repoRel.Name);
							}
						}

						foreach (var repoIntType in dict.Interfaces)
						{
							var intType = new Dict.InterfaceType(repoIntType);
							_intTypes[intType.Name] = intType;
						}
					}
				}

				LoadRelInds();

				var elapsed = DateTime.Now.Subtract(startTime);
				Log.Write(LogLevel.Info, "Successfully loaded dictionary (elapsed: {0})", elapsed);
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception when reloading DK table list.");
			}
		}

		public static IEnumerable<Dict.Table> Tables
		{
			get { return _tables.Values; }
		}

		public static bool IsProbeTable(string tableName)
		{
			if (_tables.ContainsKey(tableName)) return true;

			if (tableName.Length > 1 && char.IsDigit(tableName[tableName.Length - 1]))
			{
				var prefixTableName = tableName.Substring(0, tableName.Length - 1);
				if (_tables.ContainsKey(prefixTableName)) return true;
			}

			return false;
		}

		public static bool IsProbeTable(int tableNum)
		{
			return _tables.Any(t => { return t.Value.Number == tableNum; });
		}

		public static Dict.Table GetTable(string tableName)
		{
			Dict.Table table;
			if (_tables.TryGetValue(tableName, out table)) return table;

			if (tableName.Length > 1 && char.IsDigit(tableName[tableName.Length - 1]))
			{
				var prefixTableName = tableName.Substring(0, tableName.Length - 1);
				if (_tables.TryGetValue(prefixTableName, out table)) return table;
			}

			return null;
		}

		public static Dict.Table GetTable(int tableNum)
		{
			return (from t in _tables where t.Value.Number == tableNum select t.Value).FirstOrDefault();
		}

		public static IEnumerable<CodeModel.Definitions.Definition> DictDefinitions
		{
			get
			{
				foreach (var table in _tables.Values)
				{
					foreach (var def in table.Definitions) yield return def;
				}
				foreach (var relInd in _relInds.Values) yield return relInd.Definition;
				foreach (var stringDef in _stringDefs.Values) yield return stringDef.Definition;
				foreach (var typeDef in _typeDefs.Values) yield return typeDef.Definition;
				foreach (var intType in _intTypes.Values) yield return intType.Definition;
			}
		}

		public static Dict.StringDef GetStringDef(string name)
		{
			Dict.StringDef ret;
			if (_stringDefs.TryGetValue(name, out ret)) return ret;
			return null;
		}

		public static Dict.TypeDef GetTypeDef(string name)
		{
			Dict.TypeDef ret;
			if (_typeDefs.TryGetValue(name, out ret)) return ret;
			return null;
		}

		public static Dict.InterfaceType GetInterfaceType(string name)
		{
			Dict.InterfaceType intType;
			if (_intTypes.TryGetValue(name, out intType)) return intType;
			return null;
		}

		public static IEnumerable<Dict.InterfaceType> InterfaceTypes
		{
			get
			{
				return _intTypes.Values;
			}
		}

		public static IEnumerable<Dict.StringDef> StringDefs
		{
			get { return _stringDefs.Values; }
		}

		public static IEnumerable<Dict.TypeDef> TypeDefs
		{
			get { return _typeDefs.Values; }
		}
		#endregion

		#region RelInds
		private static Dictionary<string, Dict.RelInd> _relInds = new Dictionary<string, Dict.RelInd>();

		private static void LoadRelInds()
		{
			_relInds.Clear();

			foreach (var table in Tables)
			{
				foreach (var relInd in table.RelInds) _relInds[relInd.Name] = relInd;
			}
		}

		public static IEnumerable<Dict.RelInd> RelInds
		{
			get { return _relInds.Values; }
		}

		public static Dict.RelInd GetRelInd(string name)
		{
			Dict.RelInd relind;
			if (_relInds.TryGetValue(name, out relind)) return relind;
			return null;
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
					if (string.IsNullOrWhiteSpace(dir)) continue;
					_includeFiles.AddRange(GetAllIncludeFiles_ProcessDir(dir));
				}
			}
			return _includeFiles;
		}

		private static IEnumerable<string> GetAllIncludeFiles_ProcessDir(string dir)
		{
			foreach (var fileName in Directory.GetFiles(dir))
			{
				yield return fileName;
			}

			foreach (var subDir in Directory.GetDirectories(dir))
			{
				foreach (var fileName in GetAllIncludeFiles_ProcessDir(subDir))
				{
					yield return fileName;
				}
			}
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
			if (_env == null || _currentApp == null) return;

			var merger = new DkEnv.DkEnvVarMerger();
			var mergedVars = merger.CreateMergedVarList(_currentApp);

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
			if (_currentApp == null) return defaultValue;

			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(k_configPath + _currentApp.Name, false))
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
