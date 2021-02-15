using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace DkTools.BraceHighlighting
{
	internal class BraceHighlightParser
	{
		private string _source;

		private const string k_replace = "#replace";
		private const string k_with = "#with";
		private const string k_endreplace = "#endreplace";
		private const string k_insert = "#insert";
		private const string k_endinsert = "#endinsert";

		private enum TokenType
		{
			BracketOpen,
			BracketClose,
			BraceOpen,
			BraceClose,
			ArrayBraceOpen,
			ArrayBraceClose,
			ReplaceStart,
			ReplaceWith,
			ReplaceEnd,
			Insert,
			InsertEnd
		}

		private bool TokenOpen(TokenType type) => type == TokenType.BracketOpen || type == TokenType.BraceOpen || type == TokenType.ArrayBraceOpen;
		private bool TokenClose(TokenType type) => type == TokenType.BracketClose || type == TokenType.BraceClose || type == TokenType.ArrayBraceClose;

		private struct Token
		{
			public int pos;
			public int length;
			public TokenType type;
			public int id;

			public bool Intersects(int cursorPos)
			{
				return cursorPos >= pos && cursorPos <= pos + length;
			}
		}

		public IEnumerable<SnapshotSpan> FindMatchingBraces(ITextSnapshot snapshot, int cursorPos)
		{
			_source = snapshot.GetText();

			var search = false;
			if (cursorPos < _source.Length)
			{
				var ch = _source[cursorPos];
				if (ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']') search = true;
			}
			if (!search && cursorPos > 0)
			{
				var ch = _source[cursorPos - 1];
				if (ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']') search = true;
			}
			if (!search && CheckWordIntersect(cursorPos, k_replace)) search = true;
			if (!search && CheckWordIntersect(cursorPos, k_with)) search = true;
			if (!search && CheckWordIntersect(cursorPos, k_endreplace)) search = true;
			if (!search && CheckWordIntersect(cursorPos, k_insert)) search = true;
			if (!search && CheckWordIntersect(cursorPos, k_endinsert)) search = true;
			if (!search) yield break;

			var tokens = GetTokensForScope(cursorPos);

			var nearTokens = new List<Token>();
			foreach (var token in tokens)
            {
				if (token.Intersects(cursorPos))
                {
					if (TokenOpen(token.type))
                    {
						nearTokens.RemoveAll(t => TokenOpen(t.type) || TokenClose(t.type));
						nearTokens.Add(token);
					}
					else if (TokenClose(token.type))
                    {
						if (!nearTokens.Any(t => TokenOpen(t.type) || TokenClose(t.type))) nearTokens.Add(token);
					}
					else
                    {
						nearTokens.Add(token);
                    }
                }
            }

			var ids = (from t in nearTokens where t.Intersects(cursorPos) select t.id).ToArray();

			foreach (var token in (from t in tokens where ids.Contains(t.id) select t))
			{
				yield return new SnapshotSpan(snapshot, token.pos, token.length);
			}

			yield break;
		}

		private IEnumerable<Token> GetTokensForScope(int cursorPos)
		{
			var scope = new Stack<Token>();
			var tokens = new List<Token>();
			var pos = 0;
			var length = _source.Length;
			var id = 0;
			char ch;

			Token? replaceStart = null;
			Token? replaceWith = null;
			Token? replaceEnd = null;
			Token? insertStart = null;
			Token? insertEnd = null;

			while (pos < length)
			{
				ch = _source[pos];
				if (Char.IsWhiteSpace(ch))
				{
					pos++;
				}
				else if (ch == '/')
				{
					if (pos + 1 < length && _source[pos + 1] == '/')
					{
						var index = _source.IndexOf('\n', pos);
						if (index >= 0) pos = index + 1;
						else pos = length;
					}
					else if (pos + 1 < length && _source[pos + 1] == '*')
					{
						var index = _source.IndexOf("*/", pos);
						if (index >= 0) pos = index + 2;
						else pos = length;
					}
					else pos++;
				}
				else if (ch == '\"' || ch == '\'')
				{
					var startCh = ch;

					pos++;
					while (pos < length)
					{
						ch = _source[pos];
						pos++;
						if (ch == startCh || ch == '\n') break;
						if (ch == '\\' && pos + 1 < length) pos++;
					}
				}
				else if (ch == '(')
				{
					scope.Push(new Token { type = TokenType.BracketOpen, pos = pos, length = 1 });
					pos++;
				}
				else if (ch == ')')
				{
					if (scope.Count > 0 && scope.Peek().type == TokenType.BracketOpen)
					{
						var open = scope.Pop();
						if (open.pos <= cursorPos && pos + 1 >= cursorPos)
						{
							open.id = ++id;
							tokens.Insert(0, open);
							tokens.Add(new Token { type = TokenType.BracketClose, pos = pos, length = 1, id = id });
						}
					}
					pos++;
				}
				else if (ch == '{')
				{
					scope.Push(new Token { type = TokenType.BraceOpen, pos = pos, length = 1 });
					pos++;
				}
				else if (ch == '}')
				{
					if (scope.Count > 0 && scope.Peek().type == TokenType.BraceOpen)
					{
						var open = scope.Pop();
						if (open.pos <= cursorPos && pos + 1 >= cursorPos)
						{
							open.id = ++id;
							tokens.Insert(0, open);
							tokens.Add(new Token { type = TokenType.BraceClose, pos = pos, length = 1, id = id });
						}
					}
					pos++;
				}
				else if (ch == '[')
				{
					scope.Push(new Token { type = TokenType.ArrayBraceOpen, pos = pos, length = 1 });
					pos++;
				}
				else if (ch == ']')
				{
					if (scope.Count > 0 && scope.Peek().type == TokenType.ArrayBraceOpen)
					{
						var open = scope.Pop();
						if (open.pos <= cursorPos && pos + 1 >= cursorPos)
						{
							open.id = ++id;
							tokens.Insert(0, open);
							tokens.Add(new Token { type = TokenType.ArrayBraceClose, pos = pos, length = 1, id = id });
						}
					}
					pos++;
				}
				else if (ch == '#')
				{
					if (CheckWordFwd(k_replace, pos))
					{
						replaceStart = new Token { type = TokenType.ReplaceStart, pos = pos, length = k_replace.Length };
						replaceWith = null;
						replaceEnd = null;
						pos += k_replace.Length;
						scope.Clear();
					}
					else if (CheckWordFwd(k_with, pos))
					{
						if (replaceStart.HasValue)
						{
							replaceWith = new Token { type = TokenType.ReplaceWith, pos = pos, length = k_with.Length };
							replaceEnd = null;
						}
						pos += k_with.Length;
						scope.Clear();
					}
					else if (CheckWordFwd(k_endreplace, pos))
					{
						if (replaceStart.HasValue && replaceWith.HasValue)
						{
							replaceEnd = new Token { type = TokenType.ReplaceEnd, pos = pos, length = k_endreplace.Length };

							if (replaceStart.Value.Intersects(cursorPos) ||
								replaceWith.Value.Intersects(cursorPos) ||
								replaceEnd.Value.Intersects(cursorPos))
							{
								tokens.Add(replaceStart.Value);
								tokens.Add(replaceWith.Value);
								tokens.Add(replaceEnd.Value);
							}
						}
						replaceStart = null;
						replaceWith = null;
						replaceEnd = null;
						insertStart = null;
						insertEnd = null;
						pos += k_endreplace.Length;
						scope.Clear();
					}
					else if (CheckWordFwd(k_insert, pos))
					{
						insertStart = new Token { type = TokenType.Insert, pos = pos, length = k_insert.Length };
						insertEnd = null;
						pos += k_insert.Length;
						scope.Clear();
					}
					else if (CheckWordFwd(k_endinsert, pos))
					{
						if (insertStart.HasValue)
						{
							insertEnd = new Token { type = TokenType.InsertEnd, pos = pos, length = k_endinsert.Length };
							if (insertStart.Value.Intersects(cursorPos) || insertEnd.Value.Intersects(cursorPos))
							{
								tokens.Add(insertStart.Value);
								tokens.Add(insertEnd.Value);
							}
						}
						insertStart = null;
						insertEnd = null;
						replaceStart = null;
						replaceWith = null;
						replaceEnd = null;
						pos += k_endinsert.Length;
						scope.Clear();
					}
					else pos++;
				}
				else pos++;

				if (pos > cursorPos)
				{
					if (scope.Count == 0 &&
						!replaceStart.HasValue && !replaceWith.HasValue && !replaceEnd.HasValue &&
						!insertStart.HasValue && !insertEnd.HasValue)
					{
						break;
					}
				}
			}

			return tokens;
		}

		private bool CheckWordFwd(string word, int pos)
		{
			var end = pos + word.Length;
			if (end > _source.Length) return false;
			if (_source.Substring(pos, word.Length) != word) return false;
			if (end + 1 < _source.Length && Char.IsLetterOrDigit(_source[end])) return false;
			return true;
		}

		private bool CheckWordIntersect(int pos, string word)
		{
			var start = pos - word.Length - 1;
			if (start < 0) start = 0;

			var end = pos + word.Length + 1;
			if (end > _source.Length) end = _source.Length;

			if (end - start < word.Length) return false;

			var substr = _source.Substring(start, end - start);
			var index = substr.IndexOf(word);
			if (index <= 0) return false;

			if (index + word.Length >= substr.Length) return false;
			if (Char.IsLetterOrDigit(substr[index + word.Length])) return false;

			return true;
		}
	}
}
