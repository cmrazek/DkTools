using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal sealed class StringDef : IDictObj
	{
		private string _name;
		private string _value;
		private CodeModel.Definitions.StringDefDefinition _def;

		public StringDef(DICTSRVRLib.IPStringDefine repoStringDef)
		{
			_name = repoStringDef.Name;
			_value = repoStringDef.String[0];
			_def = new CodeModel.Definitions.StringDefDefinition(this);
		}

		public string Name
		{
			get { return _name; }
		}

		public string Value
		{
			get { return _value; }
		}

		public CodeModel.Definitions.StringDefDefinition Definition
		{
			get { return _def; }
		}

		public object CreateRepoObject(Dict dict)
		{
			return dict.GetStringDefine(_name);
		}
	}
}
