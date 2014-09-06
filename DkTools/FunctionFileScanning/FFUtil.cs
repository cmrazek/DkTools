using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;
using DkTools.FunctionFileScanning.FunctionFileDatabase;

namespace DkTools.FunctionFileScanning
{
	internal static class FFUtil
	{
		public static CodeModel.Span ToCodeModelSpan(this Span_t span)
		{
			return new CodeModel.Span(span.start.ToCodeModelPosition(), span.end.ToCodeModelPosition());
		}

		public static CodeModel.Position ToCodeModelPosition(this Position_t pos)
		{
			return new CodeModel.Position(pos.offset, pos.lineNum, pos.linePos);
		}

		public static Span_t ToFunctionFileDatabase(this CodeModel.Span span)
		{
			return new Span_t
			{
				start = span.Start.ToFunctionFileDatabase(),
				end = span.End.ToFunctionFileDatabase()
			};
		}

		public static Position_t ToFunctionFileDatabase(this CodeModel.Position pos)
		{
			return new Position_t
			{
				offset = pos.Offset,
				lineNum = pos.LineNum,
				linePos = pos.LinePos
			};
		}

		public static CodeModel.Definitions.FunctionDefinition ToDefinition(this Function_t func)
		{
			return new CodeModel.Definitions.FunctionDefinition(
				new CodeModel.Scope(),
				func.name,
				new CodeModel.ExternalToken(func.fileName, func.span.ToCodeModelSpan()),
				func.dataType != null ? func.dataType.ToCodeModelDataType() : CodeModel.DataType.Int,
				func.signature,
				CodeModel.Position.Start,
				CodeModel.Position.Start,
				func.privacySpecified ? func.privacy.ToCodeModelFunctionPrivacy() : CodeModel.FunctionPrivacy.Public,
				false);
		}

		public static CodeModel.Definitions.ClassDefinition ClassFileNameToDefinition(string fileName)
		{
			return new CodeModel.Definitions.ClassDefinition(
				new CodeModel.Scope(),
				System.IO.Path.GetFileNameWithoutExtension(fileName).ToLower(),
				fileName);
		}

		public static CodeModel.DataType ToCodeModelDataType(this DataType_t dt)
		{
#if DEBUG
			if (dt == null) throw new ArgumentNullException("dt");
#endif
			if (dt.completionOption != null && dt.completionOption.Length > 0)
			{
				return new CodeModel.DataType(dt.name, dt.completionOption, dt.name);
			}
			else
			{
				return new CodeModel.DataType(dt.name, dt.name);
			}
		}

		public static DataType_t ToFuncDbDataType(this CodeModel.DataType dt)
		{
#if DEBUG
			if (dt == null) throw new ArgumentNullException("dt");
#endif
			return new DataType_t
			{
				name = dt.Name,
				completionOption = dt.HasCompletionOptions ? dt.CompletionOptions.ToArray() : null
			};
		}

		public static CodeModel.FunctionPrivacy ToCodeModelFunctionPrivacy(this FunctionPrivacy_t priv)
		{
			switch (priv)
			{
				case FunctionPrivacy_t.Public:
					return CodeModel.FunctionPrivacy.Public;
				case FunctionPrivacy_t.Private:
					return CodeModel.FunctionPrivacy.Private;
				case FunctionPrivacy_t.Protected:
					return CodeModel.FunctionPrivacy.Protected;
#if DEBUG
				default:
					throw new ArgumentOutOfRangeException("priv", string.Format("Unknown function privacy value '{0}'.", priv));
#endif
			}
		}

		public static FunctionPrivacy_t ToFuncDbFunctionPrivacy(this CodeModel.FunctionPrivacy priv)
		{
			switch (priv)
			{
				case CodeModel.FunctionPrivacy.Public:
					return FunctionPrivacy_t.Public;
				case CodeModel.FunctionPrivacy.Private:
					return FunctionPrivacy_t.Private;
				case CodeModel.FunctionPrivacy.Protected:
					return FunctionPrivacy_t.Protected;
#if DEBUG
				default:
					throw new ArgumentOutOfRangeException("priv", string.Format("Unknown function privacy value '{0}'.", priv));
#endif
			}
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
	}
}
