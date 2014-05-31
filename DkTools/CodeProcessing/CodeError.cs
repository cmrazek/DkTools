using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeProcessing
{
	internal class CodeError
	{
		private CodeLine _line;
		private string _message;

		public CodeError(CodeLine line, string message)
		{
			_line = line;
			_message = message;
		}

		public CodeLine Line
		{
			get { return _line; }
		}

		public string Message
		{
			get { return _message; }
		}
	}
}
