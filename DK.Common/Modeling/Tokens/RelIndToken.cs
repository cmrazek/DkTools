using DK.Code;
using DK.Definitions;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class RelIndToken : WordToken
	{
		internal RelIndToken(Scope scope, CodeSpan span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = ProbeClassifierType.TableName;
			SourceDefinition = def;
		}

		public override DataType ValueDataType
		{
			get
			{
				return DataType.IndRel;
			}
		}
	}
}
