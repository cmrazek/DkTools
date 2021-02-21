using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Defs = DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class BracketsToken : GroupToken
	{
		private List<Token> _innerTokens = new List<Token>();
		private BracketToken _openToken;
		private BracketToken _closeToken;
		private DataType _cast;

		public BracketsToken(Scope scope)
			: base(scope)
		{
		}

		public BracketsToken(Scope scope, OpenBracketToken openToken, CloseBracketToken closeToken)
			: base(scope)
		{
			AddToken(openToken);
			AddToken(closeToken);
		}

		public BracketsToken(Scope scope, OpenBracketToken openToken)
			: base(scope)
		{
			AddToken(openToken);
		}

		private static readonly string[] _endTokens = new string[] { ")" };

		public static BracketsToken Parse(Scope scope, DataType expectedDataType)
		{
			var code = scope.Code;
			if (!code.ReadExact('(')) throw new InvalidOperationException("BracketsToken.Parse expected next char to be '('.");
			var openBracketSpan = code.Span;

			var indentScope = scope.CloneIndentNonRoot();
			indentScope.Hint |= ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressControlStatements | ScopeHint.SuppressStatementStarts;

			var ret = new BracketsToken(scope);
			ret.AddToken(ret._openToken = new OpenBracketToken(scope, openBracketSpan, ret));

			List<Token> dataTypeTokens = null;
			var dataType = DataType.TryParse(new DataType.ParseArgs(code, scope.AppSettings)
			{
				Scope = scope,
				DataTypeCallback = name =>
				{
					return indentScope.DefinitionProvider.GetAny<Defs.DataTypeDefinition>(openBracketSpan.End, name).FirstOrDefault();
				},
				VariableCallback = name =>
				{
					return indentScope.DefinitionProvider.GetAny<Defs.VariableDefinition>(openBracketSpan.End, name).FirstOrDefault();
				},
				TableFieldCallback = (tableName, fieldName) =>
				{
					foreach (var tableDef in indentScope.DefinitionProvider.GetGlobalFromFile(tableName))
					{
						if (tableDef.AllowsChild)
						{
							foreach (var fieldDef in tableDef.GetChildDefinitions(fieldName, indentScope.AppSettings))
							{
								return new Defs.Definition[] { tableDef, fieldDef };
							}
						}
					}

					return null;
				},
				TokenCreateCallback = token =>
				{
					if (dataTypeTokens == null) dataTypeTokens = new List<Token>();
					dataTypeTokens.Add(token);
				},
				VisibleModel = scope.Visible
			});
			if (dataType != null && code.ReadExact(')'))
			{
				ret._cast = dataType;
				if (dataTypeTokens != null)
				{
					foreach (var token in dataTypeTokens) ret.AddToken(token);
				}
				ret.AddToken(ret._closeToken = new CloseBracketToken(scope, code.Span, ret));
			}
			else
			{
				while (!code.EndOfFile)
				{
					if (code.ReadExact(')'))
					{
						ret.AddToken(ret._closeToken = new CloseBracketToken(scope, code.Span, ret));
						break;
					}

					var exp = ExpressionToken.TryParse(indentScope, _endTokens, expectedDataType: expectedDataType);
					if (exp != null)
					{
						ret._innerTokens.Add(exp);
						ret.AddToken(exp);
					}
					else break;
				}
			}

			return ret;
		}

		public BracketToken OpenToken
		{
			get { return _openToken; }
		}

		public BracketToken CloseToken
		{
			get { return _closeToken; }
		}

		public override DataType ValueDataType
		{
			get
			{
				if (_innerTokens.Count > 0) return _innerTokens[0].ValueDataType;
				return base.ValueDataType;
			}
		}

		public override string NormalizedText
		{
			get
			{
				return string.Concat("(", Token.GetNormalizedText(_innerTokens), ")");
			}
		}

		public IEnumerable<Token> InnerTokens
		{
			get { return _innerTokens; }
		}

		public void AddOpen(Span span)
		{
#if DEBUG
			if (_openToken != null) throw new InvalidOperationException("These brackets already have a opening token.");
#endif
			AddToken(_openToken = new OpenBracketToken(Scope, span, this));
		}

		public void AddClose(Span span)
		{
#if DEBUG
			if (_closeToken != null) throw new InvalidOperationException("These brackets already have a close token.");
#endif
			AddToken(_closeToken = new CloseBracketToken(Scope, span, this));
		}

		public bool IsCast
		{
			get
			{
				return _cast != null;
			}
		}

		public DataType CastDataType
		{
			get
			{
				return _cast;
			}
		}
	}

	internal abstract class BracketToken : Token, IBraceMatchingToken
	{
		private BracketsToken _bracketsToken = null;

		public BracketToken(Scope scope, Span span, BracketsToken bracketsToken)
			: base(scope, span)
		{
			_bracketsToken = bracketsToken;
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		IEnumerable<Token> IBraceMatchingToken.BraceMatchingTokens
		{
			get
			{
				yield return _bracketsToken.OpenToken;
				if (_bracketsToken.CloseToken != null) yield return _bracketsToken.CloseToken;
			}
		}
	}

	internal class OpenBracketToken : BracketToken
	{
		public OpenBracketToken(Scope scope, Span span, BracketsToken bracketsToken)
			: base(scope, span, bracketsToken)
		{
		}
	}

	internal class CloseBracketToken : BracketToken
	{
		public CloseBracketToken(Scope scope, Span span, BracketsToken bracketsToken)
			: base(scope, span, bracketsToken)
		{
		}
	}
}
