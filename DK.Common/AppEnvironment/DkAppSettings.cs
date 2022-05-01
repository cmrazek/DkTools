using DK.Diagnostics;
using DK.Repository;
using DK.Schema;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DK.AppEnvironment
{
	public class DkAppSettings
	{
		DkAppContext _app;

		public string AppName { get; set; }
		public bool Initialized { get; set; }
		public string PlatformPath { get; set; }
		public string[] AllAppNames { get; set; }
		public string[] SourceDirs { get; set; }
		public string[] IncludeDirs { get; set; }
		public string[] LibDirs { get; set; }
		public string[] ExeDirs { get; set; }
		public string ObjectDir { get; set; }
		public string TempDir { get; set; }
		public string ReportDir { get; set; }
		public string DataDir { get; set; }
		public string LogDir { get; set; }
		public string[] SourceFiles { get; set; }
		public string[] IncludeFiles { get; set; }
		public string[] SourceAndIncludeFiles { get; set; }
		public AppRepo Repo { get; set; }
		public Dict Dict { get; set; }

		public DkAppSettings(DkAppContext app)
		{
			_app = app ?? throw new ArgumentNullException(nameof(app));

			AppName = string.Empty;
			Initialized = false;
			PlatformPath = string.Empty;
			AllAppNames = new string[0];
			SourceDirs = new string[0];
			IncludeDirs = new string[0];
			LibDirs = new string[0];
			ExeDirs = new string[0];
			ObjectDir = string.Empty;
			TempDir = string.Empty;
			ReportDir = string.Empty;
			DataDir = string.Empty;
			LogDir = string.Empty;
			SourceFiles = new string[0];
			IncludeFiles = new string[0];
			SourceAndIncludeFiles = new string[0];
			Dict = new Dict();
			Repo = new AppRepo(this);
		}

		public DkAppContext Context => _app;
		public IFileSystem FileSystem => _app.FileSystem;
		public ILogger Log => _app.Log;

		public IEnumerable<string> GetAllIncludeFilesForDir(string path)
		{
			var list = new List<string>();

			list.AddRange(IncludeFiles);

			foreach (var fileName in (from f in SourceFiles where f.StartsWith(path, StringComparison.OrdinalIgnoreCase) select f))
			{
				if (!list.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(fileName);
				}
			}

			return list;
		}

		public string GetRelativePathName(string pathName)
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

		public string FindBaseFile(string pathName)
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

		public IEnumerable<string> FindLocalFiles(string pathName, bool includeBaseFile)
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

		public bool FileExistsInApp(string pathName)
		{
			return !string.IsNullOrEmpty(GetRelativePathName(Path.GetFullPath(pathName)));
		}

		public void MergeEnvironmentVariables(System.Collections.Specialized.StringDictionary vars)
		{
			if (!Initialized) return;

			var merger = new DkEnvVarMerger(this);
			var mergedVars = merger.CreateMergedVarList();

			foreach (var v in mergedVars) vars[v.Name] = v.Value;
		}

		#region DK Registry
		private const string k_configPath = @"SOFTWARE\Fincentric\WBDK\Configurations\";
		//private const string k_configPathWow64 = @"SOFTWARE\Wow6432Node\Fincentric\WBDK\Configurations";

		public string GetRegString(string name, string defaultValue)
		{
			if (!Initialized) return defaultValue;

			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(k_configPath + AppName, false))
			{
				if (key == null) return defaultValue;

				var obj = key.GetValue(name, defaultValue);
				if (obj == null) return defaultValue;
				return obj.ToString();
			}
		}
		#endregion

		private const int k_defaultPort = 5001;

		public int SamPort
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

		public static void TryUpdateDefaultCurrentApp(string appName)
		{
			if (string.IsNullOrEmpty(appName)) return;

			// Read the current value from the registry in read-only mode, to see if it needs updating.
			using (var key = Registry.LocalMachine.OpenSubKey(Constants.WbdkRegKey, false))
			{
				var value = Convert.ToString(key.GetValue("CurrentConfig", string.Empty));
				if (value == appName)
				{
					// No update required.
					return;
				}
			}

			// Try to update the registry.
			using (var key = Registry.LocalMachine.OpenSubKey(Constants.WbdkRegKey, true))
			{
				key.SetValue("CurrentConfig", appName);
			}
		}

		public void ReloadFilesList()
		{
			var sourceFiles = DkEnvironment.LoadSourceFiles(this);
			var includeFiles = DkEnvironment.LoadIncludeFiles(this);
			var sourceAndIncludeFiles = sourceFiles.Concat(includeFiles.Where(x => !sourceFiles.Contains(x))).ToArray();

			SourceFiles = sourceFiles;
			IncludeFiles = includeFiles;
			SourceAndIncludeFiles = sourceAndIncludeFiles;
		}
	}
}
