using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DkTools
{
	internal class FileUtil
	{
		/// <summary>
		/// Splits a string containing semi-colon delimited paths into a list of strings.
		/// </summary>
		/// <param name="str">The path string.</param>
		/// <returns>A list of paths contained in the string.</returns>
		public static List<string> ParsePathString(string str)
		{
			var ret = new List<string>();
			foreach (var path in str.Split(';'))
			{
				if (!string.IsNullOrWhiteSpace(path)) ret.Add(path.Trim());
			}
			return ret;
		}

		public static void CreateDirectoryRecursive(string path)
		{
			if (Directory.Exists(path)) return;
			CheckSubDirCreated(path);
			Directory.CreateDirectory(path);
		}

		private static void CheckSubDirCreated(string path)
		{
			var parent = Path.GetDirectoryName(path);
			if (parent == null) throw new DirectoryNotFoundException(string.Format("Unable to determine parent directory of path '{0}'.", path));
			if (!Directory.Exists(parent))
			{
				CheckSubDirCreated(parent);
				Directory.CreateDirectory(parent);
			}
		}

		public static string FindFileInPath(string relativeFileName)
		{
			foreach (var pathDir in Environment.GetEnvironmentVariable("path").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (Directory.Exists(pathDir))
				{
					var pathName = Path.Combine(pathDir, relativeFileName);
					if (File.Exists(pathName)) return pathName;
				}
			}

			return null;
		}

		public static void OpenExplorer(string path)
		{
			if (File.Exists(path))
			{
				Process.Start("explorer.exe", "/select," + path);
			}
			else if (Directory.Exists(path))
			{
				Process.Start("explorer.exe", path);
			}
			else
			{
				Process.Start("explorer.exe", Path.GetDirectoryName(path));
			}
		}
	}
}
