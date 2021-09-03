using DK.AppEnvironment;
using DK.Diagnostics;
using DkTools.ErrorTagging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DkTools.Compiler
{
	internal class ProbeCompiler : IDisposable
	{
		private OutputPane _pane;
		private static readonly Guid _paneGuid = new Guid("{BD87845A-95F6-4BEB-B9A8-CABE3B01E247}");

		private Thread _compileThread = null;
		private CompileMethod _method = CompileMethod.Compile;
		private CancellationTokenSource _cancelSource;
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

			_cancelSource = new CancellationTokenSource();

			_compileThread = new Thread(new ParameterizedThreadStart(CompileThread));
			_compileThread.Name = "CompileThread";
			_compileThread.Start(_cancelSource.Token);
		}

		public void KillCompile(int timeout)
		{
			if (_compileThread == null) return;

			if (_compileThread.IsAlive)
			{
				if (DkEnvironment.WbdkPlatformVersion >= DkEnvironment.DK10Version)
				{
                    try
                    {
						var pr = new ProcessRunner();
						pr.ExecuteProcess("pc.bat", "/kill", null, waitForExit: false);
					}
                    catch (Exception ex)
                    {
						Log.Warning("Expected when trying to kill existing compile: {0}", ex);
                    }
				}

				DateTime killStartTime = DateTime.Now;
				while (_compileThread.IsAlive)
				{
					_cancelSource?.Cancel();
					if (DateTime.Now.Subtract(killStartTime).TotalMilliseconds >= timeout) break;
					Thread.Sleep(k_compileKillSleep);
				}
			}

			_compileThread = null;
		}

		private void CompileThread(object cancelToken)
		{
			var cancel = (CancellationToken)cancelToken;

			try
			{
				var startTime = DateTime.Now;
				var appSettings = DkEnvironment.CurrentAppSettings;
				if (!appSettings.Initialized)
				{
					WriteLine("Aborting compile because no current WBDK app is loaded.");
					return;
				}

				while (!_mutex.WaitOne(1000))
				{
					if (cancel.IsCancellationRequested) return;
					WriteLine("Waiting for background compile to complete...");
				}

				_pane.Clear();
				ErrorTaskProvider.Instance.Clear();

				switch (_method)
				{
					case CompileMethod.Compile:
						WriteLine("Starting compile for application '{0}' at {1}.", appSettings.AppName, startTime.ToString(k_timeStampFormat));

						if (DoCompile(appSettings, cancel, startTime))
						{
							var endTime = DateTime.Now;
							var elapsed = endTime - startTime;
							WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
						}
						break;

					case CompileMethod.Dccmp:
						WriteLine("Starting dccmp for application '{0}' at {1}.", appSettings.AppName, startTime.ToString(k_timeStampFormat));
						ProbeToolsPackage.Instance.SetStatusText("DK dccmp starting...");

						if (DoDccmp(appSettings, cancel))
						{
							var endTime = DateTime.Now;
							var elapsed = endTime - startTime;
							WriteLine("Finished at {0} ({1:0}:{2:00} elapsed)", endTime.ToString(k_timeStampFormat), elapsed.TotalMinutes, elapsed.Seconds);
						}
						break;

					case CompileMethod.Credelix:
						WriteLine("Starting credelix for application '{0}' at {1}.", appSettings.AppName, startTime.ToString(k_timeStampFormat));
						ProbeToolsPackage.Instance.SetStatusText("DK credelix starting...");

						if (DoCredelix(appSettings, cancel))
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
			catch (OperationCanceledException ex) { Log.Debug(ex); }
			catch (ThreadAbortException ex) { Log.Debug(ex); }
			catch (Exception ex) { Log.Error(ex); }
			finally
			{
				_mutex.ReleaseMutex();
			}
		}

		private bool DoCompile(DkAppSettings appSettings, CancellationToken cancel, DateTime buildStartTime)
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
					if (cancel.IsCancellationRequested)
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

				_pane.WriteLine(string.Empty);

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
					if (!DoDccmp(appSettings, cancel)) return false;
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

		private bool DoDccmp(DkAppSettings appSettings, CancellationToken cancel)
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
					if (cancel.IsCancellationRequested)
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

		private bool DoCredelix(DkAppSettings appSettings, CancellationToken cancel)
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
					if (cancel.IsCancellationRequested)
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
						CompileThreadOutput(line, stdErr: false, fromBuildReport: false);
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
						CompileThreadOutput(line, stdErr: false, fromBuildReport: false);
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
						CompileThreadOutput(line, stdErr: true, fromBuildReport: false);
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
						CompileThreadOutput(line, stdErr: true, fromBuildReport: false);
					}
				}
			}
			catch (Exception ex)
			{
				WriteLine(string.Format("Error in StdErr compile thread: {0}", ex));
			}
		}

		private Regex _rxLinkError = new Regex(@"\:\s+error\s+(LNK\d{4}\:)");
		private Regex _rxFecMultipleError = new Regex(@"^Remaining Compile:\s+\d+\s+Link:\s+\d+\s+Result:\s+Failure:\s+.*\.\.\.\s+\w+\s+error\s*$");
		private string _lastStdoutLine;
		private string _lastStderrLine;

		private void CompileThreadOutput(string line, bool stdErr, bool fromBuildReport)
		{
			Match match;

			if (_pane == null) return;

			if (stdErr)
			{
				if (!string.IsNullOrWhiteSpace(line) && line == _lastStderrLine) return;
				_lastStderrLine = line;
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(line) && line == _lastStdoutLine) return;
				_lastStdoutLine = line;
			}

			var index = line.IndexOf(": error :");
			if (index >= 0)
			{
				var task = null as DkTools.ErrorTagging.ErrorTask;
				if (ParseFilePathAndLine(line.Substring(0, index), out var filePath, out var lineNum))
				{
					var message = line.Substring(index + ": error :".Length).Trim();
					task = new DkTools.ErrorTagging.ErrorTask(
						invokingFilePath: filePath,
						filePath: filePath,
						lineNum: lineNum - 1,
						lineCol: -1,
						message: message,
						type: ErrorType.Error,
						source: ErrorTaskSource.Compile,
						reportedSpan: null);
					ErrorTaskProvider.Instance.Add(task);
				}
				if (task != null) _pane.WriteLine(task.ToString());
				else _pane.WriteLine(line);
				_numErrors++;
				return;
			}

			index = line.IndexOf(": warning :");
			if (index >= 0)
			{
				var task = null as DkTools.ErrorTagging.ErrorTask;
				if (ParseFilePathAndLine(line.Substring(0, index), out var filePath, out var lineNum))
				{
					var message = line.Substring(index + ": warning :".Length).Trim();
					task = new DkTools.ErrorTagging.ErrorTask(
						invokingFilePath: filePath,
						filePath: filePath,
						lineNum: lineNum - 1,
						lineCol: -1,
						message: message,
						type: ErrorType.Warning,
						source: ErrorTaskSource.Compile,
						reportedSpan: null);
					ErrorTaskProvider.Instance.Add(task);
				}
				if (task != null) _pane.WriteLine(task.ToString());
				else _pane.WriteLine(line);
				_numWarnings++;
				return;
			}

			if ((match = _rxFecMultipleError.Match(line)).Success)
            {
				// Example:
				// Remaining Compile: 231 Link: 904 Result: Failure: x:\ccssrc1\prod\ibgate\bpyproc.f (compile) ... FEC error

				// We know the build will fail, but we don't know the specific error yet. That will come later in the build report.
				_buildFailed = true;
				_pane.WriteLine(line);
				return;
            }

			if (line.StartsWith("LINK : fatal error"))
			{
				var message = line.Substring("LINK : fatal error".Length).Trim();
				var task = new DkTools.ErrorTagging.ErrorTask(
					invokingFilePath: null,
					filePath: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: message,
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					reportedSpan: null);
				ErrorTaskProvider.Instance.Add(task);
				_pane.WriteLine(line);
				_numErrors++;
				return;
			}

			if (line.StartsWith("fecMultiple error:"))
            {
				var message = line.Trim();
				var task = new DkTools.ErrorTagging.ErrorTask(
					invokingFilePath: null,
					filePath: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: message,
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					reportedSpan: null);
				ErrorTaskProvider.Instance.Add(task);
				_pane.WriteLine(line);
				_numErrors++;
				return;
			}

			if (line.Equals("Build failed."))
			{
				var task = new DkTools.ErrorTagging.ErrorTask(
					invokingFilePath: null,
					filePath: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: "Build failed.",
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					reportedSpan: null);
				ErrorTaskProvider.Instance.Add(task);
				_buildFailed = true;
				_pane.WriteLine(line);
				return;
			}

			if (line.IndexOf("Compile failed", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				var task = new DkTools.ErrorTagging.ErrorTask(
					invokingFilePath: null,
					filePath: string.Empty,
					lineNum: 0,
					lineCol: -1,
					message: line,
					type: ErrorType.Error,
					source: ErrorTaskSource.Compile,
					reportedSpan: null);
				ErrorTaskProvider.Instance.Add(task);
				_buildFailed = true;
				_pane.WriteLine(line);
				return;
			}

			if ((match = _rxLinkError.Match(line)).Success)
			{
				_pane.WriteLineAndTask(line, line.Substring(match.Groups[1].Index), OutputPane.TaskType.Error, "", 0);
				_numErrors++;
				return;
			}

			if (!fromBuildReport) _pane.WriteLine(line);
		}

		public void WriteLine(string text)
		{
			if (_pane != null) _pane.WriteLine(text);
		}

		public void WriteLine(string format, params object[] args)
		{
			if (_pane != null) _pane.WriteLine(string.Format(format, args));
		}

		private static Regex _rxFileNameAndLine = new Regex(@"(.+)\s*\((\d{1,9})(?:\:\d{1,9})?\)\s*$");

		public static bool ParseFilePathAndLine(string str, out string fileName, out int lineNum)
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
			var switches = ProbeToolsPackage.Instance.ProbeExplorerOptions.CompileArguments;

			if (DkEnvironment.WbdkPlatformVersion >= DkEnvironment.DK10Version)
            {
				// Include all output.
				switches += " /d all";
            }

			return switches;
		}

		private string GenerateDccmpSwitches(DkAppSettings appSettings)
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

		private string GenerateCredelixSwitches(DkAppSettings appSettings)
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

		private static readonly Regex _rxBuildReportDir = new Regex(@"\\Build_([^_]+)_(\d{4})-(\d{2})-(\d{2})_(\d{2})\.(\d{2})\.(\d{2})$");

		private string FindBuildReport(DkAppSettings appSettings, DateTime buildStartTime)
        {
			_pane.WriteLine("Locating build report...");

			var dataDir = appSettings.DataDir;
			if (string.IsNullOrEmpty(dataDir) || !Directory.Exists(dataDir))
			{
				_pane.WriteLine("Data directory is not set in ACM.");
				return null;
			}

			var buildReportsDir = Path.Combine(dataDir, "BuildReports");
			if (!Directory.Exists(buildReportsDir))
			{
				_pane.WriteLine("BuildReports directory does not exist.");
				return null;
			}

			var bestFutureTime = DateTime.MinValue;
			var bestFuturePathName = null as string;
			var bestPastTime = DateTime.MinValue;
			var bestPastPathName = null as string;

			foreach (var reportDir in Directory.GetDirectories(buildReportsDir))
			{
				var match = _rxBuildReportDir.Match(reportDir);
				if (!match.Success) continue;

				var appName = match.Groups[1].Value;
				if (!string.Equals(appName, appSettings.AppName, StringComparison.OrdinalIgnoreCase)) continue;

				var reportFileName = string.Format("DKCompile_Results_{0}_{1}-{2}-{3}_{4}.{5}.{6}_Combined.txt",
					appName,
					match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value,
					match.Groups[5].Value, match.Groups[6].Value, match.Groups[7].Value);
				var reportPathName = Path.Combine(reportDir, reportFileName);
				if (!File.Exists(reportPathName))
                {
					Log.Warning("Report file does not exist in build report folder: {0}", reportPathName);
					continue;
                }

				var reportTime = new DateTime(int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value),
					int.Parse(match.Groups[5].Value), int.Parse(match.Groups[6].Value), int.Parse(match.Groups[7].Value));

				if (Math.Abs(reportTime.Subtract(buildStartTime).TotalMinutes) > 5)
                {
					Log.Debug("Build report folder is not within 5 minutes of this build: {0}", reportDir);
					continue;
                }

				if (reportTime >= buildStartTime.AddSeconds(-1))
				{
					if (bestFuturePathName == null || reportTime < bestFutureTime)
					{
						bestFutureTime = reportTime;
						bestFuturePathName = reportPathName;
					}
				}
				else
				{
					if (bestPastPathName == null || reportTime > bestPastTime)
					{
						bestPastTime = reportTime;
						bestPastPathName = reportPathName;
					}
				}
			}

			if (bestFuturePathName != null) return bestFuturePathName;
			if (bestPastPathName != null) return bestPastPathName;

			_pane.WriteLine("No build report found.");
			return null;
		}
	}
}
