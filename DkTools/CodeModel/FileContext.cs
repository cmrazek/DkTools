using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal enum FileContext
	{
		ServerTrigger,
		ServerClass,
		ServerProgram,
		ClientTrigger,
		ClientClass,
		GatewayProgram,
		NeutralClass,
		Function,
		Dictionary,
		Include
	}

	internal static class FileContextUtil
	{
		public static FileContext GetFileContextFromFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return FileContext.Include;


			var titleExt = System.IO.Path.GetFileName(fileName);
			if (titleExt.Equals("dict", StringComparison.OrdinalIgnoreCase) ||
				titleExt.Equals("dict+", StringComparison.OrdinalIgnoreCase) ||
				titleExt.Equals("dict&", StringComparison.OrdinalIgnoreCase))
			{
				return FileContext.Dictionary;
			}

			var ext = System.IO.Path.GetExtension(fileName);
			switch (ext.ToLower())
			{
				case ".sc":
				case ".sc+":
				case ".sc&":
					return FileContext.ServerClass;
				case ".st":
				case ".st+":
				case ".st&":
					return FileContext.ServerTrigger;
				case ".sp":
				case ".sp+":
				case ".sp&":
					return FileContext.ServerProgram;
				case ".cc":
				case ".cc+":
				case ".cc&":
					return FileContext.ClientClass;
				case ".ct":
				case ".ct+":
				case ".ct&":
					return FileContext.ClientTrigger;
				case ".gp":
				case ".gp+":
				case ".gp&":
					return FileContext.GatewayProgram;
				case ".nc":
				case ".nc+":
				case ".nc&":
					return FileContext.NeutralClass;
				case ".f":
				case ".f+":
				case ".f&":
					return FileContext.Function;
				default:
					return FileContext.Include;
			}
		}

		public static bool IsClass(this FileContext fc)
		{
			return fc == FileContext.ServerClass || fc == FileContext.ClientClass || fc == FileContext.NeutralClass;
		}

		public static string GetClassNameFromFileName(string fileName)
		{
			var context = GetFileContextFromFileName(fileName);
			if (!context.IsClass()) return null;
			return System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower();
		}

		public static bool IsLocalizedFile(string fileName)
		{
			var ext = System.IO.Path.GetExtension(fileName);
			return ext.EndsWith("+") || ext.EndsWith("&");
		}
	}
}
