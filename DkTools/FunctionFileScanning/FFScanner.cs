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
	internal class FFScanner : IDisposable
	{
		private Thread _thread;
		private EventWaitHandle _kill = new EventWaitHandle(false, EventResetMode.ManualReset);

		private ProbeAppSettings _appSettings;
		private FFApp _currentApp;
		private object _currentAppLock = new object();
		private Queue<ScanInfo> _scanQueue;
		private object _scanLock = new object();

		private DateTime _lastDefinitionPublish = DateTime.MinValue;
		private bool _definitionPublishRequired = false;

		private const int k_threadWaitIdle = 1000;
		private const int k_threadWaitActive = 0;

		private const string k_noProbeApp = "(none)";

		private struct ScanInfo : IComparable<ScanInfo>
		{
			public FFScanMode mode;
			public string fileName;
			public bool forceScan;	// Ignore modified date?

			public int CompareTo(ScanInfo other)
			{
				var ret = mode.CompareTo(other.mode);
				if (ret != 0) return ret;

				return string.Compare(fileName, other.fileName, true);
			}
		}

		public FFScanner()
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

		public void OnShutdown()
		{
			Kill();
		}

		public void Dispose()
		{
			Kill();

			if (_kill != null) { _kill.Dispose(); _kill = null; }
		}

		private void ProbeEnvironment_AppChanged(object sender, EventArgs e)
		{
			_appSettings = ProbeEnvironment.CurrentAppSettings;
			LoadCurrentApp();
			RestartScanning("Probe app changed");
		}

		private void Kill()
		{
			if (_thread != null)
			{
				_kill.Set();
				_thread.Join();
			}
		}

		private void ThreadProc()
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

		private void ProcessQueue()
		{
			if (_currentApp == null) return;

			using (var db = new FFDatabase())
			{
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
						ProcessFile(db, CurrentApp, scanInfo.Value);
					}
					else
					{
						_currentApp.PublishDefinitions();

						ProbeToolsPackage.Instance.SetStatusText("DkTools background purging...");
						using (var txn = db.BeginTransaction())
						{
							_currentApp.PurgeData(db);
							txn.Commit();
						}

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
		}

		private void ProcessSourceDir(FFApp app, string dir, List<ScanInfo> scanList)
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

		public void EnqueueChangedFile(string fullPath)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			var fileContext = FileContextUtil.GetFileContextFromFileName(fullPath);
			if (fileContext != FileContext.Include && fileContext != FileContext.Dictionary)
			{
				lock (_scanLock)
				{
					if (_scanQueue == null) _scanQueue = new Queue<ScanInfo>();
					if (!_scanQueue.Any(s => string.Equals(s.fileName, fullPath, StringComparison.OrdinalIgnoreCase)))
					{
						_scanQueue.Enqueue(new ScanInfo
						{
							fileName = fullPath,
							mode = FFScanMode.Deep,
							forceScan = true
						});
					}
				}
			}
		}

		private void ProcessFile(FFDatabase db, FFApp app, ScanInfo scan)
		{
			try
			{
				if (!File.Exists(scan.fileName)) return;
				if (FileContextUtil.IsLocalizedFile(scan.fileName)) return;

				var fileContext = CodeModel.FileContextUtil.GetFileContextFromFileName(scan.fileName);
				if (fileContext == FileContext.Include) return;

				DateTime modified;
				if (!app.TryGetFileDate(scan.fileName, out modified)) modified = DateTime.MinValue;

				var fileModified = File.GetLastWriteTime(scan.fileName);
				if (!scan.forceScan)
				{
					if (modified != DateTime.MinValue && fileModified.Subtract(modified).TotalSeconds < 1.0) return;
				}

				var ffFile = app.GetFileForScan(db, scan.fileName);

				Log.Debug("Processing file: {0} (modified={1}, last modified={2})", scan.fileName, fileModified, modified);
				if (scan.mode == FFScanMode.Exports) ProbeToolsPackage.Instance.SetStatusText(string.Format("DkTools background scanning file: {0} (exports only)", scan.fileName));
				else ProbeToolsPackage.Instance.SetStatusText(string.Format("DkTools background scanning file: {0}", scan.fileName));

				// Make sure all definitions are available when starting deep scanning.
				if (scan.mode != FFScanMode.Exports && _definitionPublishRequired)
				{
					app.PublishDefinitions();
				}

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

				using (var txn = db.BeginTransaction())
				{
					ffFile.UpdateFromModel(model, db, fileStore, fileModified, scan.mode);
					txn.Commit();
				}

				if (ffFile.Visible)
				{
					app.OnVisibleFileChanged(ffFile);
				}
				else
				{
					app.OnInvisibleFileChanged(ffFile);
				}

				if (scan.mode == FFScanMode.Exports)
				{
					_definitionPublishRequired = true;

					if (DateTime.Now.Subtract(_lastDefinitionPublish).TotalSeconds > 5.0)
					{
						app.PublishDefinitions();
						_definitionPublishRequired = false;
						_lastDefinitionPublish = DateTime.Now;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when background processing function name: {0} (mode: {1})", scan.fileName, scan.mode);
			}
		}

		private FFApp CurrentApp
		{
			get
			{
				lock (_currentAppLock)
				{
					return _currentApp;
				}
			}
		}

		public void RestartScanning(string reason)
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

		private void LoadCurrentApp()
		{
			try
			{
				if (!_appSettings.Initialized) return;

				Log.Write(LogLevel.Info, "Loading function file database...");
				var startTime = DateTime.Now;

				using (var db = new FFDatabase())
				{
					_currentApp = new FFApp(this, db, _appSettings);
				}

				_currentApp.PublishDefinitions();

				var elapsed = DateTime.Now.Subtract(startTime);
				Log.Write(LogLevel.Info, "Function file database loaded. (elapsed: {0})", elapsed);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error when loading function file database.");
				_currentApp = null;
			}
		}

		private void ProbeAppSettings_FileChanged(object sender, ProbeAppSettings.FileEventArgs e)
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
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private void EnqueueFilesDependentOnInclude(string includeFileName)
		{
			if (_currentApp == null) return;

			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.DisableBackgroundScan) return;

			using (var db = new FFDatabase())
			{
				var fileIds = new Dictionary<long, string>();

				using (var cmd = db.CreateCommand(@"
					select file_id, file_name from include_depends
					inner join file_ on file_.rowid = include_depends.file_id
					where include_depends.app_id = @app_id
					and include_depends.include_file_name = @include_file_name
					"))
				{
					cmd.Parameters.AddWithValue("@app_id", _currentApp.Id);
					cmd.Parameters.AddWithValue("@include_file_name", includeFileName);

					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							var fileName = rdr.GetString(1);
							var context = FileContextUtil.GetFileContextFromFileName(fileName);
							if (context != FileContext.Include && context != FileContext.Dictionary)
							{
								fileIds[rdr.GetInt64(0)] = fileName;
							}
						}
					}

				}

				if (fileIds.Any())
				{
					Log.Debug("Resetting modified date on {0} file(s).", fileIds.Count);

					using (var txn = db.BeginTransaction())
					{
						using (var cmd = db.CreateCommand("update file_ set modified = '1900-01-01' where rowid = @id"))
						{
							foreach (var item in fileIds)
							{
								cmd.Parameters.Clear();
								cmd.Parameters.AddWithValue("@id", item.Key);
								cmd.ExecuteNonQuery();

								_currentApp.TrySetFileDate(item.Value, DateTime.MinValue);
							}
						}
						txn.Commit();
					}
				}
			}

			RestartScanning("Include file changed; scanning dependent files.");
		}
	}

	public enum FFScanMode
	{
		Exports,
		Deep
	}
}
