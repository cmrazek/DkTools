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

		public void InsertOrUpdate(FFDatabase db, CodeModel.FileStore store, CodeModel.CodeModel model)
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

			UpdateIncludeDependencies(db, store, model);
		}

		private void UpdateIncludeDependencies(FFDatabase db, CodeModel.FileStore store, CodeModel.CodeModel model)
		{
			var inclList = model.PreprocessorModel.IncludeDependencies.ToArray();

			// Look for dependencies that are no longer there.
			List<int> removeList = null;
			using (var cmd = db.CreateCommand("select id, include_file_name, include, localized_file from include_depends where file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordFileName = rdr.GetOrdinal("include_file_name");
					var ordInclude = rdr.GetOrdinal("include");
					var ordLocalizedFile = rdr.GetOrdinal("localized_file");

					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var includeFileName = rdr.GetString(ordFileName);
						var include = rdr.GetTinyIntBoolean(ordInclude);
						var localizedFile = rdr.GetTinyIntBoolean(ordLocalizedFile);

						if (!inclList.Any(x => x.FileName.Equals(includeFileName, StringComparison.OrdinalIgnoreCase) && x.Include == include && x.LocalizedFile == localizedFile))
						{
							if (removeList == null) removeList = new List<int>();
							removeList.Add(id);
						}
					}
				}
			}

			// Remove outdated dependencies.
			if (removeList != null && removeList.Count > 0)
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
			foreach (var inclDepend in inclList)
			{
				int numFound;
				using (var cmd = db.CreateCommand("select count(*) from include_depends where file_id = @file_id and include_file_name = @include_file_name and include = @include and localized_file = @localized_file"))
				{
					cmd.Parameters.AddWithValue("@file_id", _id);
					cmd.Parameters.AddWithValue("@include_file_name", inclDepend.FileName);
					cmd.Parameters.AddWithValue("@include", inclDepend.Include);
					cmd.Parameters.AddWithValue("@localized_file", inclDepend.LocalizedFile);
					numFound = Convert.ToInt32(cmd.ExecuteScalar());
				}

				if (numFound == 0)
				{
					using (var cmd = db.CreateCommand(@"insert into include_depends (app_id, file_id, include_file_name, include, localized_file) values (@app_id, @file_id, @include_file_name, @include, @localized_file)"))
					{
						cmd.Parameters.AddWithValue("@app_id", _app.Id);
						cmd.Parameters.AddWithValue("@file_id", _id);
						cmd.Parameters.AddWithValue("@include_file_name", inclDepend.FileName);
						cmd.Parameters.AddWithValue("@include", inclDepend.Include);
						cmd.Parameters.AddWithValue("@localized_file", inclDepend.LocalizedFile);
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

			List<int> dbRefsToRemove = null;

			// Scan the refs in the database to find which ones should be updated or removed
			using (var cmd = db.CreateCommand("select ref.*, alt_file.file_name as true_file_name from ref "
				+ "left outer join alt_file on alt_file.id = ref.true_file_id "
				+ "where file_id = @file_id"))
			{
				cmd.Parameters.AddWithValue("@file_id", _id);

				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordExtRefId = rdr.GetOrdinal("ext_ref_id");
					var ordTrueFileName = rdr.GetOrdinal("true_file_name");
					var ordPos = rdr.GetOrdinal("pos");

					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var extRefId = rdr.GetString(ordExtRefId);
						var trueFileName = rdr.GetStringOrNull(ordTrueFileName);
						var pos = rdr.GetInt32(ordPos);

						var memRef = memRefs.FirstOrDefault(r => r.ExternalRefId == extRefId &&
							(r.TrueFileName == null) == (trueFileName == null) &&
							(r.TrueFileName == null || string.Equals(r.TrueFileName, trueFileName, StringComparison.OrdinalIgnoreCase)) &&
							r.Position == pos);
						if (memRef != null)
						{
							memRef.Exists = true;
						}
						else
						{
							if (dbRefsToRemove == null) dbRefsToRemove = new List<int>();
							dbRefsToRemove.Add(id);
						}
					}
				}
			}

			// Remove refs no longer used
			if (dbRefsToRemove != null)
			{
				foreach (var id in dbRefsToRemove)
				{
					using (var cmd = db.CreateCommand("delete from ref where id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}
				}
			}

			// Insert new refs
			if (memRefs.Any(r => !r.Exists))
			{
				// Get the list of alt file names used.
				var altFileNames = new Dictionary<string, int>();
				foreach (var altFileName in (from r in memRefs where !r.Exists && !string.IsNullOrEmpty(r.TrueFileName) select r.TrueFileName))
				{
					if (!altFileNames.Keys.Any(x => string.Equals(x, altFileName))) altFileNames[altFileName] = 0;
				}

				if (altFileNames.Any())
				{
					// Look up the IDs of any alt file names used.
					using (var cmd = db.CreateCommand("select top 1 id from alt_file where file_name = @file_name"))
					{
						foreach (var altFileName in altFileNames.Keys.ToArray())	// Put into array early to avoid collection modified error
						{
							cmd.Parameters.Clear();
							cmd.Parameters.AddWithValue("@file_name", altFileName);

							using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
							{
								if (rdr.Read())
								{
									var id = rdr.GetInt32OrNull(rdr.GetOrdinal("id"));
									if (id.HasValue) altFileNames[altFileName] = id.Value;
								}
							}
						}
					}

					// Insert new alt file names.
					if (altFileNames.Any(x => x.Value == 0))
					{
						using (var cmd = db.CreateCommand("insert into alt_file (file_name) values (@file_name)"))
						{
							foreach (var altFileName in (from a in altFileNames where a.Value == 0 select a.Key).ToArray())	// Put into array early to avoid collection modified error
							{
								cmd.Parameters.Clear();
								cmd.Parameters.AddWithValue("@file_name", altFileName);
								cmd.ExecuteNonQuery();
								altFileNames[altFileName] = db.QueryIdentityInt();
							}
						}
					}
				}

				// Inserts the ref records
				using (var cmd = db.CreateCommand("insert into ref (app_id, file_id, ext_ref_id, true_file_id, pos) values (@app_id, @file_id, @ext_ref_id, @true_file_id, @pos)"))
				{
					foreach (var newRef in memRefs.Where(r => !r.Exists))
					{
						var trueFileId = 0;
						if (!string.IsNullOrEmpty(newRef.TrueFileName))
						{
							var altFileName = (from a in altFileNames where string.Equals(a.Key, newRef.TrueFileName, StringComparison.OrdinalIgnoreCase) select a.Key).FirstOrDefault();
							if (altFileName != null) trueFileId = altFileNames[altFileName];
						}

						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@app_id", _app.Id);
						cmd.Parameters.AddWithValue("@file_id", _id);
						cmd.Parameters.AddWithValue("@ext_ref_id", newRef.ExternalRefId);
						cmd.Parameters.AddWithValue("@true_file_id", trueFileId);
						cmd.Parameters.AddWithValue("@pos", newRef.Position);
						cmd.ExecuteNonQuery();
					}
				}
			}
		}

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

		public void UpdateFromModel(CodeModel.CodeModel model, FFDatabase db, FileStore store, DateTime fileModified, FFScanMode scanMode)
		{
			if (scanMode == FFScanMode.Deep)
			{
				_modified = fileModified;
			}
			else
			{
				_modified = Constants.ZeroDate;
			}
			InsertOrUpdate(db, store, model);

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

			if (scanMode == FFScanMode.Deep)
			{
				// Get all references in the file.
				var refList = new List<Reference>();
				foreach (var token in model.File.FindDownward(t => t.SourceDefinition != null && !string.IsNullOrEmpty(t.SourceDefinition.ExternalRefId)))
				{
					var localPos = token.File.CodeSource.GetFilePosition(token.Span.Start);

					var def = token.SourceDefinition;
					var refId = def.ExternalRefId;
					if (!string.IsNullOrEmpty(refId))
					{
						refList.Add(new Reference
						{
							ExternalRefId = refId,
							TrueFileName = string.Equals(localPos.FileName, model.FileName, StringComparison.OrdinalIgnoreCase) ? null : localPos.FileName,
							Position = localPos.Position
						});
					}
				}

				foreach (var rf in model.PreprocessorReferences)
				{
					var def = rf.Definition;
					var refId = def.ExternalRefId;
					if (!string.IsNullOrEmpty(refId))
					{
						var filePos = rf.FilePosition;
						if (filePos.PrimaryFile)
						{
							refList.Add(new Reference
							{
								ExternalRefId = refId,
								TrueFileName = filePos.FileName,
								Position = filePos.Position
							});
						}
					}
				}

				UpdateRefList(db, refList);
			}
		}

		private class Reference
		{
			public string ExternalRefId { get; set; }
			public string TrueFileName { get; set; }
			public int Position { get; set; }

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
