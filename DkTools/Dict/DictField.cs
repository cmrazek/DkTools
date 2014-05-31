using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		private CodeModel.TableFieldDefinition _definition;

		public DictField(DictTable table, DICTSRVRLib.IPColumn repoCol)
		{
			Table = table;
			Name = repoCol.Name;

			var desc = repoCol as DICTSRVRLib.IPDObjDesc;
			Prompt = desc.Prompt[0];
			Comment = desc.Comment[0];

			var data = repoCol as DICTSRVRLib.IPDataDef;
			DataType = data.TypeText[0];

			_completionOptions = CodeModel.DataType.ParseCompletionOptionsFromArgText(this.DataType, null).ToArray();
			_definition = new CodeModel.TableFieldDefinition(table.Name, Name, Prompt, Comment, DataType);
		}

		public IEnumerable<string> CompletionOptions
		{
			get { return _completionOptions; }
		}

		public CodeModel.TableFieldDefinition Definition
		{
			get { return _definition; }
		}
	}
}
