using System;

namespace DK.AppEnvironment
{
    public static class PathUtil
    {
        public const char DirectorySeparatorChar = '\\';
        public const string DirectorySeparatorString = "\\";

        public static string GetFileName(string pathName)
        {
            if (pathName == null) throw new ArgumentNullException(nameof(pathName));
            if (pathName.EndsWith(DirectorySeparatorString)) pathName = pathName.TrimEnd(DirectorySeparatorChar);
            var i = pathName.LastIndexOf(DirectorySeparatorChar);
            return i >= 0 ? pathName.Substring(i + 1) : pathName;
        }
    }
}
