using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using DkTools.ErrorTagging;

namespace DkTools.Compiler
{
	internal class ProbeCompiler : IDisposable
	{
		private OutputPane _pane;
		private static readonly Guid _paneGuid = new Guid("{BD87845A-95F6-4BEB-B9A8-CABE3B01E247}");

		private Thread _compileThread = null;
		private CompileMethod _method = CompileMethod.Compile;
		private volatile bool _kill;
		private Process _proc = null;
		private int _numErrors = 0;
		private int _numWarnings = 0;
		private bool _buildFailed = false;
		private Mutex _mutex = new Mutex();

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
				ThreadHelper.ThrowIfNotOnUIThread();

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
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_compileThread != null) KillCompile(k_killCompileTimeout);

			Commands.SaveProbeFiles();

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
			var appSettings = ProbeEnvironment.CurrentAppSettings;
			if (!appSettings.Initialized)
			{
				WriteLine("Aborting compile because no current WBDK app is loaded.");
				return;
			}

			while (!_mutex.WaitOne(1000))
			{
				if (_kill) return;
				WriteLine("Waiting for background compile to complete...");
			}

			try
			{
				_pane.Clear();
				ErrorTaskProvider.Instance.Clear();

				switch (_method)
				{
					case CompileMethod.Compile:
						WriteLine("Starting compile for application '{0}' at {1}.", appSettings.AppName, startTime.ToString(k_timeStampFormat));

						if (DoCompile(appSettings))
						{
							var endTime = DateTime.Now;
							var elapsed = endTime - startTime;
							WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
						}
						break;

					case CompileMethod.Dccmp:
						WriteLine("Starting dccmp for application '{0}' at {1}.", appSettings.AppName, startTime.ToString(k_timeStampFormat));
						ProbeToolsPackage.Instance.SetStatusText("DK dccmp starting...");

						if (DoDccmp(appSettings))
						{
							var endTime = DateTime.Now;
							var elapsed = endTime - startTime;
							WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
						}
						break;

					case CompileMethod.Credelix:
						WriteLine("Starting credelix for application '{0}' at {1}.", appSettings.AppName, startTime.ToString(k_timeStampFormat));
						ProbeToolsPackage.Instance.SetStatusText("DK credelix starting...");

						if (DoCredelix(appSettings))
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
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
			finally
			{
				_mutex.ReleaseMutex();
			}
		}

		private bool DoCompile(ProbeAppSettings appSettings)
		{
			try
			{
				//Shell.SetStatusText("DK compile starting...");
				ProbeToolsPackage.Instance.SetStatusText("DK compile starting...");

				_numErrors = _numWarnings = 0;
				_buildFailed = false;

				var workingDir = appSettings.ObjectDir;
				if (string.IsNullOrWhiteSpace(workingDir))
				{
					WriteLine("DK object directory not configured.");
					ProbeToolsPackage.Instance.SetStatusText("DK compile failed");
					return false;
				}
				else if (!Directory.Exists(workingDir))
				{
					FileUtil.CreateDirectoryRecursive(workingDir);
				}

				_proc = new Process();
				var switches = GenerateCompileSwitches();
				ProcessStartInfo info = new ProcessStartInfo("pc.bat", switches);
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				info.CreateNoWindow = true;
				info.WorkingDirectory = appSettings.ObjectDir;
                appSettings.MergeEnvironmentVariables(info.EnvironmentVariables);

				WriteLine(string.Concat("pc ", switches));

				_proc.StartInfo = info;
				if (!_proc.Start())
				{
					WriteLine("Unable to start DK compiler.");
					ProbeToolsPackage.Instance.SetStatusText("DK compile failed");
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
						ProbeToolsPackage.Instance.SetStatusText("DK compile stopped");
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
					}

					if (_numErrors > 0 || _buildFailed)
					{
						ProbeToolsPackage.Instance.SetStatusText("DK compile failed");
						if (ProbeToolsPackage.Instance.ProbeExplorerOptions.ShowErrorListAfterCompile) ProbeToolsPackage.Instance.ShowErrorList();
						return false;
					}
				}

				if (ProbeToolsPackage.Instance.ProbeExplorerOptions.RunDccmpAfterCompile)
				{
					WriteLine("Compile succeeded; running DCCMP...");
					ProbeToolsPackage.Instance.SetStatusText("DK compile succeeded; running DCCMP...");
					if (!DoDccmp(appSettings)) return false;
				}

				if (_numWarnings > 0) ProbeToolsPackage.Instance.SetStatusText("DK compile succeeded with warnings");
				else ProbeToolsPackage.Instance.SetStatusText("DK compile succeeded");
				ShowErrorListIfRequired();
				
				return true;
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Fatal error in compile thread: ", ex.ToString()));
				ProbeToolsPackage.Instance.SetStatusText("DK compile failed");
				return false;
			}
		}

		private void ShowErrorListIfRequired()
		{
			if (_numErrors > 0 || _numWarnings > 0)
			{
				if (ProbeToolsPackage.Instance.ProbeExplorerOptions.ShowErrorListAfterCompile)
				{
					ProbeToolsPackage.Instance.ShowErrorList();
				}
			}
		}

		private bool DoDccmp(ProbeAppSettings appSettings)
		{
			try 
			{
				ProbeToolsPackage.Instance.SetStatusText("DCCMP starting...");

				_proc = new Process();
				var switches = GenerateDccmpSwitches(appSettings);
				var info = new ProcessStartInfo("dccmp.exe", switches);
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				info.CreateNoWindow = true;
				info.WorkingDirectory = appSettings.ObjectDir;
				_proc.StartInfo = info;

				WriteLine("dccmp " + switches);

				if (!_proc.Start())
				{
					WriteLine("Unable to start DCCMP.");
					ProbeToolsPackage.Instance.SetStatusText("DCCMP failed");
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
						WriteLine("DCCMP was stopped before completion.");
						ProbeToolsPackage.Instance.SetStatusText("DCCMP dccmp stopped");
						return false;
					}
					Thread.Sleep(k_compileSleep);
				}

				if (_proc.ExitCode != 0)
				{
					WriteLine("DCCMP failed");
					ProbeToolsPackage.Instance.SetStatusText("DCCMP failed");
					return false;
				}

				WriteLine("DCCMP succeeded");
				ProbeToolsPackage.Instance.SetStatusText("DCCMP succeeded");
				return true;
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Fatal error in dccmp thread: ", ex.ToString()));
				ProbeToolsPackage.Instance.SetStatusText("DCCMP failed");
				return false;
			}
		}

		private bool DoCredelix(ProbeAppSettings appSettings)
		{
			try
			{
				ProbeToolsPackage.Instance.SetStatusText("Probe credelix starting...");

				_proc = new Process();
				var switches = GenerateCredelixSwitches(appSettings);
				var info = new ProcessStartInfo("credelix.exe", switches);
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				info.CreateNoWindow = true;
				info.WorkingDirectory = appSettings.ObjectDir;
				_proc.StartInfo = info;

				WriteLine("credelix " + switches);

				if (!_proc.Start())
				{
					WriteLine("Unable to start CREDELIX.");
					ProbeToolsPackage.Instance.SetStatusText("CREDELIX failed");
					return false;
				}

				var stdOutThread = new Thread(new ThreadStart(StdOutThread));
				stdOutThread.Name = "StdOut CREDELIX Thread";
				stdOutThread.Start();

				var stdErrThread = new Thread(new ThreadStart(StdErrThread));
				stdErrThread.Name = "StdErr CREDELIX Thread";
				stdErrThread.Start();

				while (stdOutThread.IsAlive || stdErrThread.IsAlive)
				{
					if (_kill)
					{
						_proc.Kill();
						stdOutThread.Join();
						stdErrThread.Join();
						WriteLine("CREDELIX was stopped before completion.");
						ProbeToolsPackage.Instance.SetStatusText("CREDELIX stopped");
						return false;
					}
					Thread.Sleep(k_compileSleep);
				}

				if (_proc.ExitCode != 0)
				{
					WriteLine("CREDELIX failed");
					ProbeToolsPackage.Instance.SetStatusText("CREDELIX failed");
					return false;
				}

				WriteLine("CREDELIX succeeded");
				ProbeToolsPackage.Instance.SetStatusText("CREDELIX succeeded");
				return true;
			}
			catch (Exception ex)
			{
				WriteLine(string.Concat("Fatal error in CREDELIX thread: ", ex.ToString()));
				ProbeToolsPackage.Instance.SetStatusText("CREDELIX failed");
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
				int lineNum;
				if (ParseFileNameAndLine(line.Substring(0, index), out fileName, out lineNum))
				{
					var message = line.Substring(index + ": error :".Length).Trim();
					var task = new DkTools.ErrorTagging.ErrorTask(
						fileName: fileName,
						lineNum: lineNum - 1,
						lineCol: -1,
						message: message,
						type: ErrorType.Error,
						source: ErrorTaskSource.Compile,
						sourceFileName: null,
						reportedSpan: null,
						snapshotSpan: null);
					ErrorTaskProvider.Instance.Add(task);
				}
				_pane.WriteLine(line);
				_numErrors++;
				return;
			}

			index = line.IndexOf(": warning :");
			if (index >= 0)
			{
				string fileName;
				int lineNum;
				if (ParseFileNameAndLine(line.Substring(0, index), out fileName, out lineNum))
				{
					var message = line.Substring(index + ": warning :".Length).Trim();
					var task = new DkTools.ErrorTagging.ErrorTask(
						fileName: fileName,
						lineNum: lineNum - 1,
						lineCol: -1,
						message: message,
						type: ErrorType.Warning,
						source: ErrorTaskSource.Compile,
						sourceFileName: null,
						reportedSpan: null,
						snapshotSpan: null);
					ErrorTaskProvider.Instance.Add(task);
				}
				_pane.WriteLine(line);
				_numWarnings++;
				return;
			}

			if (line.StartsWith("LINK : fatal error"))
			{
				var message = line.Substring("LINK : fatal error".Length).Trim();
				var task = new DkTools.ErrorTagging.ErrorTask(
					fileName: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: message,
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					sourceFileName: null,
					reportedSpan: null,
					snapshotSpan: null);
				_pane.WriteLine(line);
				_numErrors++;
				return;
			}

			if (line.Equals("Build failed."))
			{
				var task = new DkTools.ErrorTagging.ErrorTask(
					fileName: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: "Build failed.",
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					sourceFileName: null,
					reportedSpan: null,
					snapshotSpan: null);
				ErrorTaskProvider.Instance.Add(task);
				_buildFailed = true;
				_pane.WriteLine(line);
				return;
			}

			if (line.IndexOf("Compile failed", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				var task = new DkTools.ErrorTagging.ErrorTask(
					fileName: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: line,
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					sourceFileName: null,
					reportedSpan: null,
					snapshotSpan: null);
				ErrorTaskProvider.Instance.Add(task);
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

		public static bool ParseFileNameAndLine(string str, out string fileName, out int lineNum)
		{
			var match = _rxFileNameAndLine.Match(str);
			if (match.Success)
			{
				fileName = match.Groups[1].Value;
				lineNum = int.Parse(match.Groups[2].Value);
				return true;
			}

			fileName = "";
			lineNum = 0;
			return false;
		}

		private string GenerateCompileSwitches()
		{
			return ProbeToolsPackage.Instance.ProbeExplorerOptions.CompileArguments;
		}

		private string GenerateDccmpSwitches(ProbeAppSettings appSettings)
		{
			var sb = new StringBuilder();
			sb.Append("/P \"");
			sb.Append(appSettings.AppName);
			sb.Append('\"');

			var custom = ProbeToolsPackage.Instance.ProbeExplorerOptions.DccmpArguments;
			if (!string.IsNullOrWhiteSpace(custom))
			{
				sb.Append(' ');
				sb.Append(custom);
			}

			return sb.ToString();
		}

		private string GenerateCredelixSwitches(ProbeAppSettings appSettings)
		{
			var sb = new StringBuilder();
			sb.Append("/P \"");
			sb.Append(appSettings.AppName);
			sb.Append('\"');

			var custom = ProbeToolsPackage.Instance.ProbeExplorerOptions.CredelixArguments;
			if (!string.IsNullOrWhiteSpace(custom))
			{
				sb.Append(' ');
				sb.Append(custom);
			}

			return sb.ToString();
		}

		public Mutex Mutex
		{
			get { return _mutex; }
		}
	}
}
