using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class TableToken : WordToken
	{
		public TableToken(Scope scope, Span span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.TableName;

			if (def != null) SourceDefinition = def;
			else
			{
				var table = DkDict.Dict.GetTable(text);
				if (table != null) this.SourceDefinition = table.BaseDefinition;
			}
		}
	}
}
