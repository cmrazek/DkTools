using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;
using DkTools.CodeModel.Tokens.Statements;

namespace DkTools.FunctionFileScanning
{
	internal class FFFile
	{
		private FFApp _app;
		private long _id;
		private string _fileName;
		private DateTime _modified;
		private FileContext _context;
		private List<FFFunction> _functions = new List<FFFunction>();
		private List<FFPermEx> _permExs = new List<FFPermEx>();
		private FFClass _class = null;
		private bool _visible;

		public FFFile(FFApp app, string fileName)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
#endif
			_app = app;
			_id = 0L;	// Will be inserted during next database update
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

		public FFFile(FFApp app, FFDatabase db, SQLiteDataReader fileRdr)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (db == null) throw new ArgumentNullException("db");
			if (fileRdr == null) throw new ArgumentNullException("rdr");
#endif
			_app = app;
			_id = fileRdr.GetInt64(fileRdr.GetOrdinal("rowid"));
			_fileName = fileRdr.GetString(fileRdr.GetOrdinal("file_name"));
			_context = FileContextUtil.GetFileContextFromFileName(_fileName);
			_modified = fileRdr.GetDateTime(fileRdr.GetOrdinal("modified"));

			var className = FileContextUtil.GetClassNameFromFileName(_fileName);
			if (!string.IsNullOrEmpty(className))
			{
				_class = new FFClass(_app, this, className);
			}

			UpdateVisibility();
		}

		public void Load(FFDatabase db)
		{
			using (var cmd = db.CreateCommand(
				"select func.rowid, func.*, alt_file.file_name as alt_file_name from func" +
				" left outer join alt_file on alt_file.rowid = func.alt_file_id" +
				" where file_id = @file_id"))
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

			using (var cmd = db.CreateCommand(
				"select permex.rowid, permex.*, alt_file.file_name as alt_file_name from permex" +
				" left outer join alt_file on alt_file.rowid = permex.alt_file_id" +
				" where permex.file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					while (rdr.Read())
					{
						_permExs.Add(new FFPermEx(this, rdr));
					}
				}
			}

			if (_class != null)
			{
				_class.ClearFunctions();
				foreach (var func in _functions) _class.AddFunction(func);
			}

			foreach (var permex in _permExs) permex.Load(db);
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public DateTime Modified
		{
			get { return _modified; }
			set { _modified = value; }
		}

		public long Id
		{
			get { return _id; }
		}

		public FFApp App
		{
			get { return _app; }
		}

		public FileContext FileContext
		{
			get { return _context; }
		}

		public void InsertOrUpdate(FFDatabase db, CodeModel.FileStore store, CodeModel.CodeModel model)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("update file_ set modified = @modified, visible = @visible where rowid = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.Parameters.AddWithValue("@modified", _modified);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using (var cmd = db.CreateCommand(
					"insert into file_ (app_id, file_name, modified, visible) values (@app_id, @file_name, @modified, @visible);"
					+ " select last_insert_rowid();"))
				{
					cmd.Parameters.AddWithValue("@app_id", _app.Id);
					cmd.Parameters.AddWithValue("@file_name", _fileName);
					cmd.Parameters.AddWithValue("@modified", _modified);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					_id = Convert.ToInt64(cmd.ExecuteScalar());
				}
			}

			UpdateIncludeDependencies(db, store, model);
		}

		private struct DbInclude
		{
			public long id;
			public string fileName;
			public bool include;
			public bool localizedFile;
		}

		private void UpdateIncludeDependencies(FFDatabase db, CodeModel.FileStore store, CodeModel.CodeModel model)
		{
			var modelIncludeList = model.PreprocessorModel.IncludeDependencies.ToArray();

			var dbIncludes = new List<DbInclude>();
			using (var cmd = db.CreateCommand("select rowid, include_file_name, include, localized_file from include_depends where file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					while (rdr.Read())
					{
						dbIncludes.Add(new DbInclude
						{
							id = rdr.GetInt64(0),
							fileName = rdr.GetString(1),
							include = rdr.GetTinyIntBoolean(2),
							localizedFile = rdr.GetTinyIntBoolean(3)
						});
					}
				}
			}

			// Remove dependencies that are no longer there
			var recordsToRemove = (from d in dbIncludes
								   where !modelIncludeList.Any(m => m.FileName.Equals(d.fileName, StringComparison.OrdinalIgnoreCase) &&
									   m.Include == d.include &&
									   m.LocalizedFile == d.localizedFile)
								   select d).ToArray();
			if (recordsToRemove.Length > 0)
			{
				using (var cmd = db.CreateCommand("delete from include_depends where rowid = @rowid"))
				{
					foreach (var id in recordsToRemove)
					{
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@rowid", id);
						cmd.ExecuteNonQuery();
					}
				}
			}

			// Add new dependencies
			var recordsToAdd = (from m in modelIncludeList
								where !dbIncludes.Any(d => d.fileName.Equals(m.FileName, StringComparison.OrdinalIgnoreCase) &&
									d.include == m.Include &&
									d.localizedFile == m.LocalizedFile)
								select m).ToArray();
			if (recordsToAdd.Length > 0)
			{
				using (var cmd = db.CreateCommand(@"
					insert into include_depends (app_id, file_id, include_file_name, include, localized_file)
					values (@app_id, @file_id, @include_file_name, @include, @localized_file)
					"))
				{
					foreach (var incl in recordsToAdd)
					{
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@app_id", _app.Id);
						cmd.Parameters.AddWithValue("@file_id", _id);
						cmd.Parameters.AddWithValue("@include_file_name", incl.FileName);
						cmd.Parameters.AddWithValue("@include", incl.Include ? 1 : 0);
						cmd.Parameters.AddWithValue("@localized_file", incl.LocalizedFile ? 1 : 0);
						cmd.ExecuteNonQuery();
					}
				}
			}
		}

		private void UpdateRefList(FFDatabase db, List<Reference> refList)
		{
			// Initialize the flags on each ref
			var memRefs = new List<Reference>();
			memRefs.AddRange(refList);
			foreach (var r in memRefs)
			{
				r.Exists = false;
			}

			List<long> dbRefsToRemove = null;

			// Scan the refs in the database to find which ones should be updated or removed
			using (var cmd = db.CreateCommand("select ref.rowid, ref.*, alt_file.file_name as true_file_name from ref "
				+ "left outer join alt_file on alt_file.rowid = ref.alt_file_id "
				+ "where file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);

				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("rowid");
					var ordExtRefId = rdr.GetOrdinal("ext_ref_id");
					var ordTrueFileName = rdr.GetOrdinal("true_file_name");
					var ordPos = rdr.GetOrdinal("pos");
					var ordFuncRefId = rdr.GetOrdinal("func_ref_id");
					var ordFuncFileName = rdr.GetOrdinal("func_file_name");
					var ordFuncPos = rdr.GetOrdinal("func_pos");

					while (rdr.Read())
					{
						var id = rdr.GetInt64(ordId);
						var extRefId = rdr.GetString(ordExtRefId);
						var trueFileName = rdr.GetStringOrNull(ordTrueFileName);
						var pos = rdr.GetInt32(ordPos);
						var funcRefId = rdr.GetStringOrNull(ordFuncRefId);
						var funcFileName = rdr.GetStringOrNull(ordFuncFileName);
						var funcPos = rdr.GetInt32OrNull(ordFuncPos);

						var memRef = memRefs.FirstOrDefault(r => r.ExternalRefId == extRefId &&
							(r.TrueFileName == null) == (trueFileName == null) &&
							(r.TrueFileName == null || string.Equals(r.TrueFileName, trueFileName, StringComparison.OrdinalIgnoreCase)) &&
							r.Position == pos &&
							r.ParentFunctionRefId == funcRefId &&
							string.Equals(r.ParentFunctionFileName, funcFileName, StringComparison.OrdinalIgnoreCase) &&
							r.ParentFunctionPosition == funcPos);
						if (memRef != null)
						{
							memRef.Exists = true;
						}
						else
						{
							if (dbRefsToRemove == null) dbRefsToRemove = new List<long>();
							dbRefsToRemove.Add(id);
						}
					}
				}
			}

			// Remove refs no longer used
			if (dbRefsToRemove != null)
			{
				using (var cmd = db.CreateCommand("delete from ref where rowid = @id"))
				{
					foreach (var id in dbRefsToRemove)
					{
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}
				}
			}

			// Insert new refs
			if (memRefs.Any(r => !r.Exists))
			{
				// Get the list of alt file names used.
				var altFileNames = new Dictionary<string, long>();
				foreach (var altFileName in (from r in memRefs where !r.Exists && !string.IsNullOrEmpty(r.TrueFileName) select r.TrueFileName))
				{
					if (!altFileNames.Keys.Any(x => string.Equals(x, altFileName))) altFileNames[altFileName] = 0;
				}

				if (altFileNames.Any())
				{
					// Look up the IDs of any alt file names used.
					using (var cmd = db.CreateCommand("select rowid from alt_file where file_name = @file_name limit 1"))
					{
						foreach (var altFileName in altFileNames.Keys.ToArray())	// Put into array early to avoid collection modified error
						{
							cmd.Parameters.Clear();
							cmd.Parameters.AddWithValue("@file_name", altFileName);

							using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
							{
								if (rdr.Read()) altFileNames[altFileName] = rdr.GetInt64(0);
							}
						}
					}

					// Insert new alt file names.
					if (altFileNames.Any(x => x.Value == 0))
					{
						using (var cmd = db.CreateCommand("insert into alt_file (file_name) values (@file_name); select last_insert_rowid();"))
						{
							foreach (var altFileName in (from a in altFileNames where a.Value == 0 select a.Key).ToArray())	// Put into array early to avoid collection modified error
							{
								cmd.Parameters.Clear();
								cmd.Parameters.AddWithValue("@file_name", altFileName);
								altFileNames[altFileName] = Convert.ToInt64(cmd.ExecuteScalar());
							}
						}
					}
				}

				// Inserts the ref records
				using (var cmd = db.CreateCommand("insert into ref (app_id, file_id, ext_ref_id, alt_file_id, pos, func_ref_id, func_file_name, func_pos)" +
					" values (@app_id, @file_id, @ext_ref_id, @alt_file_id, @pos, @func_ref_id, @func_file_name, @func_pos)"))
				{
					foreach (var newRef in memRefs.Where(r => !r.Exists))
					{
						var trueFileId = 0L;
						if (!string.IsNullOrEmpty(newRef.TrueFileName))
						{
							var altFileName = (from a in altFileNames where string.Equals(a.Key, newRef.TrueFileName, StringComparison.OrdinalIgnoreCase) select a.Key).FirstOrDefault();
							if (altFileName != null) trueFileId = altFileNames[altFileName];
						}

						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@app_id", _app.Id);
						cmd.Parameters.AddWithValue("@file_id", _id);
						cmd.Parameters.AddWithValue("@ext_ref_id", newRef.ExternalRefId);
						cmd.Parameters.AddWithValue("@alt_file_id", trueFileId);
						cmd.Parameters.AddWithValue("@pos", newRef.Position);
						cmd.Parameters.AddWithValue("@func_ref_id", newRef.ParentFunctionRefId);
						cmd.Parameters.AddWithValue("@func_file_name", newRef.ParentFunctionFileName);
						cmd.Parameters.AddWithValue("@func_pos", newRef.ParentFunctionPosition);
						cmd.ExecuteNonQuery();
					}
				}
			}
		}

		public void Remove(FFDatabase db)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("delete from file_ where rowid = @id"))
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

		public void UpdateFromModel(CodeModel.CodeModel model, FFDatabase db, FileStore store, DateTime fileModified, FFScanMode scanMode)
		{
			if (scanMode == FFScanMode.Deep) _modified = fileModified;
			else _modified = Constants.ZeroDate;
			UpdateVisibility();
			InsertOrUpdate(db, store, model);

			// Only extract functions for .f files or class files.
			switch (_context)
			{
				case CodeModel.FileContext.Function:
				case CodeModel.FileContext.ClientClass:
				case CodeModel.FileContext.ServerClass:
				case CodeModel.FileContext.NeutralClass:

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

					// Update the class
					if (_class != null)
					{
						_class.ClearFunctions();
						foreach (var func in _functions) _class.AddFunction(func);
					}

					break;
			}

			// Get all permanent extracts in the file
			UpdatePermExList(db, model.File.FindDownward<CodeModel.Tokens.Statements.ExtractStatement>().Where(x => x.IsPermanent).ToArray());

			if (scanMode == FFScanMode.Deep)
			{
				var funcDefs = model.PreprocessorModel.LocalFunctions.Where(x => !x.Definition.EntireSpan.IsEmpty).Select(x => x.Definition).ToArray();

				// Get all references in the file.
				var refList = new List<Reference>();
				foreach (var token in model.File.FindDownward(t => t.SourceDefinition != null &&
					!string.IsNullOrEmpty(t.SourceDefinition.ExternalRefId) &&
					t.File != null))
				{
					var localPos = token.File.CodeSource.GetFilePosition(token.Span.Start);

					var parentFuncDef = funcDefs.Where(x => x.RawBodySpan.Contains(token.Span.Start)).FirstOrDefault();

					var def = token.SourceDefinition;
					var refId = def.ExternalRefId;
					if (!string.IsNullOrEmpty(refId))
					{
						refList.Add(new Reference(
							rawPosition: token.Span.Start,
							externalRefId: refId,
							trueFileName: string.Equals(localPos.FileName, model.FileName, StringComparison.OrdinalIgnoreCase) ? null : localPos.FileName,
							position: localPos.Position,
							parentFunctionRefId: parentFuncDef?.ExternalRefId,
							parentFunctionFileName: parentFuncDef?.FilePosition.FileName,
							parentFunctionPosition: parentFuncDef?.FilePosition.Position ?? 0));
					}
				}

				foreach (var rf in model.PreprocessorReferences)
				{
					var parentFuncDef = funcDefs.Where(x => x.RawBodySpan.Contains(rf.RawPosition)).FirstOrDefault();

					var def = rf.Definition;
					var refId = def.ExternalRefId;
					if (!string.IsNullOrEmpty(refId))
					{
						var filePos = rf.FilePosition;
						if (filePos.PrimaryFile)
						{
							refList.Add(new Reference(
								rawPosition: rf.RawPosition,
								externalRefId: refId,
								trueFileName: filePos.FileName,
								position: filePos.Position,
								parentFunctionRefId: parentFuncDef?.ExternalRefId,
								parentFunctionFileName: parentFuncDef?.FilePosition.FileName,
								parentFunctionPosition: parentFuncDef?.FilePosition.Position ?? 0));
						}
					}
				}

				UpdateRefList(db, refList);
			}
		}

		private void UpdatePermExList(FFDatabase db, IEnumerable<CodeModel.Tokens.Statements.ExtractStatement> exList)
		{
			var keptPermExs = new List<FFPermEx>();

			foreach (var extract in exList)
			{
				FFPermEx permEx = _permExs.FirstOrDefault(p => p.Name == extract.Name);
				if (permEx != null)
				{
					permEx.UpdateFromToken(extract);
				}
				else
				{
					var token = extract.FindFirstChild<ExtractTableToken>();
					if (token == null) continue;

					var def = token.SourceDefinition as ExtractTableDefinition;
					if (def == null) continue;

					permEx = new FFPermEx(this, extract, def);
					_permExs.Add(permEx);
				}

				permEx.SyncToDatabase(db);
				keptPermExs.Add(permEx);
			}

			// Remove deleted extracts
			var exsToDelete = _permExs.Where(p => !keptPermExs.Any(k => k.Name == p.Name)).ToArray();
			if (exsToDelete.Length > 0)
			{
				using (var cmd = db.CreateCommand("delete from permex_col where permex_id = @permex_id"))
				{
					foreach (var permex in exsToDelete)
					{
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@permex_id", permex.Id);
						cmd.ExecuteNonQuery();
					}
				}

				using (var cmd = db.CreateCommand("delete from permex where rowid = @id"))
				{
					foreach (var permex in exsToDelete)
					{
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@id", permex.Id);
						cmd.ExecuteNonQuery();
					}
				}
			}
		}

		private class Reference
		{
			public Reference(int rawPosition, string externalRefId, string trueFileName, int position,
				string parentFunctionRefId, string parentFunctionFileName, int parentFunctionPosition)
			{
				RawPosition = rawPosition;
				ExternalRefId = externalRefId;
				TrueFileName = trueFileName;
				Position = position;
				ParentFunctionRefId = parentFunctionRefId;
				ParentFunctionFileName = parentFunctionFileName;
				ParentFunctionPosition = parentFunctionPosition;
			}

			public int RawPosition { get; private set; }
			public string ExternalRefId { get; private set; }
			public string TrueFileName { get; private set; }
			public int Position { get; private set; }
			public string ParentFunctionRefId { get; private set; }
			public string ParentFunctionFileName { get; private set; }
			public int ParentFunctionPosition { get; private set; }

			// Flags used for database management
			public bool Exists { get; set; }
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
			switch (_context)
			{
				case CodeModel.FileContext.Function:
				case CodeModel.FileContext.ClientClass:
				case CodeModel.FileContext.NeutralClass:
				case CodeModel.FileContext.ServerClass:
				case CodeModel.FileContext.ServerProgram:
					_visible = true;
					break;
				default:
					_visible = false;
					break;
			}
		}

		/// <summary>
		/// Indicates whether this file's export functions are visible to other files.
		/// </summary>
		public bool Visible
		{
			get { return _visible; }
		}

		public IEnumerable<FFPermEx> PermExs
		{
			get { return _permExs; }
		}
	}
}
