using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Operators
{
	class ConditionalOperator : CompletionOperator
	{
		private DataType _completionDataType;

		public ConditionalOperator(Scope scope)
			: base(scope)
		{
		}

		private static readonly string[] _endTokens = new string[] { ":" };

		public static ConditionalOperator Parse(Scope scope, Token lastToken, OperatorToken opToken, IEnumerable<string> endTokens, DataType expectedDataType)
		{
			var ret = new ConditionalOperator(scope);

			if (lastToken != null) ret.AddToken(lastToken);
			ret.AddToken(opToken);

			if (endTokens == null) endTokens = _endTokens;
			else endTokens = endTokens.Concat(_endTokens).ToArray();

			var leftResultExp = ExpressionToken.TryParse(scope, endTokens, expectedDataType: expectedDataType);
			if (leftResultExp != null)
			{
				ret.AddToken(leftResultExp);
				ret._completionDataType = leftResultExp.ValueDataType;

				if (scope.Code.ReadExact(':'))
				{
					var rightResultExp = ExpressionToken.TryParse(scope, endTokens, expectedDataType: expectedDataType);
					if (rightResultExp != null)
					{
						ret.AddToken(rightResultExp);
					}
				}
			}

			return ret;
		}

		public override DataType ValueDataType
		{
			get
			{
				return _completionDataType;
			}
		}

		public override DataType CompletionDataType
		{
			get { return _completionDataType; }
		}
	}
}
