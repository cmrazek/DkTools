using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.ErrorTagging;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.Compiler
{
	class BackgroundFec
	{
		public static void RunSync(string sourceFilePath)
		{
			if (string.IsNullOrWhiteSpace(sourceFilePath)) return;

			Log.Debug("Running background FEC for file '{0}'.", sourceFilePath);

			var counter = 10;
			while (counter > 0)
			{
				if (!ProbeCompiler.Instance.Mutex.WaitOne(1000))
				{
					Log.Debug("Waiting for other compile/FEC operation to complete...");
					counter--;
					if (counter == 0)
					{
						Log.Debug("Ran out of retries when waiting for compile/FEC operation to complete.");
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
						Log.Debug("Background FEC error: {0}", line);

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
								reportedSpan: null,
								snapshotSpan: null));
						}
						return;
					}

					index = line.IndexOf(": warning :");
					if (index >= 0)
					{
						Log.Debug("Background FEC warning: {0}", line);

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
								reportedSpan: null,
								snapshotSpan: null));
						}
						return;
					}
				});

				var workingDir = ProbeToolsPackage.TempDir;

				var runner = new ProcessRunner();
				runner.CaptureOutput = true;
				runner.CaptureError = true;

				var exitCode = runner.CaptureProcess("fec.exe", string.Concat("\"", sourceFilePath, "\""), workingDir, output);
				if (exitCode == 0)
				{
					ErrorTaskProvider.Instance.ReplaceForSourceAndInvokingFile(ErrorTaskSource.BackgroundFec, sourceFilePath, tasks);
					Log.Debug("Background FEC completed successfully.");
				}
				else
				{
					Log.Write(LogLevel.Warning, "FEC.exe returned exit code {0} when running background FEC for file '{1}'.", exitCode, sourceFilePath);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
			finally
			{
				ProbeCompiler.Instance.Mutex.ReleaseMutex();
			}
		}
	}
}
