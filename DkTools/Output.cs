using System;
using System.IO;
using System.Text;

namespace DkTools
{
	public class Output
	{
		public virtual void WriteLine(string text)
		{
		}
	}

	public class StringOutput : Output
	{
		StringBuilder _sb = new StringBuilder();

		public override void WriteLine(string text)
		{
			lock (this)
			{
				_sb.AppendLine(text);
			}
		}

		public string Text
		{
			get { lock (this) { return _sb.ToString(); } }
		}

		public void Clear()
		{
			lock (this)
			{
				_sb.Remove(0, _sb.Length);
			}
		}
	}

	/*
	public class OutputWindowOutput : Output
	{
		OutputWindowPane	_ow = null;

		public OutputWindowOutput(string outputWindowTitle)
		{
			_ow = Utils.GetOutputWindowPane(outputWindowTitle);
		}

		public OutputWindowOutput(OutputWindowPane outputWindow)
		{
			_ow = outputWindow;
		}

		public override void Write(string text)
		{
			_ow.OutputString(text);
		}

		public override void WriteLine(string text)
		{
			_ow.OutputString(text + "\r\n");
		}
	}
	*/

	public class CallbackOutput : Output
	{
		public delegate void OutputCallback(string text);

		private OutputCallback _callback;
		private object _lock = new object();

		public CallbackOutput()
		{
		}

		public CallbackOutput(OutputCallback callback)
		{
			_callback = callback;
		}

		public OutputCallback Callback
		{
			get { return _callback; }
			set { _callback = value; }
		}

		public override void WriteLine(string text)
		{
			if (_callback != null) _callback(text);
		}
	}

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
