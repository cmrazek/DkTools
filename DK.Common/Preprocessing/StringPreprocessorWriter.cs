using DK.Code;
using System.Text;

namespace DK.Preprocessing
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
			Append(source.Text, CodeAttributes.Empty);
		}

		public string Text
		{
			get { return _sb.ToString(); }
		}

		public void Flush()
		{
		}

		public int Position => _sb.Length;
	}
}
