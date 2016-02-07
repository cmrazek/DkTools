using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Operators
{
	class AssignmentOperator : CompletionOperator
	{
		private DataType _expectedDataType;

		public AssignmentOperator(Scope scope)
			: base(scope)
		{
		}

		public static AssignmentOperator Parse(Scope scope, Token lastToken, OperatorToken opToken, IEnumerable<string> endTokens)
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
