using DK.Code;
using System;
using System.Collections.Generic;
using System.Text;

namespace DK
{
	public static class StringHelper
	{
		public static readonly string[] EmptyStringArray = new string[0];

		public static string Combine(this IEnumerable<string> list)
		{
			var sb = new StringBuilder();
			foreach (var str in list)
			{
				if (str != null) sb.Append(str);
			}
			return sb.ToString();
		}

		public static string Combine(this IEnumerable<string> list, string delim)
		{
			if (string.IsNullOrEmpty(delim)) return list.Combine();

			var sb = new StringBuilder();
			var first = true;
			foreach (var str in list)
			{
				if (first) first = false;
				else sb.Append(delim);
				if (str != null) sb.Append(str);
			}
			return sb.ToString();
		}

		public static IEnumerable<T> Delim<T>(this IEnumerable<T> list, T delim)
		{
			var first = true;
			foreach (var item in list)
			{
				if (first) first = false;
				else yield return delim;
				yield return item;
			}
		}

		public static bool EqualsI(this string pathA, string pathB)
		{
			return string.Equals(pathA, pathB, StringComparison.OrdinalIgnoreCase);
		}

		public static string ToSingleLine(this string str)
		{
			var sb = new StringBuilder(str.Length);
			foreach (var ch in str)
			{
				if (ch == '\n') sb.Append(' ');
				else if (ch != '\r') sb.Append(ch);
			}
			return sb.ToString();
		}

		public static bool IsWhiteSpace(this string str)
		{
			if (string.IsNullOrEmpty(str)) return false;
			foreach (var ch in str)
			{
				if (!char.IsWhiteSpace(ch)) return false;
			}
			return true;
		}

		public static bool IsWord(this string str)
		{
			if (string.IsNullOrEmpty(str)) return false;

			var first = true;
			foreach (var ch in str)
			{
				if (!ch.IsWordChar(first)) return false;
				first = false;
			}
			return true;
		}

		public static HashSet<char> ParseCharList(this string str)
		{
			var ret = new HashSet<char>();
			foreach (var ch in str) ret.Add(ch);
			return ret;
		}

		/// <summary>
		/// Splits a string containing distinct words delimited by whitespace.
		/// </summary>
		/// <param name="wordList">The string to be split.</param>
		/// <returns>A hash set containing the word list.</returns>
		public static HashSet<string> ParseWordList(params string[] wordLists)
		{
			var ret = new HashSet<string>();
			foreach (var wordList in wordLists)
			{
				foreach (var word in wordList.Split(' ', '\t', '\r', '\n'))
				{
					if (!string.IsNullOrEmpty(word)) ret.Add(word);
				}
			}
			return ret;
		}

		public static bool HasUpper(this string str)
		{
			if (str == null) return false;
			foreach (var ch in str)
			{
				if (char.IsUpper(ch)) return true;
			}
			return false;
		}

		public static string GetFirstLine(this string str)
		{
			var index = str.IndexOfAny(new char[] { '\r', '\n' });
			if (index < 0) return str;
			return str.Substring(0, index);
		}

		public static void CalcLineAndPosFromOffset(string source, int offset, out int lineNumOut, out int linePosOut)
		{
			var length = source.Length;
			if (offset < 0 || offset > length) throw new ArgumentOutOfRangeException(nameof(offset));

			int pos = 0;
			int lineNum = 0;
			int linePos = 0;

			while (pos < offset)
			{
				if (source[pos] == '\n')
				{
					lineNum++;
					linePos = 0;
				}
				else
				{
					linePos++;
				}
				pos++;
			}

			lineNumOut = lineNum;
			linePosOut = linePos;
		}

		public static CodeSpan GetSpanForLine(string source, int position, bool excludeWhiteSpace)
		{
			var length = source.Length;
			if (position < 0 || position > length) throw new ArgumentOutOfRangeException(nameof(position));

			var start = position;
			while (start > 0 && !source[start - 1].IsEndOfLineChar()) start--;

			var end = position;
			while (end < length && !source[end].IsEndOfLineChar()) end++;

			if (excludeWhiteSpace && start < end)
			{
				// Remove leading whitespace
				var i = start;
				while (i < end && source[i].IsWhiteChar()) i++;
				if (i < end)
				{
					start = i;

					// Remove trailing whitespace
					while (end > start && source[end - 1].IsWhiteChar()) end--;
				}
			}

			return new CodeSpan(start, end);
		}
	}

	public static class CharUtil
	{
		/// <summary>
		/// Determines if this character is a standard word character.
		/// </summary>
		/// <param name="ch">The character to examine.</param>
		/// <returns>True if it is a word character; otherwise false.</returns>
		public static bool IsWordChar(this char ch, bool firstChar)
		{
			if (firstChar) return char.IsLetter(ch) || ch == '_';
			else return char.IsLetterOrDigit(ch) || ch == '_';
		}

		public static bool IsEndOfLineChar(this char ch) => ch == '\r' || ch == '\n';

		public static bool IsWhiteChar(this char ch) => ch == ' ' || ch == '\t';
	}

	public static class StringBuilderUtil
	{
		public static bool GetLastNonWhiteChar(this StringBuilder sb, out char lastCh, out int index)
		{
			for (int i = sb.Length - 1; i >= 0; i--)
			{
				var ch = sb[i];
				if (!Char.IsWhiteSpace(ch))
				{
					index = i;
					lastCh = ch;
					return true;
				}
			}
			index = -1;
			lastCh = '\0';
			return false;
		}
	}
}
