using DK.Code;
using DK.Schema;
using DK.Syntax;
using System;

namespace DK.Modeling.Tokens
{
	public class RelIndFieldToken : WordToken
	{
		Column _field;

		internal RelIndFieldToken(Scope scope, CodeSpan span, string text, Column field)
			: base(scope, span, text)
		{
#if DEBUG
			if (field == null) throw new ArgumentNullException("field");
#endif
			_field = field;

			SourceDefinition = field.Definition;
			ClassifierType = ProbeClassifierType.TableField;
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
