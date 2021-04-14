using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.FunctionFileScanning
{
	internal static class FFUtil
	{
		public static bool FileNameIsClass(string fileName, out string className)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				className = null;
				return false;
			}

			var ext = System.IO.Path.GetExtension(fileName).ToLower();
			switch (ext)
			{
				case ".cc":
				case ".cc&":
				case ".cc+":
				case ".nc":
				case ".nc&":
				case ".nc+":
				case ".sc":
				case ".sc&":
				case ".sc+":
					className = System.IO.Path.GetFileNameWithoutExtension(fileName);
					return true;
				default:
					className = null;
					return false;
			}
		}

		public static bool FileNameIsFunction(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return false;

			var ext = System.IO.Path.GetExtension(fileName).ToLower();
			switch (ext)
			{
				case ".f":
				case ".f&":
				case ".f+":
					return true;
				default:
					return false;
			}
		}
	}
}
