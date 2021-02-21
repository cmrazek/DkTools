using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using DkTools.CodeModel;

namespace DkTools.FunctionFileScanning
{
	internal static class FFScanner
	{
		private static Thread _thread;
		private static EventWaitHandle _kill = new EventWaitHandle(false, EventResetMode.ManualReset);

		private static ProbeAppSettings _appSettings;
		private static object _currentAppLock = new object();
		private static Queue<ScanInfo> _scanQueue;
		private static object _scanLock = new object();

		private const int k_threadWaitIdle = 1000;
		private const int k_threadWaitActive = 0;

		private const string k_noProbeApp = "(none)";

		private struct ScanInfo : IComparable<ScanInfo>
		{
			public FFScanMode mode;
			public string fileName;

			public int CompareTo(ScanInfo other)
			{
				var ret = mode.CompareTo(other.mode);
				if (ret != 0) return ret;

				return string.Compare(fileName, other.fileName, true);
			}
		}

		public static void OnStartup()
		{
			_appSettings = ProbeEnvironment.CurrentAppSettings;

			_thread = new Thread(new ThreadStart(ThreadProc));
			_thread.Name = "Function File Scanner";
			_thread.Priority = ThreadPriority.BelowNormal;
			_thread.Start();

			ProbeEnvironment.AppChanged += new EventHandler(ProbeEnvironment_AppChanged);
			ProbeAppSettings.FileChanged += ProbeAppSettings_FileChanged;
		}

		public static void OnShutdown()
		{
			Kill();
		}

		private static void ProbeEnvironment_AppChanged(object sender, EventArgs e)
		{
			_appSettings = ProbeEnvironment.CurrentAppSettings;
			RestartScanning("Probe app changed");
		}

		private static void Kill()
		{
			if (_thread != null)
			{
				_kill.Set();
				_thread.Join();
			}
		}

		private static void ThreadProc()
		{
			try
			{
				RestartScanning("Start of FFScanner thread");

				while (!_kill.WaitOne(k_threadWaitIdle))
				{
					var gotActivity = false;
					lock (_scanLock)
					{
						if (_scanQueue != null && _scanQueue.Count > 0) gotActivity = true;
					}

					if (gotActivity)
					{
						ProcessQueue();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception in function file scanner.");
			}
		}

		private static void ProcessQueue()
		{
			var app = _appSettings;
			if (app == null) return;

			var scanStartTime = DateTime.Now;

			while (!_kill.WaitOne(k_threadWaitActive))
			{
				ScanInfo? scanInfo = null;
				lock (_scanLock)
				{
					if (_scanQueue != null && _scanQueue.Count > 0)
					{
						scanInfo = _scanQueue.Dequeue();
					}
				}

				if (scanInfo.HasValue)
				{
					ProcessFile(app, scanInfo.Value);
				}
				else
				{
					ProbeToolsPackage.Instance.SetStatusText("Finalizing DK repository...");
					app.Repo.OnScanComplete();

					ProbeToolsPackage.Instance.SetStatusText("Saving DK repository...");
					app.Repo.Save();

					var scanElapsed = DateTime.Now.Subtract(scanStartTime);

					ProbeToolsPackage.Instance.SetStatusText(string.Format("DkTools background scanning complete.  (elapsed: {0})", scanElapsed));
					lock (_scanLock)
					{
						_scanQueue = null;
					}
					return;
				}
			}
		}

		private static void ProcessSourceDir(ProbeAppSettings app, string dir, List<ScanInfo> scanList)
		{
			try
			{
				foreach (var fileName in Directory.GetFiles(dir))
				{
					if (!FileContextUtil.IsLocalizedFile(fileName))
					{
						var fileContext = FileContextUtil.GetFileContextFromFileName(fileName);
						switch (fileContext)
						{
							case FileContext.Include:
								// Ignore include files.
								break;

							case FileContext.Dictionary:
								// Deep scan for dictionary only; no exports produced.
								scanList.Add(new ScanInfo { fileName = fileName, mode = FFScanMode.Deep });
								break;

							case FileContext.Function:
							case FileContext.ClientClass:
							case FileContext.NeutralClass:
							case FileContext.ServerClass:
							case FileContext.ServerProgram:
								// Files that export global functions must be scanned twice:
								// First for the exports before everything else, then again for the deep info.
								scanList.Add(new ScanInfo { fileName = fileName, mode = FFScanMode.Exports });
								scanList.Add(new ScanInfo { fileName = fileName, mode = FFScanMode.Deep });
								break;

							default:
								scanList.Add(new ScanInfo { fileName = fileName, mode = FFScanMode.Deep });
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

		public static void EnqueueChangedFile(ProbeAppSettings app, string fullPath)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			var fileContext = FileContextUtil.GetFileContextFromFileName(fullPath);
			if (fileContext != FileContext.Include && fileContext != FileContext.Dictionary)
			{
				app.Repo.ResetScanDateOnFile(fullPath);
			}
		}

		private static void ProcessFile(ProbeAppSettings app, ScanInfo scan)
		{
			try
			{
				if (!File.Exists(scan.fileName)) return;
				if (FileContextUtil.IsLocalizedFile(scan.fileName)) return;

				var fileContext = CodeModel.FileContextUtil.GetFileContextFromFileName(scan.fileName);
				if (fileContext == FileContext.Include) return;

				DateTime modified;
				if (!app.Repo.TryGetFileDate(scan.fileName, out modified)) modified = DateTime.MinValue;

				var fileModified = File.GetLastWriteTime(scan.fileName);
				if (modified != DateTime.MinValue && fileModified.Subtract(modified).TotalSeconds < 1.0) return;

				Log.Debug("Processing file: {0} (modified={1}, last modified={2})", scan.fileName, fileModified, modified);
				if (scan.mode == FFScanMode.Exports) ProbeToolsPackage.Instance.SetStatusText(string.Format("DkTools background scanning file: {0} (exports only)", scan.fileName));
				else ProbeToolsPackage.Instance.SetStatusText(string.Format("DkTools background scanning file: {0}", scan.fileName));

				var fileTitle = Path.GetFileNameWithoutExtension(scan.fileName);

				var defProvider = new CodeModel.DefinitionProvider(app, scan.fileName);

				var fileContent = File.ReadAllText(scan.fileName);
				var fileStore = new CodeModel.FileStore();

				var merger = new FileMerger();
				merger.MergeFile(app, scan.fileName, null, false, true);
				var includeDependencies = (from f in merger.FileNames
										   select new Preprocessor.IncludeDependency(f, false, true, merger.GetFileContent(f))).ToArray();

				var model = fileStore.CreatePreprocessedModel(app, merger.MergedContent, scan.fileName, false, string.Concat("Function file processing: ", scan.fileName), includeDependencies);
				var className = fileContext.IsClass() ? Path.GetFileNameWithoutExtension(scan.fileName) : null;

				app.Repo.UpdateFile(model, scan.mode);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when background processing function name: {0} (mode: {1})", scan.fileName, scan.mode);
			}
		}

		public static void RestartScanning(string reason)
		{
			if (!_appSettings.Initialized) return;

			Log.Debug("Starting FF scanning ({0})", reason);

			lock (_scanLock)
			{
				_scanQueue = new Queue<ScanInfo>();
			}

			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (!options.DisableBackgroundScan)
			{
				var scanList = new List<ScanInfo>();
				foreach (var dir in _appSettings.SourceDirs)
				{
					ProcessSourceDir(_appSettings, dir, scanList);
				}

				scanList.Sort();
				lock (_scanLock)
				{
					foreach (var scanItem in scanList) _scanQueue.Enqueue(scanItem);
				}
			}
		}

		private static void ProbeAppSettings_FileChanged(object sender, ProbeAppSettings.FileEventArgs e)
		{
			try
			{
				var app = _appSettings;
				if (app == null) return;

				var options = ProbeToolsPackage.Instance.EditorOptions;
				if (!options.DisableBackgroundScan)
				{
					var fileContext = FileContextUtil.GetFileContextFromFileName(e.FilePath);
					if (ProbeEnvironment.CurrentAppSettings.FileExistsInApp(e.FilePath))
					{
						if (fileContext != FileContext.Include && !FileContextUtil.IsLocalizedFile(e.FilePath))
						{
							Log.Debug("Scanner detected a saved file: {0}", e.FilePath);

							EnqueueChangedFile(app, e.FilePath);
						}
						else
						{
							Log.Debug("Scanner detected an include file was saved: {0}", e.FilePath);

							EnqueueFilesDependentOnInclude(app, e.FilePath);
						}

						RestartScanning("File changed.");
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private static void EnqueueFilesDependentOnInclude(ProbeAppSettings app, string includeFileName)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			app.Repo.ResetScanDateOnDependentFiles(includeFileName);
		}
	}

	public enum FFScanMode
	{
		Exports,
		Deep
	}
}
