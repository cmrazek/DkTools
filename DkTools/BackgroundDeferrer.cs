using DK.Diagnostics;
using System;
using System.Threading;
using System.Timers;

namespace DkTools
{
	internal class BackgroundDeferrer : IDisposable
	{
		private int _idleTime;
		private System.Timers.Timer _timer;
		private object _value;
		private int _priority;
		private CancellationTokenSource _cancel;

		public event EventHandler<IdleEventArgs> Idle;

		public class IdleEventArgs : EventArgs
		{
			public object Value { get; private set; }
			public int Priority { get; private set; }
			public CancellationToken CancellationToken { get; private set; }

			public IdleEventArgs(object value, int priority, CancellationToken cancel)
			{
				Value = value;
				Priority = priority;
				CancellationToken = cancel;
			}
		}

		public BackgroundDeferrer(int idleTime)
		{
			_idleTime = idleTime;
			_timer = new System.Timers.Timer(_idleTime);
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

			_cancel?.Cancel();
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
				_cancel = new CancellationTokenSource();

				var priority = _priority;
				_priority = 0;
				Idle?.Invoke(this, new IdleEventArgs(_value, priority, _cancel.Token));
			}
			catch (OperationCanceledException ex)
			{
				ProbeToolsPackage.Instance.App.Log.Debug(ex);
			}
			catch (Exception ex)
			{
				ProbeToolsPackage.Instance.App.Log.Error(ex);
			}
		}
	}
}
