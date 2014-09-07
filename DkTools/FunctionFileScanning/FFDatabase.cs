using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	/*
	 * Tables:
	 *	files
	 *		id int
	 */
	internal class FFDatabase : IDisposable
	{
		private static readonly string[] k_databaseInitScript = {
@"create table app
(
	id			int				identity not null primary key,
	name		nvarchar(255)	not null
);",
"create unique index app_ix_name on app (name)",
@"create table file_
(
	id			int				identity not null primary key,
	app_id		int				not null,
	file_name	nvarchar(260)	not null,
	modified	datetime		not null
);",
"create unique index file_ix_appfilename on file_ (app_id, file_name)",
@"create table class_
(
	id			int			identity not null primary key,
	name		nvarchar(8)	not null,
	app_id		int			not null,
	file_id		int			not null
);",
"create unique index class_ix_name on class_ (name)",
@"create table func
(
	id					int				identity not null primary key,
	class_id			int,
	name				nvarchar(100)	not null,
	app_id				int				not null,
	file_id				int				not null,
	sig					nvarchar(1000)	not null,
	span				nvarchar(100)	not null,
	data_type			nvarchar(1000)	not null,
	completion_options	nvarchar(1000),
	privacy				nvarchar(10)	not null
);",
"create index func_ix_classfunc on func (class_id, name);" };

		private SqlCeConnection _conn;

		public FFDatabase()
		{
			if (!CreateNewDatabaseIfNecessary()) return;
			Connect();
		}

		public void Dispose()
		{
			Disconnect();
		}

		private void Connect()
		{
			if (_conn == null)
			{
				_conn = new SqlCeConnection(ConnectionString);
				_conn.Open();
			}
		}

		private void Disconnect()
		{
			if (_conn != null)
			{
				_conn.Dispose();
				_conn = null;
			}
		}

		private bool CreateNewDatabaseIfNecessary()
		{
			try
			{
				if (File.Exists(FileName)) return true;

				Log.WriteDebug("Creating new scanner database...");

				var connStr = ConnectionString;
				var engine = new SqlCeEngine(connStr);
				engine.CreateDatabase();
				Connect();

				foreach (var query in k_databaseInitScript)
				{
					Log.WriteDebug("Executing database initialization query: {0}", query);
					ExecuteNonQuery(query);
				}
				return true;
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex, "Exception when creating scanner database.");
				Disconnect();
				return false;
			}
		}

		private string ConnectionString
		{
			get
			{
				return string.Concat("Data Source = ", FileName);
			}
		}

		private string FileName
		{
			get
			{
				return Path.Combine(ProbeToolsPackage.AppDataDir, Constants.FunctionFileDatabaseFileName_SDF);
			}
		}

		public int ExecuteNonQuery(string sql)
		{
			if (_conn == null) throw new InvalidOperationException("Database connection is not established.");

			using (var cmd = _conn.CreateCommand())
			{
				cmd.CommandText = sql;
				return cmd.ExecuteNonQuery();
			}
		}

		public int ExecuteScalarInt(string sql)
		{
			if (_conn == null) throw new InvalidOperationException("Database connection is not established.");

			using (var cmd = _conn.CreateCommand())
			{
				cmd.CommandText = sql;
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
		}

		public SqlCeConnection Connection
		{
			get { return _conn; }
		}

		public SqlCeCommand CreateCommand(string sql)
		{
			if (_conn == null) throw new InvalidOperationException("Database connection is not established.");

			var cmd = _conn.CreateCommand();
			cmd.CommandType = System.Data.CommandType.Text;
			cmd.CommandText = sql;
			return cmd;
		}

		public int QueryIdentityInt()
		{
			return ExecuteScalarInt("select @@identity");
		}
	}
}
