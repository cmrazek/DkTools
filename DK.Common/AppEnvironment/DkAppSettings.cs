using DK.Diagnostics;
using DK.Preprocessing;
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

		public DkAppSettings()
		{
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

		public void Deactivate()
		{
			StopFileSystemWatcher();
		}

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

			var merger = new DkEnvVarMerger();
			var mergedVars = merger.CreateMergedVarList(this);

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

		#region File System Watcher
		private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

		public void CreateFileSystemWatcher()
		{
			// Create a master list of parent directories only so there are no redundant file system watchers created.
			var masterDirs = new List<string>();
			foreach (var dir in SourceDirs.Concat(IncludeDirs))
			{
				var placedDir = false;

				for (int i = 0; i < masterDirs.Count; i++)
				{
					if (FileHelper.PathIsSameOrChildDir(masterDirs[i], dir))
					{
						// What we have saved is a child, so swap it with it's parent
						masterDirs[i] = dir;
						placedDir = true;
						break;
					}
					else if (FileHelper.PathIsSameOrChildDir(dir, masterDirs[i]))
					{
						// This directory is already covered by one in the master list.
						placedDir = true;
						break;
					}
				}

				if (!placedDir)
				{
					masterDirs.Add(dir);
				}
			}

			// Create a watcher for each master dir.
			foreach (var dir in masterDirs)
			{
				try
				{
					if (!Directory.Exists(dir)) continue;

					Log.Info("Creating file system watcher for directory: {0}", dir);

					var watcher = new FileSystemWatcher();
					_watchers.Add(watcher);

					watcher.Path = dir;
					watcher.Filter = "*.*";
					watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
					watcher.IncludeSubdirectories = true;

					watcher.Changed += OnFileChanged;
					watcher.Deleted += OnFileDeleted;
					watcher.Renamed += OnFileRenamed;
					watcher.Created += OnFileCreated;
					watcher.Error += OnError;

					watcher.EnableRaisingEvents = true;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when trying to create FileSystemWatcher for '{0}'.", dir);
				}
			}
		}

		private void StopFileSystemWatcher()
		{
			while (_watchers.Count > 0)
			{
				try
				{
					var watcher = _watchers.First();

					Log.Info("Stopping file system watcher for directory: {0}", watcher.Path);

					watcher.Dispose();
					_watchers.RemoveAt(0);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception when trying to stop FileSystemWatcher.");
				}
			}
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (DkEnvironment.IsProbeFile(e.FullPath))
			{
				Log.Debug("File change detected: {0}", e.FullPath);
				GlobalEvents.OnFileChanged(e.FullPath);
			}
		}

		private void OnFileDeleted(object sender, FileSystemEventArgs e)
		{
			if (DkEnvironment.IsProbeFile(e.FullPath))
			{
				Log.Debug("File deletion detected: {0}", e.FullPath);
				GlobalEvents.OnFileDeleted(e.FullPath);
			}
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			if (DkEnvironment.IsProbeFile(e.OldFullPath))
			{
				Log.Debug("File rename detected: {0} -> {1}", e.OldFullPath, e.FullPath);
				GlobalEvents.OnFileDeleted(e.OldFullPath);
			}
			else if (DkEnvironment.IsProbeFile(e.FullPath))
			{
				Log.Debug("File rename detected: {0} -> {1}", e.OldFullPath, e.FullPath);
				GlobalEvents.OnFileChanged(e.FullPath);
			}
		}

		private void OnFileCreated(object sender, FileSystemEventArgs e)
		{
			if (DkEnvironment.IsProbeFile(e.FullPath))
			{
				Log.Debug("File create detected: {0}", e.FullPath);
				GlobalEvents.OnFileChanged(e.FullPath);
			}
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			Log.Warning("File system watcher error: {0}", e.GetException());
		}
		#endregion

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
