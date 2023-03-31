using DK.Diagnostics;
using DkTools.ErrorTagging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DkTools.Compiler
{
	static class BackgroundFec
	{
		public static async Task RunAsync(string sourceFilePath, CancellationToken cancel)
		{
			if (string.IsNullOrWhiteSpace(sourceFilePath)) return;

			ProbeToolsPackage.Log.Debug("Running background FEC for file '{0}'.", sourceFilePath);

			var counter = 10;
			while (counter > 0)
			{
				cancel.ThrowIfCancellationRequested();

				if (!await ProbeToolsPackage.Instance.Compiler.Semaphore.WaitAsync(1000))
				{
					ProbeToolsPackage.Log.Debug("Waiting for other compile/FEC operation to complete...");
					counter--;
					if (counter == 0)
					{
						ProbeToolsPackage.Log.Debug("Ran out of retries when waiting for compile/FEC operation to complete.");
						return;
					}
				}
				else break;
			}

			try
			{
				var tasks = new List<ErrorTask>();

				var output = new CallbackOutput(line =>
				{
					var index = line.IndexOf(": error :");
					if (index >= 0)
					{
						ProbeToolsPackage.Log.Debug("Background FEC error: {0}", line);

						string fileName;
						int lineNum;
						if (ProbeCompiler.ParseFilePathAndLine(line.Substring(0, index), out fileName, out lineNum))
						{
							var message = line.Substring(index + ": error :".Length).Trim();
							tasks.Add(new ErrorTask(
								invokingFilePath: sourceFilePath,
								filePath: fileName,
								lineNum: lineNum - 1,
								lineCol: -1,
								message: message,
								type: ErrorType.Error,
								source: ErrorTaskSource.BackgroundFec,
								reportedSpan: null));
						}
						return;
					}

					index = line.IndexOf(": warning :");
					if (index >= 0)
					{
						ProbeToolsPackage.Log.Debug("Background FEC warning: {0}", line);

						string fileName;
						int lineNum;
						if (ProbeCompiler.ParseFilePathAndLine(line.Substring(0, index), out fileName, out lineNum))
						{
							var message = line.Substring(index + ": warning :".Length).Trim();
							tasks.Add(new ErrorTask(
								invokingFilePath: sourceFilePath,
								filePath: fileName,
								lineNum: lineNum - 1,
								lineCol: -1,
								message: message,
								type: ErrorType.Warning,
								source: ErrorTaskSource.BackgroundFec,
								reportedSpan: null));
						}
						return;
					}
				});

				var workingDir = ProbeToolsPackage.TempDir;

				var runner = new ProcessRunner();
				runner.CaptureOutput = true;
				runner.CaptureError = true;

				var exitCode = await runner.CaptureProcessAsync("fec.exe", string.Concat(ProbeToolsPackage.Instance.ProbeExplorerOptions.FecArguments,
					" \"", sourceFilePath, "\""), workingDir, output, cancel);
				if (exitCode == 0)
				{
					ProbeToolsPackage.Log.Debug("Background FEC completed successfully.");
				}
				else
				{
					ProbeToolsPackage.Log.Write(LogLevel.Warning, "FEC.exe returned exit code {0} when running background FEC for file '{1}'.", exitCode, sourceFilePath);
				}
				await ErrorTaskProvider.Instance.ReplaceForSourceAndInvokingFileAsync(ErrorTaskSource.BackgroundFec, sourceFilePath, tasks);
			}
			catch (Exception ex)
			{
				ProbeToolsPackage.Log.Error(ex);
			}
			finally
			{
				ProbeToolsPackage.Instance.Compiler.Semaphore.Release();
			}
		}
	}
}
