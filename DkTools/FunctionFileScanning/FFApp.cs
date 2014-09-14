using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.FunctionFileScanning
{
	internal class FFApp
	{
		private FFScanner _scanner;
		private string _name;
		private int _id;
		private Dictionary<string, FFFile> _files = new Dictionary<string, FFFile>();
		private Dictionary<string, FFFunction> _functions = new Dictionary<string, FFFunction>();
		private Dictionary<string, FFClass> _classes = new Dictionary<string, FFClass>();
		private CodeModel.Definitions.Definition[] _definitions = null;
		private object _definitionsLock = new object();

		public FFApp(FFScanner scanner, FFDatabase db, string name)
		{
			if (scanner == null) throw new ArgumentNullException("scanner");

			_scanner = scanner;
			_name = name;

			var conn = db.Connection;
			if (conn == null) return;

			// Load app info
			using (var cmd = db.CreateCommand("select * from app where name = @name"))
			{
				cmd.Parameters.AddWithValue("@name", _name);
				using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
				{
					if (rdr.Read())
					{
						_id = rdr.GetInt32(rdr.GetOrdinal("id"));
					}
				}
			}

			if (_id != 0)
			{
				// Load file dates
				using (var cmd = db.CreateCommand("select * from file_ where app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						var ordFileName = rdr.GetOrdinal("file_name");
						var ordModified = rdr.GetOrdinal("modified");
						while (rdr.Read())
						{
							var fileName = rdr.GetString(ordFileName);
							var modified = rdr.GetDateTime(ordModified);

							GetOrCreateFile(db, fileName).Modified = modified;
						}
					}
				}

				// Load classes
				using (var cmd = db.CreateCommand("select f.file_name, c.* from class_ c inner join file_ f on f.id = c.file_id where c.app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						var ordName = rdr.GetOrdinal("name");
						var ordFileName = rdr.GetOrdinal("file_name");

						while (rdr.Read())
						{
							var file = GetOrCreateFile(db, rdr.GetString(ordFileName));

							var cls = new FFClass(this, file, db, rdr);
							_classes[cls.Name] = cls;
						}
					}
				}

				// Load functions
				using (var cmd = db.CreateCommand("select fl.file_name, fn.* from func fn inner join file_ fl on fl.id = fn.file_id where fn.app_id = @app_id and fn.class_id is null"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						var ordFileName = rdr.GetOrdinal("file_name");

						while (rdr.Read())
						{
							var file = GetOrCreateFile(db, rdr.GetString(ordFileName));
							var func = new FFFunction(this, file, null, rdr);
							_functions[func.Name] = func;
						}
					}
				}
			}
			else // _id == 0
			{
				using (var cmd = db.CreateCommand("insert into app (name) values (@name)"))
				{
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.ExecuteNonQuery();
					_id = db.QueryIdentityInt();
				}
			}
		}

		public void OnDeactivate()
		{
		}

		public string Name
		{
			get { return _name; }
		}

		public void UpdateFunction(FFFile file, string className, FunctionDefinition funcDef, out FFClass classOut, out FFFunction funcOut)
		{
			if (funcDef == null) throw new ArgumentNullException("funcDef");

			if (!string.IsNullOrEmpty(className))
			{
				FFClass cls;
				lock (_classes)
				{
					if (!_classes.TryGetValue(className, out cls))
					{
						cls = new FFClass(this, file, className);
						_classes[className] = cls;
					}
				}

				FFFunction func;
				cls.UpdateFunction(funcDef, file, out func);

				classOut = cls;
				funcOut = func;
			}
			else
			{
				FFFunction func;
				lock (_functions)
				{
					if (!_functions.TryGetValue(funcDef.Name, out func))
					{
						func = new FFFunction(this, file, null, funcDef);
						_functions[funcDef.Name] = func;
					}
				}

				classOut = null;
				funcOut = func;
			}

			lock (_definitionsLock)
			{
				_definitions = null;
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

		public FFFunction GetFunction(string className, string funcName)
		{
			if (string.IsNullOrEmpty(className)) return GetFunction(funcName);

			var cls = TryGetClass(className);
			if (cls == null) return null;

			return cls.TryGetFunction(funcName);
		}

		/// <summary>
		/// Gets a list of definitions that are available at the global scope.
		/// Only public definitions are returned.
		/// </summary>
		public IEnumerable<CodeModel.Definitions.Definition> GlobalDefinitions
		{
			get
			{
				lock (_definitionsLock)
				{
					if (_definitions == null)
					{
						var defList = new List<Definition>();
						foreach (var func in _functions.Values)
						{
							if (func.Definition.Privacy == CodeModel.FunctionPrivacy.Public) defList.Add(func.Definition);
						}

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
			var file = TryGetFile(fileName);
			if (file != null)
			{
				modified = file.Modified;
				return true;
			}

			modified = DateTime.MinValue;
			return false;
		}

		public void MarkAllFunctionsForFileUnused(string fileName)
		{
			string className;
			if (FFUtil.FileNameIsClass(fileName, out className))
			{
				lock (_classes)
				{
					FFClass cls;
					if (_classes.TryGetValue(className, out cls)) cls.MarkAllUnused();
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
							node.Value.Used = false;
						}
					}

					if (funcsToRemove != null)
					{
						foreach (var func in funcsToRemove) _functions.Remove(func);
					}
				}
			}
		}

		public FFClass TryGetClass(string className)
		{
			lock (_classes)
			{
				FFClass cls;
				if (_classes.TryGetValue(className, out cls)) return cls;
				return null;
			}
		}

		public FFFile GetOrCreateFile(FFDatabase db, string fileName)
		{
			lock (_files)
			{
				var fileNameLower = fileName.ToLower();
				FFFile file;
				if (!_files.TryGetValue(fileNameLower, out file))
				{
					file = new FFFile(db, this, fileName);
					_files[fileNameLower] = file;
				}
				return file;
			}
		}

		public FFFile TryGetFile(string fileName)
		{
			lock (_files)
			{
				FFFile file;
				if (_files.TryGetValue(fileName.ToLower(), out file)) return file;
				return null;
			}
		}

		public FFFunction TryGetFunction(string name)
		{
			lock (_functions)
			{
				FFFunction func;
				if (_functions.TryGetValue(name, out func)) return func;
				return null;
			}
		}

		public int Id
		{
			get { return _id; }
		}

		public void PurgeUnused(FFDatabase db)
		{
			// Remove entries in memory that are known to be unused.

			List<FFClass> classesToRemove = new List<FFClass>();
			List<FFFunction> methodsToRemove = new List<FFFunction>();
			lock (_classes)
			{
				foreach (var cls in _classes.Values)
				{
					if (!cls.Used) classesToRemove.Add(cls);
					methodsToRemove.AddRange(cls.UnusedFunctions);
				}
			}
			foreach (var cls in classesToRemove)
			{
				Log.WriteDebug("Removing class from database: {0}", cls.Name);
				cls.Remove(db);
				_classes.Remove(cls.Name);
			}
			foreach (var func in methodsToRemove)
			{
				Log.WriteDebug("Removing method from database: {0}.{1}", func.Class.Name, func.Name);
				func.Class.RemoveFunction(db, func);
			}

			FFFunction[] functionsToRemove;
			lock (_functions)
			{
				functionsToRemove = (from f in _functions.Values where !f.Used select f).ToArray();
			}
			foreach (var func in functionsToRemove)
			{
				Log.WriteDebug("Removing function from database: {0}", func.Name);
				func.Remove(db);
				_functions.Remove(func.Name);
			}

			FFFile[] filesToRemove;
			lock (_files)
			{
				filesToRemove = (from f in _files.Values where !f.Used select f).ToArray();
			}
			foreach (var file in filesToRemove)
			{
				Log.WriteDebug("Removing file from database: {0}", file.FileName);
				file.Remove(db);
				_files.Remove(file.FileName.ToLower());
			}


			// Scan the database to find entries not in memory.

			var removeList = new List<int>();
			using (var cmd = db.CreateCommand("select id, name from class_ where app_id = @app_id"))
			{
				cmd.Parameters.AddWithValue("@app_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordName = rdr.GetOrdinal("name");
					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var name = rdr.GetString(ordName);

						var cls = TryGetClass(name);
						if (cls == null || cls.Id != id) removeList.Add(id);
					}
				}
			}
			foreach (var id in removeList)
			{
				Log.WriteDebug("Remove orphaned class from database: {0}", id);
				db.ExecuteNonQuery(string.Format("delete class_ where id = {0}", id));
			}
			removeList.Clear();


			using (var cmd = db.CreateCommand("select id, name from func where app_id = @app_id and class_id is null"))
			{
				cmd.Parameters.AddWithValue("@app_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordName = rdr.GetOrdinal("name");
					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var name = rdr.GetString(ordName);

						var func = TryGetFunction(name);
						if (func == null || func.Id != id) removeList.Add(id);
					}
				}
			}
			using (var cmd = db.CreateCommand("select func.id, func.name, class_.name as class_name from func inner join class_ on class_.id = func.class_id where func.app_id = @app_id and func.class_id is not null"))
			{
				cmd.Parameters.AddWithValue("@app_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordName = rdr.GetOrdinal("name");
					var ordClassName = rdr.GetOrdinal("class_name");
					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var funcName = rdr.GetString(ordName);
						var className = rdr.GetString(ordClassName);

						var cls = TryGetClass(className);
						if (cls == null) removeList.Add(id);
						else
						{
							var func = cls.TryGetFunction(funcName);
							if (func == null || func.Id != id) removeList.Add(id);
						}
					}
				}
			}
			foreach (var id in removeList)
			{
				Log.WriteDebug("Remove orphaned function from database: {0}", id);
				db.ExecuteNonQuery(string.Format("delete func where id = {0}", id));
			}
			removeList.Clear();


			using (var cmd = db.CreateCommand("select id, file_name from file_ where app_id = @app_id"))
			{
				cmd.Parameters.AddWithValue("@app_id", _id);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordFileName = rdr.GetOrdinal("file_name");
					while (rdr.Read())
					{
						var id = rdr.GetInt32(ordId);
						var fileName = rdr.GetString(ordFileName);

						var file = TryGetFile(fileName);
						if (file == null || file.Id != id) removeList.Add(id);
					}
				}
			}
			foreach (var id in removeList)
			{
				Log.WriteDebug("Remove orphaned file from database: {0}", id);
				db.ExecuteNonQuery(string.Format("delete file_ where id = {0}", id));
			}
		}
	}
}
