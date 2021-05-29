using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
	public class AssignmentOperator : CompletionOperator
	{
		private DataType _expectedDataType;

		internal AssignmentOperator(Scope scope)
			: base(scope)
		{
		}

		internal static AssignmentOperator Parse(Scope scope, Token lastToken, OperatorToken opToken, IEnumerable<string> endTokens)
		{
			var ret = new AssignmentOperator(scope);

			if (lastToken != null)
			{
				ret._expectedDataType = lastToken.ValueDataType;
				if (ret._expectedDataType != null) ret.AddToken(lastToken);
			}

			ret.AddToken(opToken);

			var rightExp = ExpressionToken.TryParse(scope, endTokens, expectedDataType: ret._expectedDataType);
			if (rightExp != null) ret.AddToken(rightExp);

			return ret;
		}

		public override DataType CompletionDataType
		{
			get { return _expectedDataType; }
		}

		public override DataType ValueDataType
		{
			get
			{
				return _expectedDataType;
			}
		}
	}
}
