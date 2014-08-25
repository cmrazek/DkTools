using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DkTools.Compiler
{
	internal class ProbeCompiler : IDisposable
	{
		private OutputPane _pane;
		private Guid _paneGuid = new Guid("{BD87845A-95F6-4BEB-B9A8-CABE3B01E247}");

		private Thread _compileThread = null;
		private CompileMethod _method = CompileMethod.Compile;
		private volatile bool _kill;
		private Process _proc = null;
		private int _numErrors = 0;
		private int _numWarnings = 0;
		private bool _buildFailed = false;

		private const int k_compileKillSleep = 10;
		private const int k_compileSleep = 100;
		private const string k_timeStampFormat = "yyyy-MM-dd HH:mm:ss";
		private const int k_compileWaitToHidePanel = 1000;
		private const int k_killCompileTimeout = 1000;

		private static ProbeCompiler _instance = new ProbeCompiler();

		public enum CompileMethod
		{
			Compile,
			Dccmp,
			Credelix
		}

		public static ProbeCompiler Instance
		{
			get { return _instance; }
		}

		public void Dispose()
		{
			if (_proc != null)
			{
				_proc.Dispose();
				_proc = null;
			}
		}

		public void Compile(CompileMethod method)
		{
			try
			{
				_method = method;

				if (_compileThread == null || !_compileThread.IsAlive)
				{
					if (_pane == null)
					{
						_pane = Shell.CreateOutputPane(_paneGuid, Constants.CompileOutputPaneTitle);
					}
					_pane.Clear();
					_pane.Show();

					StartCompile();
				}
				else
				{
					if (_pane != null) _pane.Show();
				}
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Exception when starting compile:\r\n", ex));
				Log.WriteEx(ex);
			}
		}

		public void Kill()
		{
			try
			{
				KillCompile(0);
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Exception when killing compile:\r\n", ex));
				Log.WriteEx(ex);
			}
		}

		public void StartCompile()
		{
			if (_compileThread != null) KillCompile(k_killCompileTimeout);

			Commands.SaveProbeFiles();

			_pane.Clear();
			_kill = false;

			_compileThread = new Thread(new ThreadStart(CompileThread));
			_compileThread.Name = "CompileThread";
			_compileThread.Start();
		}

		public void KillCompile(int timeout)
		{
			if (_compileThread == null) return;

			if (_compileThread.IsAlive)
			{
				DateTime killStartTime = DateTime.Now;
				while (_compileThread.IsAlive)
				{
					_kill = true;
					if (DateTime.Now.Subtract(killStartTime).TotalMilliseconds >= timeout) break;
					Thread.Sleep(k_compileKillSleep);
				}
			}

			_compileThread = null;
		}

		private void CompileThread()
		{
			var startTime = DateTime.Now;

			switch (_method)
			{
				case CompileMethod.Compile:
					WriteLine("Starting compile for application '{0}' at {1}.", ProbeEnvironment.CurrentApp, startTime.ToString(k_timeStampFormat));

					if (DoCompile())
					{
						var endTime = DateTime.Now;
						var elapsed = endTime - startTime;
						WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
					}
					break;

				case CompileMethod.Dccmp:
					WriteLine("Starting dccmp for application '{0}' at {1}.", ProbeEnvironment.CurrentApp, startTime.ToString(k_timeStampFormat));
					Shell.SetStatusText("DK dccmp starting...");

					if (DoDccmp())
					{
						var endTime = DateTime.Now;
						var elapsed = endTime - startTime;
						WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
					}
					break;

				case CompileMethod.Credelix:
					WriteLine("Starting credelix for application '{0}' at {1}.", ProbeEnvironment.CurrentApp, startTime.ToString(k_timeStampFormat));
					Shell.SetStatusText("DK credelix starting...");

					if (DoCredelix())
					{
						var endTime = DateTime.Now;
						var elapsed = endTime - startTime;
						WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
					}
					break;

				default:
					throw new InvalidOperationException("Invalid compile method.");
			}
		}

		private bool DoCompile()
		{
			try
			{
				Shell.SetStatusText("DK compile starting...");

				_numErrors = _numWarnings = 0;
				_buildFailed = false;

				var workingDir = ProbeEnvironment.ObjectDir;
				if (string.IsNullOrWhiteSpace(workingDir))
				{
					WriteLine("DK object directory not configured.");
					Shell.SetStatusText("DK compile failed");
					return false;
				}
				else if (!Directory.Exists(workingDir))
				{
					FileUtil.CreateDirectoryRecursive(workingDir);
				}

				_proc = new Process();
				ProcessStartInfo info = new ProcessStartInfo("pc.bat", "/w");
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				info.CreateNoWindow = true;
				info.WorkingDirectory = ProbeEnvironment.ObjectDir;
                ProbeEnvironment.MergeEnvironmentVariables(info.EnvironmentVariables);

				_proc.StartInfo = info;
				if (!_proc.Start())
				{
					WriteLine("Unable to start DK compiler.");
					Shell.SetStatusText("DK compile failed");
					return false;
				}

				Thread stdOutThread = new Thread(new ThreadStart(StdOutThread));
				stdOutThread.Name = "StdOut Compile Thread";
				stdOutThread.Start();

				Thread stdErrThread = new Thread(new ThreadStart(StdErrThread));
				stdErrThread.Name = "StdErr Compile Thread";
				stdErrThread.Start();

				while (stdOutThread.IsAlive || stdErrThread.IsAlive)
				{
					if (_kill)
					{
						_proc.Kill();
						stdOutThread.Join();
						stdErrThread.Join();
						WriteLine("Compile was stopped before completion.");
						Shell.SetStatusText("DK compile stopped");
						return false;
					}
					Thread.Sleep(k_compileSleep);
				}

				if (_numErrors > 0 || _numWarnings > 0 || _buildFailed)
				{
					if (_numErrors > 0 || _numWarnings > 0)
					{
						string str = "";
						if (_numErrors == 1) str = "1 error";
						else if (_numErrors > 1) str = string.Concat(_numErrors, " errors");

						if (_numWarnings > 0 && !string.IsNullOrEmpty(str)) str += " ";
						if (_numWarnings == 1) str += "1 warning";
						else if (_numWarnings > 1) str += string.Concat(_numWarnings, " warnings");

						WriteLine(str);
						if (ProbeToolsPackage.Instance.ProbeExplorerOptions.ShowErrorListAfterCompile)
						{
							Shell.ShowErrorList();
						}
					}

					if (_numErrors > 0 || _buildFailed)
					{
						Shell.SetStatusText("DK compile failed");
						return false;
					}
					else
					{
						WriteLine("Running dccmp...");
					}
				}

				WriteLine("Compile succeeded; running dccmp...");
				if (!DoDccmp()) return false;

				Shell.SetStatusText("DK compile succeeded");
				return true;
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Fatal error in compile thread: ", ex.ToString()));
				Shell.SetStatusText("DK compile failed");
				return false;
			}
		}

		private bool DoDccmp()
		{
			try 
			{
				Shell.SetStatusText("Probe dccmp starting...");

				_proc = new Process();
				var info = new ProcessStartInfo("dccmp.exe", string.Format("/z /D /P \"{0}\"", ProbeEnvironment.CurrentApp));
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				info.CreateNoWindow = true;
				info.WorkingDirectory = ProbeEnvironment.ObjectDir;
				_proc.StartInfo = info;
				if (!_proc.Start())
				{
					WriteLine("Unable to start dccmp.");
					Shell.SetStatusText("Probe dccmp failed");
					return false;
				}

				var stdOutThread = new Thread(new ThreadStart(StdOutThread));
				stdOutThread.Name = "StdOut Dccmp Thread";
				stdOutThread.Start();

				var stdErrThread = new Thread(new ThreadStart(StdErrThread));
				stdErrThread.Name = "StdErr Dccmp Thread";
				stdErrThread.Start();

				while (stdOutThread.IsAlive || stdErrThread.IsAlive)
				{
					if (_kill)
					{
						_proc.Kill();
						stdOutThread.Join();
						stdErrThread.Join();
						WriteLine("Dccmp was stopped before completion.");
						Shell.SetStatusText("Probe dccmp stopped");
						return false;
					}
					Thread.Sleep(k_compileSleep);
				}

				if (_proc.ExitCode != 0)
				{
					WriteLine("Dccmp failed");
					Shell.SetStatusText("Probe dccmp failed");
					return false;
				}

				WriteLine("Dccmp succeeded");
				Shell.SetStatusText("Probe dccmp succeeded");
				return true;
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Fatal error in dccmp thread: ", ex.ToString()));
				Shell.SetStatusText("Probe dccmp failed");
				return false;
			}
		}

		private bool DoCredelix()
		{
			try
			{
				Shell.SetStatusText("Probe credelix starting...");

				_proc = new Process();
				var info = new ProcessStartInfo("credelix.exe", string.Format("/p /P \"{0}\"", ProbeEnvironment.CurrentApp));
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				info.CreateNoWindow = true;
				info.WorkingDirectory = ProbeEnvironment.ObjectDir;
				_proc.StartInfo = info;
				if (!_proc.Start())
				{
					WriteLine("Unable to start credelix.");
					Shell.SetStatusText("Probe credelix failed");
					return false;
				}

				var stdOutThread = new Thread(new ThreadStart(StdOutThread));
				stdOutThread.Name = "StdOut Credelix Thread";
				stdOutThread.Start();

				var stdErrThread = new Thread(new ThreadStart(StdErrThread));
				stdErrThread.Name = "StdErr Credelix Thread";
				stdErrThread.Start();

				while (stdOutThread.IsAlive || stdErrThread.IsAlive)
				{
					if (_kill)
					{
						_proc.Kill();
						stdOutThread.Join();
						stdErrThread.Join();
						WriteLine("Credelix was stopped before completion.");
						Shell.SetStatusText("Probe credelix stopped");
						return false;
					}
					Thread.Sleep(k_compileSleep);
				}

				if (_proc.ExitCode != 0)
				{
					WriteLine("Credelix failed");
					Shell.SetStatusText("Probe credelix failed");
					return false;
				}

				WriteLine("Credelix succeeded");
				Shell.SetStatusText("Probe credelix succeeded");
				return true;
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Fatal error in credelix thread: ", ex.ToString()));
				Shell.SetStatusText("Probe credelix failed");
				return false;
			}
		}

		private void StdOutThread()
		{
			try
			{
				StreamReader stream = _proc.StandardOutput;

				while (!_proc.HasExited)
				{
					string line = stream.ReadLine();
					if (line == null)
					{
						Thread.Sleep(k_compileSleep);
					}
					else
					{
						CompileThreadOutput(line, false);
					}
				}

				while (!_proc.StandardOutput.EndOfStream)
				{
					string line = stream.ReadLine();
					if (line == null)
					{
						Thread.Sleep(k_compileSleep);
					}
					else
					{
						CompileThreadOutput(line, false);
					}
				}
			}
			catch (Exception ex)
			{
				WriteLine(string.Format("Error in StdOut compile thread: {0}", ex));
			}
		}

		private void StdErrThread()
		{
			try
			{
				StreamReader stream = _proc.StandardError;

				while (!_proc.HasExited)
				{
					string line = stream.ReadLine();
					if (line == null)
					{
						Thread.Sleep(k_compileSleep);
					}
					else
					{
						CompileThreadOutput(line, true);
					}
				}

				while (!_proc.StandardOutput.EndOfStream)
				{
					string line = stream.ReadLine();
					if (line == null)
					{
						Thread.Sleep(k_compileSleep);
					}
					else
					{
						CompileThreadOutput(line, true);
					}
				}
			}
			catch (Exception ex)
			{
				WriteLine(string.Format("Error in StdErr compile thread: {0}", ex));
			}
		}

		private Regex _rxLinkError = new Regex(@"\:\s+error\s+(LNK\d{4}\:)");

		private void CompileThreadOutput(string line, bool stdErr)
		{
			if (_pane == null) return;

			var index = line.IndexOf(": error :");
			if (index >= 0)
			{
				string fileName;
				uint lineNum;
				ParseFileNameAndLine(line.Substring(0, index), out fileName, out lineNum);
				_pane.WriteLineAndTask(line, line.Substring(index + ": error :".Length).Trim(), OutputPane.TaskType.Error, fileName, lineNum);
				_numErrors++;
				return;
			}

			index = line.IndexOf(": warning :");
			if (index >= 0)
			{
				string fileName;
				uint lineNum;
				ParseFileNameAndLine(line.Substring(0, index), out fileName, out lineNum);
				_pane.WriteLineAndTask(line, line.Substring(index + ": warning :".Length).Trim(), OutputPane.TaskType.Warning, fileName, lineNum);
				_numWarnings++;
				return;
			}

			if (line.StartsWith("LINK : fatal error"))
			{
				_pane.WriteLineAndTask(line, line.Substring("LINK : fatal error".Length).Trim(), OutputPane.TaskType.Error, "", 0);
				_numErrors++;
				return;
			}

			if (line.Equals("Build failed."))
			{
				_buildFailed = true;
				_pane.WriteLine(line);
				return;
			}

			Match match;
			if ((match = _rxLinkError.Match(line)).Success)
			{
				_pane.WriteLineAndTask(line, line.Substring(match.Groups[1].Index), OutputPane.TaskType.Error, "", 0);
				_numErrors++;
				return;
			}

			_pane.WriteLine(line);
		}

		public void WriteLine(string text)
		{
			if (_pane != null) _pane.WriteLine(text);
		}

		public void WriteLine(string format, params object[] args)
		{
			if (_pane != null) _pane.WriteLine(string.Format(format, args));
		}

		private static Regex _rxFileNameAndLine = new Regex(@"(.+)\s*\((\d{1,9})\)\s*$");

		private bool ParseFileNameAndLine(string str, out string fileName, out uint lineNum)
		{
			var match = _rxFileNameAndLine.Match(str);
			if (match.Success)
			{
				fileName = match.Groups[1].Value;
				lineNum = uint.Parse(match.Groups[2].Value);
				return true;
			}

			fileName = "";
			lineNum = 0;
			return false;
		}
	}
}
