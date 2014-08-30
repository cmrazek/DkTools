using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal class StringPreprocessorWriter : IPreprocessorWriter
	{
		private StringBuilder _sb = new StringBuilder();
		private bool _isEmptyLine = true;

		public void Append(string text, CodeAttributes attribs)
		{
			_sb.Append(text);

			foreach (var ch in text)
			{
				if (ch == '\n') _isEmptyLine = true;
				else if (!char.IsWhiteSpace(ch)) _isEmptyLine = false;
			}
		}

		public void Append(CodeSource source)
		{
			Append(source.Text, CodeAttributes.Empty);
		}

		public string Text
		{
			get { return _sb.ToString(); }
		}

		public void Flush()
		{
		}

		public bool IsEmptyLine
		{
			get { return _isEmptyLine; }
		}
	}
}
