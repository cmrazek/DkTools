using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.FunctionFileScanning
{
	internal class FFSearcher : IDisposable
	{
		internal FFApp _app;
		internal FFDatabase _db;

		public FFSearcher(FFApp app)
		{
			_app = app;
			_db = new FFDatabase();
		}

		public void Dispose()
		{
			if (_db != null)
			{
				_db.Dispose();
				_db = null;
			}
		}

		public IEnumerable<FunctionDefinition> SearchForFunctionDefinitions(string funcName)
		{
			if (_db == null) yield break;

			using (var cmd = _db.CreateCommand("select file_.file_name, func.*, alt_file.file_name as alt_file_name from func" +
				" inner join file_ on file_.id = func.file_id" +
				" left outer join alt_file on alt_file.id = func.alt_file_id" +
				" where func.app_id = @app_id" +
				" and func.name = @func_name"))
			{
				cmd.Parameters.AddWithValue("@app_id", _app.Id);
				cmd.Parameters.AddWithValue("@func_name", funcName);
				using (var rdr = cmd.ExecuteReader())
				{
					var ordFileName = rdr.GetOrdinal("file_name");

					while (rdr.Read())
					{
						yield return FFFunction.CreateFunctionDefinitionFromSqlReader(rdr, rdr.GetString(ordFileName));
					}
				}
			}
		}
	}
}
