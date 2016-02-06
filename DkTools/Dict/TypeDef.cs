using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.Dict
{
	internal sealed class TypeDef : IDictObj
	{
		private string _name;
		private CodeModel.DataType _dataType;
		private CodeModel.Definitions.DataTypeDefinition _def;

		public TypeDef(DICTSRVRLib.IPTypeDefine repoTypeDef)
		{
			_name = repoTypeDef.Name;

			var data = repoTypeDef as DICTSRVRLib.IPDataDef;

			if (data.Enumcount > 0)
			{
				var completionList = new List<CodeModel.Definitions.Definition>();
				for (int e = 1, ee = data.Enumcount; e <= ee; e++)
				{
					var opt = data.Enumitem[0, e];
					if (opt == null) opt = "\" \"";
					else if (opt.IsWhiteSpace()) opt = string.Concat("\"", opt, "\"");
					completionList.Add(new CodeModel.Definitions.EnumOptionDefinition(opt, null));
				}

				_dataType = new CodeModel.DataType(CodeModel.ValType.Enum, _name, data.TypeText[0], completionList.ToArray(), CodeModel.DataType.CompletionOptionsType.EnumOptionsList);

				foreach (var opt in completionList)
				{
					(opt as CodeModel.Definitions.EnumOptionDefinition).SetEnumDataType(_dataType);
				}
			}
			else
			{
				var dataTypeText = data.TypeText[0];
				_dataType = DataType.TryParse(new CodeModel.DataType.ParseArgs
				{
					Code = new CodeParser(dataTypeText),
					Flags = CodeModel.DataType.ParseFlag.FromRepo
				});
				if (_dataType == null)
				{
					Log.WriteDebug("Failed to parse typedef data type: {0}", dataTypeText);
					_dataType = new CodeModel.DataType(ValType.Unknown, _name, dataTypeText);
				}
			}

			_def = new CodeModel.Definitions.DataTypeDefinition(_name, _dataType, global: true);
		}

		public string Name
		{
			get { return _name; }
		}

		public CodeModel.DataType DataType
		{
			get { return _dataType; }
		}

		public CodeModel.Definitions.DataTypeDefinition Definition
		{
			get { return _def; }
		}

		public object CreateRepoObject(Dict dict)
		{
			return dict.GetTypeDefine(_name);
		}
	}
}
