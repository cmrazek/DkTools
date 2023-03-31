using System;
using System.Collections.Generic;

namespace DK.AppEnvironment
{
    public interface IAppConfigSource
    {
        string GetDefaultAppName();

        IEnumerable<string> GetAllAppNames();

        string GetAppPath(string appName, WbdkAppPath path);

        IEnumerable<string> GetAppPathMulti(string appName, WbdkAppPath path);

        string GetAppConfig(string appName, WbdkAppConfig config);

        bool TryUpdateDefaultApp(string appName);

        WbdkPlatformInfo GetWbdkPlatformInfo();

        Version PlatformVersion { get; }

        int DefaultSamPort { get; }
    }

    public class WbdkPlatformInfo
    {
        public Version Version { get; set; }
        public string VersionText { get; set; }
        public string PlatformPath { get; set; }
    }

    public enum WbdkAppPath
    {
        SourcePaths,
        IncludePaths,
        LibPaths,
        ExecutablePaths,
        ObjectPath,
        DiagPath,
        ListingPath,
        DataPath,
        LogPath
    }

    public enum WbdkAppConfig
    {
        DB1ServerName,
        DB1SocketNumber,
        DB2ServerName,
        DB2SocketNumber,
        DB3ServerName,
        DB3SocketNumber,
        DB4ServerName,
        DB4SocketNumber,
    }
}
