using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace DkTools
{
	internal class BackgroundDeferrer : IDisposable
	{
		private int _idleTime;
		private Timer _timer;
		private object _value;

		public event EventHandler<IdleEventArgs> Idle;

		public class IdleEventArgs : EventArgs
		{
			public object Value { get; set; }
		}

		public BackgroundDeferrer(int idleTime = 1000)
		{
			_idleTime = idleTime;
			_timer = new Timer(idleTime);
			_timer.AutoReset = false;
			_timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
		}

		void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				var ev = Idle;
				if (ev != null) ev(this, new IdleEventArgs { Value = _value });
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.WriteLine("Exception in BackgroundDeferrer.Elapse: " + ex.ToString());
			}
		}

		public void OnActivity(object value = null)
		{
			_value = value;
			_timer.Stop();
			_timer.Start();
		}

		public void Dispose()
		{
			if (_timer != null)
			{
				_timer.Dispose();
				_timer = null;
			}
		}
	}
}
