using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace DkTools
{
	public static class Util
	{
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

		public static HashSet<char> ParseCharList(string str)
		{
			var ret = new HashSet<char>();
			foreach (var ch in str) ret.Add(ch);
			return ret;
		}

		/// <summary>
		/// Tests if a string only consists of word characters (a-z 0-9 _)
		/// </summary>
		/// <param name="str">The string to be tested.</param>
		/// <returns>True if all characters are word chars; otherwise false.</returns>
		public static bool IsWordCharsOnly(this string str)
		{
			return str.All(c => char.IsLetterOrDigit(c) || c == '_');
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

		public static bool IsTagNameChar(this char ch, bool firstChar)
		{
			if (firstChar) return char.IsLetter(ch) || ch == '_';
			else return char.IsLetterOrDigit(ch) || ch == '_' || ch == ':';
		}

		public static bool IsIdentifier(this string str)
		{
			var first = true;
			foreach (var ch in str)
			{
				if (first)
				{
					if (!Char.IsLetter(ch) && ch != '_') return false;
					first = false;
				}
				else
				{
					if (!Char.IsLetterOrDigit(ch) && ch != '_') return false;
				}
			}
			return true;
		}

		public static bool IsEndOfLineChar(this char ch)
		{
			return ch == '\r' || ch == '\n';
		}

		public static bool GetWordExtent(this string str, int pos, out int startPos, out int length)
		{
			if (pos < str.Length && str[pos].IsWordChar(false))
			{
				var endPos = pos + 1;
				while (endPos < str.Length && str[endPos].IsWordChar(false)) endPos++;

				startPos = pos;
				while (startPos > 0 && str[startPos - 1].IsWordChar(false)) startPos--;

				length = endPos - startPos;
				return true;
			}

			startPos = pos;
			length = 0;
			return false;
		}

		public static string GetFirstLine(this string str)
		{
			var index = str.IndexOfAny(new char[] { '\r', '\n' });
			if (index < 0) return str;
			return str.Substring(0, index);
		}

		public static void ShowError(this System.Windows.Controls.UserControl ctrl, Exception ex)
		{
			Log.WriteEx(ex);
			ShowErrorDialog(ex.Message, ex.ToString());
		}

		public static void ShowError(this System.Windows.Controls.UserControl ctrl, string message)
		{
			Log.Write(LogLevel.Error, message);
			ShowErrorDialog(message, null);
		}

		public static void ShowError(this System.Windows.Window window, Exception ex)
		{
			Log.WriteEx(ex);
			ShowErrorDialog(ex.Message, ex.ToString());
		}

		public static void ShowError(this System.Windows.Forms.IWin32Window window, Exception ex)
		{
			Log.WriteEx(ex);
			ShowErrorDialog(ex.Message, ex.ToString());
		}

		public static void ShowError(this System.Windows.Forms.IWin32Window window, string message)
		{
			Log.Write(LogLevel.Error, message);
			ShowErrorDialog(message, null);
		}

		public static void ShowErrorDialog(string message, string details)
		{
			try
			{
				var dlg = new ErrorDialog(message, details);
				dlg.Owner = System.Windows.Application.Current.MainWindow;
				dlg.ShowDialog();
			}
			catch (Exception ex2)
			{
				MessageBox.Show(ex2.ToString(), Constants.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#region Indenting
		public static int AddIndentTab(this int indent, int tabSize)
		{
			if (indent % tabSize == 0) return indent + tabSize;
			else return indent + tabSize - (tabSize % tabSize);
		}

		public static string GetIndentText(this string str)
		{
			var pos = 0;
			while (pos < str.Length)
			{
				if (!char.IsWhiteSpace(str[pos])) return str.Substring(0, pos);
				pos++;
			}
			return string.Empty;
		}

		public static int GetIndentCount(this string lineText, int tabSize)
		{
			var pos = 0;
			var length = lineText.Length;
			char ch;
			var indent = 0;

			while (pos < length)
			{
				ch = lineText[pos++];

				if (!char.IsWhiteSpace(ch)) return indent;

				if (ch == '\t') indent = indent.AddIndentTab(tabSize);
				else indent++;
			}

			return 0;
		}

		public static string AdjustIndent(this string str, int desiredIndent, int tabSize, bool keepTabs)
		{
			var oldIndentText = str.GetIndentText();
			if (oldIndentText.Length > 0) str = str.Substring(oldIndentText.Length);

			var sb = new StringBuilder();

			if (keepTabs)
			{
				var remain = desiredIndent;
				while (remain >= tabSize)
				{
					sb.Append('\t');
					remain -= tabSize;
				}
				sb.Append(' ', remain);
			}
			else
			{
				sb.Append(' ', desiredIndent);
			}

			return string.Concat(sb, str);
		}
		#endregion

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

		public static bool IsWhiteSpace(this string str)
		{
			if (string.IsNullOrEmpty(str)) return false;
			foreach (var ch in str)
			{
				if (!char.IsWhiteSpace(ch)) return false;
			}
			return true;
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
	}
}
