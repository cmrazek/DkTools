using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
	internal sealed class CsvWriter : IDisposable
	{
		private StreamWriter _file;
		private bool _first = true;
		private StringBuilder _sb = new StringBuilder();

		public CsvWriter(string fileName)
		{
			_file = new StreamWriter(fileName);
		}

		public void Dispose()
		{
			if (_file != null)
			{
				_file.Close();
				_file = null;
			}
		}

		public void Write(string cellContent)
		{
			if (_first) _first = false;
			else _file.Write(',');

			_sb.Clear();
			var quotesRequired = false;

			foreach (var ch in cellContent)
			{
				if (ch == ' ' || ch == '\t')
				{
					quotesRequired = true;
					_sb.Append(ch);
				}
				else if (ch == '\r')
				{
					// Ignore
				}
				else if (ch == '\n')
				{
					quotesRequired = true;
					_sb.Append(' ');
				}
				else if (ch == '\"')
				{
					quotesRequired = true;
					_sb.Append("\"\"");
				}
				else
				{
					_sb.Append(ch);
				}
			}

			if (quotesRequired) _file.Write('\"');
			_file.Write(_sb.ToString());
			if (quotesRequired) _file.Write('\"');
		}

		public void EndLine()
		{
			_file.WriteLine();
			_first = true;
		}
	}
}
