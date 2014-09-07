using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	internal class FFFile
	{
		private FFApp _app;
		private int _id;
		private string _fileName;
		private DateTime _modified;
		private bool _used = true;

		public FFFile(FFDatabase db, FFApp app, string fileName)
		{
			if (app == null) throw new ArgumentNullException("app");
			_app = app;

			if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
			_fileName = fileName;

			using (var cmd = db.CreateCommand("select * from file_ where file_name = @file_name and app_id = @app_id"))
			{
				cmd.Parameters.AddWithValue("@file_name", fileName);
				cmd.Parameters.AddWithValue("@app_id", _app.Id);
				using (var rdr = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow))
				{
					if (rdr.Read())
					{
						_id = rdr.GetInt32(rdr.GetOrdinal("id"));
						_modified = rdr.GetDateTime(rdr.GetOrdinal("modified"));
					}
				}
			}
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

		public void InsertOrUpdate(FFDatabase db)
		{
			if (_id != 0)
			{
				using (var cmd = db.CreateCommand("update file_ set modified = @modified where id = @id"))
				{
					cmd.Parameters.AddWithValue("@id", _id);
					cmd.Parameters.AddWithValue("@modified", _modified);
					cmd.ExecuteNonQuery();
				}
			}
			else
			{
				using (var cmd = db.CreateCommand("insert into file_ (app_id, file_name, modified) values (@app_id, @file_name, @modified)"))
				{
					cmd.Parameters.AddWithValue("@app_id", _app.Id);
					cmd.Parameters.AddWithValue("@file_name", _fileName);
					cmd.Parameters.AddWithValue("@modified", _modified);
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
			}
		}
	}
}
