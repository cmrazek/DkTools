using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	internal class FFClass
	{
		private string _name;
		private string _fileName;
		private Dictionary<string, FFFunction> _funcs = new Dictionary<string, FFFunction>();
		private CodeModel.Definitions.ClassDefinition _def;

		private FFClass()
		{ }

		public FFClass(string name, string fileName)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
			if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
			_name = name;
			_fileName = fileName;
			_def = new CodeModel.Definitions.ClassDefinition(new CodeModel.Scope(), _name, _fileName);
		}

		public static FFClass FromDatabase(FunctionFileDatabase.Class_t cls)
		{
			if (string.IsNullOrEmpty(cls.name) || string.IsNullOrEmpty(cls.fileName))
			{
				return null;
			}

			var ret = new FFClass
			{
				_name = cls.name,
				_fileName = cls.fileName,
				_def = new CodeModel.Definitions.ClassDefinition(new CodeModel.Scope(), cls.name, cls.fileName)
			};

			if (cls.function != null)
			{
				foreach (var func in cls.function) ret.AddFunction(func);
			}

			return ret;
		}

		public FunctionFileDatabase.Class_t ToDatabase()
		{
			return new FunctionFileDatabase.Class_t
			{
				name = _name,
				fileName = _fileName,
				function = (from f in _funcs.Values select f.ToDatabase()).ToArray()
			};
		}

		public string Name
		{
			get { return _name; }
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public void AddFunction(FunctionFileDatabase.Function_t func)
		{
			var ffFunc = FFFunction.FromDatabase(func);
			if (ffFunc != null) _funcs[ffFunc.Name] = ffFunc;
		}

		public void AddFunction(FFFunction func)
		{
			_funcs[func.Name] = func;
		}

		public CodeModel.Definitions.ClassDefinition ClassDefinition
		{
			get { return _def; }
		}

		public IEnumerable<CodeModel.Definitions.FunctionDefinition> FunctionDefinitions
		{
			get
			{
				foreach (var func in _funcs.Values) yield return func.Definition;
			}
		}

		public FFFunction GetFunction(string name)
		{
			FFFunction func;
			if (_funcs.TryGetValue(name, out func)) return func;
			return null;
		}

		public CodeModel.Definitions.FunctionDefinition GetFunctionDefinition(string name)
		{
			FFFunction func;
			if (_funcs.TryGetValue(name, out func)) return func.Definition;
			return null;
		}

		public bool IsFunction(string funcName)
		{
			return _funcs.ContainsKey(funcName);
		}
	}
}
