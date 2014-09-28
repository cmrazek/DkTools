#if REPORT_ERRORS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.ErrorTagging
{
	internal class ErrorInfo
	{
		public ErrorCode Code { get; set; }
		public string Message { get; set; }
		public Span Span { get; set; }

		public ErrorInfo(ErrorCode code, string message, Span span)
		{
			Code = code;
			Message = message;
			Span = span;
		}
	}
}
#endif
