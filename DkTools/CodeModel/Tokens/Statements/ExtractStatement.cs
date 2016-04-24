using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens.Statements
{
	internal class ExtractStatement : GroupToken
	{
		private string _name;
		private bool _permanent;

		private ExtractStatement(Scope scope, IEnumerable<Token> tokens)
			: base(scope)
		{
			foreach (var token in tokens) AddToken(token);
		}

		public static ExtractStatement Parse(Scope scope, KeywordToken extractKeywordToken)
		{
			var code = scope.Code;

			var tokens = new List<Token>();
			tokens.Add(extractKeywordToken);

			var permanent = false;
			if (code.ReadExactWholeWord("permanent"))
			{
				var permToken = new KeywordToken(scope, code.Span, "permanent");
				tokens.Add(permToken);
				permanent = true;
			}

			ExtractTableDefinition exDef = null;
			string name = null;

			if (!code.ReadWord())
			{
				// Not correct syntax; exit here.
				return new ExtractStatement(scope, tokens);
			}
			else if ((exDef = scope.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(code.Text).FirstOrDefault()) == null)
			{
				// The extract definition would have been added by the preprocessor. If it's not recognized here, then it's incorrect syntax.
				return new ExtractStatement(scope, tokens);
			}
			else
			{
				name = code.Text;
				tokens.Add(new ExtractTableToken(scope, code.Span, name, exDef));
			}

			var ret = new ExtractStatement(scope, tokens);
			ret._name = name;
			ret._permanent = permanent;
			//ret.SourceDefinition = exDef;

			var scope2 = scope.CloneIndent();
			scope2.Hint |= ScopeHint.SuppressControlStatements | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl;

			var fieldTokens = new List<ExtractFieldToken>();

			while (!code.EndOfFile)
			{
				if (code.ReadExact(';'))
				{
					ret.AddToken(new StatementEndToken(scope2, code.Span));
					break;
				}

				var exp = ExpressionToken.TryParse(scope, null, (parseWord, parseWordSpan) =>
				{
					if (!code.PeekExact("==") && code.PeekExact('='))
					{
						var fieldDef = exDef.GetField(parseWord);
						if (fieldDef != null)
						{
							var fieldToken = new ExtractFieldToken(scope, parseWordSpan, parseWord, fieldDef);
							fieldTokens.Add(fieldToken);

							var equalsToken = new OperatorToken(scope, code.MovePeekedSpan(), code.Text);

							// Return the field token and the '='; otherwise the AssignmentOperator parsing will consume the next field's token.
							return new CompositeToken(scope, fieldDef.DataType, fieldToken, equalsToken);
						}
					}

					return null;
				});
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			// Try to get the data types for the fields
			foreach (var fieldToken in fieldTokens)
			{
				var fieldDef = fieldToken.SourceDefinition as ExtractFieldDefinition;
				if (fieldDef == null) continue;
				if (fieldDef.DataType != null) continue;	// Data type already known, so don't bother

				var compToken = fieldToken.Parent as CompositeToken;
				if (compToken == null) continue;

				var expToken = compToken.Parent as ExpressionToken;
				if (expToken == null) continue;

				var valueToken = expToken.FindNextSibling(compToken);
				if (valueToken == null) continue;

				var dataType = valueToken.ValueDataType;
				if (dataType != null) fieldDef.SetDataType(dataType);
			}

			return ret;
		}

		public string Name
		{
			get { return _name; }
		}

		public IEnumerable<ExtractFieldDefinition> Fields
		{
			get
			{
				if (SourceDefinition != null && SourceDefinition is ExtractTableDefinition)
				{
					return (SourceDefinition as ExtractTableDefinition).Fields;
				}
				return new ExtractFieldDefinition[0];
			}
		}

		public bool IsPermanent
		{
			get { return _permanent; }
		}
	}
}
