using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	class DefinitionStore
	{
		public static DefinitionStore Current { get; private set; }

		private string _appName;
		private GroupedList<string, FunctionDefinition> _functions = new GroupedList<string, FunctionDefinition>();
		private GroupedList<string, ClassDefinition> _classes = new GroupedList<string, ClassDefinition>();
		private GroupedList<string, ExtractTableDefinition> _permExtracts = new GroupedList<string, ExtractTableDefinition>();

		public static event EventHandler DefinitionStoreChanged;

		public DefinitionStore(string appName)
		{
			if (string.IsNullOrEmpty(appName)) throw new ArgumentNullException(nameof(appName));
			_appName = appName;
		}

		public string AppName => _appName;

		public static void Publish(DefinitionStore ds)
		{
			if (ds == null) throw new ArgumentNullException(nameof(ds));
			Current = ds;
			DefinitionStoreChanged?.Invoke(null, EventArgs.Empty);
		}

		public IEnumerable<Definition> GlobalDefinitions
		{
			get
			{
				foreach (var func in _functions.Values)
				{
					yield return func;
				}

				foreach (var cls in _classes.Values)
				{
					yield return cls;
				}

				foreach (var permex in _permExtracts.Values)
				{
					yield return permex;
				}
			}
		}

		public IEnumerable<ClassDefinition> GetClasses(string name)
		{
			return _classes[name.ToLower()];
		}

		public IEnumerable<ExtractTableDefinition> GetPermanentExtracts(string name)
		{
			return _permExtracts[name.ToLower()];
		}

		public void AddFunction(FunctionDefinition func)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));
			_functions.Add(func.Name, func);
		}

		public void AddClass(ClassDefinition cls)
		{
			if (cls == null) throw new ArgumentNullException(nameof(cls));
			_classes.Add(cls.Name.ToLower(), cls);
		}

		public void AddPermanentExtract(ExtractTableDefinition ext)
		{
			if (ext == null) throw new ArgumentNullException(nameof(ext));
			_permExtracts.Add(ext.Name.ToLower(), ext);
		}

		public IEnumerable<FunctionDefinition> SearchForFunctionDefinitions(string funcName)
		{
			using (var db = new FunctionFileScanning.FFDatabase())
			{
				var appId = db.ExecuteScalar<long>("select rowid from app where name = @app_name collate nocase", "@app_name", _appName);

				using (var cmd = db.CreateCommand(@"
					select file_.file_name, func.*, alt_file.file_name as alt_file_name from func
					inner join file_ on file_.rowid = func.file_id
					left outer join alt_file on alt_file.rowid = func.alt_file_id
					where func.app_id = @app_id
					and func.name = @func_name",
					"@app_id", appId, "@func_name", funcName))
				{
					using (var rdr = cmd.ExecuteReader())
					{
						var ordFileName = rdr.GetOrdinal("file_name");

						while (rdr.Read())
						{
							yield return FunctionFileScanning.FFFunction.CreateFunctionDefinitionFromSqlReader(rdr, rdr.GetString(ordFileName));
						}
					}
				}
			}
		}

		public IEnumerable<string> GetIncludeParentFiles(string includePathName, int limit)
		{
			var files = new List<string>();

			using (var db = new FunctionFileScanning.FFDatabase())
			{
				var appId = db.ExecuteScalar<long>("select rowid from app where name = @app_name collate nocase", "@app_name", _appName);

				using (var cmd = db.CreateCommand(@"
					select distinct f.file_name from include_depends i
					inner join file_ f on f.rowid = i.file_id
					where i.include_file_name = @file_name
					and i.app_id = @app_id
					limit @limit",
					"@app_id", appId, "@file_name", includePathName, "@limit", limit))
				{
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read()) files.Add(rdr.GetString(0));
					}
				}
			}

			return files;
		}
	}
}
