using EnvDTE;
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
		private int _priority;

		public event EventHandler<IdleEventArgs> Idle;

		public class IdleEventArgs : EventArgs
		{
			public object Value { get; private set; }
			public int Priority { get; private set; }

			public IdleEventArgs(object value, int priority)
			{
				Value = value;
				Priority = priority;
			}
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
				Execute();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.WriteLine("Exception in BackgroundDeferrer.Elapse: " + ex.ToString());
			}
		}

		public void OnActivity(object value = null, int priority = 0)
		{
			_value = value;
			if (priority > _priority) _priority = priority;
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

		public void ExecuteNowIfPending()
		{
			if (_timer.Enabled)
			{
				_timer.Stop();
				Execute();
			}
		}

		private void Execute()
		{
			try
			{
				var priority = _priority;
				_priority = 0;
				Idle?.Invoke(this, new IdleEventArgs(_value, priority));
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}
	}
}
