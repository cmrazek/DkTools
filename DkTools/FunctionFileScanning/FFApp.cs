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
		private Dictionary<string, DateTime> _invisibleFiles = new Dictionary<string, DateTime>();

		private GroupedList<string, FFFunction> _consolidatedFunctions;
		private GroupedList<string, FFClass> _consolidatedClasses;

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
				// Load files
				using (var cmd = db.CreateCommand("select * from file_ where app_id = @app_id and visible != 0"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							var file = new FFFile(this, db, rdr);
							_files[file.FileName.ToLower()] = file;
						}
					}
				}

				using (var cmd = db.CreateCommand("select file_name, modified from file_ where app_id = @app_id and visible = 0"))
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
							_invisibleFiles[fileName.ToLower()] = modified;
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

				Shell.ShowNotificationAsync(Res.FFDatabaseCreationNotification, Res.FFDatabaseCreationCaption);
			}
		}

		public void OnDeactivate()
		{
		}

		public string Name
		{
			get { return _name; }
		}

		public void OnVisibleFileChanged(FFFile file)
		{
			_consolidatedFunctions = null;
			_consolidatedClasses = null;

			var fileNameLower = file.FileName.ToLower();
			if (!_files.ContainsKey(fileNameLower))
			{
				_files[fileNameLower] = file;
			}
		}

		public void OnInvisibleFileChanged(FFFile file)
		{
			var fileNameLower = file.FileName.ToLower();
			_invisibleFiles[fileNameLower] = file.Modified;
		}

		private void CheckConsolidatedLists()
		{
			if (_consolidatedFunctions == null)
			{
				_consolidatedFunctions = new GroupedList<string, FFFunction>();
				_consolidatedClasses = new GroupedList<string, FFClass>();
				foreach (var file in _files.Values)
				{
					foreach (var func in file.Functions)
					{
						if (!func.Visible) continue;
						_consolidatedFunctions.Add(func.Name, func);
					}

					var cls = file.Class;
					if (cls != null) _consolidatedClasses.Add(cls.Name, cls);
				}
			}
		}

		public GroupedList<string, FFFunction> Functions
		{
			get
			{
				CheckConsolidatedLists();
				return _consolidatedFunctions;
			}
		}

		public GroupedList<string, FFClass> Classes
		{
			get
			{
				CheckConsolidatedLists();
				return _consolidatedClasses;
			}
		}

		public IEnumerable<FFClass> GetClasses(string name)
		{
			CheckConsolidatedLists();

			return _consolidatedClasses[name];
		}

		/// <summary>
		/// Gets a list of definitions that are available at the global scope.
		/// Only public definitions are returned.
		/// </summary>
		public IEnumerable<CodeModel.Definitions.Definition> GlobalDefinitions
		{
			get
			{
				CheckConsolidatedLists();
				foreach (var func in _consolidatedFunctions.Values)
				{
					yield return func.Definition;
				}

				foreach (var cls in _consolidatedClasses.Values)
				{
					yield return cls.ClassDefinition;
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

			if (_invisibleFiles.TryGetValue(fileName.ToLower(), out modified)) return true;

			modified = DateTime.MinValue;
			return false;
		}

		public bool TrySetFileDate(string fileName, DateTime modified)
		{
			var file = TryGetFile(fileName);
			if (file != null)
			{
				file.Modified = modified;
				return true;
			}

			var fileNameLower = fileName.ToLower();
			if (_invisibleFiles.ContainsKey(fileNameLower))
			{
				_invisibleFiles[fileNameLower] = modified;
				return true;
			}

			return false;
		}

		public FFFile GetFileForScan(FFDatabase db, string fileName)
		{
			FFFile file;
			var fileNameLower = fileName.ToLower();

			lock (_files)
			{
				if (_files.TryGetValue(fileNameLower, out file)) return file;
			}

			// Check if this is in the list of invisible files.
			if (_invisibleFiles.ContainsKey(fileNameLower))
			{
				using (var cmd = db.CreateCommand("select * from file_ where app_id = @app_id and file_name = @file_name"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					cmd.Parameters.AddWithValue("@file_name", fileName);
					using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
					{
						if (rdr.Read())
						{
							return new FFFile(this, db, rdr);
						}
					}
				}
			}

			// New file
			file = new FFFile(this, fileName);
			return file;
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

		public int Id
		{
			get { return _id; }
		}

		public void PurgeData(FFDatabase db)
		{
			// Remove files in memory that don't exist on disk.
			{
				var removeFiles = new List<FFFile>();

				foreach (var file in _files.Values)
				{
					if (!File.Exists(file.FileName))
					{
						removeFiles.Add(file);
					}
				}

				foreach (var removeFile in removeFiles)
				{
					_files.Remove(removeFile.FileName.ToLower());
					removeFile.Remove(db);
				}
			}

			// Remove files in the database that aren't in memory.
			{
				var removeFiles = new List<int>();

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

							var fileNameLower = fileName.ToLower();
							if (!_files.ContainsKey(fileNameLower) &&
								!_invisibleFiles.ContainsKey(fileNameLower))
							{
								removeFiles.Add(id);
							}
						}
					}
				}

				foreach (var id in removeFiles)
				{
					using (var cmd = db.CreateCommand("delete from file_ where id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}

					using (var cmd = db.CreateCommand("delete from func where file_id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}

					using (var cmd = db.CreateCommand("delete from ref where file_id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}

					using (var cmd = db.CreateCommand("delete from include_depends where file_id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}
				}
			}

			PurgeNonexistentApps(db);
		}

		private void PurgeNonexistentApps(FFDatabase db)
		{
			var appsToRemove = new Dictionary<int, string>();
			var appNames = ProbeEnvironment.AppNames.ToArray();

			using (var cmd = db.CreateCommand("select id, name from app"))
			{
				using (var rdr = cmd.ExecuteReader())
				{
					var ordId = rdr.GetOrdinal("id");
					var ordName = rdr.GetOrdinal("name");

					while (rdr.Read())
					{
						var name = rdr.GetString(ordName);
						if (!appNames.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)))
						{
							appsToRemove[rdr.GetInt32(ordId)] = name;
						}
					}
				}
			}

			foreach (var appId in appsToRemove.Keys)
			{
				Log.Write(LogLevel.Info, "Removing app {0} from database because it no longer exists in the DK environment.", appsToRemove[appId]);

				using (var cmd = db.CreateCommand("delete from func where app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", appId);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = db.CreateCommand("delete from include_depends where app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", appId);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = db.CreateCommand("delete from ref where app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", appId);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = db.CreateCommand("delete from file_ where app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", appId);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = db.CreateCommand("delete from app where id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", appId);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public FFSearcher CreateSearcher()
		{
			try
			{
				return new FFSearcher(this);
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
				return null;
			}
		}
	}
}
