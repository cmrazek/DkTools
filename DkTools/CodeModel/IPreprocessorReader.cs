using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal interface IPreprocessorReader
	{
		void SetWriter(IPreprocessorWriter writer);
		bool EOF { get; }

		char Peek();
		string Peek(int numChars);
		string PeekUntil(Func<char, bool> callback);

		void Use(int numChars);
		void UseUntil(Func<char, bool> callback);

		void Ignore(int numChars);
		void IgnoreUntil(Func<char, bool> callback);

		string FileName { get; }
		Position Position { get; }

		void Insert(string text);

		bool Suppress { get; set; }
	}

	internal static class PreprocessorReaderExtensions
	{
		public static string PeekIdentifier(this IPreprocessorReader rdr)
		{
			var first = true;
			return rdr.PeekUntil(ch =>
			{
				if (first)
				{
					first = false;
					return ch.IsWordChar(true);
				}
				else return ch.IsWordChar(false);
			});
		}

		public static string PeekToken(this IPreprocessorReader rdr, bool stayOnSameLine)
		{
			bool lineChange;
			if (rdr.IgnoreComments(out lineChange))
			{
				if (lineChange && stayOnSameLine) return null;
				return string.Empty;
			}

			var ch = rdr.Peek();
			if ((ch == '\r' || ch == '\n') && stayOnSameLine)
			{
				return null;
			}

			if (char.IsWhiteSpace(ch))
			{
				return rdr.PeekUntil(c => char.IsWhiteSpace(c));
			}

			if (ch.IsWordChar(true))
			{
				return rdr.PeekIdentifier();
			}

			if (char.IsDigit(ch))
			{
				var gotDecimal = false;
				return rdr.PeekUntil(c =>
				{
					if (char.IsDigit(c)) return true;
					if (c == '.')
					{
						if (gotDecimal) return false;
						gotDecimal = true;
						return true;
					}
					return false;
				});
			}

			if (ch == '\"' || ch == '\'')
			{
				var sb = new StringBuilder();

				var lastCh = '\0';
				var first = true;
				var gotEnd = false;
				sb.Append(rdr.PeekUntil(c =>
				{
					if (gotEnd) return false;

					if (first)
					{
						first = false;
						return true;
					}

					if (c == ch && lastCh != '\\')
					{
						gotEnd = true;
						return true;
					}

					if (c == '\\' && lastCh == '\\') lastCh = '\0';
					else lastCh = c;

					return true;
				}));
				return sb.ToString();
			}

			if (ch == '#')
			{
				var index = -1;
				return rdr.PeekUntil(c =>
					{
						index++;
						if (index == 0) return c == '#';
						else if (index == 1) return c.IsWordChar(true);
						else return c.IsWordChar(false);
					});
			}

			return ch.ToString();
		}

		public static bool IgnoreComments(this IPreprocessorReader rdr)
		{
			bool lineChange;
			return rdr.IgnoreComments(out lineChange, false);
		}

		public static bool IgnoreComments(this IPreprocessorReader rdr, out bool lineChangeFound)
		{
			return rdr.IgnoreComments(out lineChangeFound, false);
		}

		public static bool IgnoreComments(this IPreprocessorReader rdr, out bool lineChangeFound, bool multiLineOnly)
		{
			lineChangeFound = false;
			if (rdr.Peek() == '/')
			{
				var str = rdr.Peek(2);
				if (str == "/*")
				{
					rdr.Ignore(2);
					rdr.IgnoreUntil(c => c != '/' && c != '*' && c != '\r' && c != '\n');

					char ch;
					while (!rdr.EOF)
					{
						ch = rdr.Peek();
						if (ch == '*')
						{
							if (rdr.Peek(2) == "*/")
							{
								rdr.Ignore(2);
								return true;
							}
							else rdr.Ignore(1);
						}
						else if (ch == '/')
						{
							bool lineChange;
							if (rdr.IgnoreComments(out lineChange, true))
							{
								if (lineChange) lineChangeFound = true;
							}
							else rdr.Ignore(1);
						}
						else if (ch == '\r' || ch == '\n')
						{
							lineChangeFound = true;
							rdr.Ignore(1);
						}
						rdr.IgnoreUntil(c => c != '/' && c != '*' && c != '\r' && c != '\n');
					}
					return true;
				}
				else if (str == "//" && multiLineOnly == false)
				{
					rdr.IgnoreUntil(c => c != '\r' && c != '\n');
					return true;
				}
			}
			return false;
		}

		public static void IgnoreWhiteSpaceAndComments(this IPreprocessorReader rdr, bool stayOnSameLine)
		{
			char ch;

			while (true)
			{
				ch = rdr.Peek();

				if (stayOnSameLine && (ch == '\r' || ch == '\n')) break;

				if (Char.IsWhiteSpace(ch))
				{
					rdr.IgnoreUntil(c => Char.IsWhiteSpace(c));
					continue;
				}

				if (!rdr.IgnoreComments()) break;
			}
		}

		public static string ReadAndIgnoreNestableContent(this IPreprocessorReader rdr, string endToken)
		{
			var sb = new StringBuilder();
			string str;

			rdr.IgnoreWhiteSpaceAndComments(false);

			while (!rdr.EOF)
			{
				str = rdr.PeekToken(false);
				if (string.IsNullOrEmpty(str)) continue;

				if (str == endToken)
				{
					rdr.Ignore(str.Length);
					break;
				}
				else if (str == "(")
				{
					sb.Append(str);
					rdr.Ignore(str.Length);
					sb.Append(rdr.ReadAndIgnoreNestableContent(")"));
					sb.Append(")");
				}
				else if (str == "{")
				{
					sb.Append(str);
					rdr.Ignore(str.Length);
					sb.Append(rdr.ReadAndIgnoreNestableContent("}"));
					sb.Append("}");
				}
				else if (str == "[")
				{
					sb.Append(str);
					rdr.Ignore(str.Length);
					sb.Append(rdr.ReadAndIgnoreNestableContent("]"));
					sb.Append("]");
				}
				else
				{
					sb.Append(str);
					rdr.Ignore(str.Length);
				}
			}

			return sb.ToString();
		}
	}
}
