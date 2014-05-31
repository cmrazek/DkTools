using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Dict
{
	internal class DictTypeDef
	{
		private string _name;
		private CodeModel.DataType _dataType;
		private CodeModel.DataTypeDefinition _def;

		public DictTypeDef(DICTSRVRLib.IPTypeDefine repoTypeDef)
		{
			_name = repoTypeDef.Name;

			var data = repoTypeDef as DICTSRVRLib.IPDataDef;

			if (data.Enumcount > 0)
			{
				var completionList = new List<string>();
				for (int e = 1, ee = data.Enumcount; e <= ee; e++)
				{
					completionList.Add(data.Enumitem[0, e]);
				}

				_dataType = new CodeModel.DataType(_name, completionList.ToArray(), data.TypeText[0]);
			}
			else
			{
				_dataType = new CodeModel.DataType(_name, data.TypeText[0]);
			}

			_def = new CodeModel.DataTypeDefinition(_name, _dataType);
		}

		public string Name
		{
			get { return _name; }
		}

		public CodeModel.DataType DataType
		{
			get { return _dataType; }
		}

		public CodeModel.DataTypeDefinition Definition
		{
			get { return _def; }
		}
	}
}
