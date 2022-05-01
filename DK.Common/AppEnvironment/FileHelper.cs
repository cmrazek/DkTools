using System;
using System.IO;

namespace DK.AppEnvironment
{
	public static class FileHelper
	{
		public static bool PathIsSameOrChildDir(string childDir, string parentDir)
		{
			if (parentDir.Length > childDir.Length) return false;
			if (!string.Equals(parentDir, childDir.Substring(0, parentDir.Length), StringComparison.OrdinalIgnoreCase)) return false;
			if (childDir.Length > parentDir.Length && childDir[parentDir.Length] != Path.DirectorySeparatorChar) return false;
			return true;
		}
	}
}
