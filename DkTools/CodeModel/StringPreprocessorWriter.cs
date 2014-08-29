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

		public void Append(string text, CodeAttributes attribs)
		{
			_sb.Append(text);
		}

		public void Append(CodeSource source)
		{
			_sb.Append(source.Text);
		}

		public string Text
		{
			get { return _sb.ToString(); }
		}

		public void Flush()
		{
		}
	}
}
