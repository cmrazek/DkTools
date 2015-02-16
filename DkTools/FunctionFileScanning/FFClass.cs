using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	internal class FFClass
	{
		private FFApp _app;
		private FFFile _file;
		private string _name;
		private CodeModel.Definitions.ClassDefinition _def;

		private FFClass()
		{ }

		public FFClass(FFApp app, FFFile file, string name)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (file == null) throw new ArgumentNullException("file");
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
#endif

			_app = app;
			_file = file;
			_name = name;
			_def = new CodeModel.Definitions.ClassDefinition(_name, _file.FileName);
		}

		public string Name
		{
			get { return _name; }
		}

		public CodeModel.Definitions.ClassDefinition ClassDefinition
		{
			get { return _def; }
		}

		public IEnumerable<CodeModel.Definitions.FunctionDefinition> FunctionDefinitions
		{
			get
			{
				foreach (var func in _file.Functions)
				{
					yield return func.Definition;
				}
			}
		}

		public IEnumerable<CodeModel.Definitions.FunctionDefinition> GetFunctionDefinitions(string name)
		{
			foreach (var func in _file.GetFunctions(name)) yield return func.Definition;
		}

		public FFFile File
		{
			get { return _file; }
		}

		public static string GetExternalRefId(string className)
		{
			return string.Concat("class:", className);
		}
	}
}
