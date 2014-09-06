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
		private LockedValue<bool> _running = new LockedValue<bool>(false);
		private LockedValue<int> _threadWait = new LockedValue<int>(k_threadWaitIdle);

		private FFApp _currentApp;
		private object _currentAppLock = new object();
		private Queue<ProcessDir> _dirsToProcess = new Queue<ProcessDir>();
		private Queue<string> _filesToProcess = new Queue<string>();
		private Dictionary<string, FFApp> _apps = new Dictionary<string, FFApp>();
		private LockedValue<bool> _changesMade = new LockedValue<bool>();

		private const int k_threadWaitActive = 0;
		private const int k_threadWaitIdle = 100;
		private const int k_threadSleep = 100;

		private const string k_noProbeApp = "(none)";

		public static readonly Regex FunctionFilePattern = new Regex(@"\\\w+\.(?:f|cc|nc|sc)(?:\&|\+)?$", RegexOptions.IgnoreCase);

		private class ProcessDir
		{
			public string dir;
			public bool root;
		}

		public FFScanner()
		{
			LoadSettings();

			_thread = new Thread(new ThreadStart(ThreadProc));
			_thread.Name = "Function File Scanner";
			_thread.Priority = ThreadPriority.BelowNormal;
			_thread.Start();

			ProbeEnvironment.AppChanged += new EventHandler(ProbeEnvironment_AppChanged);
		}

		public void OnShutdown()
		{
			SaveSettings();
			Kill();
		}

		public void Dispose()
		{
			Kill();

			if (_kill != null) { _kill.Dispose(); _kill = null; }
		}

		private void ProbeEnvironment_AppChanged(object sender, EventArgs e)
		{
			RestartScanning();
		}

		public void Start()
		{
			_threadWait.Value = k_threadWaitActive;
			_running.Value = true;
		}

		public void Stop()
		{
			_threadWait.Value = k_threadWaitIdle;
			_running.Value = false;
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

				string path;
				ProcessDir processDir;

				while (!_kill.WaitOne(_threadWait.Value))
				{
					if (_running.Value)
					{
						path = null;
						lock (_filesToProcess)
						{
							if (_filesToProcess.Count > 0) path = _filesToProcess.Dequeue();
						}
						if (path != null)
						{
							ProcessFunctionFile(GetCurrentApp(), path);
						}
						else
						{
							processDir = null;
							lock (_dirsToProcess)
							{
								if (_dirsToProcess.Count > 0) processDir = _dirsToProcess.Dequeue();
							}
							if (processDir != null)
							{
								ProcessSourceDir(GetCurrentApp(), processDir.dir, processDir.root);
							}
							else
							{
								_threadWait.Value = k_threadWaitIdle;
								_running.Value = false;

								if (_changesMade.Value)
								{
									_changesMade.Value = false;
									SaveSettings();
								}

								Shell.SetStatusText("DkTools Background parsing complete.");
							}
						}
					}
					else
					{
						Thread.Sleep(k_threadSleep);
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception in function file scanner.");
			}
		}

		private void ProcessSourceDir(FFApp app, string dir, bool root)
		{
			try
			{
				foreach (var fileName in Directory.GetFiles(dir))
				{
					if (FunctionFilePattern.IsMatch(fileName)) _filesToProcess.Enqueue(fileName);
				}

				foreach (var subDir in Directory.GetDirectories(dir))
				{
					_dirsToProcess.Enqueue(new ProcessDir { dir = subDir, root = false });
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, string.Format("Exception when scanning directory '{0}' for functions.", dir));
			}
		}

		private void ProcessFunctionFile(FFApp app, string fileName)
		{
			try
			{
				if (!File.Exists(fileName)) return;

				DateTime modified;
				if (!app.TryGetFileDate(fileName, out modified)) modified = DateTime.MinValue;

				if (modified == DateTime.MinValue || Math.Abs(File.GetLastWriteTime(fileName).Subtract(modified).TotalSeconds) >= 1.0)
				{
					Log.WriteDebug("Processing function file: {0}", fileName);
					Shell.SetStatusText(string.Format("DkTools background parsing file: {0}", fileName));

					_changesMade.Value = true;

					var fileTitle = Path.GetFileNameWithoutExtension(fileName);

					var defProvider = new CodeModel.DefinitionProvider();

					app.RemoveAllFunctionsForFile(fileName);

					var fileContent = File.ReadAllText(fileName);
					var fileStore = new CodeModel.FileStore();
					var model = fileStore.CreatePreprocessedModel(fileContent, fileName, string.Concat("Function file processing: ", fileName));

					var ext = Path.GetExtension(fileName).ToLower();
					string className;
					FFUtil.FileNameIsClass(fileName, out className);

					foreach (var funcDef in model.GetDefinitions<CodeModel.Definitions.FunctionDefinition>())
					{
						if (funcDef.Extern) continue;
						if (!string.IsNullOrEmpty(className))
						{
							if (funcDef.Privacy != CodeModel.FunctionPrivacy.Public) continue;
						}
						else
						{
							if (!funcDef.Name.Equals(fileTitle, StringComparison.OrdinalIgnoreCase)) continue;
						}

						string funcFileName = funcDef.SourceFileName;
						CodeModel.Span funcSpan = funcDef.SourceSpan;
						bool primaryFile;

						// Resolve to the actual filename/span, rather than the location within the merged content.
						if (funcDef.SourceFile != null && funcDef.SourceFile.CodeSource != null) funcDef.SourceFile.CodeSource.GetFileSpan(funcDef.SourceSpan, out funcFileName, out funcSpan, out primaryFile);

						app.AddFunction(className, FFFunction.FromCodeModelDefinition(funcDef.CloneAsExtern()));
					}

					app.UpdateFile(fileName, File.GetLastWriteTime(fileName));
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception when background processing function name: {0}", fileName);
				try
				{
					// Don't show the error because this is just a non-critical background thread.
					if (File.Exists(fileName)) app.UpdateFile(fileName, File.GetLastWriteTime(fileName));
				}
				catch (Exception)
				{ }
			}
		}

		private FFApp GetCurrentApp()
		{
			var currentApp = ProbeEnvironment.CurrentApp;
			if (string.IsNullOrEmpty(currentApp)) currentApp = k_noProbeApp;

			FFApp app = null;

			lock (_currentAppLock)
			{
				if (_currentApp == null || _currentApp.Name != currentApp)
				{
					if (_currentApp != null) _currentApp.OnDeactivate();

					if (!_apps.TryGetValue(currentApp, out app))
					{
						app = new FFApp(this, currentApp);
						_apps[currentApp] = app;
					}

					_currentApp = app;
				}
				else app = _currentApp;

			}
			return app;
		}

		public FFFunction GetFunction(string funcName)
		{
			return GetCurrentApp().GetFunction(funcName);
		}

		public IEnumerable<CodeModel.Definitions.Definition> GlobalDefinitions
		{
			get { return GetCurrentApp().GlobalDefinitions; }
		}

		public void RestartScanning()
		{
			lock (_dirsToProcess)
			{
				_dirsToProcess.Clear();
				foreach (var dir in ProbeEnvironment.SourceDirs) _dirsToProcess.Enqueue(new ProcessDir { dir = dir, root = true });
			}
			lock (_filesToProcess)
			{
				_filesToProcess.Clear();
			}

			Start();
		}

		public void SaveSettings()
		{
			try
			{
				Log.WriteDebug("Saving function file database.");

				var db = new FunctionFileDatabase.Database_t();
				db.application = (from a in _apps.Values select a.Save()).ToArray();

				XmlUtil.SerializeToFile(db, FunctionFileDatabaseFileName, true);
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Error when saving function file database.");
			}
		}

		public void LoadSettings()
		{
			try
			{
				Log.WriteDebug("Loading function file database.");

				lock (_apps)
				{
					_apps.Clear();

					var fileName = FunctionFileDatabaseFileName;
					if (File.Exists(fileName))
					{
						var db = XmlUtil.DeserializeFromFile<FunctionFileDatabase.Database_t>(fileName);
						if (db != null)
						{
							foreach (var dbApp in db.application)
							{
								_apps[dbApp.name] = new FFApp(this, dbApp);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Error when loading function file database.");
			}
		}

		private string FunctionFileDatabaseFileName
		{
			get
			{
				return Path.Combine(ProbeToolsPackage.AppDataDir, Constants.FunctionFileDatabaseFileName);
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
			return GetCurrentApp().GetClass(className);
		}
	}
}
