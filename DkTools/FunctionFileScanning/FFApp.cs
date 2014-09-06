using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;
using DkTools.FunctionFileScanning.FunctionFileDatabase;

namespace DkTools.FunctionFileScanning
{
	internal class FFApp
	{
		private FFScanner _scanner;
		private string _name;
		private Dictionary<string, FFFunction> _functions = new Dictionary<string, FFFunction>();
		private Dictionary<string, FFClass> _classes = new Dictionary<string, FFClass>();
		private Dictionary<string, DateTime> _fileDates = new Dictionary<string, DateTime>();
		private CodeModel.Definitions.Definition[] _definitions = null;
		private object _definitionsLock = new object();

		public FFApp(FFScanner scanner, string name)
		{
			if (scanner == null) throw new ArgumentNullException("scanner");
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

			_scanner = scanner;
			_name = name;
		}

		public FFApp(FFScanner scanner, Application_t dbApp)
		{
			if (scanner == null) throw new ArgumentNullException("scanner");
			if (dbApp == null) throw new ArgumentNullException("dbApp");

			_scanner = scanner;
			_name = dbApp.name;

			if (dbApp.function != null)
			{
				foreach (var func in dbApp.function)
				{
					if (func != null)
					{
						var ffFunc = FFFunction.FromDatabase(func);
						if (ffFunc != null) _functions[ffFunc.Name] = ffFunc;
					}
				}
			}

			if (dbApp.@class != null)
			{
				foreach (var cls in dbApp.@class)
				{
					if (cls != null)
					{
						var ffClass = FFClass.FromDatabase(cls);
						if (ffClass != null) _classes[ffClass.Name] = ffClass;
					}
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


			//// Check for incomplete data in functions.
			//foreach (var func in (from f in _functions.Values
			//					  where string.IsNullOrWhiteSpace(f.signature) || f.dataType == null
			//					  select f))
			//{
			//	UpdateFile(func.fileName, DateTime.MinValue);	// Set to an invalid time so it gets reprocessed.
			//}
		}

		public FunctionFileDatabase.Application_t Save()
		{
			return new FunctionFileDatabase.Application_t
			{
				name = _name,
				function = (from f in _functions.Values select f.ToDatabase()).ToArray(),
				@class = (from c in _classes.Values select c.ToDatabase()).ToArray(),
				file = (from f in _fileDates select new FunctionFileDatabase.FunctionFile_t { fileName = f.Key, modified = f.Value }).ToArray()
			};
		}

		public void OnDeactivate()
		{
		}

		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		/// Adds a function to the database.
		/// </summary>
		/// <param name="className">Optional class name for this function to reside under.</param>
		/// <param name="funcDef">Function definition object.</param>
		public void AddFunction(string className, FFFunction funcDef)
		{
			if (funcDef == null) throw new ArgumentNullException("funcDef");

			if (!string.IsNullOrEmpty(className))
			{
				FFClass cls;
				lock (_classes)
				{
					if (!_classes.TryGetValue(className, out cls))
					{
						cls = new FFClass(className, funcDef.FileName);
						_classes[className] = cls;
					}
					cls.AddFunction(funcDef);
				}
			}
			else
			{
				lock (_functions)
				{
					_functions[funcDef.Name] = funcDef;
				}
			}

			lock (_definitionsLock)
			{
				_definitions = null;
			}
		}

		public void UpdateFile(string fileName, DateTime modified)
		{
			lock (_fileDates)
			{
				_fileDates[fileName.ToLower()] = modified;
			}
		}

		public IEnumerable<FFFunction> GetFunctionSignatures()
		{
			lock (_functions)
			{
				return _functions.Values.ToArray();
			}
		}

		public FFFunction GetFunction(string funcName)
		{
			lock (_functions)
			{
				FFFunction func;
				if (_functions.TryGetValue(funcName, out func)) return func;
				return null;
			}
		}

		public IEnumerable<CodeModel.Definitions.Definition> GlobalDefinitions
		{
			get
			{
				lock (_definitionsLock)
				{
					if (_definitions == null)
					{
						var defList = new List<Definition>();
						defList.AddRange(from f in _functions.Values select f.Definition);

						// Create a distinct list of filenames that contain classes.
						foreach (var cls in _classes.Values)
						{
							defList.Add(cls.ClassDefinition);
						}

						_definitions = defList.ToArray();
					}
					return _definitions;
				}
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

		public void RemoveAllFunctionsForFile(string fileName)
		{
			string className;
			if (FFUtil.FileNameIsClass(fileName, out className))
			{
				lock (_classes)
				{
					_classes.Remove(className);
				}
			}
			else
			{
				lock (_functions)
				{
					var funcName = Path.GetFileNameWithoutExtension(fileName);

					List<string> funcsToRemove = null;
					foreach (var node in _functions)
					{
						if (node.Key.Equals(funcName, StringComparison.OrdinalIgnoreCase))
						{
							if (funcsToRemove == null) funcsToRemove = new List<string>();
							funcsToRemove.Add(node.Key);
						}
					}

					if (funcsToRemove != null)
					{
						foreach (var func in funcsToRemove) _functions.Remove(func);
					}
				}
			}
		}

		public FFClass GetClass(string className)
		{
			FFClass cls;
			if (_classes.TryGetValue(className, out cls)) return cls;
			return null;
		}
	}
}
