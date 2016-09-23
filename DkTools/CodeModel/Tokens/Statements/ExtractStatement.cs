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
		private ExtractFieldDefinition[] _fields;

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
				return new ExtractStatement(scope, tokens) { _fields = new ExtractFieldDefinition[0] };
			}
			else if ((exDef = scope.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(code.Text).FirstOrDefault()) == null)
			{
				// The extract definition would have been added by the preprocessor. If it's not recognized here, then it's incorrect syntax.
				return new ExtractStatement(scope, tokens) { _fields = new ExtractFieldDefinition[0] };
			}
			else
			{
				name = code.Text;
				tokens.Add(new ExtractTableToken(scope, code.Span, name, exDef));
			}

			var ret = new ExtractStatement(scope, tokens);
			ret._name = name;
			ret._permanent = permanent;

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

				ExtractFieldDefinition fieldDef;
				if ((fieldDef = exDef.GetField(code.PeekWordR())) != null)
				{
					var fieldToken = new ExtractFieldToken(scope, code.MovePeekedSpan(), code.Text, fieldDef);
					fieldTokens.Add(fieldToken);
					ret.AddToken(fieldToken);
				}
				else break;

				if (!code.PeekExact("==") && code.PeekExact('='))
				{
					var equalsToken = new OperatorToken(scope, code.MovePeekedSpan(), code.Text);
					ret.AddToken(equalsToken);
				}
				else break;

				var oldValue = code.StopAtLineEnd;
				code.StopAtLineEnd = true;
				try
				{
					while (true)
					{
						var exp = ExpressionToken.TryParse(scope, null, expectedDataType: fieldDef.DataType);
						if (exp != null)
						{
							ret.AddToken(exp);
							if (fieldDef.DataType == null) fieldDef.SetDataType(exp.ValueDataType);
						}
						else break;
					}
				}
				finally
				{
					code.StopAtLineEnd = oldValue;
				}
			}

			ret._fields = (from f in fieldTokens select f.SourceDefinition as ExtractFieldDefinition).ToArray();

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
				return _fields;
			}
		}

		public bool IsPermanent
		{
			get { return _permanent; }
		}
	}
}
