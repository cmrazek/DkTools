using DK.Diagnostics;
using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DkTools
{
	public sealed class ProcessRunner : IDisposable
	{
		bool _captureOutput = true;
		bool _captureError = true;
		Output _output = null;
		ArrayList _outputLines = new ArrayList();
		ProcessRunnerThread _outputThread = null;
		ProcessRunnerThread _errorThread = null;
		Process _proc = null;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_proc != null)
				{
					_proc.Dispose();
					_proc = null;
				}
			}
		}

		public int CaptureProcess(string fileName, string args, string workingDir, Output output, CancellationToken cancel)
		{
			_output = output;
			return DoCapture(fileName, args, workingDir, cancel);
		}

		public async Task<int> CaptureProcessAsync(string fileName, string args, string workingDir, Output output, CancellationToken cancel)
		{
			_output = output;
			return await DoCaptureAsync(fileName, args, workingDir, cancel);
		}

		public int ExecuteProcess(string fileName, string args, string workingDir, bool waitForExit)
		{
			using (Process proc = new Process())
			{
				ProcessStartInfo info = new ProcessStartInfo(fileName, args);
				info.UseShellExecute = false;
				info.RedirectStandardInput = false;
				info.RedirectStandardOutput = false;
				info.CreateNoWindow = true;
				info.WorkingDirectory = workingDir;
				proc.StartInfo = info;
				if (!proc.Start()) return 1;

				if (waitForExit)
				{
					proc.WaitForExit();
					return proc.ExitCode;
				}
				else
				{
					return 0;
				}
			}
		}

		private int DoCapture(string fileName, string args, string workingDir, CancellationToken cancel)
		{
			Kill();

			_proc = new Process();
			ProcessStartInfo info = new ProcessStartInfo(fileName, args);
			info.UseShellExecute = false;
			info.RedirectStandardOutput = _captureOutput;
			info.RedirectStandardError = _captureError;
			info.CreateNoWindow = true;
			info.WorkingDirectory = workingDir;
			_proc.StartInfo = info;
			if (!_proc.Start()) throw new ProcessRunnerException(string.Format("Failed to start process '{0}'.", fileName));

			lock(_outputLines)
			{
				_outputLines.Clear();
			}

			_outputThread = null;
			_errorThread = null;
			if (_captureOutput)
			{
				_outputThread = new ProcessRunnerThread(this, false, _proc);
				_outputThread.Start("StdOut Capture Thread");
			}
			if (_captureError)
			{
				_errorThread = new ProcessRunnerThread(this, true, _proc);
				_errorThread.Start("StdErr Capture Thread");
			}

			string line;

			// Grabs the lines while the process runs.
			while ((_outputThread != null && _outputThread.IsAlive) ||
				(_errorThread != null && _errorThread.IsAlive))
			{
				if (cancel.IsCancellationRequested)
				{
					Kill();
					throw new OperationCanceledException(cancel);
				}

				lock(_outputLines)
				{
					while (_outputLines.Count > 0)
					{
						line = (string)_outputLines[0];
						if (line != null)
						{
							if (_output != null) _output.WriteLine(line);
							_outputLines.RemoveAt(0);
						}
					}
				}
				System.Threading.Thread.Sleep(100);
			}

			// Process has finished. Grab the rest of the lines.
			lock(_outputLines)
			{
				while (_outputLines.Count > 0)
				{
					line = (string)_outputLines[0];
					if (line != null)
					{
						if (_output != null) _output.WriteLine(line);
						_outputLines.RemoveAt(0);
					}
				}
			}

			while (!_proc.HasExited)
			{
				System.Threading.Thread.Sleep(100);
			}

			int exitCode = _proc.ExitCode;
			_proc = null;
			return exitCode;
		}

		private async Task<int> DoCaptureAsync(string fileName, string args, string workingDir, CancellationToken cancel)
		{
			Kill();

			_proc = new Process();
			ProcessStartInfo info = new ProcessStartInfo(fileName, args);
			info.UseShellExecute = false;
			info.RedirectStandardOutput = _captureOutput;
			info.RedirectStandardError = _captureError;
			info.CreateNoWindow = true;
			info.WorkingDirectory = workingDir;
			_proc.StartInfo = info;
			if (!_proc.Start()) throw new ProcessRunnerException(string.Format("Failed to start process '{0}'.", fileName));

			lock (_outputLines)
			{
				_outputLines.Clear();
			}

			_outputThread = null;
			_errorThread = null;
			if (_captureOutput)
			{
				_outputThread = new ProcessRunnerThread(this, false, _proc);
				_outputThread.Start("StdOut Capture Thread");
			}
			if (_captureError)
			{
				_errorThread = new ProcessRunnerThread(this, true, _proc);
				_errorThread.Start("StdErr Capture Thread");
			}

			string line;

			// Grabs the lines while the process runs.
			while ((_outputThread != null && _outputThread.IsAlive) ||
				(_errorThread != null && _errorThread.IsAlive))
			{
				if (cancel.IsCancellationRequested)
				{
					Kill();
					throw new OperationCanceledException(cancel);
				}

				lock (_outputLines)
				{
					while (_outputLines.Count > 0)
					{
						line = (string)_outputLines[0];
						if (line != null)
						{
							if (_output != null) _output.WriteLine(line);
							_outputLines.RemoveAt(0);
						}
					}
				}
				System.Threading.Thread.Sleep(100);
			}

			// Process has finished. Grab the rest of the lines.
			lock (_outputLines)
			{
				while (_outputLines.Count > 0)
				{
					line = (string)_outputLines[0];
					if (line != null)
					{
						if (_output != null) _output.WriteLine(line);
						_outputLines.RemoveAt(0);
					}
				}
			}

			while (!_proc.HasExited)
			{
				await Task.Delay(100);
			}

			int exitCode = _proc.ExitCode;
			_proc = null;
			return exitCode;
		}

		void OutputLine(string line)
		{
			lock(_outputLines)
			{
				_outputLines.Add(line);
			}
		}

		public bool CaptureOutput
		{
			get { return _captureOutput; }
			set { _captureOutput = value; }
		}

		public bool CaptureError
		{
			get { return _captureError; }
			set { _captureError = value; }
		}

		public bool IsAlive
		{
			get
			{
				return (_outputThread != null && _outputThread.IsAlive) ||
					(_errorThread != null && _errorThread.IsAlive);
			}
		}

		public void Kill()
		{
			if (_outputThread != null)
			{
				if (_outputThread.IsAlive) _outputThread.Abort();
				_outputThread = null;
			}

			if (_errorThread != null)
			{
				if (_errorThread.IsAlive) _errorThread.Abort();
				_errorThread = null;
			}

			if (_proc != null)
			{
				if (!_proc.HasExited) _proc.Kill();
				_proc = null;
			}
		}

		#region Runner thread
		class ProcessRunnerThread
		{
			bool _doErrors = false;
			ProcessRunner _runner = null;
			Thread _thread = null;
			Process _proc = null;
			StreamReader _reader = null;

			public ProcessRunnerThread(ProcessRunner runner, bool doErrors, Process proc)
			{
				_runner = runner;
				_doErrors = doErrors;
				_proc = proc;
			}

			public void Start(string name)
			{
				_thread = new System.Threading.Thread(new System.Threading.ThreadStart(RunnerThread));
				_thread.Name = name;
				_thread.Start();
			}

			void RunnerThread()
			{
				try
				{
					if (_doErrors) _reader = _proc.StandardError;
					else _reader = _proc.StandardOutput;

					string line = "";
					while (line != null)
					{
						line = _reader.ReadLine();
						if (line != null) _runner.OutputLine(line);
					}
				}
				catch (Exception ex)
				{
					_runner.OutputLine("Exception: " + ex.Message);
					_runner.OutputLine(ex.StackTrace);
				}
			}

			public bool IsAlive
			{
				get
				{
					if (_thread != null) return _thread.IsAlive;
					return false;
				}
			}

			public void Abort()
			{
				if (_thread != null)
				{
					_thread.Abort();
					_thread = null;
				}
			}
		}
		#endregion
	}

	[Serializable]
	public class ProcessRunnerException : Exception
	{
		public ProcessRunnerException(string message)
			: base(message)
		{
		}
	}
}
