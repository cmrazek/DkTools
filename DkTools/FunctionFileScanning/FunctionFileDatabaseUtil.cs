using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.FunctionFileScanning.FunctionFileDatabase;

namespace DkTools.FunctionFileScanning
{
	internal static class FunctionFileDatabaseUtil
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

		public static CodeModel.FunctionDefinition ToDefinition(this Function_t func)
		{
			return new CodeModel.FunctionDefinition(
				func.name,
				new CodeModel.ExternalToken(func.fileName, func.span.ToCodeModelSpan()),
				func.dataType != null ? func.dataType.ToCodeModelDataType() : CodeModel.DataType.Int,
				func.signature);
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
	}
}
