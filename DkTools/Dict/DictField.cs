using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal class DictField
	{
		public DictTable Table { get; private set; }
		public string Name { get; private set; }
		public string Prompt { get; private set; }
		public string Comment { get; private set; }
		public string DataType { get; private set; }

		private string[] _completionOptions;
		private CodeModel.Definitions.TableFieldDefinition _definition;

		public DictField(DictTable table, DICTSRVRLib.IPColumn repoCol)
		{
			Table = table;
			Name = repoCol.Name;

			var desc = repoCol as DICTSRVRLib.IPDObjDesc;
			Prompt = desc.Prompt[0];
			Comment = desc.Comment[0];

			var data = repoCol as DICTSRVRLib.IPDataDef;
			DataType = data.TypeText[0];

			var dictObj = repoCol as DICTSRVRLib.IPDictObj;
			string description = null;
			if (dictObj != null)
			{
				description = dictObj.DevInfo;
				if (description != null && description.Length == 0) description = null;
			}

			_completionOptions = CodeModel.DataType.ParseCompletionOptionsFromArgText(this.DataType, null).ToArray();
			_definition = new CodeModel.Definitions.TableFieldDefinition(new CodeModel.Scope(), table.Name, Name, Prompt, Comment, DataType, description);
		}

		public IEnumerable<string> CompletionOptions
		{
			get { return _completionOptions; }
		}

		public CodeModel.Definitions.TableFieldDefinition Definition
		{
			get { return _definition; }
		}
	}
}
