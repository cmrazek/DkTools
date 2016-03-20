using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal class RelIndFieldToken : WordToken
	{
		DkDict.Column _field;

		public RelIndFieldToken(Scope scope, Span span, string text, DkDict.Column field)
			: base(scope, span, text)
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
