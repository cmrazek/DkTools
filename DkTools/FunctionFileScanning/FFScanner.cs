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
		private static FFApp _currentApp;
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
			LoadCurrentApp();

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
			LoadCurrentApp();
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
			if (_currentApp == null) return;

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
					ProcessFile(CurrentApp, scanInfo.Value);
				}
				else
				{
					ProbeToolsPackage.Instance.SetStatusText("Finalizing DK repository...");
					CurrentApp.Repo.OnScanComplete();

					ProbeToolsPackage.Instance.SetStatusText("Saving DK repository...");
					CurrentApp.Repo.Save();

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

		private static void ProcessSourceDir(FFApp app, string dir, List<ScanInfo> scanList)
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

		public static void EnqueueChangedFile(string fullPath)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			var fileContext = FileContextUtil.GetFileContextFromFileName(fullPath);
			if (fileContext != FileContext.Include && fileContext != FileContext.Dictionary)
			{
				_currentApp.Repo.ResetScanDateOnFile(fullPath);
			}
		}

		private static void ProcessFile(FFApp app, ScanInfo scan)
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

				var defProvider = new CodeModel.DefinitionProvider(_appSettings, scan.fileName);

				var fileContent = File.ReadAllText(scan.fileName);
				var fileStore = new CodeModel.FileStore();

				var merger = new FileMerger();
				merger.MergeFile(_appSettings, scan.fileName, null, false, true);
				var includeDependencies = (from f in merger.FileNames
										   select new Preprocessor.IncludeDependency(f, false, true, merger.GetFileContent(f))).ToArray();

				var model = fileStore.CreatePreprocessedModel(_appSettings, merger.MergedContent, scan.fileName, false, string.Concat("Function file processing: ", scan.fileName), includeDependencies);
				var className = fileContext.IsClass() ? Path.GetFileNameWithoutExtension(scan.fileName) : null;

				app.Repo.UpdateFile(model, scan.mode);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when background processing function name: {0} (mode: {1})", scan.fileName, scan.mode);
			}
		}

		public static FFApp CurrentApp
		{
			get
			{
				lock (_currentAppLock)
				{
					return _currentApp;
				}
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
					ProcessSourceDir(_currentApp, dir, scanList);
				}

				scanList.Sort();
				lock (_scanLock)
				{
					foreach (var scanItem in scanList) _scanQueue.Enqueue(scanItem);
				}
			}
		}

		private static void LoadCurrentApp()
		{
			try
			{
				if (!_appSettings.Initialized) return;

				Log.Write(LogLevel.Info, "Loading DK repository...");
				var startTime = DateTime.Now;

				_currentApp = new FFApp(_appSettings);

				var elapsed = DateTime.Now.Subtract(startTime);
				Log.Write(LogLevel.Info, "DK repository loaded. (elapsed: {0})", elapsed);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error when loading DK repository.");
				_currentApp = null;
			}
		}

		private static void ProbeAppSettings_FileChanged(object sender, ProbeAppSettings.FileEventArgs e)
		{
			try
			{
				var options = ProbeToolsPackage.Instance.EditorOptions;
				if (!options.DisableBackgroundScan)
				{
					var fileContext = FileContextUtil.GetFileContextFromFileName(e.FilePath);
					if (ProbeEnvironment.CurrentAppSettings.FileExistsInApp(e.FilePath))
					{
						if (fileContext != FileContext.Include && !FileContextUtil.IsLocalizedFile(e.FilePath))
						{
							Log.Debug("Scanner detected a saved file: {0}", e.FilePath);

							EnqueueChangedFile(e.FilePath);
						}
						else
						{
							Log.Debug("Scanner detected an include file was saved: {0}", e.FilePath);

							EnqueueFilesDependentOnInclude(e.FilePath);
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

		private static void EnqueueFilesDependentOnInclude(string includeFileName)
		{
			if (_currentApp == null) return;

			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			_currentApp.Repo.ResetScanDateOnDependentFiles(includeFileName);
		}
	}

	public enum FFScanMode
	{
		Exports,
		Deep
	}
}
