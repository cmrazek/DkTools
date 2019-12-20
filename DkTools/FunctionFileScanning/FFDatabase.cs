using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.FunctionFileScanning
{
	internal class FFDatabase : IDisposable
	{
		#region Initialization Scripts
		private static readonly string[] k_databaseInitScript = {

@"create table app
(
	name		varchar(255)	not null
);",
"create unique index app_ix_name on app (name)",

@"create table file_
(
	app_id		integer			not null,
	file_name	varchar(260)	not null collate nocase,
	modified	datetime		not null,
	visible		tinyint			not null
);",
"create unique index file_ix_appfilename on file_ (app_id, file_name)",

@"create table func
(
	name				varchar(100)	not null,
	app_id				integer			not null,
	file_id				integer			not null,
	alt_file_id			integer			not null,
	pos					int				not null,
	sig					text			not null,
	description			text,
	visible				tinyint			not null
);",
"create index func_ix_appid on func (app_id, name)",
"create index func_ix_fileid on func (file_id)",
"create index func_ix_altfileid on func (alt_file_id)",

@"create table include_depends
(
	app_id				integer			not null,
	file_id				integer			not null,
	include_file_name	varchar(260)	not null collate nocase,
	include				tinyint			not null,
	localized_file		tinyint			not null
)",
@"create index include_depends_ix_fileid on include_depends (file_id)",
@"create index include_depends_ix_inclfile on include_depends (include_file_name, app_id)",

@"create table ref
(
	app_id				integer			not null,
	file_id				integer			not null,
	ext_ref_id			varchar(100)	not null,
	alt_file_id			integer			not null,
	pos					int				not null
)",
@"create index ref_ix_extrefid on ref (ext_ref_id, app_id)",
@"create index ref_ix_fileid on ref (file_id)",
@"create index ref_ix_altfileid on ref (alt_file_id)",

@"create table alt_file
(
	file_name			varchar(260)	not null
)",
@"create index alt_file_ix_filename on alt_file (file_name)",

@"create table permex
(
	app_id				integer			not null,
	file_id				integer			not null,
	name				varchar(8)		not null,
	alt_file_id			integer			not null,
	pos					int				not null
)",
@"create index permex_ix_file on permex (file_id)",
@"create index permex_ix_altfile on permex (alt_file_id)",

@"create table permex_col
(
	permex_id			integer			not null,
	file_id				integer			not null,
	name				varchar(255)	not null,
	data_type			text			not null,
	alt_file_id			integer			not null,
	pos					int				not null
)",
@"create index permexcol_ix_permexid on permex_col (permex_id, name)",
@"create index permexcol_ix_fileid on permex_col (file_id)",
@"create index permexcol_ix_altfileid on permex_col (alt_file_id)"
};
		#endregion

		public const string DatabaseFileName = "DkScan_v8.sqlite";	// New for 1.4
		public static readonly string[] OldDatabaseFileNames = new string[]
		{
			"DkScan.sdf",
			"DkScan_v2.sdf",
			"DkScan_v3.sdf",		// last used 1.2.10
			"DkScan_v4.sdf",		// last used 1.2.11
			"DkScan_v5.sdf",		// last used 1.2.23
			"DkScan_v6.sdf",		// last used 1.3
			"DkScan_v7.sdf",		// last used 1.3.5
		};

		private SQLiteConnection _conn;

		public FFDatabase()
		{
			CreateNewDatabaseIfNecessary();
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
				Log.Debug("Connecting to database.");
				_conn = new SQLiteConnection(ConnectionString);
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

		private void CreateNewDatabaseIfNecessary()
		{
			try
			{
				if (File.Exists(FileName)) return;

				Log.Debug("Creating new scanner database...");

				SQLiteConnection.CreateFile(FileName);
				Connect();

				foreach (var query in k_databaseInitScript)
				{
					Log.Debug("Executing database initialization query: {0}", query);
					ExecuteNonQuery(query);
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
							Log.Error(ex, "Exception when deleting old scanner database.");
						}
					}
				}
#endif
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception when creating scanner database.");
			}
		}

		private string ConnectionString
		{
			get
			{
				return string.Format("Data Source={0};Version=3;", FileName);
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
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				return cmd.ExecuteNonQuery();
			}
		}

		public int ExecuteScalarInt(string sql)
		{
			if (_conn == null) throw new InvalidOperationException("Database connection is not established.");

			using (var cmd = _conn.CreateCommand())
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
		}

		public T ExecuteScalar<T>(string sql, params object[] args)
		{
			if (_conn == null) throw new InvalidOperationException("Database connection is not established.");
			if (args.Length % 2 != 0) throw new ArgumentException("There must be an even number of arguments for key/value pairs.");

			using (var cmd = _conn.CreateCommand())
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				for (int i = 0; i < args.Length; i += 2)
				{
					cmd.Parameters.AddWithValue(args[i].ToString(), args[i + 1]);
				}
				var obj = cmd.ExecuteScalar();
				if (obj == null || Convert.IsDBNull(obj)) return default(T);
				return (T)obj;
			}
		}

		public SQLiteConnection Connection
		{
			get { return _conn; }
		}

		public SQLiteCommand CreateCommand(string sql, params object[] args)
		{
			if (_conn == null) throw new InvalidOperationException("Database connection is not established.");
			if (args.Length % 2 != 0) throw new ArgumentException("There must be an even number of arguments for key/value pairs.");

			var cmd = _conn.CreateCommand();
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = sql;
			for (int i = 0; i < args.Length; i += 2)
			{
				cmd.Parameters.AddWithValue(args[i].ToString(), args[i + 1]);
			}
			return cmd;
		}

		public SQLiteTransaction BeginTransaction()
		{
			return _conn.BeginTransaction();
		}

#if DEBUG
		public static void DumpMemoryStats()
		{
			IDictionary<string, long> stats = new Dictionary<string, long>();
			SQLiteConnection.GetMemoryStatistics(ref stats);

			var sb = new StringBuilder();
			sb.AppendLine("SQLite Memory Statistics:");
			foreach (var stat in stats)
			{
				sb.Append(stat.Key);
				sb.Append(": ");
				sb.Append(stat.Value);
				sb.AppendLine();
			}

			Log.Debug(sb.ToString());
		}
#endif
	}
}
