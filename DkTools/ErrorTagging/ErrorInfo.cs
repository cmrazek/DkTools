using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.ErrorTagging
{
	internal class ErrorInfo
	{
		public ErrorCode Code { get; set; }
		public string Message { get; set; }
		public Span Span { get; set; }
		public ErrorType Type { get; set; }
		public VsText.ITextSnapshot Snapshot { get; set; }

		public ErrorInfo(ErrorCode code, ErrorType type, string message, Span span, VsText.ITextSnapshot snapshot)
		{
			Code = code;
			Type = type;
			Message = message;
			Span = span;
			Snapshot = snapshot;
		}
	}
}
