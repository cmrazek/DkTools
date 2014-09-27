using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal sealed class ErrorProvider
	{
		public ErrorProvider()
		{
		}

		public void ReportError(TokenParser.Parser parser, Span span, ErrorCode code, params object[] args)
		{
		}
	}

	internal enum ErrorCode
	{
		[Description("Expected ',' in enum option list.")]
		Enum_NoComma = 100,

		[Description("Duplicate enum option '{0}'.")]
		Enum_DuplicateOption = 101,

		[Description("Unexpected end of file in enum option list.")]
		Enum_UnexpectedEndOfFile = 102,

		[Description("Expected enum option rather than ','.")]
		Enum_UnexpectedComma = 103,
	}
}
