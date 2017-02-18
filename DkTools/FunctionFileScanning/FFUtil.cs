using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.FunctionFileScanning
{
	internal static class FFUtil
	{
		public static CodeModel.Definitions.ClassDefinition ClassFileNameToDefinition(string fileName)
		{
			return new CodeModel.Definitions.ClassDefinition(
				System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower(),
				fileName);
		}

		public static bool FileNameIsClass(string fileName, out string className)
		{
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
					className = System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower();
					return true;
				default:
					className = null;
					return false;
			}
		}

		public static bool FileNameIsFunction(string fileName)
		{
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

		public static string GetStringOrNull(this SQLiteDataReader rdr, int ordinal)
		{
			if (rdr.IsDBNull(ordinal)) return null;
			return rdr.GetString(ordinal);
		}

		public static int? GetInt32OrNull(this SQLiteDataReader rdr, int ordinal)
		{
			if (rdr.IsDBNull(ordinal)) return null;
			return rdr.GetInt32(ordinal);
		}

		public static bool GetTinyIntBoolean(this SQLiteDataReader rdr, int ordinal)
		{
			return rdr.GetByte(ordinal) != 0;
		}
	}
}
