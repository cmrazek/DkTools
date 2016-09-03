using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens.Operators;

namespace DkTools.CodeModel.Tokens
{
	class ExpressionToken : GroupToken
	{
		private ExpressionToken(Scope scope)
			: base(scope)
		{
		}

		public delegate Token WordTokenCallback(string word, Span wordSpan);

		public static ExpressionToken TryParse(Scope scope, IEnumerable<string> endTokens, WordTokenCallback wordCallback = null, DataType expectedDataType = null)
		{
			var code = scope.Code;
			code.SkipWhiteSpace();
			var startPos = code.Position;

			// Statement breaking tokens
			if (code.PeekExact(';') || code.PeekExact('{') || code.PeekExact('}')) return null;

			// Caller-specific end tokens
			if (endTokens != null)
			{
				code.Peek();
				if (endTokens.Contains(code.Text)) return null;
			}

			var exp = new ExpressionToken(scope);

			var dataTypeArgs = new DataType.ParseArgs
			{
				Code = code,
				DataTypeCallback = (name) =>
				{
					return scope.DefinitionProvider.GetAny<DataTypeDefinition>(code.Position, name).FirstOrDefault();
				},
				VariableCallback = (name) =>
				{
					return scope.DefinitionProvider.GetLocal<VariableDefinition>(code.Position, name).FirstOrDefault();
				},
				Scope = scope,
				TokenCreateCallback = (token) =>
				{
					exp.AddToken(token);
				},
				VisibleModel = true
			};

			var abortParsing = false;

			while (!code.EndOfFile && !abortParsing)
			{
				// Statement breaking tokens
				if (code.PeekExact(';') || code.PeekExact('{') || code.PeekExact('}')) break;
				if (endTokens != null && code.Peek() && endTokens.Contains(code.Text)) break;

				if (!code.Read()) break;

				switch (code.Type)
				{
					case CodeType.Word:
						{
							var word = code.Text;
							var wordSpan = code.Span;

							Token token;
							if (wordCallback != null && (token = wordCallback(word, wordSpan)) != null)
							{
								exp.AddToken(token);
							}
							else if (expectedDataType != null && expectedDataType.HasCompletionOptions && expectedDataType.IsValidEnumOption(word))
							{
								exp.AddToken(new EnumOptionToken(scope, wordSpan, word, expectedDataType));
							}
							else
							{
								var oldPos = code.Position;
								code.Position = wordSpan.Start;	// DataType.TryParse() needs to be before the first word
								var dt = DataType.TryParse(dataTypeArgs);
								if (dt == null)
								{
									code.Position = oldPos;
									var wordToken = ProcessWord(exp, scope, word, wordSpan);
									if (wordToken != null) exp.AddToken(wordToken);
									else
									{
										code.Position = wordSpan.Start;
										abortParsing = true;
									}
								}
							}
						}
						break;

					case CodeType.Number:
						exp.AddToken(new NumberToken(scope, code.Span, code.Text));
						break;

					case CodeType.StringLiteral:
						if (expectedDataType != null && expectedDataType.HasCompletionOptions && expectedDataType.IsValidEnumOption(code.Text))
						{
							exp.AddToken(new EnumOptionToken(scope, code.Span, code.Text, expectedDataType));
						}
						else
						{
							exp.AddToken(new StringLiteralToken(scope, code.Span, code.Text));
						}
						break;

					case CodeType.Operator:
						switch (code.Text)
						{
							case "(":
								{
									code.Position = code.Span.Start;
									var bracketsToken = BracketsToken.Parse(scope);
									if (bracketsToken.IsCast) exp.AddToken(Statements.CastStatement.Parse(scope, bracketsToken, endTokens));
									else exp.AddToken(bracketsToken);
								}
								break;
							case "{":
								code.Position = code.Span.Start;
								exp.AddToken(BracesToken.Parse(scope, null));
								break;
							case "[":
								code.Position = code.Span.Start;
								exp.AddToken(ArrayBracesToken.Parse(scope));
								break;
							case ",":
								exp.AddToken(new DelimiterToken(scope, code.Span));
								break;
							case ".":
								exp.AddToken(new DotToken(scope, code.Span));
								break;
							case "&":
								exp.AddToken(new ReferenceToken(scope, code.Span));
								break;
							case "==":
							case "!=":
							case "<":
							case "<=":
							case ">":
							case ">=":
								if ((scope.Hint & ScopeHint.SuppressLogic) == 0)
								{
									exp.AddToken(ComparisonOperator.Parse(scope, exp.LastChild, new OperatorToken(scope, code.Span, code.Text), endTokens));
								}
								else
								{
									exp.AddToken(new OperatorToken(scope, code.Span, code.Text));
								}
								break;
							case "=":
							case "+=":
							case "-=":
							case "*=":
							case "/=":
							case "%=":
								if ((scope.Hint & ScopeHint.SuppressLogic) == 0)
								{
									exp.AddToken(AssignmentOperator.Parse(scope, exp.LastChild, new OperatorToken(scope, code.Span, code.Text), endTokens));
								}
								else
								{
									exp.AddToken(new OperatorToken(scope, code.Span, code.Text));
								}
								break;
							default:
								exp.AddToken(new OperatorToken(scope, code.Span, code.Text));
								break;
						}
						break;

					case CodeType.Preprocessor:
						exp.AddToken(new PreprocessorToken(scope, code.Span, code.Text));
						break;

					default:
						exp.AddToken(new UnknownToken(scope, code.Span, code.Text));
						break;
				}
			}

			if (exp.ChildrenCount == 0) return null;
			return exp;
		}

		private static Token ProcessWord(ExpressionToken exp, Scope scope, string word, Span wordSpan)
		{
			// Global keyword that take effect anywhere.
			switch (word)
			{
				case "static":
					return new KeywordToken(scope, wordSpan, word);
			}

			var code = scope.Code;

			if (code.PeekExact('.'))
			{
				var dotSpan = code.MovePeekedSpan();
				var word2 = code.PeekWordR();
				if (!string.IsNullOrEmpty(word2))
				{
					var word2Span = code.MovePeekedSpan();
					var argsPresent = (scope.Hint & ScopeHint.SuppressFunctionCall) == 0 && code.PeekExact('(');
					var argsOpenBracketSpan = argsPresent ? code.MovePeekedSpan() : Span.Empty;

					foreach (var def in scope.DefinitionProvider.GetAny(wordSpan.Start, word))
					{
						if (def.AllowsChild)
						{
							// When arguments are present, take only the definitions that accept arguments
							var childDefs = def.GetChildDefinitions(word2).Where(x => argsPresent ? x.ArgumentsRequired : true).ToArray();
							if (childDefs.Any())
							{
								ArgsToken argsToken = null;
								Definition childDef = null;

								if (argsPresent)
								{
									var openBracketToken = new OperatorToken(scope, argsOpenBracketSpan, "(");
									argsToken = ArgsToken.ParseAndChooseArguments(scope, openBracketToken, childDefs, out childDef);
								}
								else
								{
									childDef = childDefs[0];
								}

								var word1Token = new IdentifierToken(scope, wordSpan, word, def);
								var dotToken = new DotToken(scope, dotSpan);
								var word2Token = new IdentifierToken(scope, word2Span, word2, childDef);
								var compToken = new CompositeToken(scope, childDef.DataType);
								compToken.AddToken(word1Token);
								compToken.AddToken(dotToken);
								compToken.AddToken(word2Token);
								if (argsToken != null) compToken.AddToken(argsToken);
								return compToken;
							}

							// TODO: remove
							//foreach (var childDef in def.GetChildDefinitions(word2))
							//{
							//	var childRequiresArgs = childDef.ArgumentsRequired;
							//	if (childRequiresArgs && (scope.Hint & ScopeHint.SuppressFunctionCall) != 0) childRequiresArgs = false;

							//	var word1Token = new IdentifierToken(scope, wordSpan, word, def);
							//	var dotToken = new DotToken(scope, dotSpan);
							//	var word2Token = new IdentifierToken(scope, word2Span, word2, childDef);
							//	var compToken = new CompositeToken(scope, childDef.DataType);
							//	compToken.AddToken(word1Token);
							//	compToken.AddToken(dotToken);
							//	compToken.AddToken(word2Token);

							//	if (argsPresent && childRequiresArgs)
							//	{
							//		var openBracketToken = new OperatorToken(scope, argsOpenBracketSpan, "(");
							//		compToken.AddToken(ArgsToken.Parse(scope, openBracketToken, childDef.Arguments));
							//	}

							//	return compToken;
							//}
						}
					}
				}
				else
				{
					code.Position = dotSpan.Start;
					return new UnknownToken(scope, wordSpan, word);
				}
			}

			if ((scope.Hint & ScopeHint.SuppressFunctionCall) == 0 && code.PeekExact('('))
			{
				var argsOpenBracketSpan = code.MovePeekedSpan();

				foreach (var def in scope.DefinitionProvider.GetAny(wordSpan.Start, word))
				{
					if (def.ArgumentsRequired)
					{
						var wordToken = new IdentifierToken(scope, wordSpan, word, def);
						var openBracketToken = new OperatorToken(scope, argsOpenBracketSpan, "(");
						var argsToken = ArgsToken.Parse(scope, openBracketToken, def.ArgumentsSignature);

						var compToken = new CompositeToken(scope, def.DataType);
						compToken.AddToken(wordToken);
						compToken.AddToken(argsToken);

						if (def.AllowsFunctionBody && (scope.Hint & ScopeHint.SuppressFunctionDefinition) == 0)
						{
							ParseFunctionAttributes(exp, scope, compToken);
							if (code.PeekExact('{')) compToken.AddToken(BracesToken.Parse(scope, def, argsToken.Span.End + 1));
						}

						return compToken;
					}
				}
			}

			foreach (var def in scope.DefinitionProvider.GetAny(wordSpan.Start, word))
			{
				if (def.ArgumentsRequired || def.RequiresChild) continue;

				return new IdentifierToken(scope, wordSpan, word, def);
			}

			if (StatementToken.IsStatementBreakingWord(scope, word))
			{
				// There could be a statement without a terminating ';' before this.
				// This can happen if it's a macro that already includes the ';'.
				return null;
			}

			if (Constants.HighlightKeywords.Contains(word))
			{
				return new KeywordToken(scope, wordSpan, word);
			}

			return new UnknownToken(scope, wordSpan, word);
		}

		private static readonly string[] _functionAttribsEndTokens = new string[] { "description", "prompt", "comment", "nomenu", "BEGINHLP", "ENDHLP", "tag" };

		private static void ParseFunctionAttributes(ExpressionToken exp, Scope scope, CompositeToken funcToken)
		{
			var code = scope.Code;
			string word;
			ExpressionToken exp2;

			while (true)
			{
				word = code.PeekWordR();
				switch (word)
				{
					case "description":
					case "prompt":
					case "comment":
					case "accel":
						funcToken.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), code.Text));
						exp2 = ExpressionToken.TryParse(scope, _functionAttribsEndTokens);
						if (exp2 != null) funcToken.AddToken(exp2);
						break;

					case "nomenu":
						funcToken.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), code.Text));
						break;

					case "BEGINHLP":
						funcToken.AddToken(new KeywordToken(scope, code.MovePeekedSpan(), code.Text));
						while (code.ReadStringLiteral())
						{
							funcToken.AddToken(new StringLiteralToken(scope, code.Span, code.Text));
						}
						if (code.ReadExactWholeWord("ENDHLP")) funcToken.AddToken(new KeywordToken(scope, code.Span, code.Text));
						break;

					case "tag":
						if (code.ReadTagName()) funcToken.AddToken(new KeywordToken(scope, code.Span, code.Text));
						exp2 = ExpressionToken.TryParse(scope, _functionAttribsEndTokens);
						if (exp2 != null) funcToken.AddToken(exp2);
						break;

					default:
						return;
				}
			}
		}

		public override DataType ValueDataType
		{
			get
			{
				if (ChildrenCount > 0) return Children.First().ValueDataType;
				return base.ValueDataType;
			}
		}

		public override bool IsDataTypeDeclaration
		{
			get
			{
				return ChildrenCount > 0 && Children.First().IsDataTypeDeclaration;
			}
		}
	}
}
