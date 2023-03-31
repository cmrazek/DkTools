using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DK.Modeling;
using DK.Preprocessing;
using DK.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DkTools.FunctionFileScanning
{
	internal static class FFScanner
	{
		private static List<Thread> _threads = new List<Thread>();
		private static CancellationTokenSource _cancel;
		private static List<ScanJob> _pendingQueue = new List<ScanJob>();
		private static List<ScanJob> _runningQueue = new List<ScanJob>();
		private static object _queueLock = new object();
		private static DateTime _scanStartTime;
		private static BackgroundDeferrer _scanDelay = new BackgroundDeferrer(Constants.FFScannerDelay);

		private static ILogger Log => ProbeToolsPackage.Instance.App.Log;

		private class ScanJob : IComparable<ScanJob>
		{
			public FFScanMode Mode { get; private set; }
			public string Path { get; private set; }
			public DkAppSettings App { get; private set; }

			public ScanJob(FFScanMode mode, string path, DkAppSettings app)
			{
				Mode = mode;
				Path = path;
				App = app ?? throw new ArgumentNullException(nameof(app));
			}

			public int CompareTo(ScanJob other)
			{
				var ret = Mode.CompareTo(other.Mode);
				if (ret != 0) return ret;

				return string.Compare(Path, other.Path, true);
			}
		}

		public static void OnStartup()
		{
            ProbeToolsPackage.Instance.App.AppChanged += ProbeEnvironment_AppChanged;
            ProbeToolsPackage.Instance.App.FileChanged += ProbeAppSettings_FileChanged;
			_scanDelay.Idle += ScanTimerElapsed;

			StartScanning(ProbeToolsPackage.Instance.App.Settings);
		}

		public static void OnShutdown()
		{
			Kill();
		}

		private static void ProbeEnvironment_AppChanged(object sender, AppSettingsEventArgs e)
		{
			StartScanning(e.AppSettings);
		}

		private static void Kill()
		{
			lock (_queueLock)
			{
				if (_threads.Any(t => t.IsAlive))
				{
					Log.Debug("Stopping existing scan threads.");
					_cancel?.Cancel();

					var remainingThread = _threads.FirstOrDefault(t => t.IsAlive);
					while (remainingThread != null)
					{
						remainingThread.Join();
					}
				}
			}
		}

		private static void StartScanningLater()
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan)
			{
				Log.Debug("Scanning aborted because it's disabled in the options.");
				return;
			}

			var app = ProbeToolsPackage.Instance.App.Settings;
			if (app == null)
			{
				Log.Warning("Scanning aborted because there is no current app.");
				return;
			}

			lock (_queueLock)
			{
				if (_threads.Any(x => x.IsAlive))
				{
					Log.Debug("FFScanner already in progress.");
					return;
				}
			}

			Log.Debug("FFScanner defer...");
			_scanDelay.OnActivity();
		}

		private static void ScanTimerElapsed(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			Log.Debug("FFScanner delay elapsed.");
			StartScanning(ProbeToolsPackage.Instance.App.Settings);
		}

		private static void StartScanning(DkAppSettings appSettings)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan)
			{
				Log.Debug("Scanning aborted because it's disabled in the options.");
				return;
			}

			lock (_queueLock)
			{
				Log.Info("FFScanner starting.");

				Kill();
				_cancel = new CancellationTokenSource();
				_threads.Clear();

				_scanStartTime = DateTime.Now;
				_pendingQueue.Clear();
				_runningQueue.Clear();

				foreach (var sourceDir in appSettings.SourceDirs)
				{
					_pendingQueue.Add(new ScanJob(FFScanMode.FolderSearch, sourceDir, appSettings));
				}

				_pendingQueue.Add(new ScanJob(FFScanMode.ExportsComplete, null, appSettings));
				_pendingQueue.Add(new ScanJob(FFScanMode.Completion, null, appSettings));
				_pendingQueue.Sort();

				var numThreads = Environment.ProcessorCount - 1;
				if (numThreads < 1) numThreads = 1;
				Log.Debug("Starting {0} worker threads.", numThreads);

				for (int i = 0; i < numThreads; i++)
				{
					var thread = new Thread(new ParameterizedThreadStart(ThreadProc));
					thread.Name = $"FFScanner {i + 1}";
					thread.Priority = ThreadPriority.BelowNormal;
					thread.Start(_cancel.Token);
				}
			}
		}

		private static void ThreadProc(object cancelToken)
		{
			try
			{
				var cancel = (CancellationToken)cancelToken;

				Log.Debug("FFScanner thread starting.");

				ScanJob job;
				int timeout = 0;

				while (!cancel.IsCancellationRequested)
				{
					if (!GetJobFromQueue(out job)) break;
					if (job != null)
					{
						ProcessJob(job, cancel);
						timeout = 0;
					}
					else
					{
						timeout = 10;
					}

					if (timeout > 0) Thread.Sleep(timeout);
				}

				Log.Debug("FFScanner thread stopping.");
			}
			catch (OperationCanceledException ex)
			{
				Log.Debug(ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception in FFScanner thread.");
			}
		}

		private static bool GetJobFromQueue(out ScanJob job)
		{
			// This function will return true if there is still more jobs remaining in the queue.
			// It may return a job of null but true when the thread needs to wait for other jobs in a previous phase to complete first.

			lock (_queueLock)
			{
				if (_pendingQueue.Count > 0)
				{
					var nextJob = _pendingQueue[0];

					if (_runningQueue.Any(x => x.Mode != nextJob.Mode))
					{
						// Other threads are still process jobs of a previous phase.
						// Tell the thread to wait for now.
						job = null;
					}
					else
					{
						job = nextJob;
						_pendingQueue.RemoveAt(0);
						_runningQueue.Add(nextJob);
					}

					return true;
				}
				else if (_runningQueue.Count > 0)
				{
					// There's nothing in the queue, but other threads are still processing jobs.
					// Tell the thread to wait for now.
					job = null;
					return true;
				}
				else
				{
					// All jobs finished. Time to exit the threads.
					job = null;
					return false;
				}
			}
		}

		private static void CompleteJob(ScanJob job)
		{
			lock (_queueLock)
			{
				_runningQueue.Remove(job);
			}
		}

		private static void ProcessJob(ScanJob job, CancellationToken cancel)
		{
			try
			{
				Log.Debug("FFScanner [{2}] Job: {0} {1}", job.Mode, job.Path, Thread.CurrentThread.ManagedThreadId);

				switch (job.Mode)
				{
					case FFScanMode.FolderSearch:
						{
							var scanList = new List<ScanJob>();
							ProcessSourceDir(job.App, job.Path, scanList);
							if (scanList.Count > 0)
							{
								lock (_queueLock)
								{
									_pendingQueue.AddRange(scanList);
									_pendingQueue.Sort();
								}
							}
						}
						break;

					case FFScanMode.Exports:
					case FFScanMode.Deep:
						ProcessFile(job.App, job, cancel);
						break;

					case FFScanMode.ExportsComplete:
						job.App.Repo.OnExportsComplete();
						break;

					case FFScanMode.Completion:
						{
							ProbeToolsPackage.Instance.SetStatusText("Finalizing DK repository...");
							job.App.Repo.OnScanComplete();

							var scanElapsed = DateTime.Now.Subtract(_scanStartTime);
							ProbeToolsPackage.Instance.SetStatusText(string.Format("DkTools scan complete.  (elapsed: {0})", scanElapsed));
						}
						break;
				}
			}
			catch (OperationCanceledException ex)
			{
				Log.Debug(ex);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "FFScanner job exception.");
			}
			finally
			{
				CompleteJob(job);
			}
		}

		private static void ProcessSourceDir(DkAppSettings app, string dir, List<ScanJob> scanList)
		{
			try
			{
				foreach (var fileName in Directory.GetFiles(dir))
				{
					if (!FileContextHelper.IsLocalizedFile(fileName))
					{
						var fileContext = FileContextHelper.GetFileContextFromFileName(fileName);
						switch (fileContext)
						{
							case FileContext.Include:
								// Ignore include files.
								break;

							case FileContext.Dictionary:
								// Deep scan for dictionary only; no exports produced.
								if (FileRequiresScan(app, fileName))
								{
									scanList.Add(new ScanJob(FFScanMode.Deep, fileName, app));
								}
								break;

							case FileContext.Function:
							case FileContext.ClientClass:
							case FileContext.NeutralClass:
							case FileContext.ServerClass:
							case FileContext.ServerProgram:
								// Files that export global functions must be scanned twice:
								// First for the exports before everything else, then again for the deep info.
								if (FileRequiresScan(app, fileName))
								{
									scanList.Add(new ScanJob(FFScanMode.Exports, fileName, app));
									scanList.Add(new ScanJob(FFScanMode.Deep, fileName, app));
								}
								break;

							default:
								if (FileRequiresScan(app, fileName))
								{
									scanList.Add(new ScanJob(FFScanMode.Deep, fileName, app));
								}
								break;
						}
					}
				}

				foreach (var subDir in Directory.GetDirectories(dir))
				{
					ProcessSourceDir(app, subDir, scanList);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, string.Format("Exception when scanning directory '{0}' for functions.", dir));
			}
		}

		private static bool FileRequiresScan(DkAppSettings app, string fileName)
		{
			var modified = File.GetLastWriteTime(fileName);

			if (!app.Repo.TryGetFileDate(fileName, out var scanDate)) return true;
			if (scanDate.AddSeconds(1) < modified) return true;

			return false;
		}

		private static void ProcessFile(DkAppSettings appSettings, ScanJob scan, CancellationToken cancel)
		{
			try
			{
				if (!File.Exists(scan.Path)) return;
				if (FileContextHelper.IsLocalizedFile(scan.Path)) return;

				var fileContext = FileContextHelper.GetFileContextFromFileName(scan.Path);
				if (fileContext == FileContext.Include) return;

				DateTime modified;
				if (!appSettings.Repo.TryGetFileDate(scan.Path, out modified)) modified = DateTime.MinValue;

				var fileModified = File.GetLastWriteTime(scan.Path);
				if (modified != DateTime.MinValue && fileModified.Subtract(modified).TotalSeconds < 1.0) return;

				Log.Debug("Processing file: {0} (modified={1}, last modified={2})", scan.Path, fileModified, modified);
				if (scan.Mode == FFScanMode.Exports) ProbeToolsPackage.Instance.SetStatusText(string.Format("DK Scan: {0} (exports only)", scan.Path));
				else ProbeToolsPackage.Instance.SetStatusText(string.Format("DK Scan: {0}", scan.Path));

				var fileTitle = Path.GetFileNameWithoutExtension(scan.Path);

				var defProvider = new DefinitionProvider(appSettings, scan.Path);

				var fileContent = File.ReadAllText(scan.Path);
				var fileStore = new FileStore(appSettings.Context);

				var merger = new FileMerger(appSettings);
				merger.MergeFile(scan.Path, null, false, true);
				var includeDependencies = (from f in merger.FileNames
										   select new IncludeDependency(f, false, true, merger.GetFileContent(f))).ToArray();

				var model = fileStore.CreatePreprocessedModel(
					appSettings: appSettings,
					source: merger.MergedContent,
					fileName: scan.Path,
					visible: false,
					reason: string.Concat("Function file processing: ", scan.Path),
					cancel: cancel,
					includeDependencies: includeDependencies);

				var className = fileContext.IsClass() ? Path.GetFileNameWithoutExtension(scan.Path) : null;

				appSettings.Repo.UpdateFile(model, scan.Mode);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when background processing function name: {0} (mode: {1})", scan.Path, scan.Mode);
			}
		}

		private static void ProbeAppSettings_FileChanged(object sender, FileEventArgs e)
		{
			try
			{
				var app = ProbeToolsPackage.Instance.App.Settings;
				if (app == null) return;

				var options = ProbeToolsPackage.Instance.EditorOptions;
				if (!options.DisableBackgroundScan)
				{
					var fileContext = FileContextHelper.GetFileContextFromFileName(e.FilePath);
					if (app.FileExistsInApp(e.FilePath))
					{
						if (fileContext != FileContext.Include && !FileContextHelper.IsLocalizedFile(e.FilePath))
						{
							Log.Debug("Scanner detected a saved file: {0}", e.FilePath);

							ResetDateOnFile(app, e.FilePath);
						}
						else
						{
							Log.Debug("Scanner detected an include file was saved: {0}", e.FilePath);

							ResetDateOnDependentFiles(app, e.FilePath);
						}

						StartScanningLater();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public static void ResetDateOnFile(DkAppSettings app, string fullPath)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			var fileContext = FileContextHelper.GetFileContextFromFileName(fullPath);
			if (fileContext != FileContext.Include && fileContext != FileContext.Dictionary)
			{
				app.Repo.ResetScanDateOnFile(fullPath);
			}
		}

		private static void ResetDateOnDependentFiles(DkAppSettings app, string includeFileName)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			app.Repo.ResetScanDateOnDependentFiles(includeFileName);
		}
	}
}
