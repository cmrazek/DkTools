﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	internal class FFFunction
	{
		private FFApp _app;
		private FFFile _file;
		private FFClass _class;
		private int _id;
		private string _name;
		private string _sig;
		private CodeModel.Span _span;
		private CodeModel.DataType _dataType;
		private CodeModel.FunctionPrivacy _privacy;
		private CodeModel.Definitions.FunctionDefinition _def;
		private bool _used = true;

		private FFFunction()
		{ }

		public FFFunction(FFApp app, FFFile file, FFClass cls, CodeModel.Definitions.FunctionDefinition def)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (file == null) throw new ArgumentNullException("file");
			if (def == null) throw new ArgumentNullException("def");
#endif

			_app = app;
			_file = file;
			_class = cls;
			_name = def.Name;
			_sig = def.Signature;
			_span = def.SourceSpan;
			_dataType = def.DataType;
			_privacy = def.Privacy;
			_def = def;
		}

		public FFFunction(FFApp app, FFFile file, FFClass cls, SqlCeDataReader rdr)
		{
#if DEBUG
			if (app == null) throw new ArgumentNullException("app");
			if (file == null) throw new ArgumentNullException("file");
#endif

			_app = app;
			_file = file;
			_class = cls;

			_id = rdr.GetInt32(rdr.GetOrdinal("id"));
			_name = rdr.GetString(rdr.GetOrdinal("name"));
			_sig = rdr.GetString(rdr.GetOrdinal("sig"));
			_span = CodeModel.Span.FromSaveString(rdr.GetString(rdr.GetOrdinal("span")));

			var dataTypeText = rdr.GetString(rdr.GetOrdinal("data_type"));

			var optionsValue = rdr["completion_options"];
			if (!Convert.IsDBNull(optionsValue))
			{
				var options = (from o in rdr.GetString(rdr.GetOrdinal("completion_options")).Split('|') select new CodeModel.Definitions.EnumOptionDefinition(o)).ToArray();
				_dataType = new CodeModel.DataType(dataTypeText, options, dataTypeText);
			}

			var str = rdr.GetString(rdr.GetOrdinal("privacy"));
			if (!Enum.TryParse<CodeModel.FunctionPrivacy>(str, out _privacy)) _privacy = CodeModel.FunctionPrivacy.Public;

			_def = new CodeModel.Definitions.FunctionDefinition(new CodeModel.Scope(), _class != null ? _class.Name : null, _name, new CodeModel.Tokens.ExternalToken(_file.FileName, _span), _dataType, _sig,
					0, 0, _privacy, true);
		}

		public void UpdateFromDefinition(CodeModel.Definitions.FunctionDefinition def)
		{
			_sig = def.Signature;
			_span = def.SourceSpan;
			_dataType = def.DataType;
			_privacy = def.Privacy;
			_def = def;
		}

		public string Name
		{
			get { return _name; }
		}

		public string Signature
		{
			get { return _sig; }
		}

		public CodeModel.Definitions.FunctionDefinition Definition
		{
			get { return _def; }
		}

		public FFClass Class
		{
			get { return _class; }
		}

		public void InsertOrUpdate(FFDatabase db)
		{
			string completionOptions = null;
			if (_dataType.HasCompletionOptions)
			{
				var sb = new StringBuilder();
				foreach (var opt in _dataType.CompletionOptions)
				{
					if (sb.Length > 0) sb.Append('|');
					sb.Append(opt);
				}
				completionOptions = sb.ToString();
			}

			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("update func set file_id = @file_id, name = @name, sig = @sig, span = @span, data_type = @data_type, completion_options = @completion_options, privacy = @privacy where id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@sig", _sig);
					cmd.Parameters.AddWithValue("@span", _span.SaveString);
					cmd.Parameters.AddWithValue("@data_type", _dataType.Name);
					if (completionOptions != null) cmd.Parameters.AddWithValue("@completion_options", completionOptions);
					else cmd.Parameters.AddWithValue("@completion_options", DBNull.Value);
					cmd.Parameters.AddWithValue("@privacy", _privacy.ToString());
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using (var cmd = db.CreateCommand(@"insert into func (class_id, name, app_id, file_id, sig, span, data_type, completion_options, privacy)
													values (@class_id, @name, @app_id, @file_id, @sig, @span, @data_type, @completion_options, @privacy)"))
				{
					if (_class != null) cmd.Parameters.AddWithValue("@class_id", _class.Id);
					else cmd.Parameters.AddWithValue("@class_id", DBNull.Value);
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@app_id", _app.Id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@sig", _sig);
					cmd.Parameters.AddWithValue("@span", _span.SaveString);
					cmd.Parameters.AddWithValue("@data_type", _dataType.Name);
					if (completionOptions != null) cmd.Parameters.AddWithValue("@completion_options", completionOptions);
					else cmd.Parameters.AddWithValue("@completion_options", DBNull.Value);
					cmd.Parameters.AddWithValue("@privacy", _privacy.ToString());
					cmd.ExecuteNonQuery();
					_id = db.QueryIdentityInt();
				}
			}
		}

		public bool Used
		{
			get { return _used; }
			set { _used = value; }
		}

		public void MarkUsed()
		{
			_used = true;
			_file.Used = true;
			if (_class != null) _class.Used = true;
		}

		public void Remove(FFDatabase db)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("delete from func where id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public int Id
		{
			get { return _id; }
		}
	}
}
