﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.FunctionFileScanning
{
	internal class FFFunction
	{
		private FFApp _app;
		private FFFile _file;
		private FFClass _class;
		private int _id;
		private string _name;
		private FunctionSignature _sig;
		private CodeModel.Span _span;
		private CodeModel.Definitions.FunctionDefinition _def;
		private string _devDesc;
		private bool _visible;

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
			_span = new CodeModel.Span(def.SourceStartPos, def.SourceStartPos);
			_devDesc = def.DevDescription;
			_def = def;

			UpdateVisibility();
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
			_sig = FunctionSignature.ParseFromDb(rdr.GetString(rdr.GetOrdinal("sig")));

			var devDescValue = rdr["description"];
			if (!Convert.IsDBNull(devDescValue)) _devDesc = Convert.ToString(devDescValue);
			else _devDesc = null;

			var fileName = _file.FileName;
			var altFileName = rdr.GetStringOrNull(rdr.GetOrdinal("alt_file_name"));
			if (!string.IsNullOrEmpty(altFileName)) fileName = altFileName;
			var pos = rdr.GetInt32(rdr.GetOrdinal("pos"));
			var filePos = new FilePosition(fileName, pos);

			_def = new CodeModel.Definitions.FunctionDefinition(_sig, filePos, 0, 0, 0, _span, _devDesc);

			UpdateVisibility();
		}

		public static CodeModel.Definitions.FunctionDefinition CreateFunctionDefinitionFromSqlReader(SqlCeDataReader rdr, string fileName)
		{
			var className = FileContextUtil.GetClassNameFromFileName(fileName);

			var funcName = rdr.GetString(rdr.GetOrdinal("name"));
			var sig = FunctionSignature.ParseFromDb(rdr.GetString(rdr.GetOrdinal("sig")));

			string devDesc = null;
			var devDescValue = rdr["description"];
			if (!Convert.IsDBNull(devDescValue)) devDesc = Convert.ToString(devDescValue);

			var trueFileName = fileName;
			var altFileName = rdr.GetStringOrNull(rdr.GetOrdinal("alt_file_name"));
			if (!string.IsNullOrEmpty(altFileName)) trueFileName = altFileName;
			var pos = rdr.GetInt32(rdr.GetOrdinal("pos"));
			var filePos = new FilePosition(trueFileName, pos);

			return new CodeModel.Definitions.FunctionDefinition(sig, filePos, 0, 0, 0, Span.Empty, devDesc);
		}

		private static IEnumerable<ArgumentDescriptor> ParseArguments(string argsString)
		{
			if (string.IsNullOrEmpty(argsString)) yield break;

			foreach (var argString in argsString.Split('|'))
			{
				var arg = ArgumentDescriptor.ParseFromDb(argString);
				if (arg.HasValue) yield return arg.Value;
			}
		}

		public void UpdateFromDefinition(CodeModel.Definitions.FunctionDefinition def)
		{
#if DEBUG
			if (def == null) throw new ArgumentNullException("def");
			if (def.DataType == null) throw new ArgumentNullException("def.DataType");
#endif
			_sig = def.Signature;
			_span = new CodeModel.Span(def.SourceStartPos, def.SourceStartPos);
			_def = def;
			_devDesc = def.DevDescription;

			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			if (_file.FileContext.IsClass())
			{
				_visible = _def.Privacy == FunctionPrivacy.Public;
			}
			else if (_file.FileContext == FileContext.Function)
			{
				_visible = _name.Equals(System.IO.Path.GetFileNameWithoutExtension(_file.FileName), StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				_visible = false;
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public FunctionSignature Signature
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
			var sb = new StringBuilder();
			var first = true;
			foreach (var arg in _def.Arguments)
			{
				if (first) first = false;
				else sb.Append('|');
				sb.Append(arg.ToDbString());
			}
			var argsString = sb.ToString();

			var altFileId = 0;
			var filePos = _def.FilePosition;
			if (!string.Equals(filePos.FileName, _file.FileName, StringComparison.OrdinalIgnoreCase)) altFileId = _app.GetAltFileId(db, filePos.FileName);

			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("update func set file_id = @file_id, name = @name, sig = @sig, alt_file_id = @alt_file_id, pos = @pos, " +
					"description = @dev_desc, visible = @visible where id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@sig", _sig.ToDbString());
					cmd.Parameters.AddWithValue("@alt_file_id", altFileId);
					cmd.Parameters.AddWithValue("@pos", filePos.Position);
					if (string.IsNullOrEmpty(_devDesc)) cmd.Parameters.AddWithValue("@dev_desc", DBNull.Value);
					else cmd.Parameters.AddWithValue("@dev_desc", _devDesc);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using (var cmd = db.CreateCommand(@"insert into func (name, app_id, file_id, alt_file_id, pos, sig, description, visible)
													values (@name, @app_id, @file_id, @alt_file_id, @pos, @sig, @dev_desc, @visible)"))
				{
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@app_id", _app.Id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@alt_file_id", altFileId);
					cmd.Parameters.AddWithValue("@pos", filePos.Position);
					cmd.Parameters.AddWithValue("@sig", _sig.ToDbString());
					if (string.IsNullOrEmpty(_devDesc)) cmd.Parameters.AddWithValue("@dev_desc", DBNull.Value);
					else cmd.Parameters.AddWithValue("@dev_desc", _devDesc);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					cmd.ExecuteNonQuery();
					_id = db.QueryIdentityInt();
				}
			}
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

		public bool Visible
		{
			get { return _visible; }
		}
	}
}
