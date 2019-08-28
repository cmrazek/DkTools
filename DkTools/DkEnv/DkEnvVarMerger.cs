using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DkTools.DkEnv
{
	class DkEnvVarMerger
	{
		// This code is based on DK's method of generating environment variables.
		// It is required because DK's built-in code requires administrator privileges in order to access some registry keys.

		private const string k_vsSetup = "Software\\Microsoft\\VisualStudio\\11.0\\Setup\\VS";
		private const string k_vcSetup = "Software\\Microsoft\\VisualStudio\\11.0\\Setup\\VC";
		private const string k_windowsKits = "Software\\Microsoft\\Windows Kits\\Installed Roots";
		private const string k_windowsDotNet = "Software\\Microsoft\\.NETFramework";

		private string _platformFolder;

		public EnvVarList CreateMergedVarList(ProbeAppSettings app)
		{
			if (app == null) throw new ArgumentNullException("app");

			// Create the base list of environment vars
			var mergedVars = new EnvVarList();
			mergedVars.LoadFromEnvironment();

			mergedVars["pcurrentapp"] = app.AppName;

			var appEnv = app as PROBEENVSRVRLib.IProbeEnvPlatform;
			if (appEnv != null)
			{
				_platformFolder = appEnv.Folder;
				mergedVars["WbdkFrameworkVersion"] = appEnv.Version;
			}
			else
			{
				_platformFolder = null;
			}


			// Add Exe paths
			var path = new List<string>();
			var excludeExeSubDirs = new string[] { "ccdll", "ctdef", "ctdll", "ncdll", "rtdll", "scdll", "st", "stdef", "stdll", "stn" };

			for (int e = 1, ee = app.NumExePath; e <= ee; e++)
			{
				var exePath = app.ExePath[e];
				if (!Directory.Exists(exePath)) continue;

				var helpPath = Path.Combine(exePath, "help");
				if (Directory.Exists(helpPath)) path.Add(helpPath);

				path.Add(exePath);

				AddSubDirs(path, exePath, excludeExeSubDirs);
			}

			// Add development studio exe paths
			AddDevelopmentExePaths(path);

			// Append the current environment variable data
			path.AddRange(mergedVars.GetDirectoryList("path"));
			RemoveDuplicatePaths(path);
			mergedVars.SetDirectoryList("path", path, 2048);


			// Include paths
			var includes = new List<string>();

			for (int i = 1, ii = app.NumIncludePath; i <= ii; i++)
			{
				var includePath = app.IncludePath[i];
				if (!Directory.Exists(includePath)) continue;

				includes.Add(includePath);
			}

			AddDevelopmentIncludePaths(includes);
			includes.AddRange(mergedVars.GetDirectoryList("include"));
			RemoveDuplicatePaths(includes);
			mergedVars.SetDirectoryList("include", includes);


			// Lib paths
			var libs = new List<string>();

			for (int l = 1, ll = app.NumLibraryPath; l <= ll; l++)
			{
				var libPath = app.LibraryPath[l];
				if (!Directory.Exists(libPath)) continue;

				libs.Add(libPath);
			}

			AddDevelopmentLibPaths(libs);
			libs.AddRange(mergedVars.GetDirectoryList("lib"));
			RemoveDuplicatePaths(libs);
			mergedVars.SetDirectoryList("lib", libs);


			mergedVars.Sort();
			return mergedVars;
		}

		private void AddSubDirs(List<string> list, string dirPath, IEnumerable<string> excludeNames)
		{
			foreach (var subDirPath in Directory.GetDirectories(dirPath))
			{
				var dirInfo = new DirectoryInfo(subDirPath);
				if ((dirInfo.Attributes & (FileAttributes.System | FileAttributes.Hidden)) != 0) continue;  // Don't include hidden or system directories

				var dirName = Path.GetFileName(subDirPath);
				if (excludeNames != null && excludeNames.Any(x => x.Equals(dirName, StringComparison.OrdinalIgnoreCase))) continue;

				list.Add(subDirPath);
				AddSubDirs(list, subDirPath, excludeNames);
			}
		}

		private void RemoveDuplicatePaths(List<string> dirList)
		{
			var list = new List<string>();

			foreach (var dir in dirList)
			{
				if (!list.Any(x => x.Equals(dir, StringComparison.OrdinalIgnoreCase))) list.Add(dir);
			}

			dirList.Clear();
			dirList.AddRange(list);
		}

		private void AddDevelopmentExePaths(List<string> path)
		{
			var environmentDirectory = "";
			var vsCommonBinDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_vsSetup, false))
			{
				if (key != null)
				{
					environmentDirectory = key.GetString("EnvironmentDirectory");
					vsCommonBinDir = key.GetString("VS7CommonBinDir");
				}
			}

			var vcProductDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup, false))
			{
				if (key != null)
				{
					vcProductDir = key.GetString("ProductDir");
				}
			}

			var sdkInstallDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_windowsKits, false))
			{
				if (key != null)
				{
					sdkInstallDir = key.GetString("KitsRoot");
				}
			}

			var netFramework = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_windowsDotNet, false))
			{
				if (key != null)
				{
					netFramework = key.GetString("InstallRoot");
				}
			}

			if (!string.IsNullOrEmpty(_platformFolder)) path.Add(_platformFolder);
			if (!string.IsNullOrEmpty(environmentDirectory)) path.Add(environmentDirectory);
			if (!string.IsNullOrEmpty(vcProductDir)) path.Add(Path.Combine(vcProductDir, "bin"));
			if (!string.IsNullOrEmpty(vsCommonBinDir)) path.Add(vsCommonBinDir);
			if (!string.IsNullOrEmpty(netFramework))
			{
				path.Add(Path.Combine(netFramework, "v4.0"));
				path.Add(Path.Combine(netFramework, "v3.5"));
				path.Add(Path.Combine(netFramework, "v2.0.50727"));
			}
			if (!string.IsNullOrEmpty(vcProductDir)) path.Add(Path.Combine(vcProductDir, "VCPackages"));
			if (!string.IsNullOrEmpty(sdkInstallDir)) path.Add(Path.Combine(sdkInstallDir, "bin\\x86"));
		}

		private void AddDevelopmentIncludePaths(List<string> include)
		{
			var vcProductDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup, false))
			{
				if (key != null)
				{
					vcProductDir = key.GetString("ProductDir");
				}
			}

			var sdkInstallDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_windowsKits, false))
			{
				if (key != null)
				{
					sdkInstallDir = key.GetString("KitsRoot");
				}
			}

			if (!string.IsNullOrEmpty(_platformFolder)) include.Add(Path.Combine(_platformFolder, "include"));
			if (!string.IsNullOrEmpty(vcProductDir))
			{
				include.Add(Path.Combine(vcProductDir, "atlmfc\\include"));
				include.Add(Path.Combine(vcProductDir, "include"));
			}
			if (!string.IsNullOrEmpty(sdkInstallDir))
			{
				include.Add(Path.Combine(sdkInstallDir, "include\\um"));
				include.Add(Path.Combine(sdkInstallDir, "include\\shared"));
			}
		}

		private void AddDevelopmentLibPaths(List<string> lib)
		{
			var vcProductDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup, false))
			{
				if (key != null)
				{
					vcProductDir = key.GetString("ProductDir");
				}
			}

			var sdkInstallDir = "";
			using (var key = Registry.LocalMachine.OpenSubKey(k_windowsKits, false))
			{
				if (key != null)
				{
					sdkInstallDir = key.GetString("KitsRoot");
				}
			}

			if (!string.IsNullOrEmpty(_platformFolder)) lib.Add(_platformFolder);
			if (!string.IsNullOrEmpty(vcProductDir))
			{
				lib.Add(Path.Combine(vcProductDir, "atlmfc\\lib"));
				lib.Add(Path.Combine(vcProductDir, "lib"));
			}
			if (!string.IsNullOrEmpty(sdkInstallDir))
			{
				lib.Add(Path.Combine(sdkInstallDir, "lib\\win8\\um\\x86"));
			}
		}
	}
}
