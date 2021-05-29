using DK.Diagnostics;
using System;
using System.IO;

namespace DkTools
{

	public class TempFileOutput : Output, IDisposable
	{
		private string _fileName = "";
		private StreamWriter _writer = null;

		public TempFileOutput(string fileTitle, string extension)
		{
			_fileName = TempManager.GetNewTempFileName(fileTitle, extension);
			_writer = new StreamWriter(_fileName);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_writer != null)
				{
					_writer.Close();
					_writer = null;
				}
			}
		}

		public override void WriteLine(string text)
		{
			if (_writer != null) _writer.WriteLine(text);
		}

		public void Close()
		{
			Dispose();
		}

		public string FileName
		{
			get { return _fileName; }
		}
	}
}
