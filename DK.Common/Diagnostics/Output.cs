using System.Text;

namespace DK.Diagnostics
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
}
