#if REPORT_ERRORS
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ErrorTagging
{
	internal sealed class ErrorProvider
	{
		private List<ErrorInfo> _errors = new List<ErrorInfo>();

		/// <summary>
		/// Called when an error has been reported. The recipient of this event can modify the details of the error, if desired.
		/// </summary>
		public event EventHandler<ErrorReportedEventArgs> ErrorReported;

		public void ReportError(TokenParser.Parser parser, CodeModel.Span span, ErrorCode code, params object[] args)
		{
			var message = GetErrorCodeDescription(code);
			if (!string.IsNullOrEmpty(message))
			{
				if (args != null && args.Length > 0)
				{
					message = string.Format(message, args);
				}
			}
			else
			{
				message = "(unknown)";
			}

			if (parser.DocumentOffset != 0)
			{
				span = new CodeModel.Span(span.Start + parser.DocumentOffset, span.End + parser.DocumentOffset);
			}

			var err = new ErrorInfo(code, message, span);

			var ev = ErrorReported;
			if (ev != null)
			{
				var evArgs = new ErrorReportedEventArgs(parser, span, code, message);
				ev(this, evArgs);
				err.Code = evArgs.Code;
				err.Message = evArgs.Message;
				err.Span = evArgs.Span;
			}

			_errors.Add(err);
		}

		public static string GetErrorCodeDescription(ErrorCode code)
		{
			var memInfo = typeof(ErrorCode).GetMember(code.ToString()).FirstOrDefault();
			if (memInfo == null) return null;

			foreach (DescriptionAttribute attrib in memInfo.GetCustomAttributes(typeof(DescriptionAttribute), false))
			{
				return attrib.Description;
			}

			return null;
		}

		public IEnumerable<ErrorInfo> GetErrorsForPos(int pos)
		{
			foreach (var error in _errors)
			{
				if (error.Span.Contains(pos)) yield return error;
			}
		}

		public IEnumerable<ErrorInfo> Errors
		{
			get { return _errors; }
		}
	}

	internal class ErrorReportedEventArgs : EventArgs
	{
		private TokenParser.Parser _parser;
		private CodeModel.Span _span;
		private ErrorCode _code;
		private string _message;

		public ErrorReportedEventArgs(TokenParser.Parser parser, CodeModel.Span span, ErrorCode code, string message)
		{
			_parser = parser;
			_span = span;
			_code = code;
			_message = message;
		}

		public TokenParser.Parser Parser
		{
			get { return _parser; }
		}

		public CodeModel.Span Span
		{
			get { return _span; }
			set { _span = value; }
		}

		public ErrorCode Code
		{
			get { return _code; }
			set { _code = value; }
		}

		public string Message
		{
			get { return _message; }
			set { _message = value; }
		}
	}
}
#endif
