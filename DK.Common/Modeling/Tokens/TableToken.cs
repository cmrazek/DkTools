using DK.Code;
using DK.Definitions;
using DK.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DK.Modeling.Tokens
{
	public class TableToken : WordToken
	{
		internal TableToken(Scope scope, CodeSpan span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = ProbeClassifierType.TableName;

			if (def != null) SourceDefinition = def;
			else
			{
				var table = scope.AppSettings.Dict.GetTable(text);
				if (table != null) this.SourceDefinition = table.Definition;
			}
		}

		public override DataType ValueDataType
		{
			get
			{
				return DataType.Table;
			}
		}
	}
}
