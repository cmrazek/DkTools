using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class VariableDeclarationToken : GroupToken
	{
		private Token _dataTypeToken;
		private List<IdentifierToken> _nameTokens = new List<IdentifierToken>();

		private VariableDeclarationToken(GroupToken parent, Scope scope, Position startPos, DataTypeToken dataTypeToken)
			: base(parent, scope, startPos)
		{
			_dataTypeToken = dataTypeToken;
		}

		protected override void OnChildTokenAdded(Token child)
		{
			if (child is IdentifierToken)
			{
				var def = new VariableDefinition(child.Text, child, DataType.FromToken(_dataTypeToken));
				this.AddDefinition(def);
				child.SourceDefinition = def;
			}
		}

		public override bool BreaksStatement
		{
			get
			{
				return true;
			}
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

			var ret = new VariableDeclarationToken(parent, scope, file.Position, dataTypeToken);
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
