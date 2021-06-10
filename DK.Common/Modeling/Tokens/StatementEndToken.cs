using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class StatementEndToken : Token
	{
		internal StatementEndToken(Scope scope, CodeSpan span)
			: base(scope, span)
		{
			ClassifierType = ProbeClassifierType.Operator;
		}

		public override bool BreaksStatement
		{
			get { return true; }
		}

		public override string Text
		{
			get
			{
				return ";";
			}
		}
	}
}
