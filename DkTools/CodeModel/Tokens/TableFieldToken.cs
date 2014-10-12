using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class TableFieldToken : WordToken
	{
		private Dict.Field _field;

		public TableFieldToken(GroupToken parent, Scope scope, Span span, string text, Dict.Field field)
			: base(parent, scope, span, text)
		{
#if DEBUG
			if (field == null) throw new ArgumentNullException("field");
#endif
			_field = field;

			SourceDefinition = field.Definition;
			ClassifierType = Classifier.ProbeClassifierType.TableField;
		}

		public override DataType ValueDataType
		{
			get
			{
				return _field.DataType;
			}
		}
	}
}
