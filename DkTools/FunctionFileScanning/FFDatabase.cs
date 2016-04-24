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
	modified	datetime		not null,
	visible		tinyint			not null
);",
"create unique index file_ix_appfilename on file_ (app_id, file_name)",

@"create table func
(
	id					int				identity not null primary key,
	name				nvarchar(100)	not null,
	app_id				int				not null,
	file_id				int				not null,
	alt_file_id			int				not null,
	pos					int				not null,
	sig					ntext			not null,
	description			ntext,
	visible				tinyint			not null
);",
"create index func_ix_appid on func (app_id, name)",
"create index func_ix_fileid on func (file_id)",
"create index func_ix_altfileid on func (alt_file_id)",

@"create table include_depends
(
	id					int				identity not null primary key,
	app_id				int				not null,
	file_id				int				not null,
	include_file_name	nvarchar(260)	not null,
	include				tinyint			not null,
	localized_file		tinyint			not null
)",
@"create index include_depends_ix_fileid on include_depends (file_id)",
@"create index include_depends_ix_inclfile on include_depends (include_file_name, app_id)",

@"create table ref
(
	id					int				identity not null primary key,
	app_id				int				not null,
	file_id				int				not null,
	ext_ref_id			nvarchar(100)	not null,
	alt_file_id			int				not null,
	pos					int				not null
)",
@"create index ref_ix_extrefid on ref (ext_ref_id, app_id)",
@"create index ref_ix_fileid on ref (file_id)",
@"create index ref_ix_altfileid on ref (alt_file_id)",

@"create table alt_file
(
	id					int				identity not null primary key,
	file_name			nvarchar(260)	not null
)",
@"create index alt_file_ix_filename on alt_file (file_name)",

@"create table permex
(
	id					int				identity not null primary key,
	app_id				int				not null,
	file_id				int				not null,
	name				nvarchar(8)		not null,
	alt_file_id			int				not null,
	pos					int				not null
)",
@"create index permex_ix_file on permex (file_id)",
@"create index permex_ix_altfile on permex (alt_file_id)",

@"create table permex_col
(
	id					int				identity not null primary key,
	permex_id			int				not null,
	file_id				int				not null,
	name				nvarchar(255)	not null,
	data_type			ntext			not null,
	alt_file_id			int				not null,
	pos					int				not null
)",
@"create index permexcol_ix_permexid on permex_col (permex_id, name)",
@"create index permexcol_ix_fileid on permex_col (file_id)",
@"create index permexcol_ix_altfileid on permex_col (alt_file_id)"
};

		public const string DatabaseFileName = "DkScan_v7.sdf";	// New for 1.3.1
		public static readonly string[] OldDatabaseFileNames = new string[]
		{
			"DkScan.sdf",
			"DkScan_v2.sdf",
			"DkScan_v3.sdf",		// last used 1.2.10
			"DkScan_v4.sdf",		// last used 1.2.11
			"DkScan_v5.sdf",		// last used 1.2.23
			"DkScan_v6.sdf",		// last used 1.3
		};

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
				if (!File.Exists(FileName))
				{
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
				}

#if !DEBUG
				// Purge old databases
				foreach (var oldTitleExt in OldDatabaseFileNames)
				{
					var oldFileName = Path.Combine(ProbeToolsPackage.AppDataDir, oldTitleExt);
					if (File.Exists(oldFileName))
					{
						try
						{
							var attribs = File.GetAttributes(oldFileName);
							if ((attribs & FileAttributes.ReadOnly) != 0) File.SetAttributes(oldFileName, attribs & ~FileAttributes.ReadOnly);
							File.Delete(oldFileName);
						}
						catch (Exception ex)
						{
							Log.WriteEx(ex, "Exception when deleting old scanner database.");
						}
					}
				}
#endif

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
				return string.Concat("Data Source = ", FileName, ";Max Database Size = 1000");
			}
		}

		private string FileName
		{
			get
			{
				return Path.Combine(ProbeToolsPackage.AppDataDir, DatabaseFileName);
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
