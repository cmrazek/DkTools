using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
	internal static class RegistryExtensions
	{
		public static string GetString(this RegistryKey key, string valueName, string defaultValue = null)
		{
			return key?.GetValue(valueName)?.ToString() ?? defaultValue;
		}

		private static bool WbdkPathIsRooted(string path)
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

		public static string CombineWbdkPath(string rootPath, string path)
		{
			if (string.IsNullOrWhiteSpace(rootPath)) return path;
			if (string.IsNullOrWhiteSpace(path)) return rootPath;
			if (WbdkPathIsRooted(path)) return path;

			return string.Concat(
				rootPath?.TrimEnd(Path.DirectorySeparatorChar),
				Path.DirectorySeparatorChar,
				path?.TrimStart(Path.DirectorySeparatorChar));
		}

		public static string LoadWbdkPath(this RegistryKey key, string valueName, string rootPath)
		{
			return CombineWbdkPath(rootPath, key?.GetString(valueName));
		}

		public static string[] LoadWbdkMultiPath(this RegistryKey key, string valueName, string rootPath)
		{
			return (key?.GetString(valueName) ?? string.Empty)
				.Split(';')
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => CombineWbdkPath(rootPath, x))
				.ToArray();
		}
	}
}
