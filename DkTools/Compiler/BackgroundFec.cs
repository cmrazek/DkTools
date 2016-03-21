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
		public static void Run(string sourceFileName, VsText.ITextSnapshot snapshot)
		{
			if (string.IsNullOrWhiteSpace(sourceFileName)) return;

			Log.WriteDebug("Running background FEC for file '{0}'.", sourceFileName);

			var counter = 10;
			while (counter > 0)
			{
				if (!ProbeCompiler.Instance.Mutex.WaitOne(1000))
				{
					Log.WriteDebug("Waiting for other compile/FEC operation to complete...");
					counter--;
					if (counter == 0)
					{
						Log.WriteDebug("Ran out of retries when waiting for compile/FEC operation to complete.");
						return;
					}
				}
				else break;
			}

			try
			{
				var first = true;
				var reportError = new Action<ErrorTask>(task =>
					{
						if (first)
						{
							first = false;
							ErrorTaskProvider.Instance.RemoveAllForFile(sourceFileName);
							ErrorTaskProvider.Instance.RemoveAllForSource(ErrorTaskSource.BackgroundFec, sourceFileName);
						}
						if (task != null) ErrorTaskProvider.Instance.Add(task);
					});

				var output = new CallbackOutput(line =>
					{
						var index = line.IndexOf(": error :");
						if (index >= 0)
						{
							Log.WriteDebug("Background FEC error: {0}", line);

							string fileName;
							int lineNum;
							if (ProbeCompiler.ParseFileNameAndLine(line.Substring(0, index), out fileName, out lineNum))
							{
								var message = line.Substring(index + ": error :".Length).Trim();
								var task = new ErrorTask(fileName, lineNum - 1, message, ErrorType.Error, ErrorTaskSource.BackgroundFec, sourceFileName, snapshot);
								reportError(task);
							}
							return;
						}

						index = line.IndexOf(": warning :");
						if (index >= 0)
						{
							Log.WriteDebug("Background FEC warning: {0}", line);

							string fileName;
							int lineNum;
							if (ProbeCompiler.ParseFileNameAndLine(line.Substring(0, index), out fileName, out lineNum))
							{
								var message = line.Substring(index + ": warning :".Length).Trim();
								var task = new ErrorTask(fileName, lineNum - 1, message, ErrorType.Warning, ErrorTaskSource.BackgroundFec, sourceFileName, snapshot);
								reportError(task);
							}
							return;
						}
					});

				var workingDir = ProbeToolsPackage.TempDir;

				var runner = new ProcessRunner();
				runner.CaptureOutput = true;
				runner.CaptureError = true;

				var exitCode = runner.CaptureProcess("fec.exe", string.Concat("\"", sourceFileName, "\""), workingDir, output);
				if (exitCode == 0)
				{
					if (first) reportError(null);
					Log.WriteDebug("Background FEC completed successfully.");
				}
				else
				{
					Log.Write(LogLevel.Warning, "FEC.exe returned exit code {0} when running background FEC for file '{1}'.", exitCode, sourceFileName);
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
