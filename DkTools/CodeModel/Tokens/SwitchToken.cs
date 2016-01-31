using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class SwitchToken : GroupToken
	{
		private ExpressionToken _expressionToken;
		private OperatorToken _compareOpToken;	// Could be null if not used
		private BracesToken _bodyToken;	// Could be null for unfinished code.

		private SwitchToken(GroupToken parent, Scope scope, KeywordToken switchToken)
			: base(parent, scope, new Token[] { switchToken })
		{
		}

		public static SwitchToken Parse(GroupToken parent, Scope scope, KeywordToken switchToken)
		{
			var file = scope.File;

			var ret = new SwitchToken(parent, scope, switchToken);

			var expressionScope = scope.Clone();
			expressionScope.Hint |= ScopeHint.SuppressDataType | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			ret._expressionToken = ExpressionToken.TryParse(ret, expressionScope, null);
			if (ret._expressionToken != null) ret.AddToken(ret._expressionToken);

			file.SkipWhiteSpaceAndComments(scope);
			if (file.IsMatch('{'))
			{
				var bodyScope = scope.Clone();
				bodyScope.Hint |= ScopeHint.SuppressFunctionDefinition;

				ret._bodyToken = new BracesToken(ret, bodyScope, file.MoveNextSpan());
				ret.AddToken(ret._bodyToken);

				while (file.SkipWhiteSpaceAndComments(bodyScope))
				{
					if (file.IsMatch('}'))
					{
						ret._bodyToken.AddCloseBrace(bodyScope, file.MoveNextSpan());
						return ret;
					}

					var stmt = StatementToken.TryParse(ret._bodyToken, bodyScope);
					if (stmt != null) ret._bodyToken.AddToken(stmt);
					else break;
				}
			}

			return ret;

			// TODO: remove
			//var expressionTokens = new List<Token>();
			//ret.ParseScope(expressionScope, t =>
			//	{
			//		if (t.BreaksStatement || t is BracesToken) return ParseScopeResult.StopAndReject;
			//		expressionTokens.Add(t);

			//		file.SkipWhiteSpaceAndComments(expressionScope);
			//		return file.PeekChar() == '{' ? ParseScopeResult.StopAndKeep : ParseScopeResult.Continue;
			//	});

			//// If the last token is an operator, then it's the compare-op.
			//if (expressionTokens.Count > 1 && expressionTokens.Last() is OperatorToken)
			//{
			//	ret._compareOpToken = expressionTokens.Last() as OperatorToken;
			//	expressionTokens.RemoveAt(expressionTokens.Count - 1);
			//}

			//ret._expressionTokens = expressionTokens.ToArray();

			//// Body
			//var bodyScope = scope.Clone();
			//bodyScope.Hint |= ScopeHint.SuppressFunctionDefinition;
			//if ((ret._bodyToken = BracesToken.TryParse(ret, bodyScope)) != null) ret.AddToken(ret._bodyToken);

			//return ret;
		}

		// TODO: remove
		//public DataType ExpressionDataType
		//{
		//	get
		//	{
		//		if (_expressionTokens.Length == 1)
		//		{
		//			var expToken = _expressionTokens[0];
		//			var dt = expToken.ValueDataType;
		//			if (dt != null) return dt;
		//		}

		//		return null;
		//	}
		//}

		public override DataType ValueDataType
		{
			get
			{
				return _expressionToken != null ? _expressionToken.ValueDataType : null;
			}
		}
	}
}
