using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
		private long _id;
		private string _name;
		private FunctionSignature _sig;
		private CodeModel.Span _span;
		private CodeModel.Definitions.FunctionDefinition _def;
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
			_def = def;

			UpdateVisibility();
		}

		public FFFunction(FFApp app, FFFile file, FFClass cls, SQLiteDataReader rdr)
		{
			_app = app ?? throw new ArgumentNullException(nameof(app));
			_file = file ?? throw new ArgumentNullException(nameof(file));
			_class = cls;

			_id = rdr.GetInt64(rdr.GetOrdinal("rowid"));
			_name = rdr.GetString(rdr.GetOrdinal("name"));
			_sig = FunctionSignature.ParseFromDb(rdr.GetString(rdr.GetOrdinal("sig")));

			var fileName = _file.FileName;
			var altFileName = rdr.GetStringOrNull(rdr.GetOrdinal("alt_file_name"));
			if (!string.IsNullOrEmpty(altFileName)) fileName = altFileName;
			var pos = rdr.GetInt32(rdr.GetOrdinal("pos"));
			var filePos = new FilePosition(fileName, pos);

			_def = new CodeModel.Definitions.FunctionDefinition(
				signature: _sig,
				filePos: filePos,
				argsStartPos: 0,
				argsEndPos: 0,
				bodyStartPos: 0,
				entireSpan: _span,
				rawBodySpan: Span.Empty);

			UpdateVisibility();
		}

		public static CodeModel.Definitions.FunctionDefinition CreateFunctionDefinitionFromSqlReader(SQLiteDataReader rdr, string fileName)
		{
			var className = FileContextUtil.GetClassNameFromFileName(fileName);

			var funcName = rdr.GetString(rdr.GetOrdinal("name"));
			var sig = FunctionSignature.ParseFromDb(rdr.GetString(rdr.GetOrdinal("sig")));

			var trueFileName = fileName;
			var altFileName = rdr.GetStringOrNull(rdr.GetOrdinal("alt_file_name"));
			if (!string.IsNullOrEmpty(altFileName)) trueFileName = altFileName;
			var pos = rdr.GetInt32(rdr.GetOrdinal("pos"));
			var filePos = new FilePosition(trueFileName, pos);

			return new CodeModel.Definitions.FunctionDefinition(
				signature: sig,
				filePos: filePos,
				argsStartPos: 0,
				argsEndPos: 0,
				bodyStartPos: 0,
				entireSpan: Span.Empty,
				rawBodySpan: Span.Empty);
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

			var altFileId = 0L;
			var filePos = _def.FilePosition;
			if (!string.Equals(filePos.FileName, _file.FileName, StringComparison.OrdinalIgnoreCase)) altFileId = _app.GetAltFileId(db, filePos.FileName);

			if (_id != 0)
			{
				using (var cmd = db.CreateCommand(@"
					update func set file_id = @file_id, name = @name, sig = @sig, alt_file_id = @alt_file_id, pos = @pos,
					visible = @visible, ext_ref_id = @ext_ref_id where rowid = @id
					"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@sig", _sig.ToDbString());
					cmd.Parameters.AddWithValue("@alt_file_id", altFileId);
					cmd.Parameters.AddWithValue("@pos", filePos.Position);
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					cmd.Parameters.AddWithValue("@ext_ref_id", _def.ExternalRefId);
					
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using (var cmd = db.CreateCommand(@"
					insert into func (name, app_id, file_id, alt_file_id, pos, sig, visible, ext_ref_id)
					values (@name, @app_id, @file_id, @alt_file_id, @pos, @sig, @visible, @ext_ref_id);
					select last_insert_rowid();
					"))
				{
					cmd.Parameters.AddWithValue("@name", _name);
					cmd.Parameters.AddWithValue("@app_id", _app.Id);
					cmd.Parameters.AddWithValue("@file_id", _file.Id);
					cmd.Parameters.AddWithValue("@alt_file_id", altFileId);
					cmd.Parameters.AddWithValue("@pos", filePos.Position);
					cmd.Parameters.AddWithValue("@sig", _sig.ToDbString());
					cmd.Parameters.AddWithValue("@visible", _visible ? 1 : 0);
					cmd.Parameters.AddWithValue("@ext_ref_id", _def.ExternalRefId);

					_id = Convert.ToInt64(cmd.ExecuteScalar());
				}
			}
		}

		public void Remove(FFDatabase db)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("delete from func where rowid = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public long Id
		{
			get { return _id; }
		}

		public bool Visible
		{
			get { return _visible; }
		}
	}
}
