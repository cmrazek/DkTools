using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;
using DkTools.FunctionFileScanning.FunctionFileDatabase;

namespace DkTools.FunctionFileScanning
{
	internal class FunctionFileApp : IDisposable
	{
		private FunctionFileScanner _scanner;
		private string _name;
		private Dictionary<string, FunctionFileDatabase.Function_t> _functions = new Dictionary<string, FunctionFileDatabase.Function_t>();
		private Dictionary<string, DateTime> _fileDates = new Dictionary<string, DateTime>();
		private FileSystemWatcherCollection _watchers = new FileSystemWatcherCollection();
		private CodeModel.Definitions.Definition[] _definitions = null;

		public FunctionFileApp(FunctionFileScanner scanner, string name)
		{
			if (scanner == null) throw new ArgumentNullException("scanner");
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

			_scanner = scanner;
			_name = name;
		}

		public FunctionFileApp(FunctionFileScanner scanner, Application_t dbApp)
		{
			if (scanner == null) throw new ArgumentNullException("scanner");
			if (dbApp == null) throw new ArgumentNullException("dbApp");

			_scanner = scanner;
			_name = dbApp.name;

			if (dbApp.function != null)
			{
				foreach (var func in dbApp.function)
				{
					if (func == null || string.IsNullOrWhiteSpace(func.name) || string.IsNullOrWhiteSpace(func.signature)) continue;
					_functions[func.name] = func;
				}
			}

			if (dbApp.file != null)
			{
				foreach (var file in dbApp.file)
				{
					if (file == null || string.IsNullOrWhiteSpace(file.fileName)) continue;
					_fileDates[file.fileName] = file.modified;
				}
			}

			// Check for incomplete data in functions.
			foreach (var func in (from f in _functions.Values
								  where string.IsNullOrWhiteSpace(f.signature) || f.dataType == null
								  select f))
			{
				UpdateFile(func.fileName, DateTime.MinValue);	// Set to an invalid time so it gets reprocessed.
			}
		}

		public FunctionFileDatabase.Application_t Save()
		{
			return new FunctionFileDatabase.Application_t
			{
				name = _name,
				function = _functions.Values.ToArray(),
				file = (from f in _fileDates select new FunctionFileDatabase.FunctionFile_t { fileName = f.Key, modified = f.Value }).ToArray()
			};
		}

		public void OnDeactivate()
		{
			foreach (var watcher in _watchers) watcher.Dispose();
			_watchers.Clear();
		}

		public void WatchDir(string dir)
		{
			try
			{
				if (_watchers.ContainsPath(dir.ToLower())) return;

				var fsw = new FileSystemWatcher();
				try
				{
					fsw.Path = dir;
					fsw.Filter = "*.f*";
					fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
					fsw.IncludeSubdirectories = true;

					fsw.Changed += new FileSystemEventHandler(fsw_Changed);
					fsw.Created += new FileSystemEventHandler(fsw_Created);
					fsw.Renamed += new RenamedEventHandler(fsw_Renamed);

					fsw.EnableRaisingEvents = true;

					_watchers.Add(fsw);
				}
				catch
				{
					fsw.Dispose();
					throw;
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Error when watching directory for function file changes.");
			}
		}

		private void fsw_Changed(object sender, FileSystemEventArgs e)
		{
			try
			{
				if (FunctionFileScanner.FunctionFilePattern.IsMatch(e.FullPath))
				{
					_scanner.EnqueueFile(e.FullPath);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private void fsw_Created(object sender, FileSystemEventArgs e)
		{
			try
			{
				if (FunctionFileScanner.FunctionFilePattern.IsMatch(e.FullPath))
				{
					_scanner.EnqueueFile(e.FullPath);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		private void fsw_Renamed(object sender, RenamedEventArgs e)
		{
			try
			{
				if (FunctionFileScanner.FunctionFilePattern.IsMatch(e.FullPath))
				{
					_scanner.EnqueueFile(e.FullPath);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		public void Dispose()
		{
			if (_watchers != null) { _watchers.Dispose(); _watchers = null; }
		}

		public string Name
		{
			get { return _name; }
		}

		public void AddFunction(FunctionFileDatabase.Function_t funcDef)
		{
			if (funcDef == null) throw new ArgumentNullException("funcDef");
			if (string.IsNullOrEmpty(funcDef.name)) throw new ArgumentException("Function name is missing.");
			if (string.IsNullOrEmpty(funcDef.signature)) throw new ArgumentException("Function signature is missing.");

			lock (_functions)
			{
				_functions[funcDef.name] = funcDef;
				_definitions = null;
			}
		}

		public void RemoveFunctionIgnoreCase(string name)
		{
			lock (_functions)
			{
				var names = (from f in _functions.Keys where string.Equals(f, name, StringComparison.OrdinalIgnoreCase) select f).ToArray();
				foreach (var funcName in names) _functions.Remove(funcName);
			}
		}

		public void UpdateFile(string fileName, DateTime modified)
		{
			lock (_fileDates)
			{
				_fileDates[fileName.ToLower()] = modified;
			}
		}

		public IEnumerable<FunctionFileDatabase.Function_t> GetFunctionSignatures()
		{
			lock (_functions)
			{
				return _functions.Values.ToArray();
			}
		}

		public Function_t GetFunction(string funcName)
		{
			lock (_functions)
			{
				Function_t func;
				if (_functions.TryGetValue(funcName, out func)) return func;
				return null;
			}
		}

		public IEnumerable<CodeModel.Definitions.Definition> AllDefinitions
		{
			get
			{
				var defs = _definitions;
				if (defs == null)
				{
					lock (_functions)
					{
						_definitions = defs = (from f in _functions.Values select f.ToDefinition()).ToArray();
					}
				}
				return defs;
			}
		}

		public bool TryGetFileDate(string fileName, out DateTime modified)
		{
			lock (_fileDates)
			{
				DateTime mod;
				if (!_fileDates.TryGetValue(fileName.ToLower(), out mod))
				{
					modified = DateTime.MinValue;
					return false;
				}

				modified = mod;
				return true;
			}
		}
	}
}
