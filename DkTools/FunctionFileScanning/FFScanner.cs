using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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

		public static readonly Regex FunctionFilePattern = new Regex(@"\\\w+\.(?:f|cc|nc|sc)(?:\&|\+)?$", RegexOptions.IgnoreCase);

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
				while (!_kill.WaitOne(k_threadWaitActive))
				{
					string path = null;
					lock (_filesToProcess)
					{
						if (_filesToProcess.Count > 0) path = _filesToProcess.Dequeue();
					}
					if (path != null)
					{
						ProcessFunctionFile(db, CurrentApp, path);
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
							Shell.SetStatusText("DkTools background scanning complete.");
							_currentApp.PurgeUnused(db);
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
					if (FunctionFilePattern.IsMatch(fileName))
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

		private void ProcessFunctionFile(FFDatabase db, FFApp app, string fileName)
		{
			try
			{
				if (!File.Exists(fileName)) return;

				var ffFile = app.GetOrCreateFile(db, fileName);

				DateTime modified;
				if (!app.TryGetFileDate(fileName, out modified)) modified = DateTime.MinValue;

				var fileModified = File.GetLastWriteTime(fileName);
				if (modified != DateTime.MinValue && Math.Abs(fileModified.Subtract(modified).TotalSeconds) < 1.0) return;

				Log.WriteDebug("Processing function file: {0} (modified={1}, last modified={2})", fileName, fileModified, modified);
				Shell.SetStatusText(string.Format("DkTools background scanning file: {0}", fileName));

				var fileTitle = Path.GetFileNameWithoutExtension(fileName);

				var defProvider = new CodeModel.DefinitionProvider(fileName);

				app.MarkAllFunctionsForFileUnused(fileName);

				var fileContent = File.ReadAllText(fileName);
				var fileStore = new CodeModel.FileStore();
				var model = fileStore.CreatePreprocessedModel(fileContent, fileName, false, string.Concat("Function file processing: ", fileName));

				var ext = Path.GetExtension(fileName).ToLower();
				string className;
				FFUtil.FileNameIsClass(fileName, out className);
				var classList = new List<FFClass>();
				var funcList = new List<FFFunction>();

				foreach (var funcDef in model.DefinitionProvider.GetGlobal<CodeModel.Definitions.FunctionDefinition>())
				{
					if (funcDef.Extern) continue;
					if (!string.IsNullOrEmpty(className))
					{
						// This is a class file
						if (funcDef.Privacy != CodeModel.FunctionPrivacy.Public) continue;
					}
					else
					{
						// This is a function file
						if (!funcDef.Name.Equals(fileTitle, StringComparison.OrdinalIgnoreCase)) continue;
					}

					var funcFileName = funcDef.SourceFileName;
					var funcPos = funcDef.SourceStartPos;

					if (string.IsNullOrEmpty(funcFileName) || funcPos < 0 || !string.Equals(funcFileName, fileName, StringComparison.OrdinalIgnoreCase)) continue;

					FFClass ffClass;
					FFFunction ffFunc;
					app.UpdateFunction(ffFile, className, funcDef, out ffClass, out ffFunc);
					if (ffClass != null && !classList.Contains(ffClass))
					{
						classList.Add(ffClass);
						ffClass.MarkUsed();
					}
					funcList.Add(ffFunc);
					ffFunc.MarkUsed();
				}

				ffFile.Modified = fileModified;

				// Save the new info to the database
				ffFile.InsertOrUpdate(db);
				ffFile.MarkUsed();
				foreach (var ffClass in classList) ffClass.InsertOrUpdate(db);
				foreach (var ffFunc in funcList) ffFunc.InsertOrUpdate(db);
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

		public FFFunction GetFunction(string funcName)
		{
			if (_currentApp != null) return CurrentApp.GetFunction(funcName);
			return null;
		}

		public FFFunction GetFunction(string className, string funcName)
		{
			if (_currentApp != null) return CurrentApp.GetFunction(className, funcName);
			return null;
		}

		/// <summary>
		/// Gets a list of definitions that are available at the global scope.
		/// Only public definitions are returned.
		/// </summary>
		public IEnumerable<CodeModel.Definitions.Definition> GlobalDefinitions
		{
			get
			{
				if (_currentApp != null) return CurrentApp.GlobalDefinitions;
				return new CodeModel.Definitions.Definition[0];
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

		private string FunctionFileDatabaseFileName
		{
			get
			{
				return Path.Combine(ProbeToolsPackage.AppDataDir, Constants.FunctionFileDatabaseFileName_XML);
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

		public FFClass GetClass(string className)
		{
			if (_currentApp != null) return CurrentApp.TryGetClass(className);
			return null;
		}

		private void Shell_FileSaved(object sender, Shell.FileSavedEventArgs e)
		{
			if (FunctionFilePattern.IsMatch(e.FileName) && ProbeEnvironment.FileExistsInApp(e.FileName))
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
