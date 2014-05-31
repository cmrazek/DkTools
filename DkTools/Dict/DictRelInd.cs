using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.Dict
{
	internal class DictRelInd
	{
		private string _name;
		private CodeModel.RelIndDefinition _def;

		public DictRelInd(string name, string baseTableName, string infoText)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException();
#endif
			_name = name;
			_def = new CodeModel.RelIndDefinition(name, baseTableName, infoText);
		}

		public DictRelInd(DictTable table, DICTSRVRLib.IPIndex repoIndex)
		{
			_name = repoIndex.Name;

			// Info text will be the list of columns in the index.
			var sb = new StringBuilder();
			for (int c = 1, cc = repoIndex.ColumnCount; c <= cc; c++)
			{
				if (sb.Length > 0) sb.Append(", ");
				sb.Append(repoIndex.Columns[c].Name);
			}

			_def = new CodeModel.RelIndDefinition(_name, table.Name, sb.ToString());
		}

		public string Name
		{
			get { return _name; }
		}

		public CodeModel.RelIndDefinition Definition
		{
			get { return _def; }
		}
	}
}
