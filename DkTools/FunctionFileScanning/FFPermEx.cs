using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Tokens;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens.Statements;

namespace DkTools.FunctionFileScanning
{
	class FFPermEx
	{
		private FFFile _file;
		private string _name;
		private ExtractTableDefinition _def;
		private List<ExtractFieldDefinition> _fields = new List<ExtractFieldDefinition>();
		private int _id;
		private FilePosition _filePos;

		public FFPermEx(FFFile file, ExtractStatement exToken, ExtractTableDefinition exDef)
		{
			_file = file;
			_name = exToken.Name;
			_def = exDef;
			_fields.AddRange(exToken.Fields);
			_filePos = exToken.FilePosition;
		}

		public FFPermEx(FFFile file, SqlCeDataReader rdr)
		{
			_file = file;
			_name = rdr.GetString(rdr.GetOrdinal("name"));
			_id = rdr.GetInt32(rdr.GetOrdinal("id"));

			var fileName = rdr.GetStringOrNull(rdr.GetOrdinal("alt_file_name"));
			if (string.IsNullOrEmpty(fileName)) fileName = file.FileName;
			var pos = rdr.GetInt32(rdr.GetOrdinal("pos"));
			_filePos = new FilePosition(fileName, pos);

			_def = new ExtractTableDefinition(_name, _filePos, true);
		}

		public void Load(FFDatabase db)
		{
			_fields.Clear();

			using (var cmd = db.CreateCommand(
				"select name, pos, data_type, alt_file.file_name as alt_file_name from permex_col" +
				" left outer join alt_file on alt_file.id = permex_col.alt_file_id" +
				" where permex_id = @id"))
			{
				cmd.Parameters.AddWithValue("@id", _id);

				using (var rdr = cmd.ExecuteReader())
				{
					var ordName = rdr.GetOrdinal("name");
					var ordAltFileName = rdr.GetOrdinal("alt_file_name");
					var ordPos = rdr.GetOrdinal("pos");
					var ordDataType = rdr.GetOrdinal("data_type");

					while (rdr.Read())
					{
						var name = rdr.GetString(ordName);

						var fileName = rdr.GetStringOrNull(ordAltFileName);
						if (string.IsNullOrEmpty(fileName)) fileName = _file.FileName;
						var pos = rdr.GetInt32(ordPos);
						var filePos = new FilePosition(fileName, pos);

						var fieldDef = new ExtractFieldDefinition(name, filePos, _def);
						_fields.Add(fieldDef);
						_def.AddField(fieldDef);

						var dataTypeString = rdr.GetString(ordDataType);
						if (!string.IsNullOrEmpty(dataTypeString))
						{
							var dataType = DataType.TryParse(new DataType.ParseArgs { Code = new CodeParser(dataTypeString) });
							if (dataType == null)
							{
								Log.WriteDebug("Failed to parse permanent extract data type from database: {0}", dataTypeString);
								dataType = new CodeModel.DataType(ValType.Unknown, null, dataTypeString);
							}
							fieldDef.SetDataType(dataType);
						}
					}
				}
			}
		}

		public void UpdateFromToken(ExtractStatement exToken)
		{
			if (exToken == null) throw new ArgumentNullException("exToken");
			if (_name != exToken.Name) throw new ArgumentException("Extract token does not have the same name as FFPermEx.");

			var nameToken = exToken.FindFirstChild<ExtractTableToken>();
			if (nameToken != null)
			{
				_def = nameToken.SourceDefinition as ExtractTableDefinition;
			}
			else
			{
				_def = new ExtractTableDefinition(_name, exToken.FilePosition, true);
			}

			_fields.Clear();
			_fields.AddRange(exToken.Fields);
			_filePos = exToken.FilePosition;
		}

		public void SyncToDatabase(FFDatabase db)
		{
			int trueFileId = string.Equals(_filePos.FileName, _file.FileName, StringComparison.OrdinalIgnoreCase) ? 0 : _file.App.GetOrCreateAltFileId(db, _filePos.FileName);

			if (_id == 0)
			{
				// Insert the master record
				using (var cmd = db.CreateCommand("insert into permex (app_id, file_id, name, alt_file_id, pos)" +
					" values (@app_id, @file_id, @name, @alt_file_id, @pos)"))
				{
					cmd.Parameters.AddWithValue("@app_id", _file.App.Id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@alt_file_id", trueFileId);
					cmd.Parameters.AddWithValue("@pos", _filePos.Position);
					cmd.ExecuteNonQuery();
				}

				_id = db.QueryIdentityInt();

				// Insert all the fields
				foreach (var field in _fields)
				{
					var fieldFilePos = field.FilePosition;
					var fieldTrueFileId = string.Equals(fieldFilePos.FileName, _file.FileName, StringComparison.OrdinalIgnoreCase) ? 0 : _file.App.GetOrCreateAltFileId(db, fieldFilePos.FileName);
					var fieldDataType = field.DataType != null ? field.DataType.ToCodeString() : string.Empty;

					using (var cmd = db.CreateCommand("insert into permex_col (permex_id, file_id, name, data_type, alt_file_id, pos)" +
						" values (@permex_id, @file_id, @name, @data_type, @alt_file_id, @pos)"))
					{
						cmd.Parameters.AddWithValue("@permex_id", _id);
						cmd.Parameters.AddWithValue("@file_id", _file.Id);
						cmd.Parameters.AddWithValue("@name", field.Name);
						cmd.Parameters.AddWithValue("@data_type", fieldDataType);
						cmd.Parameters.AddWithValue("@alt_file_id", fieldTrueFileId);
						cmd.Parameters.AddWithValue("@pos", fieldFilePos.Position);
						cmd.ExecuteNonQuery();
					}
				}
			}
			else
			{
				// Update the master record
				using (var cmd = db.CreateCommand("update permex set alt_file_id = @alt_file_id, pos = @pos"))
				{
					cmd.Parameters.AddWithValue("@alt_file_id", trueFileId);
					cmd.Parameters.AddWithValue("@pos", _filePos.Position);
				}

				// Get a list of fields under this extract
				var dbNames = new Dictionary<string, int>();
				using (var cmd = db.CreateCommand("select id, name from permex_col where permex_id = @permex_id"))
				{
					cmd.Parameters.AddWithValue("@permex_id", _id);
					using (var rdr = cmd.ExecuteReader())
					{
						var ordId = rdr.GetOrdinal("id");
						var ordName = rdr.GetOrdinal("name");
						while (rdr.Read())
						{
							var id = rdr.GetInt32(ordId);
							var name = rdr.GetString(ordName);
							dbNames[name] = id;
						}
					}
				}

				// Sync the fields under this record
				foreach (var field in _fields)
				{
					var fieldFilePos = field.FilePosition;
					var fieldTrueFileId = string.Equals(fieldFilePos.FileName, _file.FileName, StringComparison.OrdinalIgnoreCase) ? 0 : _file.App.GetOrCreateAltFileId(db, fieldFilePos.FileName);
					var fieldDataType = field.DataType != null ? field.DataType.ToCodeString() : string.Empty;

					if (dbNames.ContainsKey(field.Name))
					{
						// Field already exists in the database, so update it
						using (var cmd = db.CreateCommand(
							"update permex_col set" +
							" data_type = @data_type," +
							" alt_file_id = @alt_file_id," +
							" pos = @pos" +
							" where id = @id"))
						{
							cmd.Parameters.AddWithValue("@id", dbNames[field.Name]);
							cmd.Parameters.AddWithValue("@data_type", fieldDataType);
							cmd.Parameters.AddWithValue("@alt_file_id", fieldTrueFileId);
							cmd.Parameters.AddWithValue("@pos", fieldFilePos.Position);
							cmd.ExecuteNonQuery();
						}
					}
					else
					{
						// Field does not yet exist in the database, so insert a new one
						using (var cmd = db.CreateCommand("insert into permex_col (permex_id, file_id, name, data_type, alt_file_id, pos)" +
						" values (@permex_id, @file_id, @name, @data_type, @alt_file_id, @pos)"))
						{
							cmd.Parameters.AddWithValue("@permex_id", _id);
							cmd.Parameters.AddWithValue("@file_id", _file.Id);
							cmd.Parameters.AddWithValue("@name", field.Name);
							cmd.Parameters.AddWithValue("@data_type", fieldDataType);
							cmd.Parameters.AddWithValue("@alt_file_id", fieldTrueFileId);
							cmd.Parameters.AddWithValue("@pos", fieldFilePos.Position);
							cmd.ExecuteNonQuery();
						}
					}
				}

				// Check for fields that no longer exist and need to be deleted from the database
				var fieldIdsToDelete = (from d in dbNames where !_fields.Any(f => d.Key == f.Name) select d.Value).ToArray();
				if (fieldIdsToDelete.Length > 0)
				{
					using (var cmd = db.CreateCommand("delete from permex_col where id = @id"))
					{
						foreach (var fieldId in fieldIdsToDelete)
						{
							cmd.Parameters.Clear();
							cmd.Parameters.AddWithValue("@id", fieldId);
							cmd.ExecuteNonQuery();
						}
					}
				}
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public int Id
		{
			get { return _id; }
		}

		public ExtractTableDefinition Definition
		{
			get { return _def; }
		}

		public IEnumerable<ExtractFieldDefinition> Fields
		{
			get
			{
				return _fields;
			}
		}
	}
}
