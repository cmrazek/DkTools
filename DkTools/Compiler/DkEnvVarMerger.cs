﻿using DK.AppEnvironment;
using DK.Diagnostics;
using DkTools.AppEnvironment;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DkTools.Compiler
{
    class DkEnvVarMerger
    {
        // This code is based on DK's method of generating environment variables.
        // It is required because DK's built-in code requires administrator privileges in order to access some registry keys.

        private const string k_vsSetup32 = "Software\\Microsoft\\VisualStudio\\11.0\\Setup\\VS";
        private const string k_vsSetup64 = "Software\\WOW6432Node\\Microsoft\\VisualStudio\\11.0\\Setup\\VS";
        private const string k_vcSetup32 = "Software\\Microsoft\\VisualStudio\\11.0\\Setup\\VC";
        private const string k_vcSetup64 = "Software\\WOW6432Node\\Microsoft\\VisualStudio\\11.0\\Setup\\VC";
        private const string k_windowsKits = "Software\\Microsoft\\Windows Kits\\Installed Roots";
        private const string k_windowsDotNet = "Software\\Microsoft\\.NETFramework";

        private const string DotNetInstallRootKey = "InstallRoot";

        private const string DK10NetFrameworkVersion = "v4.0.30319";
        private const string DK10KitsRootKey = "KitsRoot10";
        private const string DK10WindowsSdkVersion = "10.0.19041.0";
        private const int DK10VSVersion = 16;
        private const string DK10MSVCSubDir = @"VC\Tools\MSVC";
        private const int DK10MSVCVersion = 14;
        private const string DK10MSVCBinDir = @"bin\Hostx64\x86";

        private DkAppSettings _app;
        private string _platformFolder;

        public DkEnvVarMerger(DkAppSettings app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public EnvVarList CreateMergedVarList()
        {
            // Create the base list of environment vars
            var mergedVars = new EnvVarList();
            mergedVars.LoadFromEnvironment();

            mergedVars["pcurrentapp"] = _app.AppName;

            var platformInfo = _app.Config.GetWbdkPlatformInfo();

            _platformFolder = platformInfo?.PlatformPath;

            var platformVersion = platformInfo?.VersionText;
            if (platformVersion != null) mergedVars["WbdkFrameworkVersion"] = platformVersion;

            // Add Exe paths
            var path = new List<string>();
            var excludeExeSubDirs = new string[] { "ccdll", "ctdef", "ctdll", "ncdll", "rtdll", "scdll", "st", "stdef", "stdll", "stn" };

            foreach (var exePath in _app.ExeDirs)
            {
                if (!_app.FileSystem.DirectoryExists(exePath)) continue;

                var helpPath = _app.FileSystem.CombinePath(exePath, "help");
                if (_app.FileSystem.DirectoryExists(helpPath)) path.Add(helpPath);

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

            foreach (var includePath in _app.IncludeDirs)
            {
                if (!_app.FileSystem.DirectoryExists(includePath)) continue;

                includes.Add(includePath);
            }

            AddDevelopmentIncludePaths(includes);
            includes.AddRange(mergedVars.GetDirectoryList("include"));
            RemoveDuplicatePaths(includes);
            mergedVars.SetDirectoryList("include", includes);


            // Lib paths
            var libs = new List<string>();

            foreach (var libPath in _app.LibDirs)
            {
                if (!_app.FileSystem.DirectoryExists(libPath)) continue;

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
            foreach (var subDirPath in _app.FileSystem.GetDirectoriesInDirectory(dirPath))
            {
                if (_app.FileSystem.IsDirectoryHiddenOrSystem(subDirPath)) continue;	// Don't include hidden or system directories

                var dirName = _app.FileSystem.GetFileName(subDirPath);
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
            var platformInfo = _app.Config.GetWbdkPlatformInfo();
            if (platformInfo != null && platformInfo.Version >= DkEnvironment.DK10Version)
            {
                _app.Log.Debug("Merging development EXE paths in DK10 mode.");

                if (string.IsNullOrEmpty(_platformFolder)) _app.Log.Warning("The WBDK platform folder is not set.");
                else path.Add(_platformFolder);

                var msvcPath = GetDK10MSVCPath();
                if (string.IsNullOrEmpty(msvcPath)) _app.Log.Warning("MSVC path was not found.");
                else path.Add(msvcPath);

                var dotnetPath = GetDK10DotNetFrameworkPath();
                if (string.IsNullOrEmpty(dotnetPath)) _app.Log.Warning(".NET Framework path was not found.");
                else path.Add(dotnetPath);

                var windowsSdkPath = GetDK10WindowsSdkPath();
                if (string.IsNullOrEmpty(windowsSdkPath)) _app.Log.Warning("Windows SDK path was not found.");
                else path.Add(windowsSdkPath);
            }
            else
            {
                _app.Log.Debug("Merging development EXE paths in DK7 mode.");

                var environmentDirectory = null as string;
                var vsCommonBinDir = null as string;
                using (var key = Registry.LocalMachine.OpenSubKey(k_vsSetup32, false))
                {
                    if (key != null)
                    {
                        environmentDirectory = key.GetString("EnvironmentDirectory");
                        vsCommonBinDir = key.GetString("VS7CommonBinDir");
                    }
                }
                if (environmentDirectory == null || vsCommonBinDir == null)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(k_vsSetup64, false))
                    {
                        if (key != null)
                        {
                            environmentDirectory = key.GetString("EnvironmentDirectory");
                            vsCommonBinDir = key.GetString("VS7CommonBinDir");
                        }
                    }
                }

                var vcProductDir = null as string;
                using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup32, false))
                {
                    if (key != null)
                    {
                        vcProductDir = key.GetString("ProductDir");
                    }
                }
                if (vcProductDir == null)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup64, false))
                    {
                        if (key != null)
                        {
                            vcProductDir = key.GetString("ProductDir");
                        }
                    }
                }

                var sdkInstallDir = null as string;
                using (var key = Registry.LocalMachine.OpenSubKey(k_windowsKits, false))
                {
                    if (key != null)
                    {
                        sdkInstallDir = key.GetString("KitsRoot");
                    }
                }

                var netFramework = null as string;
                using (var key = Registry.LocalMachine.OpenSubKey(k_windowsDotNet, false))
                {
                    if (key != null)
                    {
                        netFramework = key.GetString(DotNetInstallRootKey);
                    }
                }

                if (!string.IsNullOrEmpty(_platformFolder)) path.Add(_platformFolder);
                if (!string.IsNullOrEmpty(environmentDirectory)) path.Add(environmentDirectory);
                if (!string.IsNullOrEmpty(vcProductDir)) path.Add(_app.FileSystem.CombinePath(vcProductDir, "bin"));
                if (!string.IsNullOrEmpty(vsCommonBinDir)) path.Add(vsCommonBinDir);
                if (!string.IsNullOrEmpty(netFramework))
                {
                    path.Add(_app.FileSystem.CombinePath(netFramework, "v4.0"));
                    path.Add(_app.FileSystem.CombinePath(netFramework, "v3.5"));
                    path.Add(_app.FileSystem.CombinePath(netFramework, "v2.0.50727"));
                }
                if (!string.IsNullOrEmpty(vcProductDir)) path.Add(_app.FileSystem.CombinePath(vcProductDir, "VCPackages"));
                if (!string.IsNullOrEmpty(sdkInstallDir)) path.Add(_app.FileSystem.CombinePath(sdkInstallDir, "bin\\x86"));
            }
        }

        private class VSInstance
        {
            public Version Version { get; set; }
            public string InstallPath { get; set; }
        }

        private string GetDK10MSVCPath()
        {
            // MSVC path: C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Tools\MSVC\14.29.30133\bin\Hostx64\x86
            // VSInstallPath + "\VC\Tools\MSVC\" + MSVCVersion + "\bin\Hostx64\x86\"

            var scc = new SetupConfiguration() as ISetupConfiguration2;
            if (scc == null)
            {
                _app.Log.Warning("Failed to get VS ISetupConfiguration object.");
                return null;
            }

            var setupEnum = scc.EnumInstances();
            if (setupEnum == null)
            {
                _app.Log.Warning("Couldn't get VS setup enumeration object.");
                return null;
            }

            var vsInstances = new List<VSInstance>();
            var setupInstanceFetch = new ISetupInstance[1];
            while (true)
            {
                setupEnum.Next(1, setupInstanceFetch, out var fetched);
                if (fetched > 0)
                {
                    var versionString = setupInstanceFetch[0].GetInstallationVersion();
                    if (!Version.TryParse(versionString, out var version))
                    {
                        _app.Log.Warning("Failed to parse Version out of '{0}'.", versionString);
                        continue;
                    }

                    vsInstances.Add(new VSInstance
                    {
                        Version = version,
                        InstallPath = setupInstanceFetch[0].GetInstallationPath()
                    });
                }
                else break;
            }

            if (!vsInstances.Any())
            {
                _app.Log.Warning("No VS instances found.");
                return null;
            }

            // Create a list of install paths with the preferred version at the front.
            var vsInstallPaths = new List<string>();
            foreach (var vsInstance in vsInstances)
            {
                if (vsInstance.Version.Major == DK10VSVersion) vsInstallPaths.Insert(0, vsInstance.InstallPath);
                else vsInstallPaths.Add(vsInstance.InstallPath);
            }

            // Search each install path for MSVC with the preferred version at the front.
            var vcPaths = new List<string>();
            foreach (var vsInstallPath in vsInstallPaths)
            {
                _app.Log.Debug("Searching VS install path for MSVC: {0}", vsInstallPath);

                var path = _app.FileSystem.CombinePath(vsInstallPath, DK10MSVCSubDir);
                if (!_app.FileSystem.DirectoryExists(path)) continue;

                foreach (var vcVersionDir in _app.FileSystem.GetDirectoriesInDirectory(path))
                {
                    if (!Version.TryParse(_app.FileSystem.GetFileName(vcVersionDir), out var vcVersion)) continue;
                    path = _app.FileSystem.CombinePath(vcVersionDir, DK10MSVCBinDir);
                    if (!_app.FileSystem.DirectoryExists(path)) continue;

                    if (vcVersion.Major == DK10MSVCVersion) vcPaths.Insert(0, path);
                    else vcPaths.Add(path);
                }
            }

            if (!vcPaths.Any())
            {
                _app.Log.Warning("Unable to find any MSVC path.");
                return null;
            }

            return vcPaths[0];
        }

        private string GetDK10DotNetFrameworkPath()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(k_windowsDotNet, false))
            {
                if (key != null)
                {
                    var dir = key.GetString(DotNetInstallRootKey);
                    if (!string.IsNullOrEmpty(dir) && _app.FileSystem.DirectoryExists(dir))
                    {
                        dir = _app.FileSystem.CombinePath(dir, DK10NetFrameworkVersion);
                        if (_app.FileSystem.DirectoryExists(dir))
                        {
                            return dir;
                        }
                    }
                }
            }

            return null;
        }

        private string GetDK10WindowsSdkPath()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(k_windowsKits, false))
            {
                if (key != null)
                {
                    var dir = key.GetString(DK10KitsRootKey);
                    if (!string.IsNullOrEmpty(dir) && _app.FileSystem.DirectoryExists(dir))
                    {
                        dir = _app.FileSystem.CombinePath(dir, "bin", DK10WindowsSdkVersion, "x86");
                        if (_app.FileSystem.DirectoryExists(dir))
                        {
                            return dir;
                        }
                    }
                }
            }

            return null;
        }

        private void AddDevelopmentIncludePaths(List<string> include)
        {
            var vcProductDir = null as string;
            using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup32, false))
            {
                if (key != null)
                {
                    vcProductDir = key.GetString("ProductDir");
                }
            }
            if (vcProductDir == null)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup64, false))
                {
                    if (key != null)
                    {
                        vcProductDir = key.GetString("ProductDir");
                    }
                }
            }

            var sdkInstallDir = null as string;
            using (var key = Registry.LocalMachine.OpenSubKey(k_windowsKits, false))
            {
                if (key != null)
                {
                    sdkInstallDir = key.GetString("KitsRoot");
                }
            }

            if (!string.IsNullOrEmpty(_platformFolder)) include.Add(_app.FileSystem.CombinePath(_platformFolder, "include"));
            if (!string.IsNullOrEmpty(vcProductDir))
            {
                include.Add(_app.FileSystem.CombinePath(vcProductDir, "atlmfc\\include"));
                include.Add(_app.FileSystem.CombinePath(vcProductDir, "include"));
            }
            if (!string.IsNullOrEmpty(sdkInstallDir))
            {
                include.Add(_app.FileSystem.CombinePath(sdkInstallDir, "include\\um"));
                include.Add(_app.FileSystem.CombinePath(sdkInstallDir, "include\\shared"));
            }
        }

        private void AddDevelopmentLibPaths(List<string> lib)
        {
            var vcProductDir = null as string;
            using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup32, false))
            {
                if (key != null)
                {
                    vcProductDir = key.GetString("ProductDir");
                }
            }
            if (vcProductDir == null)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(k_vcSetup64, false))
                {
                    if (key != null)
                    {
                        vcProductDir = key.GetString("ProductDir");
                    }
                }
            }

            var sdkInstallDir = null as string;
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
                lib.Add(_app.FileSystem.CombinePath(vcProductDir, "atlmfc\\lib"));
                lib.Add(_app.FileSystem.CombinePath(vcProductDir, "lib"));
            }
            if (!string.IsNullOrEmpty(sdkInstallDir))
            {
                lib.Add(_app.FileSystem.CombinePath(sdkInstallDir, "lib\\win8\\um\\x86"));
            }
        }
    }
}
