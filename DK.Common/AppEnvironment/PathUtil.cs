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
            if (pathName.EndsWith(DirectorySeparatorString)) return string.Empty;

            var i = pathName.LastIndexOf(DirectorySeparatorChar);
            if (i >= 0)
            {
                return pathName.Substring(i + 1);
            }
            else
            {
                if (IsPathRooted(pathName)) return string.Empty;
                return pathName;
            }
        }

        public static string GetExtension(string pathName)
        {
            if (pathName == null) throw new ArgumentNullException(nameof(pathName));
            if (pathName.EndsWith(DirectorySeparatorString)) pathName = pathName.TrimEnd(DirectorySeparatorChar);

            // Find last '.' after the last '\'
            var s = pathName.LastIndexOf(DirectorySeparatorChar);
            var d = pathName.LastIndexOf('.');
            if (d >= 0 && d > s) return pathName.Substring(d);
            return string.Empty;
        }

        public static string GetFileNameWithoutExtension(string pathName)
        {
            var fileName = GetFileName(pathName);

            var d = fileName.LastIndexOf('.');
            if (d >= 0) return fileName.Substring(0, d);
            return fileName;
        }

        public static string GetDirectoryName(string pathName)
        {
            if (pathName == null) throw new ArgumentNullException(nameof(pathName));
            if (pathName.EndsWith(DirectorySeparatorString)) pathName = pathName.TrimEnd(DirectorySeparatorChar);

            var s = pathName.LastIndexOf(DirectorySeparatorChar);
            var dir = s >= 0 ? pathName.Substring(0, s) : string.Empty;
            return dir.TrimEnd(DirectorySeparatorChar);
        }

        public static string CombinePath(string path1, string path2)
        {
            if (IsPathRooted(path2)) return path2;

            path2 = path2.TrimStart(DirectorySeparatorChar);

            if (!string.IsNullOrEmpty(path1))
            {
                if (!string.IsNullOrEmpty(path2))
                {
                    if (path1.EndsWith(DirectorySeparatorString))
                    {
                        if (path2.StartsWith(DirectorySeparatorString))
                        {
                            return string.Concat(path1, path2.Substring(1));
                        }
                        else
                        {
                            return string.Concat(path1, path2);
                        }
                    }
                    else
                    {
                        if (path2.StartsWith(DirectorySeparatorString))
                        {
                            return string.Concat(path1, path2);
                        }
                        else
                        {
                            return string.Concat(path1, DirectorySeparatorString, path2);
                        }
                    }
                }
                else
                {
                    return path1;
                }
            }
            else
            {
                return path2;
            }
        }

        public static bool IsPathRooted(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith(DirectorySeparatorString)) return true;
            if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':') return true;
            return false;
        }
    }
}
