using DK.Diagnostics;
using DK.Repository;
using DK.Schema;
using System;
using System.Collections.Generic;
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

		public IAppConfigSource Config => _app.Config;
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

			string fullFileName = _app.FileSystem.GetFullPath(pathName);

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

			string relPathName = GetRelativePathName(_app.FileSystem.GetFullPath(pathName));
			if (string.IsNullOrEmpty(relPathName)) return "";
			if (relPathName.EndsWith("&")) relPathName = relPathName.Substring(0, relPathName.Length - 1);

			foreach (string dir in SourceDirs)
			{
				string testPathName = _app.FileSystem.CombinePath(dir, relPathName);
				if (_app.FileSystem.FileExists(testPathName)) return testPathName;
			}

			return "";
		}

		public IEnumerable<string> FindLocalFiles(string pathName, bool includeBaseFile)
		{
			List<string> files = new List<string>();

			if (string.IsNullOrEmpty(pathName)) return files;

			string relPathName = GetRelativePathName(_app.FileSystem.GetFullPath(pathName));
			if (string.IsNullOrEmpty(relPathName)) return files;
			if (relPathName.EndsWith("&")) relPathName = relPathName.Substring(0, relPathName.Length - 1);

			foreach (string dir in SourceDirs)
			{
				string testPathName = _app.FileSystem.CombinePath(dir, relPathName);
				if (includeBaseFile && _app.FileSystem.FileExists(testPathName)) files.Add(testPathName);

				testPathName += "&";
				if (_app.FileSystem.FileExists(testPathName)) files.Add(testPathName);
			}

			return files;
		}

		public bool FileExistsInApp(string pathName)
		{
			return !string.IsNullOrEmpty(GetRelativePathName(_app.FileSystem.GetFullPath(pathName)));
		}

		#region DK Registry
		
		#endregion

		public int SamPort
		{
			get
			{
				var portString = _app.Config.GetAppConfig(AppName, WbdkAppConfig.DB1SocketNumber);
				if (string.IsNullOrEmpty(portString)) return _app.Config.DefaultSamPort;

				int port;
				if (!int.TryParse(portString, out port)) return _app.Config.DefaultSamPort;
				return port;
			}
		}

		public bool TryUpdateDefaultCurrentApp(string appName) => _app.Config.TryUpdateDefaultApp(appName);

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
