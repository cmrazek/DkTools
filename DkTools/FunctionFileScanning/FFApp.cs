﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.FunctionFileScanning
{
	internal class FFApp
	{
		private FFScanner _scanner;
		private ProbeAppSettings _appSettings;
		private long _id;
		private Dictionary<string, FFFile> _files = new Dictionary<string, FFFile>();
		private Dictionary<string, DateTime> _invisibleFiles = new Dictionary<string, DateTime>();

		private GroupedList<string, FFFunction> _consolidatedFunctions;
		private GroupedList<string, FFClass> _consolidatedClasses;
		private GroupedList<string, FFPermEx> _consolidatedPermExs;

		public FFApp(FFScanner scanner, FFDatabase db, ProbeAppSettings appSettings)
		{
			_scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

			if (!_appSettings.Initialized) return;

			var conn = db.Connection;
			if (conn == null) return;

			// Load app info
			using (var cmd = db.CreateCommand("select rowid from app where name = @name"))
			{
				cmd.Parameters.AddWithValue("@name", _appSettings.AppName);
				using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
				{
					if (rdr.Read())
					{
						_id = rdr.GetInt64(0);
					}
				}
			}

			if (_id != 0)
			{
				// Load files
				var loadedFiles = new List<FFFile>();
				using (var cmd = db.CreateCommand("select rowid, * from file_ where app_id = @app_id and visible != 0"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							loadedFiles.Add(new FFFile(this, db, rdr));
						}
					}
				}

				foreach (var file in loadedFiles)
				{
					file.Load(db);
					_files[file.FileName.ToLower()] = file;
				}

				using (var cmd = db.CreateCommand("select file_name, modified from file_ where app_id = @app_id and visible = 0"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							var fileName = rdr.GetString(0);
							var modified = rdr.GetDateTime(1);
							_invisibleFiles[fileName.ToLower()] = modified;
						}
					}
				}
			}
			else // _id == 0
			{
				using (var cmd = db.CreateCommand("insert into app (name) values (@name); select last_insert_rowid();"))
				{
					cmd.Parameters.AddWithValue("@name", _appSettings.AppName);
					_id = Convert.ToInt64(cmd.ExecuteScalar());
				}

				var options = ProbeToolsPackage.Instance.EditorOptions;
				if (!options.DisableBackgroundScan)
				{
					Shell.ShowNotificationAsync(Res.FFDatabaseCreationNotification, Res.FFDatabaseCreationCaption);
				}
			}
		}

		public void OnDeactivate()
		{
		}

		public string Name
		{
			get { return _appSettings.AppName; }
		}

		public void OnVisibleFileChanged(FFFile file)
		{
			_consolidatedFunctions = null;
			_consolidatedClasses = null;
			_consolidatedPermExs = null;

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
				_consolidatedPermExs = new GroupedList<string, FFPermEx>();

				foreach (var file in _files.Values)
				{
					foreach (var func in file.Functions)
					{
						if (!func.Visible) continue;
						_consolidatedFunctions.Add(func.Name, func);
					}

					var cls = file.Class;
					if (cls != null) _consolidatedClasses.Add(cls.Name, cls);

					foreach (var permex in file.PermExs)
					{
						_consolidatedPermExs.Add(permex.Name, permex);
					}
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

			return _consolidatedClasses[name.ToLower()];
		}

		public IEnumerable<FFPermEx> GetPermExs(string name)
		{
			CheckConsolidatedLists();

			return _consolidatedPermExs[name];
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

				foreach (var permex in _consolidatedPermExs.Values)
				{
					yield return permex.Definition;
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
				using (var cmd = db.CreateCommand("select rowid, * from file_ where app_id = @app_id and file_name = @file_name"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					cmd.Parameters.AddWithValue("@file_name", fileName);
					using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
					{
						if (rdr.Read()) file = new FFFile(this, db, rdr);
					}
				}
			}

			if (file != null)
			{
				// Was loaded as invisible file
				file.Load(db);
				return file;
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

		public long Id
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
				var removeFiles = new List<long>();

				using (var cmd = db.CreateCommand("select rowid, file_name from file_ where app_id = @app_id"))
				{
					cmd.Parameters.AddWithValue("@app_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							var id = rdr.GetInt64(0);
							var fileNameLower = rdr.GetString(1).ToLower();

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
					using (var cmd = db.CreateCommand("delete from file_ where rowid = @id"))
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

					using (var cmd = db.CreateCommand("delete from permex_col where file_id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}

					using (var cmd = db.CreateCommand("delete from permex where file_id = @id"))
					{
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}
				}
			}

			// Purge alt_file records that are no longer used

			List<long> altFilesToRemove = null;

			using (var cmd = db.CreateCommand("select rowid from alt_file" +
				" where not exists (select * from func where alt_file_id = alt_file.rowid)" +
				" and not exists (select * from ref where alt_file_id = alt_file.rowid)" +
				" and not exists (select * from permex where alt_file_id = alt_file.rowid)" +
				" and not exists (select * from permex_col where alt_file_id = alt_file.rowid)"))
			{
				using (var rdr = cmd.ExecuteReader())
				{
					while (rdr.Read())
					{
						if (altFilesToRemove == null) altFilesToRemove = new List<long>();
						altFilesToRemove.Add(rdr.GetInt64(0));
					}
				}
			}

			if (altFilesToRemove != null)
			{
				using (var cmd = db.CreateCommand("delete from alt_file where rowid = @id"))
				{
					foreach (var id in altFilesToRemove)
					{
						cmd.Parameters.Clear();
						cmd.Parameters.AddWithValue("@id", id);
						cmd.ExecuteNonQuery();
					}
				}
			}

			PurgeNonexistentApps(db);
		}

		private void PurgeNonexistentApps(FFDatabase db)
		{
			var appsToRemove = new Dictionary<long, string>();
			var appNames = _appSettings.AllAppNames.ToArray();

			using (var cmd = db.CreateCommand("select rowid, name from app"))
			{
				using (var rdr = cmd.ExecuteReader())
				{
					while (rdr.Read())
					{
						var name = rdr.GetString(1);
						if (!appNames.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)))
						{
							appsToRemove[rdr.GetInt64(0)] = name;
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

				using (var cmd = db.CreateCommand("delete from app where rowid = @app_id"))
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

		public long GetOrCreateAltFileId(FFDatabase db, string fileName)
		{
			using (var cmd = db.CreateCommand("select rowid from alt_file where file_name = @file_name limit 1"))
			{
				cmd.Parameters.AddWithValue("@file_name", fileName);
				using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
				{
					if (rdr.Read())
					{
						return rdr.GetInt64(0);
					}
				}
			}

			using (var cmd = db.CreateCommand("insert into alt_file (file_name) values (@file_name); select last_insert_rowid();"))
			{
				cmd.Parameters.AddWithValue("@file_name", fileName);
				return Convert.ToInt64(cmd.ExecuteScalar());
			}
		}

		public long GetAltFileId(FFDatabase db, string fileName)
		{
			using (var cmd = db.CreateCommand("select rowid from alt_file where file_name = @file_name limit 1"))
			{
				cmd.Parameters.AddWithValue("@file_name", fileName);
				using (var rdr = cmd.ExecuteReader(CommandBehavior.SingleRow))
				{
					if (rdr.Read())
					{
						return rdr.GetInt64(0);
					}
				}
			}

			return 0L;
		}
	}
}
