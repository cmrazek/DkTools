using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.FunctionFileScanning
{
	internal class FFFile
	{
		private FFApp _app;
		private int _id;
		private string _fileName;
		private DateTime _modified;
		//private bool _used = true;
		private FileContext _context;
		private List<FFFunction> _functions = new List<FFFunction>();
		private FFClass _class = null;
		private bool _visible;

		public FFFile(FFApp app, string fileName)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
#endif
			_app = app;
			_id = 0;	// Will be inserted during next database update
			_fileName = fileName;
			_modified = Constants.ZeroDate;
			_context = FileContextUtil.GetFileContextFromFileName(_fileName);

			var className = FileContextUtil.GetClassNameFromFileName(_fileName);
			if (!string.IsNullOrEmpty(className))
			{
				_class = new FFClass(_app, this, className);
			}

			UpdateVisibility();
		}

		public FFFile(FFApp app, FFDatabase db, SqlCeDataReader fileRdr)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (db == null) throw new ArgumentNullException("db");
			if (fileRdr == null) throw new ArgumentNullException("rdr");
#endif
			_app = app;
			_id = fileRdr.GetInt32(fileRdr.GetOrdinal("id"));
			_fileName = fileRdr.GetString(fileRdr.GetOrdinal("file_name"));
			_context = FileContextUtil.GetFileContextFromFileName(_fileName);
			_modified = fileRdr.GetDateTime(fileRdr.GetOrdinal("modified"));

			var className = FileContextUtil.GetClassNameFromFileName(_fileName);
			if (!string.IsNullOrEmpty(className))
			{
				_class = new FFClass(_app, this, className);
			}

			using (var cmd = db.CreateCommand("select * from func where file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);
				using (var funcRdr = cmd.ExecuteReader())
				{
					while (funcRdr.Read())
					{
						_functions.Add(new FFFunction(_app, this, _class, funcRdr));
					}
				}
			}

			UpdateVisibility();
		}

		// TODO: remove
		//public FFFile(FFDatabase db, FFApp app, string fileName)
		//{
		//	if (app == null) throw new ArgumentNullException("app");
		//	_app = app;

		//	if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
		//	_fileName = fileName;
		//	_context = FileContextUtil.GetFileContextFromFileName(fileName);

		//	using (var cmd = db.CreateCommand("select * from file_ where file_name = @file_name and app_id = @app_id"))
		//	{
		//		cmd.Parameters.AddWithValue("@file_name", fileName);
		//		cmd.Parameters.AddWithValue("@app_id", _app.Id);
		//		using (var rdr = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow))
		//		{
		//			if (rdr.Read())
		//			{
		//				_id = rdr.GetInt32(rdr.GetOrdinal("id"));
		//				_modified = rdr.GetDateTime(rdr.GetOrdinal("modified"));
		//			}
		//		}
		//	}
		//}

		public string FileName
		{
			get { return _fileName; }
		}

		public DateTime Modified
		{
			get { return _modified; }
			set { _modified = value; }
		}

		public int Id
		{
			get { return _id; }
		}

		public FileContext FileContext
		{
			get { return _context; }
		}

		public void InsertOrUpdate(FFDatabase db, CodeModel.FileStore store)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("update file_ set modified = @modified, visible = @visible where id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.Parameters.AddWithValue("@modified", _modified);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using (var cmd = db.CreateCommand("insert into file_ (app_id, file_name, modified, visible) values (@app_id, @file_name, @modified, @visible)"))
				{
					cmd.Parameters.AddWithValue("@app_id", _app.Id);
					cmd.Parameters.AddWithValue("@file_name", _fileName);
					cmd.Parameters.AddWithValue("@modified", _modified);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					cmd.ExecuteNonQuery();
					_id = db.QueryIdentityInt();
				}
			}

			UpdateIncludeDependencies(db, store);
		}

		private void UpdateIncludeDependencies(FFDatabase db, CodeModel.FileStore store)
		{
			var inclList = store.IncludeDependencies.ToList();

			// Look for dependencies that are no longer there.
			var removeList = new List<int>();
			using (var cmd = db.CreateCommand("select id, include_file_name from include_depends where file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordFileName = rdr.GetOrdinal("include_file_name");
					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var includeFileName = rdr.GetString(ordFileName);

						if (!inclList.Any(x => x.Equals(includeFileName, StringComparison.OrdinalIgnoreCase)))
						{
							removeList.Add(id);
						}
					}
				}
			}

			// Remove outdated dependencies.
			if (removeList.Count > 0)
			{
				var sb = new StringBuilder();
				sb.Append("delete from include_depends where id in (");
				var first = true;
				for (int i = 0, ii = removeList.Count; i < ii; i++)
				{
					if (first) first = false;
					else sb.Append(',');
					sb.AppendFormat("@id{0}", i);
				}
				sb.Append(')');

				using (var cmd = db.CreateCommand(sb.ToString()))
				{
					for (int i = 0, ii = removeList.Count; i < ii; i++)
					{
						cmd.Parameters.AddWithValue(string.Format("@id{0}", i), removeList[i]);
					}
					cmd.ExecuteNonQuery();
				}
			}

			// Add new dependencies.
			foreach (var inclFileName in inclList)
			{
				int numFound;
				using (var cmd = db.CreateCommand("select count(*) from include_depends where file_id = @file_id and include_file_name = @include_file_name"))
				{
					cmd.Parameters.AddWithValue("@file_id", _id);
					cmd.Parameters.AddWithValue("@include_file_name", inclFileName);
					numFound = Convert.ToInt32(cmd.ExecuteScalar());
				}

				if (numFound == 0)
				{
					using (var cmd = db.CreateCommand(@"insert into include_depends (app_id, file_id, include_file_name) values (@app_id, @file_id, @include_file_name)"))
					{
						cmd.Parameters.AddWithValue("@app_id", _app.Id);
						cmd.Parameters.AddWithValue("@file_id", _id);
						cmd.Parameters.AddWithValue("@include_file_name", inclFileName);
						cmd.ExecuteNonQuery();
					}
				}
			}
		}

		//public bool Used
		//{
		//	get { return _used; }
		//	set { _used = value; }
		//}

		//public void MarkUsed()
		//{
		//	_used = true;
		//}

		public void Remove(FFDatabase db)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("delete from file_ where id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = db.CreateCommand("delete from func where file_id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public void UpdateFromModel(CodeModel.CodeModel model, FFDatabase db, FileStore store, DateTime fileModified)
		{
			_modified = fileModified;
			InsertOrUpdate(db, store);

			// Get the list of functions defined in the file.
			var modelFuncs = (from f in model.DefinitionProvider.GetGlobalFromFile<CodeModel.Definitions.FunctionDefinition>()
							  where f.Extern == false
							  select f).ToArray();

			// Insert/update the functions that exist in the model.
			foreach (var modelFunc in modelFuncs)
			{
				var func = _functions.FirstOrDefault(f => f.Name == modelFunc.Name);
				if (func != null)
				{
					func.UpdateFromDefinition(modelFunc);
				}
				else
				{
					func = new FFFunction(_app, this, _class, modelFunc);
					_functions.Add(func);
				}

				func.InsertOrUpdate(db);
			}

			// Purge functions that no longer exist in the model.
			var removeFuncs = (from f in _functions where !modelFuncs.Any(m => m.Name == f.Name) select f).ToArray();
			foreach (var removeFunc in removeFuncs)
			{
				removeFunc.Remove(db);
				_functions.Remove(removeFunc);
			}

			// Get all definitions defined in the file.
			var refList = new List<Reference>();
			foreach (var token in model.File.FindDownward(t => t.SourceDefinition != null))
			{
				//model.PreprocessorModel.Source.GetFilePosition(token.Span.Start);

				var def = token.SourceDefinition;
				var refId = def.ExternalRefId;
				if (!string.IsNullOrEmpty(refId))
				{
					refList.Add(new Reference { ExternalRefId = refId, Position = token.Span.Start });
				}
			}
		}

		private class Reference
		{
			public string ExternalRefId { get; set; }
			public string TrueFileName { get; set; }
			public int Position { get; set; }
		}

		public IEnumerable<FFFunction> Functions
		{
			get { return _functions; }
		}

		public FFClass Class
		{
			get { return _class; }
		}

		public IEnumerable<FFFunction> GetFunctions(string name)
		{
			foreach (var func in _functions)
			{
				if (func.Name == name) yield return func;
			}
		}

		private void UpdateVisibility()
		{
			if (_context == CodeModel.FileContext.Function || _context.IsClass())
			{
				_visible = true;
			}
			else
			{
				_visible = false;
			}
		}

		public bool Visible
		{
			get { return _visible; }
		}
	}
}
