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
		//private int _id;
		private string _name;
		//private GroupedList<string, FFFunction> _funcs = new GroupedList<string, FFFunction>();		TODO: remove
		private CodeModel.Definitions.ClassDefinition _def;
		//private bool _used = true;

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

		// TODO: remove
		//public FFClass(FFApp app, FFFile file, FFDatabase db, SqlCeDataReader classRdr)
		//{
		//	if (app == null) throw new ArgumentNullException("app");
		//	if (file == null) throw new ArgumentNullException("file");

		//	_app = app;
		//	_file = file;

		//	_id = classRdr.GetInt32(classRdr.GetOrdinal("id"));
		//	_name = classRdr.GetString(classRdr.GetOrdinal("name"));
		//	_def = new CodeModel.Definitions.ClassDefinition(_name, _file.FileName);

		//	using (var cmd = db.CreateCommand("select * from func where class_id = @class_id"))
		//	{
		//		cmd.Parameters.AddWithValue("@class_id", _id);
		//		using (var funcRdr = cmd.ExecuteReader())
		//		{
		//			while (funcRdr.Read())
		//			{
		//				var func = new FFFunction(_app, file, this, funcRdr);
		//				_funcs[func.Name] = func;
		//			}
		//		}
		//	}
		//}

		public string Name
		{
			get { return _name; }
		}

		// TODO: remove
		//public int Id
		//{
		//	get { return _id; }
		//}

		// TODO: remove
		//public void AddFunction(FFFunction func)
		//{
		//	_funcs[func.Name] = func;
		//}

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

		// TODO: remove
		//public IEnumerable<FFFunction> GetMethods(string name)
		//{
		//	return _funcs[name];
		//}

		public IEnumerable<CodeModel.Definitions.FunctionDefinition> GetFunctionDefinitions(string name)
		{
			foreach (var func in _file.GetFunctions(name)) yield return func.Definition;
		}

		// TODO: remove
		//public bool IsFunction(string funcName)
		//{
		//	return _funcs.ContainsKey(funcName);
		//}

		//public void UpdateFunction(CodeModel.Definitions.FunctionDefinition funcDef, FFFile file, out FFFunction funcOut)
		//{
		//	FFFunction func;
		//	if (_funcs.TryGetValue(funcDef.Name, out func))
		//	{
		//		func.UpdateFromDefinition(funcDef);
		//	}
		//	else
		//	{
		//		func = new FFFunction(_app, file, this, funcDef);
		//		_funcs[funcDef.Name] = func;
		//	}
		//	funcOut = func;
		//}

		//public void InsertOrUpdate(FFDatabase db)
		//{
		//	if (_id != 0)
		//	{
		//		using (var cmd = db.CreateCommand("update class_ set file_id = @file_id where id = @id"))
		//		{
		//			cmd.Parameters.AddWithValue("@file_id", _file.Id);
		//			cmd.Parameters.AddWithValue("@id", _id);
		//			cmd.ExecuteNonQuery();
		//		}
		//	}
		//	else
		//	{
		//		using (var cmd = db.CreateCommand("insert into class_ (name, app_id, file_id) values (@name, @app_id, @file_id)"))
		//		{
		//			cmd.Parameters.AddWithValue("@name", _name);
		//			cmd.Parameters.AddWithValue("@app_id", _app.Id);
		//			cmd.Parameters.AddWithValue("@file_id", _file.Id);
		//			cmd.ExecuteNonQuery();
		//			_id = db.QueryIdentityInt();
		//		}
		//	}
		//}

		//public bool Used
		//{
		//	get { return _used; }
		//	set { _used = value; }
		//}

		//public void MarkAllUnused()
		//{
		//	_used = false;

		//	foreach (var func in _funcs.Values)
		//	{
		//		func.Used = false;
		//	}
		//}

		//public void MarkUsed()
		//{
		//	_used = true;
		//	_file.Used = true;
		//}

		//public IEnumerable<FFFunction> UnusedFunctions
		//{
		//	get
		//	{
		//		foreach (var func in _funcs.Values)
		//		{
		//			if (!func.Used) yield return func;
		//		}
		//	}
		//}

		//public void Remove(FFDatabase db)
		//{
		//	if (_id != 0)
		//	{
		//		using (var cmd = db.CreateCommand("delete from func where class_id = @id"))
		//		{
		//			cmd.Parameters.AddWithValue("@id", _id);
		//			cmd.ExecuteNonQuery();
		//		}

		//		using (var cmd = db.CreateCommand("delete from class_ where id = @id"))
		//		{
		//			cmd.Parameters.AddWithValue("@id", _id);
		//			cmd.ExecuteNonQuery();
		//		}
		//	}
		//}

		//public void RemoveFunction(FFDatabase db, FFFunction func)
		//{
		//	func.Remove(db);
		//	_funcs.Remove(func.Name);
		//}

		public FFFile File
		{
			get { return _file; }
		}
	}
}
