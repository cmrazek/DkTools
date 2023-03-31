using DK.AppEnvironment;
using DK.Diagnostics;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DK.Implementation.Windows
{
    public class WindowsAppConfigSource : IAppConfigSource
    {
        private ILogger _log;

        public WindowsAppConfigSource(ILogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public const string WbdkRegKey = "Software\\Fincentric\\WBDK";

        private const string BaseKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK";
        private const string BaseKey32 = @"SOFTWARE\Fincentric\WBDK";
        private const string CurrentConfigName = "CurrentConfig";

        private const string ConfigurationsKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK\Configurations";
        private const string ConfigurationsKey32 = @"SOFTWARE\Fincentric\WBDK\Configurations";

        private const string AppKey64 = @"SOFTWARE\WOW6432Node\Fincentric\WBDK\Configurations\{0}";
        private const string AppKey32 = @"SOFTWARE\Fincentric\WBDK\Configurations\{0}";

        public int DefaultSamPort => 5001;

        public string GetDefaultAppName()
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

        public IEnumerable<string> GetAllAppNames()
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

        public string GetAppPath(string appName, WbdkAppPath path)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey64, appName), writable: false))
            {
                if (key != null)
                {
                    var rootPath = key.GetValue("RootPath")?.ToString();
                    var regPath = key.GetValue(WbdkAppPathToString(path))?.ToString();
                    return CombineWbdkPath(rootPath, regPath);
                }
            }

            using (var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey32, appName), writable: false))
            {
                if (key != null)
                {
                    var rootPath = key.GetValue("RootPath")?.ToString();
                    var regPath = key.GetValue(WbdkAppPathToString(path))?.ToString();
                    return CombineWbdkPath(rootPath, regPath);
                }
            }

            return null;
        }

        public IEnumerable<string> GetAppPathMulti(string appName, WbdkAppPath path)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey64, appName), writable: false))
            {
                if (key != null)
                {
                    var rootPath = key.GetValue("RootPath")?.ToString();
                    var regPath = key.GetValue(WbdkAppPathToString(path))?.ToString();
                    return regPath?.Split(';')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => CombineWbdkPath(rootPath, x));
                }
            }

            using (var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey32, appName), writable: false))
            {
                if (key != null)
                {
                    var rootPath = key.GetValue("RootPath")?.ToString();
                    var regPath = key.GetValue(WbdkAppPathToString(path))?.ToString();
                    return regPath?.Split(';')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => CombineWbdkPath(rootPath, x));
                }
            }

            return new string[0];
        }

        public string GetAppConfig(string appName, WbdkAppConfig config)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey64, appName), writable: false))
            {
                if (key != null)
                {
                    return key.GetValue(config.ToString())?.ToString();
                }
            }

            using (var key = Registry.LocalMachine.OpenSubKey(string.Format(AppKey32, appName), writable: false))
            {
                if (key != null)
                {
                    return key.GetValue(config.ToString())?.ToString();
                }
            }

            return null;
        }

        public bool TryUpdateDefaultApp(string appName)
        {
            if (appName == null) throw new ArgumentNullException(nameof(appName));

            try
            {
                // Read the current value from the registry in read-only mode, to see if it needs updating.
                using (var key = Registry.LocalMachine.OpenSubKey(WbdkRegKey, false))
                {
                    var value = Convert.ToString(key.GetValue(CurrentConfigName, string.Empty));
                    if (value == appName)
                    {
                        // No update required.
                        return true;
                    }
                }

                // Try to update the registry.
                using (var key = Registry.LocalMachine.OpenSubKey(WbdkRegKey, true))
                {
                    key.SetValue(CurrentConfigName, appName);
                }

                return true;
            }
            catch (System.Security.SecurityException ex)
            {
                _log.Warning(ex, "Failed to update default DK application.");
                return false;
            }
        }

        private WbdkPlatformInfo _platformInfo;

        public WbdkPlatformInfo GetWbdkPlatformInfo()
        {
            // FEC.exe will be located in the platform folder, and the version number
            // on that file is the same as the WBDK platform version.
            try
            {
                if (_platformInfo != null) return _platformInfo;

                foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(';').Select(x => x.Trim()))
                {
                    var fileName = Path.Combine(path, "fec.exe");
                    if (File.Exists(fileName))
                    {
                        var platformInfo = new WbdkPlatformInfo();

                        _log.Debug("Located FEC.exe: {0}", fileName);

                        platformInfo.VersionText = FileVersionInfo.GetVersionInfo(fileName)?.FileVersion ?? string.Empty;

                        if (!Version.TryParse(platformInfo.VersionText, out var version)) platformInfo.Version = version;
                        else platformInfo.Version = new Version(1, 0);

                        platformInfo.PlatformPath = path;
                        _log.Debug("WBDK Platform Directory: {0}", platformInfo.PlatformPath);
                        _log.Debug("WBDK Platform Version: {0}", platformInfo.VersionText);

                        return _platformInfo = platformInfo;
                    }
                }

                throw new FileNotFoundException("FEC.exe not found.");
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to get WBDK platform info.");
                return null;
            }
        }

        public Version PlatformVersion => GetWbdkPlatformInfo()?.Version ?? new Version(1, 0);

        private bool WbdkPathIsRooted(string path)
        {
            if (path == null) return false;

            // Check for drive letter followed by ':'.
            // Don't use char.IsLetter() because that can be a false-positive for unicode letters.
            if (path.Length >= 2
                && ((path[0] >= 'A' && path[0] <= 'Z') || (path[0] >= 'a' && path[0] <= 'z'))
                && path[1] == ':')
            {
                return true;
            }

            // Check for \\servername
            if (path.Length >= 3 && path[0] == '\\' && path[1] == '\\' && char.IsLetterOrDigit(path[2]))
            {
                return true;
            }

            return false;
        }

        public string CombineWbdkPath(string rootPath, string path)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) return path;
            if (string.IsNullOrWhiteSpace(path)) return rootPath;
            if (WbdkPathIsRooted(path)) return path;

            return string.Concat(
                rootPath?.TrimEnd(PathUtil.DirectorySeparatorChar),
                PathUtil.DirectorySeparatorChar,
                path?.TrimStart(PathUtil.DirectorySeparatorChar));
        }

        private string WbdkAppPathToString(WbdkAppPath path)
        {
            switch (path)
            {
                case WbdkAppPath.SourcePaths: return "SourcePaths";
                case WbdkAppPath.IncludePaths: return "IncludePaths";
                case WbdkAppPath.LibPaths: return "LibPaths";
                case WbdkAppPath.ExecutablePaths: return "ExecutablePaths";
                case WbdkAppPath.ObjectPath: return "ObjectPath";
                case WbdkAppPath.DiagPath: return "DiagPath";
                case WbdkAppPath.ListingPath: return "ListingPath";
                case WbdkAppPath.DataPath: return "DataPath";
                case WbdkAppPath.LogPath: return "LogPath";
                default: throw new InvalidWbdkAppPathException();
            }
        }

        class InvalidWbdkAppPathException : Exception { }
    }
}
