using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
			Reload();

			// TODO: remove
			//if (!string.IsNullOrEmpty(_probeIniFileName))
			//{
			//	_probeIniWatcher = new FileSystemWatcher();
			//	_probeIniWatcher.Path = Path.GetDirectoryName(_probeIniFileName);
			//	_probeIniWatcher.Filter = Path.GetFileName(_probeIniFileName);
			//	_probeIniWatcher.NotifyFilter = NotifyFilters.LastWrite;
			//	_probeIniWatcher.Changed += new FileSystemEventHandler(_probeIniWatcher_Changed);
			//	_probeIniWatcher.EnableRaisingEvents = true;
			//}
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

		public static void ReloadCurrentApp()
		{
			_sourceDirs = null;
			_includeDirs = null;
			_libDirs = null;
			_exeDirs = null;

			_env = new PROBEENVSRVRLib.ProbeEnv();
			_currentApp = _env.FindApp(_env.DefaultAppName);
			Debug.WriteLine("Current App: " + _currentApp.Name);

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

		public static void Reload()
		{
			ReloadCurrentApp();
			ReloadTableList();
		}

		public static IEnumerable<string> SourceDirs
		{
			get
			{
				if (_sourceDirs == null)
				{
					if (_currentApp != null)
					{
						_sourceDirs = new string[_currentApp.NumSourcePath];
						for (int i = 1, ii = _currentApp.NumSourcePath; i <= ii; i++) _sourceDirs[i - 1] = _currentApp.SourcePath[i];
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
						_exeDirs = new string[_currentApp.NumExePath];
						for (int i = 1, ii = _currentApp.NumExePath; i <= ii; i++) _exeDirs[i - 1] = _currentApp.ExePath[i];
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
						var list = new List<string>(_currentApp.NumIncludePath + 1);

						for (int i = 1, ii = _currentApp.NumLibraryPath; i <= ii; i++) list.Add(_currentApp.LibraryPath[i]);
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
						var list = new List<string>(_currentApp.NumIncludePath + 1);

						for (int i = 1, ii = _currentApp.NumIncludePath; i <= ii; i++) list.Add(_currentApp.IncludePath[i]);

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

		public static int SamPort
		{
			get
			{
				return 5001;	// TODO: figure out where to get the port number from

				// TODO: remove
				//int port;
				//if (!Int32.TryParse(_nvFile[_currentApp, "dp1"].Trim(), out port)) return 0;
				//return port;
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
					if (_currentApp == null || _currentApp.Name != value)
					{
						_env.DefaultAppName = value;

						ReloadCurrentApp();

						EventHandler ev = AppChanged;
						if (ev != null) ev(null, EventArgs.Empty);
					}

					// TODO: remove
					//if (value != _currentApp)
					//{
					//	string exeFileName = LocateFileInPath("ProbeNV.exe");
					//	if (string.IsNullOrEmpty(exeFileName)) throw new FileNotFoundException("ProbeNV.exe not found.");

					//	using (ProcessRunner pr = new ProcessRunner())
					//	{
					//		int exitCode = pr.ExecuteProcess(exeFileName, value, Path.GetDirectoryName(exeFileName), true);
					//		if (exitCode != 0) throw new ProbeException(string.Format("ProbeNV.exe returned exit code {0}.", exitCode));
					//	}

					//	ReloadCurrentApp();

					//	EventHandler ev = AppChanged;
					//	if (ev != null) ev(null, EventArgs.Empty);
					//}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex, "Error when retrieving current Probe app.");
				}
			}
		}

		public static string PlatformPath
		{
			get { return _platformPath; }
		}
		#endregion

		#region Table List
		private static Dictionary<string, Dict.DictTable> _tables = new Dictionary<string, Dict.DictTable>();
		private static Dictionary<string, Dict.DictStringDef> _stringDefs = new Dictionary<string, Dict.DictStringDef>();
		private static Dictionary<string, Dict.DictTypeDef> _typeDefs = new Dictionary<string, Dict.DictTypeDef>();

		private static void ReloadTableList()
		{
			try
			{
#if DEBUG
				Debug.WriteLine("Loading dictionary...");
#endif

				_tables.Clear();
				_stringDefs.Clear();
				_typeDefs.Clear();

				if (_currentApp != null)
				{
					var repo = new DICTSRVRLib.PRepository();
					var dict = repo.LoadDictionary(_currentApp.Name, string.Empty, DICTSRVRLib.PDS_Access.Access_BROWSE);
					if (dict != null)
					{
						for (int t = 1, tt = dict.TableCount; t <= tt; t++)
						{
							var table = new Dict.DictTable(dict.Tables[t]);
							_tables[table.Name] = table;
						}

						for (int s = 1, ss = dict.StringDefineCount; s <= ss; s++)
						{
							var stringDef = new Dict.DictStringDef(dict.StringDefines[s]);
							_stringDefs[stringDef.Name] = stringDef;
						}

						for (int t = 1, tt = dict.TypeDefineCount; t <= tt; t++)
						{
							var typeDef = new Dict.DictTypeDef(dict.TypeDefines[t]);
							_typeDefs[typeDef.Name] = typeDef;
						}
					}
				}

				LoadRelInds();

#if DEBUG
				Debug.WriteLine("Dictionary loaded successfully.");
#endif
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception when reloading DK table list.");
			}
		}

		public static IEnumerable<Dict.DictTable> Tables
		{
			get { return _tables.Values; }
		}

		public static bool IsProbeTable(string tableName)
		{
			return _tables.ContainsKey(tableName);
		}

		public static bool IsProbeTable(int tableNum)
		{
			return _tables.Any(t => { return t.Value.Number == tableNum; });
		}

		public static Dict.DictTable GetTable(string tableName)
		{
			Dict.DictTable table;
			return _tables.TryGetValue(tableName, out table) ? table : null;
		}

		public static Dict.DictTable GetTable(int tableNum)
		{
			return (from t in _tables where t.Value.Number == tableNum select t.Value).FirstOrDefault();
		}

		public static IEnumerable<CodeModel.Definition> DictDefinitions
		{
			get
			{
				foreach (var table in _tables.Values) yield return table.Definition;
				foreach (var relInd in _relInds.Values) yield return relInd.Definition;
				foreach (var stringDef in _stringDefs.Values) yield return stringDef.Definition;
				foreach (var typeDef in _typeDefs.Values) yield return typeDef.Definition;
			}
		}

		public static Dict.DictStringDef GetStringDef(string name)
		{
			Dict.DictStringDef ret;
			if (_stringDefs.TryGetValue(name, out ret)) return ret;
			return null;
		}

		public static Dict.DictTypeDef GetTypeDef(string name)
		{
			Dict.DictTypeDef ret;
			if (_typeDefs.TryGetValue(name, out ret)) return ret;
			return null;
		}
		#endregion

		#region RelInds
		private static Dictionary<string, Dict.DictRelInd> _relInds = new Dictionary<string, Dict.DictRelInd>();

		private static void LoadRelInds()
		{
			_relInds.Clear();

			foreach (var table in Tables)
			{
				foreach (var relInd in table.RelInds) _relInds[relInd.Name] = relInd;
			}
		}

		public static IEnumerable<Dict.DictRelInd> RelInds
		{
			get { return _relInds.Values; }
		}

		public static Dict.DictRelInd GetRelInd(string name)
		{
			Dict.DictRelInd relind;
			if (_relInds.TryGetValue(name, out relind)) return relind;
			return null;
		}
		#endregion

		#region File Paths
		private static HashSet<string> _probeExtensions = null;

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

		public static IEnumerable<string> GetAllSourceIncludeFiles()
		{
			var fileList = new List<string>();
			try
			{
				foreach (var dir in SourceDirs) GetAllSourceIncludeFiles_ProcessDir(Path.GetFullPath(dir), fileList);
				foreach (var dir in IncludeDirs) GetAllSourceIncludeFiles_ProcessDir(Path.GetFullPath(dir), fileList);
			}
			catch (Exception)
			{ }
			return fileList;
		}

		private static void GetAllSourceIncludeFiles_ProcessDir(string path, List<string> fileList)
		{
			try
			{
				foreach (var fileName in System.IO.Directory.GetFiles(path))
				{
					if (!fileList.Any(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
					{
						fileList.Add(fileName);
					}
				}
				foreach (var dir in System.IO.Directory.GetDirectories(path)) GetAllSourceIncludeFiles_ProcessDir(dir, fileList);
			}
			catch (Exception)
			{ }
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
	}
}
