using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Dict
{
	internal class DictStringDef
	{
		private string _name;
		private string _value;
		private CodeModel.StringDefDefinition _def;

		public DictStringDef(DICTSRVRLib.IPStringDefine repoStringDef)
		{
			_name = repoStringDef.Name;
			_value = repoStringDef.String[0];
			_def = new CodeModel.StringDefDefinition(this);
		}

		public string Name
		{
			get { return _name; }
		}

		public string Value
		{
			get { return _value; }
		}

		public CodeModel.StringDefDefinition Definition
		{
			get { return _def; }
		}
	}
}
