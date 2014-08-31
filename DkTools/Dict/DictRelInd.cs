using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal class DictRelInd
	{
		private string _name;
		private CodeModel.Definitions.RelIndDefinition _def;
		private string _repoDesc;

		public DictRelInd(DictTable table, DICTSRVRLib.IPIndex repoIndex)
		{
			_name = repoIndex.Name;

			var dev = repoIndex as DICTSRVRLib.IPDictObj;
			if (dev != null)
			{
				var devInfo = dev.DevInfo;
				if (!string.IsNullOrWhiteSpace(devInfo)) _repoDesc = devInfo;
			}

			// Info text will be the list of columns in the index.
			var sb = new StringBuilder();
			for (int c = 1, cc = repoIndex.ColumnCount; c <= cc; c++)
			{
				if (sb.Length > 0) sb.Append(", ");
				sb.Append(repoIndex.Columns[c].Name);
			}
			if (!string.IsNullOrWhiteSpace(_repoDesc))
			{
				sb.AppendLine();
				sb.AppendFormat("Description: {0}", _repoDesc);
			}

			_def = new CodeModel.Definitions.RelIndDefinition(_name, table.Name, sb.ToString());
		}

		public string Name
		{
			get { return _name; }
		}

		public CodeModel.Definitions.RelIndDefinition Definition
		{
			get { return _def; }
		}
	}
}
