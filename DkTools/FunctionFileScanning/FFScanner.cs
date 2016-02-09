using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DkTools.CodeModel;

namespace DkTools.FunctionFileScanning
{
	internal class FFScanner : IDisposable
	{
		private Thread _thread;
		private EventWaitHandle _kill = new EventWaitHandle(false, EventResetMode.ManualReset);

		private FFApp _currentApp;
		private object _currentAppLock = new object();
		private Queue<ScanInfo> _scanQueue = new Queue<ScanInfo>();

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
			LoadCurrentApp(ProbeEnvironment.CurrentApp);

			_thread = new Thread(new ThreadStart(ThreadProc));
			_thread.Name = "Function File Scanner";
			_thread.Priority = ThreadPriority.BelowNormal;
			_thread.Start();

			ProbeEnvironment.AppChanged += new EventHandler(ProbeEnvironment_AppChanged);
			Shell.FileSaved += Shell_FileSaved;
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
			LoadCurrentApp(ProbeEnvironment.CurrentApp);
			RestartScanning();
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
				RestartScanning();

				while (!_kill.WaitOne(k_threadWaitIdle))
				{
					var gotActivity = false;
					lock (_scanQueue)
					{
						if (_scanQueue.Count > 0) gotActivity = true;
					}

					if (gotActivity)
					{
						ProcessQueue();
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception in function file scanner.");
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
					lock (_scanQueue)
					{
						if (_scanQueue.Count > 0)
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
						Shell.SetStatusText("DkTools background purging...");
						_currentApp.PurgeData(db);

						var scanElapsed = DateTime.Now.Subtract(scanStartTime);

						Shell.SetStatusText(string.Format("DkTools background scanning complete.  (elapsed: {0})", scanElapsed));
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
							case FileContext.Dictionary:
								// Ignore dictionary and include files
								break;

							case FileContext.ClientClass:
							case FileContext.Function:
							case FileContext.NeutralClass:
							case FileContext.ServerClass:
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
				Log.WriteEx(ex, string.Format("Exception when scanning directory '{0}' for functions.", dir));
			}
		}

		public void EnqueueChangedFile(string fullPath)
		{
			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (!options.BackgroundScan) return;

			var fileContext = FileContextUtil.GetFileContextFromFileName(fullPath);
			if (fileContext != FileContext.Include && fileContext != FileContext.Dictionary)
			{
				lock (_scanQueue)
				{
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
				if (fileContext == FileContext.Include || fileContext == FileContext.Dictionary) return;

				DateTime modified;
				if (!app.TryGetFileDate(scan.fileName, out modified)) modified = DateTime.MinValue;

				var fileModified = File.GetLastWriteTime(scan.fileName);
				if (!scan.forceScan)
				{
					if (modified != DateTime.MinValue && fileModified.Subtract(modified).TotalSeconds < 1.0) return;
				}

				var ffFile = app.GetFileForScan(db, scan.fileName);

				Log.WriteDebug("Processing file: {0} (modified={1}, last modified={2})", scan.fileName, fileModified, modified);
				if (scan.mode == FFScanMode.Exports) Shell.SetStatusText(string.Format("DkTools background scanning file: {0} (exports only)", scan.fileName));
				else Shell.SetStatusText(string.Format("DkTools background scanning file: {0}", scan.fileName));

				var fileTitle = Path.GetFileNameWithoutExtension(scan.fileName);

				var defProvider = new CodeModel.DefinitionProvider(scan.fileName);

				var fileContent = File.ReadAllText(scan.fileName);
				var fileStore = new CodeModel.FileStore();

				var merger = new FileMerger();
				merger.MergeFile(scan.fileName, null, false, true);
				var includeDependencies = (from f in merger.LocalFileNames select new Preprocessor.IncludeDependency(f, false, true)).ToArray();

				var model = fileStore.CreatePreprocessedModel(merger.MergedContent, scan.fileName, false, string.Concat("Function file processing: ", scan.fileName), includeDependencies);

				var className = fileContext.IsClass() ? Path.GetFileNameWithoutExtension(scan.fileName) : null;
				var classList = new List<FFClass>();
				var funcList = new List<FFFunction>();

				ffFile.UpdateFromModel(model, db, fileStore, fileModified, scan.mode);

				if (ffFile.Visible)
				{
					app.OnVisibleFileChanged(ffFile);
				}
				else
				{
					app.OnInvisibleFileChanged(ffFile);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception when background processing function name: {0} (mode: {1})", scan.fileName, scan.mode);
			}
		}

		public FFApp CurrentApp
		{
			get
			{
				lock (_currentAppLock)
				{
					return _currentApp;
				}
			}
		}

		public void RestartScanning()
		{
			lock (_scanQueue)
			{
				_scanQueue.Clear();
			}

			var options = ProbeToolsPackage.Instance.EditorOptions;
			if (options.BackgroundScan)
			{
				var scanList = new List<ScanInfo>();
				foreach (var dir in ProbeEnvironment.SourceDirs)
				{
					ProcessSourceDir(_currentApp, dir, scanList);
				}

				scanList.Sort();
				lock (_scanQueue)
				{
					foreach (var scanItem in scanList) _scanQueue.Enqueue(scanItem);
				}
			}
		}

		private void LoadCurrentApp(string appName)
		{
			try
			{
				Log.WriteDebug("Loading function file database.");

				using (var db = new FFDatabase())
				{
					_currentApp = new FFApp(this, db, appName);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Error when loading function file database.");
				_currentApp = null;
			}
		}

		private void Shell_FileSaved(object sender, Shell.FileSavedEventArgs e)
		{
			try
			{
				var options = ProbeToolsPackage.Instance.EditorOptions;
				if (options.BackgroundScan)
				{
					var fileContext = FileContextUtil.GetFileContextFromFileName(e.FileName);
					if (ProbeEnvironment.FileExistsInApp(e.FileName))
					{
						if (fileContext != FileContext.Include && !FileContextUtil.IsLocalizedFile(e.FileName))
						{
							Log.WriteDebug("Scanner detected a saved file: {0}", e.FileName);

							EnqueueChangedFile(e.FileName);
						}
						else
						{
							Log.WriteDebug("Scanner detected an include file was saved: {0}", e.FileName);

							EnqueueFilesDependentOnInclude(e.FileName);
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
			if (!options.BackgroundScan) return;

			using (var db = new FFDatabase())
			{
				var fileIds = new Dictionary<int, string>();

				using (var cmd = db.CreateCommand(
					"select file_id, file_name from include_depends"
					+ " inner join file_ on file_.id = include_depends.file_id"
					+ " where include_depends.app_id = @app_id"
					+ " and include_depends.include_file_name = @include_file_name"
					))
				{
					cmd.Parameters.AddWithValue("@app_id", _currentApp.Id);
					cmd.Parameters.AddWithValue("@include_file_name", includeFileName);

					using (var rdr = cmd.ExecuteReader())
					{
						var ordId = rdr.GetOrdinal("file_id");
						var ordFileName = rdr.GetOrdinal("file_name");

						while (rdr.Read())
						{
							var fileName = rdr.GetString(ordFileName);
							var context = FileContextUtil.GetFileContextFromFileName(fileName);
							if (context != FileContext.Include && context != FileContext.Dictionary)
							{
								fileIds[rdr.GetInt32(ordId)] = fileName;
							}
						}
					}

				}

				if (fileIds.Any())
				{
					Log.WriteDebug("Resetting modified date on {0} file(s).", fileIds.Count);

					using (var cmd = db.CreateCommand("update file_ set modified = '1900-01-01' where id = @id"))
					{
						foreach (var item in fileIds)
						{
							cmd.Parameters.Clear();
							cmd.Parameters.AddWithValue("@id", item.Key);
							cmd.ExecuteNonQuery();

							_currentApp.TrySetFileDate(item.Value, DateTime.MinValue);
						}
					}
				}
			}

			RestartScanning();
		}
	}

	public enum FFScanMode
	{
		Exports,
		Deep
	}
}
