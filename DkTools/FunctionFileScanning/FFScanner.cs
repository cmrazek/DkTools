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
		private Queue<ProcessDir> _dirsToProcess = new Queue<ProcessDir>();
		private Queue<string> _filesToProcess = new Queue<string>();

		private const int k_threadWaitIdle = 1000;
		private const int k_threadWaitActive = 0;

		private const string k_noProbeApp = "(none)";

		private class ProcessDir
		{
			public string dir;
			public bool root;
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
					lock (_filesToProcess)
					{
						if (_filesToProcess.Count > 0) gotActivity = true;
					}
					if (!gotActivity)
					{
						lock (_dirsToProcess)
						{
							if (_dirsToProcess.Count > 0) gotActivity = true;
						}
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
					string path = null;
					lock (_filesToProcess)
					{
						if (_filesToProcess.Count > 0) path = _filesToProcess.Dequeue();
					}
					if (path != null)
					{
						ProcessFile(db, CurrentApp, path);
					}
					else
					{
						ProcessDir processDir = null;
						lock (_dirsToProcess)
						{
							if (_dirsToProcess.Count > 0) processDir = _dirsToProcess.Dequeue();
						}
						if (processDir != null)
						{
							ProcessSourceDir(CurrentApp, processDir.dir, processDir.root);
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
		}

		private void ProcessSourceDir(FFApp app, string dir, bool root)
		{
			try
			{
				foreach (var fileName in Directory.GetFiles(dir))
				{
					var fileContext = FileContextUtil.GetFileContextFromFileName(fileName);
					if (fileContext != FileContext.Include)
					{
						lock (_filesToProcess)
						{
							_filesToProcess.Enqueue(fileName);
						}
					}
				}

				foreach (var subDir in Directory.GetDirectories(dir))
				{
					lock (_dirsToProcess)
					{
						_dirsToProcess.Enqueue(new ProcessDir { dir = subDir, root = false });
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, string.Format("Exception when scanning directory '{0}' for functions.", dir));
			}
		}

		private void ProcessFile(FFDatabase db, FFApp app, string fileName)
		{
			try
			{
				if (!File.Exists(fileName)) return;

				DateTime modified;
				if (!app.TryGetFileDate(fileName, out modified)) modified = DateTime.MinValue;

				var fileModified = File.GetLastWriteTime(fileName);
				if (modified != DateTime.MinValue && fileModified.Subtract(modified).TotalSeconds < 1.0) return;

				var ffFile = app.GetFileForScan(db, fileName);

				Log.WriteDebug("Processing file: {0} (modified={1}, last modified={2})", fileName, fileModified, modified);
				Shell.SetStatusText(string.Format("DkTools background scanning file: {0}", fileName));

				var fileTitle = Path.GetFileNameWithoutExtension(fileName);

				var defProvider = new CodeModel.DefinitionProvider(fileName);

				//app.MarkAllFunctionsForFileUnused(fileName);

				var fileContext = CodeModel.FileContextUtil.GetFileContextFromFileName(fileName);
				var fileContent = File.ReadAllText(fileName);
				var fileStore = new CodeModel.FileStore();
				var model = fileStore.CreatePreprocessedModel(fileContent, fileName, false, string.Concat("Function file processing: ", fileName));

				var className = fileContext.IsClass() ? Path.GetFileNameWithoutExtension(fileName) : null;
				var classList = new List<FFClass>();
				var funcList = new List<FFFunction>();

				ffFile.UpdateFromModel(model, db, fileStore, fileModified);

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
				Log.WriteEx(ex, "Exception when background processing function name: {0}", fileName);
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
			lock (_filesToProcess)
			{
				_filesToProcess.Clear();
			}
			lock (_dirsToProcess)
			{
				_dirsToProcess.Clear();
				foreach (var dir in ProbeEnvironment.SourceDirs) _dirsToProcess.Enqueue(new ProcessDir { dir = dir, root = true });
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

		public void EnqueueFile(string fullPath)
		{
			lock (_filesToProcess)
			{
				if (!_filesToProcess.Any(x => string.Equals(x, fullPath, StringComparison.OrdinalIgnoreCase)))
				{
					_filesToProcess.Enqueue(fullPath);
				}
			}
		}

		private void Shell_FileSaved(object sender, Shell.FileSavedEventArgs e)
		{
			var fileContext = FileContextUtil.GetFileContextFromFileName(e.FileName);
			if (fileContext != FileContext.Include && ProbeEnvironment.FileExistsInApp(e.FileName))
			{
				Log.WriteDebug("Function file scanner detected a saved file: {0}", e.FileName);

				lock (_filesToProcess)
				{
					if (!_filesToProcess.Contains(e.FileName))
					{
						_filesToProcess.Enqueue(e.FileName);
					}
				}
			}
		}
	}
}
