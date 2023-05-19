using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DK.Diagnostics
{
	public abstract class Output
	{
		public abstract void WriteLine(string text);

		public abstract Task WriteLineAsync(string text);
	}

	public class StringOutput : Output
	{
		StringBuilder _sb = new StringBuilder();
		SemaphoreSlim _sem = new SemaphoreSlim(1);

		public override void WriteLine(string text)
		{
			_sem.Wait();
			_sb.AppendLine(text);
			_sem.Release();
		}

        public override async Task WriteLineAsync(string text)
        {
			await _sem.WaitAsync();
			_sb.AppendLine(text);
			_sem.Release();
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

        public override Task WriteLineAsync(string text)
        {
			if (_callback != null) _callback(text);
			return Task.CompletedTask;
        }
    }
}
