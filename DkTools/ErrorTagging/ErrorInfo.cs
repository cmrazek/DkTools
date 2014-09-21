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
		public Span Span { get; private set; }
		public string Message { get; private set; }

		public ErrorInfo(string message, Span span)
		{
			Message = message;
			Span = span;
		}
	}
}
