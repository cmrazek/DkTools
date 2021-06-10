using System;
using System.IO;

namespace DK.Diagnostics
{
	public class DumpWriter : IDisposable
	{
		private StreamWriter _writer;
		private int _indent;

		public DumpWriter(string fileName)
		{
			_writer = new StreamWriter(fileName);
		}

		public void Dispose()
		{
			Close();
		}

		public void Close()
		{
			if (_writer != null)
			{
				_writer.Close();
				_writer = null;
			}
		}

		public void WriteLine(string line)
		{
			for (int i = 0; i < _indent; i++) _writer.Write('\t');
			_writer.WriteLine(line);
		}

		public IndentScope Indent()
		{
			return new IndentScope(this);
		}

		public class IndentScope : IDisposable
		{
			private DumpWriter _dw;

			public IndentScope(DumpWriter dw)
			{
				_dw = dw;
				_dw._indent++;
			}

			public void Dispose()
			{
				_dw._indent--;
			}
		}
	}
}
