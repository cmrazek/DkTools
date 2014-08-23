using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class TableFieldToken : WordToken
	{
		public TableFieldToken(GroupToken parent, Scope scope, Span span, string text, TableToken tableToken)
			: base(parent, scope, span, text)
		{
#if DEBUG
			if (tableToken == null) throw new ArgumentNullException();
#endif
			var table = ProbeEnvironment.GetTable(tableToken.Text);
			if (table != null)
			{
				var field = table.GetField(text);
				if (field != null) this.SourceDefinition = field.Definition;
			}

			ClassifierType = Classifier.ProbeClassifierType.TableField;
		}
	}
}
