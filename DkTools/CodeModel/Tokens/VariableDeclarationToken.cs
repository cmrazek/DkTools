using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class VariableDeclarationToken : GroupToken
	{
		private Token _dataTypeToken;
		private List<IdentifierToken> _nameTokens = new List<IdentifierToken>();
		private bool _args;

		private VariableDeclarationToken(GroupToken parent, Scope scope, Position startPos, DataTypeToken dataTypeToken, bool args)
			: base(parent, scope, startPos)
		{
			_dataTypeToken = dataTypeToken;
			_args = args;
		}

		protected override void OnChildTokenAdded(Token child)
		{
			if (child is IdentifierToken && CreateDefinitions)
			{
				var def = new VariableDefinition(Scope, child.Text, child, DataType.FromToken(_dataTypeToken), _args);
				AddDefinition(def);
				child.SourceDefinition = def;
			}
		}

		public override bool BreaksStatement
		{
			get { return true; }
		}

		public static VariableDeclarationToken TryParse(GroupToken parent, Scope scope)
		{
			if (scope.Hint.HasFlag(ScopeHint.SuppressVarDecl)) return null;

			var file = scope.File;

			var dataTypeToken = DataTypeToken.TryParse(parent, scope);
			if (dataTypeToken == null) return null;

			Token refToken = null;
			if (scope.Hint.HasFlag(ScopeHint.FunctionArgs))
			{
				refToken = ReferenceToken.TryParse(parent, scope);
			}

			var nameToken = IdentifierToken.TryParse(parent, scope);
			if (nameToken == null) { file.Position = dataTypeToken.Span.Start; return null; }

			var ret = new VariableDeclarationToken(parent, scope, file.Position, dataTypeToken, (scope.Hint & ScopeHint.FunctionArgs) != 0);
			if (dataTypeToken != null) ret.AddToken(dataTypeToken);
			if (refToken != null) ret.AddToken(refToken);
			ret.AddToken(nameToken);

			Token token;

			if (!scope.Hint.HasFlag(ScopeHint.FunctionArgs))
			{
				while (file.SkipWhiteSpaceAndComments(scope))
				{
					if ((token = StatementEndToken.TryParse(ret, scope)) != null)
					{
						ret.AddToken(token);
						break;
					}

					if ((token = DelimiterToken.TryParse(ret, scope)) != null)
					{
						ret.AddToken(token);
						continue;
					}

					if ((token = ArrayBracesToken.TryParse(ret, scope)) != null)
					{
						ret.AddToken(token);
						continue;
					}

					if ((token = IdentifierToken.TryParse(ret, scope)) != null)
					{
						ret.AddToken(token);
						continue;
					}

					break;
				}
			}
			else
			{
				if ((token = ArrayBracesToken.TryParse(ret, scope)) != null)
				{
					ret.AddToken(token);
				}
			}

			return ret;
		}
	}
}
