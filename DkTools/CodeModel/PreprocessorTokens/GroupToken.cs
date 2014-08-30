using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.PreprocessorTokens
{
	internal class GroupToken : Token
	{
		private List<Token> _tokens = new List<Token>();
		private long? _value;
		private bool _calcValue;

		public GroupToken(GroupToken parent)
			: base(parent)
		{
		}

		public static Token Parse(GroupToken parent, TokenParser.Parser parser, string endToken)
		{
			var token = new GroupToken(parent);

			while (parser.Read())
			{
				if (endToken != null && parser.TokenText == endToken) break;

				var type = parser.TokenType;
				if (type == TokenParser.TokenType.Operator)
				{
					if (parser.TokenText == "(")
					{
						token.Add(GroupToken.Parse(token, parser, ")"));
					}
					else
					{
						token.Add(new OperatorToken(token, parser.TokenText));
					}
				}
				else if (type == TokenParser.TokenType.Number)
				{
					token.Add(new NumberToken(token, parser.TokenText));
				}
				else
				{
					token.Add(new NumberToken(token, (long?)null));
				}
			}

			return token;
		}

		public void Add(Token token)
		{
#if DEBUG
			if (token == null) throw new ArgumentNullException("token");
#endif
			_tokens.Add(token);
		}

		public Token GetTokenOnLeft(Token token)
		{
			Token lastToken = null;
			foreach (var t in _tokens)
			{
				if (t == token) return lastToken;
				lastToken = t;
			}
			throw new InvalidOperationException("Token does not belong to this group.");
		}

		public Token GetTokenOnRight(Token token)
		{
			var found = false;
			foreach (var t in _tokens)
			{
				if (found) return t;
				if (t == token) found = true;
			}
			if (!found) throw new InvalidOperationException("Token does not belong to this group.");
			return null;
		}

		public void ReplaceTokens(Token newToken, params Token[] oldTokens)
		{
			var minIndex = -1;
			foreach (var tok in oldTokens)
			{
				var index = _tokens.FindIndex(t => t == tok);
				if (index == -1) throw new InvalidOperationException("Token does not belong to this group.");
				if (minIndex == -1 || index < minIndex) minIndex = index;

				_tokens.RemoveAt(index);
			}

			if (minIndex < 0) throw new InvalidOperationException("No tokens removed.");
			_tokens.Insert(minIndex, newToken);
		}

		public override long? Value
		{
			get
			{
				if (!_calcValue)
				{
					_value = CalcValue();
					_calcValue = true;
				}
				return _value;
			}
		}

		private long? CalcValue()
		{
			int maxPrecedence = 1;
			OperatorToken op;
			bool foundToken;

			while (maxPrecedence > 0)
			{
				// Find the highest precedence.
				maxPrecedence = 0;
				foreach (var tok in _tokens)
				{
					if ((op = tok as OperatorToken) != null)
					{
						if (op.Precedence > maxPrecedence) maxPrecedence = op.Precedence;
					}
				}
				if (maxPrecedence == 0) break;

				// Execute all operators with the same precedence.
				// Don't need to worry about associativity for preprocessor operators (all are left-to-right)
				foundToken = true;
				while (foundToken)
				{
					foundToken = false;
					foreach (var tok in _tokens)
					{
						if ((op = tok as OperatorToken) != null)
						{
							if (op.Precedence == maxPrecedence)
							{
								foundToken = true;
								op.Execute();
								break;
							}
						}
					}
				}
			}

			// If syntax is correct, there should only be a single token remaining at the end.
			if (_tokens.Count != 1)
			{
				Log.WriteDebug("Syntax error.");
				return null;
			}
			return _tokens[0].Value;
		}
	}
}
