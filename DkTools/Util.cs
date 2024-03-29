﻿using DK;
using DK.Diagnostics;
using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace DkTools
{
	public static class Util
	{
		/// <summary>
		/// Tests if a string only consists of word characters (a-z 0-9 _)
		/// </summary>
		/// <param name="str">The string to be tested.</param>
		/// <returns>True if all characters are word chars; otherwise false.</returns>
		public static bool IsWordCharsOnly(this string str)
		{
			return str.All(c => char.IsLetterOrDigit(c) || c == '_');
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

		public static void ShowError(this System.Windows.Controls.UserControl ctrl, Exception ex)
		{
			ProbeToolsPackage.Instance.App.Log.Error(ex);
			ShowErrorDialog(ex.Message, ex.ToString());
		}

		public static void ShowError(this System.Windows.Controls.UserControl ctrl, string message)
		{
			ProbeToolsPackage.Instance.App.Log.Error(message);
			ShowErrorDialog(message, null);
		}

		public static void ShowError(this System.Windows.Window window, Exception ex)
		{
			ProbeToolsPackage.Instance.App.Log.Error(ex);
			ShowErrorDialog(ex.Message, ex.ToString());
		}

		public static void ShowError(this System.Windows.Forms.IWin32Window window, Exception ex)
		{
			ProbeToolsPackage.Instance.App.Log.Error(ex);
			ShowErrorDialog(ex.Message, ex.ToString());
		}

		public static void ShowError(this System.Windows.Forms.IWin32Window window, string message)
		{
			ProbeToolsPackage.Instance.App.Log.Error(message);
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
			else return indent + tabSize - (indent % tabSize);
		}

		public static int GetTabWidth(this int currentColumn, int tabSize)
        {
			if (currentColumn % tabSize == 0) return tabSize;
			else return tabSize - (currentColumn % tabSize);
        }

		public static string GetIndentText(this string str)
		{
			var pos = 0;
			char ch;
			while (pos < str.Length)
			{
				ch = str[pos];
				if (ch != ' ' && ch != '\t') return str.Substring(0, pos);
				pos++;
			}
			return string.Empty;
		}

		public static int GetIndentOffset(this string str)
		{
			var pos = 0;
			char ch;
			while (pos < str.Length)
			{
				ch = str[pos];
				if (ch != ' ' && ch != '\t') return pos;
				pos++;
			}
			return 0;
		}

		public static int GetIndentCount(this string lineText, int tabSize, int length = -1)
		{
			var pos = 0;
			char ch;
			var indent = 0;

			if (length < 0) length = lineText.Length;

			while (pos < length)
			{
				ch = lineText[pos++];

				if (ch != ' ' && ch != '\t') return indent;

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

		public static string TabsToSpaces(this string str, int tabSize)
        {
			var sb = new StringBuilder();

			foreach (var ch in str)
            {
				if (ch == '\t') sb.Append(' ', sb.Length.GetTabWidth(tabSize));
				else sb.Append(ch);
            }

			return sb.ToString();
        }
		#endregion
	}
}
