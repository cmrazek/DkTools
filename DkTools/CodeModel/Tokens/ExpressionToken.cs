using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	class ExpressionToken : GroupToken
	{
		private ExpressionToken(Scope scope)
			: base(scope)
		{
		}

		public delegate Token WordTokenCallback(string word, Span wordSpan);

		public static ExpressionToken TryParse(Scope scope, IEnumerable<string> endTokens, WordTokenCallback wordCallback = null)
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
				}
			};

			while (!code.EndOfFile)
			{
				// Statement breaking tokens
				if (code.PeekExact(';') || code.PeekExact('{') || code.PeekExact('}')) return exp;
				if (endTokens != null)
				{
					if (code.Peek() && endTokens.Contains(code.Text)) return exp;
				}

				var dt = DataType.TryParse(dataTypeArgs);
				if (dt != null) continue;

				if (!code.Read()) break;

				switch (code.Type)
				{
					case TokenParser.TokenType.Word:
						{
							var word = code.Text;
							var wordSpan = code.Span;

							Token token;
							if (wordCallback != null && (token = wordCallback(word, wordSpan)) != null) exp.AddToken(token);
							else exp.AddToken(ProcessWord(exp, scope, word, wordSpan));
						}
						break;

					case TokenParser.TokenType.Number:
						exp.AddToken(new NumberToken(scope, code.Span, code.Text));
						break;

					case TokenParser.TokenType.StringLiteral:
						exp.AddToken(new StringLiteralToken(scope, code.Span, code.Text));
						break;

					case TokenParser.TokenType.Operator:
						switch (code.Text)
						{
							case "(":
								code.Position = code.Span.Start;
								exp.AddToken(BracketsToken.Parse(scope));
								break;
							case "{":
								code.Position = code.Span.Start;
								exp.AddToken(BracesToken.Parse(scope));
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
							default:
								exp.AddToken(new OperatorToken(scope, code.Span, code.Text));
								break;
						}
						break;

					case TokenParser.TokenType.Preprocessor:
						exp.AddToken(new PreprocessorToken(scope, code.Span, code.Text));
						break;

					default:
						exp.AddToken(new UnknownToken(scope, code.Span, code.Text));
						break;
				}
			}

			return exp;
		}

		private static Token ProcessWord(ExpressionToken exp, Scope scope, string word, Span wordSpan)
		{
			var code = scope.Code;

			if (code.PeekExact('.'))
			{
				var dotSpan = code.MovePeekedSpan();
				var word2 = code.PeekWord();
				if (!string.IsNullOrEmpty(word2))
				{
					var word2Span = code.MovePeekedSpan();
					var argsPresent = code.PeekExact('(');
					var argsOpenBracketSpan = argsPresent ? code.MovePeekedSpan() : Span.Empty;

					foreach (var def in scope.DefinitionProvider.GetAny(wordSpan.Start, word))
					{
						if (def.AllowsChild)
						{
							var childDef = def.GetChildDefinition(word2);
							if (childDef != null)
							{
								if (argsPresent == childDef.RequiresArguments)
								{
									var word1Token = new IdentifierToken(scope, wordSpan, word, def);
									var dotToken = new DotToken(scope, dotSpan);
									var word2Token = new IdentifierToken(scope, word2Span, word2, childDef);
									var compToken = new CompositeToken(scope);
									compToken.AddToken(word1Token);
									compToken.AddToken(dotToken);
									compToken.AddToken(word2Token);

									if (argsPresent)
									{
										var openBracketToken = new OperatorToken(scope, argsOpenBracketSpan, "(");
										compToken.AddToken(ArgsToken.Parse(scope, openBracketToken));
									}

									return compToken;
								}
							}
						}
					}
				}
				else
				{
					code.Position = dotSpan.Start;
					return new UnknownToken(scope, wordSpan, word);
				}
			}

			if (code.PeekExact('('))
			{
				var argsOpenBracketSpan = code.MovePeekedSpan();

				foreach (var def in scope.DefinitionProvider.GetAny(wordSpan.Start, word))
				{
					if (def.RequiresArguments)
					{
						var wordToken = new IdentifierToken(scope, wordSpan, word, def);
						var openBracketToken = new OperatorToken(scope, argsOpenBracketSpan, "(");
						var argsToken = ArgsToken.Parse(scope, openBracketToken);

						var compToken = new CompositeToken(scope);
						compToken.AddToken(wordToken);
						compToken.AddToken(argsToken);

						if (def.AllowsFunctionBody)
						{
							ParseFunctionAttributes(exp, scope, compToken);
							if (code.PeekExact('{')) compToken.AddToken(BracesToken.Parse(scope, argsToken.Span.End + 1));
						}

						return compToken;
					}
				}
			}

			foreach (var def in scope.DefinitionProvider.GetAny(wordSpan.Start, word))
			{
				if (def.RequiresArguments || def.RequiresChild) continue;

				return new IdentifierToken(scope, wordSpan, word, def);
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
				word = code.PeekWord();
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
	}
}
