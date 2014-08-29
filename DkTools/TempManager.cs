using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DkTools
{
	internal static class TempManager
	{
		private static string _tempDir;
		private static bool _owned;

		public static void Init(string tempDir)
		{
			_tempDir = tempDir;
			_owned = true;
			try
			{
				if (!Directory.Exists(_tempDir)) Directory.CreateDirectory(_tempDir);
			}
			catch (AccessViolationException)
			{
				_tempDir = Environment.GetEnvironmentVariable("TEMP");
				_owned = false;
			}
		}

		public static string GetNewTempFileName(string fileTitle, string extension)
		{
			if (string.IsNullOrEmpty(_tempDir)) throw new InvalidOperationException("No temporary directory found.");

			if (_owned)
			{
				// Check for any files that can be purged out
				DateTime purgeTime = DateTime.Now.AddDays(-1);
				foreach (string file in Directory.GetFiles(_tempDir))
				{
					if (Directory.GetLastAccessTime(file) <= purgeTime)
					{
						try
						{
							File.Delete(file);
						}
						catch (Exception)
						{ }
					}
				}
			}

			if (string.IsNullOrWhiteSpace(fileTitle)) fileTitle = "Untitled";

			if (!extension.StartsWith(".")) extension = "." + extension;
			string pathName = Path.Combine(_tempDir, fileTitle + extension);
			int index = 0;
			while (File.Exists(pathName))
			{
				pathName = Path.Combine(_tempDir, string.Format("{0} ({1}){2}", fileTitle, ++index, extension));
			}

			return pathName;
		}
	}
}
