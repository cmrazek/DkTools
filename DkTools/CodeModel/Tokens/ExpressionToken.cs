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
		private ExpressionToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, startPos)
		{
		}

		public delegate Token WordTokenCallback(string word, Span wordSpan);

		public static ExpressionToken TryParse(GroupToken parent, Scope scope, IEnumerable<string> endTokens, WordTokenCallback wordCallback = null)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);
			var startPos = file.Position;

			// Statement breaking tokens
			if (file.IsMatch(';')) return null;
			if (file.IsMatch('{')) return null;
			if (file.IsMatch('}')) return null;

			// Caller-specific end tokens
			if (endTokens != null && file.IsWholeMatch(endTokens)) return null;

			var exp = new ExpressionToken(parent, scope, startPos);

			while (file.SkipWhiteSpaceAndComments(scope))
			{
				// Statement breaking tokens
				var ch = file.PeekChar();
				if (ch == ';' || ch == '{' || ch == '}') return exp;

				// Caller-specific end tokens
				if (endTokens != null && file.IsWholeMatch(endTokens)) return exp;

				if (ch.IsWordChar(true))
				{
					var word = file.PeekWord();
					var wordSpan = file.MoveNextSpan(word.Length);

					Token token;
					if (wordCallback != null && (token = wordCallback(word, wordSpan)) != null) exp.AddToken(token);
					else exp.AddToken(ProcessWord(exp, scope, word, wordSpan));
				}
				else if (char.IsDigit(ch))
				{
					exp.AddToken(NumberToken.Parse(exp, scope));
				}
				else if (ch == '\"' || ch == '\'')
				{
					file.ParseStringLiteral();
					var span = new Span(startPos, file.Position);
					exp.AddToken(new StringLiteralToken(exp, scope, span, file.GetText(span)));
				}
				else if (ch == '-')
				{
					if (char.IsDigit(file.PeekChar(1)))
					{
						// Number with leading minus sign
						file.ParseNumber();
						var span = new Span(startPos, file.Position);
						exp.AddToken(new NumberToken(exp, scope, span, file.GetText(span)));
					}
					else if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "-="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "-"));
				}
				else if (ch == '+')
				{
					if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "+="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "+"));
				}
				else if (ch == '*')
				{
					if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "*="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "*"));
				}
				else if (ch == '/')
				{
					if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "/="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "/"));
				}
				else if (ch == '%')
				{
					if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "%="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "%"));
				}
				else if (ch == '=')
				{
					if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "=="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "="));
				}
				else if (ch == '!')
				{
					if (file.PeekChar(1) == '=') exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(2), "!="));
					else exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), "!"));	// Not technically a probe operator...
				}
				else if (ch == '(')
				{
					exp.AddToken(BracketsToken.Parse(exp, scope));
				}
				else if (ch == ')')
				{
					exp.AddToken(new CloseBracketToken(exp, scope, file.MoveNextSpan(), null));
				}
				else if (ch == '{')
				{
					exp.AddToken(BracesToken.Parse(exp, scope));
				}
				else if (ch == '}')
				{
					exp.AddToken(new BraceToken(exp, scope, file.MoveNextSpan(), null, false));
				}
				else if (ch == '[')
				{
					exp.AddToken(ArrayBracesToken.Parse(exp, scope));
				}
				else if (ch == ']')
				{
					exp.AddToken(new ArrayBraceToken(exp, scope, file.MoveNextSpan(), null, false));
				}
				else if (ch == ',')
				{
					exp.AddToken(new DelimiterToken(exp, scope, file.MoveNextSpan()));
				}
				else if (ch == '.')
				{
					exp.AddToken(new DotToken(exp, scope, file.MoveNextSpan()));
				}
				else if (ch == ';')
				{
					exp.AddToken(new StatementEndToken(exp, scope, file.MoveNextSpan()));
				}
				else if (ch == ':')
				{
					exp.AddToken(new OperatorToken(exp, scope, file.MoveNextSpan(), ":"));
				}
				else if (ch == '&')
				{
					exp.AddToken(new ReferenceToken(exp, scope, file.MoveNextSpan()));
				}
				else
				{
					exp.AddToken(new UnknownToken(exp, scope, file.MoveNextSpan(), ch.ToString()));
				}
			}

			return exp;
		}

		private static Token ProcessWord(ExpressionToken exp, Scope scope, string word, Span wordSpan)
		{
			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);

			if (file.IsMatch('.'))
			{
				var dotSpan = file.MoveNextSpan();
				file.SkipWhiteSpaceAndComments(scope);
				var word2 = file.PeekWord();
				if (!string.IsNullOrEmpty(word2))
				{
					var word2Span = file.MoveNextSpan(word2.Length);
					file.SkipWhiteSpaceAndComments(scope);
					var argsPresent = file.IsMatch('(');
					var argsOpenBracketSpan = argsPresent ? file.MoveNextSpan() : Span.Empty;

					foreach (var def in scope.DefinitionProvider.GetAny(file.Position, word))
					{
						if (def.AllowsChild)
						{
							var childDef = def.GetChildDefinition(word2);
							if (childDef != null)
							{
								if (!argsPresent || childDef.RequiresArguments)
								{
									var word1Token = new IdentifierToken(exp, scope, wordSpan, word, def);
									var dotToken = new DotToken(exp, scope, dotSpan);
									var word2Token = new IdentifierToken(exp, scope, word2Span, word2, childDef);
									if (argsPresent)
									{
										var openBracketToken = new OperatorToken(exp, scope, argsOpenBracketSpan, "(");
										var argsToken = ArgsToken.Parse(exp, scope, openBracketToken);
										return new CompositeToken(exp, scope, new Token[] { word1Token, dotToken, word2Token, argsToken });
									}
									else
									{
										return new CompositeToken(exp, scope, new Token[] { word1Token, dotToken, word2Token });
									}
								}
							}
						}
					}
				}
				else
				{
					file.Position = dotSpan.Start;
					return new UnknownToken(exp, scope, wordSpan, word);
				}
			}

			if (file.IsMatch('('))
			{
				var argsOpenBracketSpan = file.MoveNextSpan();

				foreach (var def in scope.DefinitionProvider.GetAny(file.Position, word))
				{
					if (def.RequiresArguments)
					{
						var wordToken = new IdentifierToken(exp, scope, wordSpan, word, def);
						var openBracketToken = new OperatorToken(exp, scope, argsOpenBracketSpan, "(");
						var argsToken = ArgsToken.Parse(exp, scope, openBracketToken);
						return new CompositeToken(exp, scope, new Token[] { wordToken, argsToken });
					}
				}
			}

			foreach (var def in scope.DefinitionProvider.GetAny(file.Position, word))
			{
				if (def.RequiresArguments || def.RequiresChild) continue;

				return new IdentifierToken(exp, scope, wordSpan, word, def);
			}

			return new UnknownToken(exp, scope, wordSpan, word);
		}
	}
}
