using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class ComparisonOperator : CompletionOperator
	{
		private DataType _expectedDataType;

		public ComparisonOperator(Scope scope)
			: base(scope)
		{
		}

		private static readonly string[] _endTokens = new string[] { "and", "or", "?", ":", "=", "+=", "-=", "*=", "/=", "%=" };

		public static ComparisonOperator Parse(Scope scope, Token lastToken, OperatorToken opToken, IEnumerable<string> endTokens)
		{
			var ret = new ComparisonOperator(scope);

			if (lastToken != null)
			{
				ret._expectedDataType = lastToken.ValueDataType;
				if (ret._expectedDataType != null) ret.AddToken(lastToken);
			}

			ret.AddToken(opToken);

			if (endTokens == null || !endTokens.Any()) endTokens = _endTokens;
			else endTokens = endTokens.Concat(_endTokens).ToArray();

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
