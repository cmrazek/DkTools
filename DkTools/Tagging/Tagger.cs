using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DK.AppEnvironment;
using DkTools.CodeModeling;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace DkTools.Tagging
{
	internal static class Tagger
	{
		public static void InsertDate()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Shell.DTE;
            var activeDoc = dte.ActiveDocument;
            if (activeDoc != null)
            {
                var options = ProbeToolsPackage.Instance.TaggingOptions;
                var dateFormat = options.DateFormat;
                if (string.IsNullOrWhiteSpace(dateFormat)) dateFormat = Constants.DefaultDateFormat;

                var sel = activeDoc.Selection as TextSelection;
                sel.Insert(DateTime.Now.ToString(options.DateFormat));
            }
		}

		public static string GetFileHeaderText(string fileName)
		{
			var options = ProbeToolsPackage.Instance.TaggingOptions;

			var workOrder = options.WorkOrder;
			if (workOrder == null) workOrder = string.Empty;

			var initials = options.Initials;
			if (initials == null) initials = string.Empty;

			var dateFormat = options.DateFormat;
			if (string.IsNullOrWhiteSpace(dateFormat)) dateFormat = Constants.DefaultDateFormat;

			var sb = new StringBuilder();
			sb.AppendLine("//**************************************************************************************************");
			sb.Append("// File Name: ");
			sb.AppendLine(Path.GetFileName(fileName));
			sb.AppendLine("//  ");
			sb.AppendLine("//");
			sb.AppendLine("// Modification History:");
			sb.AppendLine("//  Date        Who #       Description of Changes");
			sb.AppendLine("//  ----------- --- ------- ------------------------------------------------------------------------");
			sb.Append("//  ");
			sb.Append(DateTime.Now.ToString(dateFormat));
			sb.Append("   ");
			sb.Append(initials.PadRight(4));
			sb.Append(workOrder.PadRight(8));

			var defect = options.Defect;
			if (!string.IsNullOrWhiteSpace(defect))
			{
				sb.Append(defect);
				sb.Append(" Created");
			}
			else
			{
				sb.Append("Created");
			}
			sb.AppendLine();
			sb.AppendLine("//**************************************************************************************************");
			sb.AppendLine();

			return sb.ToString();
		}

		public static void InsertFileHeader()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc != null)
			{
				var fileHeaderText = GetFileHeaderText(activeDoc.FullName);

				var sel = activeDoc.Selection as TextSelection;
				sel.StartOfDocument();
				sel.Insert(fileHeaderText);
			}
		}

		public static void InsertDiag()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc != null)
			{
				var options = ProbeToolsPackage.Instance.TaggingOptions;

				var sel = activeDoc.Selection as TextSelection;
				var selText = sel.Text.Trim();
				if (selText.IndexOf('\n') >= 0) selText = string.Empty;

				var sb = new StringBuilder();
				sb.Append("diag(\"");
				if (options.InitialsInDiags && !string.IsNullOrWhiteSpace(options.Initials))
				{
					sb.Append(options.Initials);
					sb.Append(": ");
				}

				if (options.FileNameInDiags)
				{
					sb.Append(Path.GetFileName(activeDoc.FullName));
					sb.Append(": ");
				}

				if (options.FunctionNameInDiags)
				{
					var funcName = string.Empty;
					var buf = Shell.ActiveBuffer;
					if (buf != null)
					{
						var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(buf);
						if (fileStore != null)
						{
							var appSettings = DkEnvironment.CurrentAppSettings;
							var fileName = VsTextUtil.TryGetDocumentFileName(buf);
							var funcs = fileStore.GetFunctionDropDownList(appSettings, fileName, buf.CurrentSnapshot);
							var model = fileStore.GetMostRecentModel(appSettings, fileName, buf.CurrentSnapshot, "Insert diag");
							var modelSnapshot = model.Snapshot as ITextSnapshot;
							if (modelSnapshot != null)
							{
								var modelPos = model.AdjustPosition(Shell.ActiveView.Caret.Position.BufferPosition.Position, modelSnapshot);
								foreach (var func in funcs)
								{
									if (func.EntireFunctionSpan.Contains(modelPos))
									{
										funcName = func.Name;
										break;
									}
								}
							}
						}
					}

					if (!string.IsNullOrWhiteSpace(funcName))
					{
						sb.Append(funcName);
						sb.Append("(): ");
					}
				}

				if (!string.IsNullOrWhiteSpace(selText))
				{
					sb.Append(DkEnvironment.StringEscape(selText));
					sb.Append(" [\", ");
					sb.Append(selText);
					sb.Append(", \"]");
				}

				int lengthBefore = sb.Length;

				sb.Append("\\n\");");
				if (options.TodoAfterDiags) sb.Append("\t// TODO");

				sel.Insert(sb.ToString());

				if (string.IsNullOrWhiteSpace(selText))
				{
					sel.CharLeft(false, sb.Length - lengthBefore);
				}
			}
		}

		public static void InsertTag()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = Shell.DTE;
			var activeDoc = dte.ActiveDocument;
			if (activeDoc == null) return;

			var options = ProbeToolsPackage.Instance.TaggingOptions;

			var sel = activeDoc.Selection as TextSelection;

			var sb = new StringBuilder();
			sb.Append("//");
			if (!string.IsNullOrWhiteSpace(options.Initials))
			{
				sb.Append(" ");
				sb.Append(options.Initials);
			}
			if (!string.IsNullOrWhiteSpace(options.WorkOrder))
			{
				sb.Append(" ");
				sb.Append(options.WorkOrder);
			}
			if (!string.IsNullOrWhiteSpace(options.Defect))
			{
				sb.Append(" ");
				sb.Append(options.Defect);
			}

			if (sel.TopPoint.AbsoluteCharOffset == sel.BottomPoint.AbsoluteCharOffset)
			{
				sb.Insert(0, "\t\t");
				sel.EndOfLine();
				sel.Insert(sb.ToString());
			}
			else
			{
				var topLine = sel.TopPoint.Line;
				var bottomLine = sel.BottomPoint.Line;

				sel.MoveToLineAndOffset(bottomLine, 1);
				sel.EndOfLine();
				sel.NewLine();
				sel.Insert(sb.ToString() + " End");
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, false);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, true);
				sel.Tabify();
				sel.EndOfLine();

				sel.MoveToLineAndOffset(topLine, 1);
				sel.LineUp();
				sel.EndOfLine();
				sel.NewLine();
				sel.Insert(sb.ToString() + " Start");
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstColumn, false);
				sel.StartOfLine(vsStartOfLineOptions.vsStartOfLineOptionsFirstText, true);
				sel.Tabify();
				sel.EndOfLine();
			}
		}

		public static void CommentBlock()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var view = Shell.ActiveView;
			var snapshot = view.TextSnapshot;
			var buffer = view.TextBuffer;

			var startPt = view.Selection.Start.Position;
			var endPt = view.Selection.End.Position;

			if (startPt.Position == endPt.Position)
			{
				// Use // style comment on single line

				var line = snapshot.GetLineFromPosition(startPt.Position);
				var lineText = line.GetText();
				if (!string.IsNullOrWhiteSpace(lineText))
				{
					var startOffset = 0;
					while (startOffset < lineText.Length)
					{
						var ch = lineText[startOffset];
						if (ch == ' ' || ch == '\t') startOffset++;
						else break;
					}

					if (startOffset < lineText.Length)
					{
						buffer.Insert(line.Start.Position + startOffset, "//");
					}
				}

				return;
			}

			var strBefore = snapshot.GetLineTextUpToPosition(startPt.Position);
			var strAfter = snapshot.GetLineTextAfterPosition(endPt.Position);

			if (string.IsNullOrWhiteSpace(strBefore) && string.IsNullOrWhiteSpace(strAfter))
			{
				// Can do multi line // style comments

				var startLineNumber = snapshot.GetLineNumberFromPosition(startPt.Position);
				var endLineNumber = snapshot.GetLineNumberFromPosition(endPt.Position);

				// Calculate the indent level at which the comments should be inserted
				var tabSize = view.GetTabSize();
				var keepTabs = view.GetKeepTabs();
				var desiredIndent = -1;
				var lines = new List<string>();
				for (int i = startLineNumber; i <= endLineNumber; i++)
				{
					var line = snapshot.GetLineFromLineNumber(i);
					var lineText = line.GetText();
					lines.Add(lineText);
					if (!string.IsNullOrWhiteSpace(lineText))
					{
						var indent = lineText.GetIndentCount(tabSize);
						if (desiredIndent == -1 || indent < desiredIndent) desiredIndent = indent;
					}
				}

				// Apply the comments to the line text
				for (int i = 0, ii = lines.Count; i < ii; i++)
				{
					var line = lines[i];
					AddCommentToLine(ref line, desiredIndent, tabSize, keepTabs);
					lines[i] = line;
				}

				var sb = new StringBuilder();
				var first = true;
				foreach (var line in lines)
				{
					if (first) first = false;
					else sb.AppendLine();
					sb.Append(line);
				}

				var startLine = snapshot.GetLineFromPosition(startPt.Position);
				var endLine = snapshot.GetLineFromPosition(endPt.Position);
				buffer.Replace(new Span(startLine.Start.Position, endLine.End.Position - startLine.Start.Position), sb.ToString());
			}
			else
			{
				// Use /*..*/ style comments

				var span = new Span(startPt.Position, endPt.Position - startPt.Position);
				var sb = new StringBuilder(snapshot.GetText(span));
				sb.Insert(0, "/*");
				sb.Append("*/");
				buffer.Replace(span, sb.ToString());
			}
		}

		public static void UncommentBlock()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var view = Shell.ActiveView;
			var snapshot = view.TextSnapshot;
			var buffer = view.TextBuffer;

			var startPt = view.Selection.Start.Position;
			var endPt = view.Selection.End.Position;

			if (startPt.Position == endPt.Position)
			{
				// Uncomment // on single line

				var line = snapshot.GetLineFromPosition(startPt.Position);
				var lineText = line.GetText();

				UncommentLine(ref lineText);

				buffer.Replace(line.GetSpan(), lineText);

				return;
			}

			var strBefore = snapshot.GetLineTextUpToPosition(startPt.Position);
			var strAfter = snapshot.GetLineTextAfterPosition(endPt.Position);

			if (string.IsNullOrWhiteSpace(strBefore) && string.IsNullOrWhiteSpace(strAfter))
			{
				// Uncomment // from multiple lines

				var startLineNumber = snapshot.GetLineNumberFromPosition(startPt.Position);
				var endLineNumber = snapshot.GetLineNumberFromPosition(endPt.Position);
				var sb = new StringBuilder();
				var first = true;
				for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
				{
					var line = snapshot.GetLineFromLineNumber(lineNumber);
					var lineText = line.GetText();

					UncommentLine(ref lineText);

					if (first) first = false;
					else sb.AppendLine();
					sb.Append(lineText);
				}

				var startLine = snapshot.GetLineFromPosition(startPt.Position);
				var endLine = snapshot.GetLineFromPosition(endPt.Position);
				buffer.Replace(new Span(startLine.Start.Position, endLine.End.Position - startLine.Start.Position), sb.ToString());
			}
			else
			{
				// Uncomment from middle of text

				var span = new Span(startPt.Position, endPt.Position - startPt.Position);
				var selText = snapshot.GetText(span);
				var beforeText = snapshot.GetLineTextUpToPosition(startPt.Position);
				var afterText = snapshot.GetLineTextAfterPosition(endPt.Position);
				var origSpan = span;
				var origText = selText;

				if (selText.StartsWith("/*"))
				{
					selText = selText.Substring(2);
				}
				else if (beforeText.EndsWith("/*"))
				{
					span = new Span(span.Start - 2, span.Length + 2);
				}
				else if (beforeText.EndsWith("/") && selText.StartsWith("*"))
				{
					selText = selText.Substring(1);
					span = new Span(span.Start - 1, span.Length + 1);
				}

				if (selText.EndsWith("*/"))
				{
					selText = selText.Substring(0, selText.Length - 2);
				}
				else if (afterText.StartsWith("*/"))
				{
					span = new Span(span.Start, span.Length + 2);
				}
				else if (selText.EndsWith("*") && afterText.StartsWith("/"))
				{
					selText = selText.Substring(0, selText.Length - 1);
					span = new Span(span.Start, span.Length + 1);
				}

				if (selText != origText || span != origSpan)
				{
					buffer.Replace(span, selText);
				}
			}
		}

		private static void AddCommentToLine(ref string line, int commentIndent, int tabSize, bool keepTabs)
		{
			if (string.IsNullOrWhiteSpace(line)) return;

			var sb = new StringBuilder(line);

			var indentRemoved = 0;
			var charsRemoved = 0;
			while (indentRemoved < commentIndent)
			{
				var ch = sb[charsRemoved];
				if (ch == ' ')
				{
					charsRemoved++;
					indentRemoved++;
				}
				else if (ch == '\t')
				{
					charsRemoved++;
					indentRemoved = ((indentRemoved / tabSize) + 1) * tabSize;
				}
				else break;
			}

			if (charsRemoved > 0) sb.Remove(0, charsRemoved);

			var indentAdded = 0;
			var insertPos = 0;
			if (keepTabs)
			{
				while (indentAdded < commentIndent)
				{
					if (indentAdded + tabSize <= commentIndent)
					{
						sb.Insert(insertPos, '\t');
						indentAdded += tabSize;
						insertPos++;
					}
					else
					{
						sb.Insert(insertPos, new string(' ', commentIndent - indentAdded));
						insertPos += commentIndent - indentAdded;
						indentAdded = commentIndent;
					}
				}
			}
			else
			{
				sb.Insert(0, new string(' ', commentIndent));
				insertPos = commentIndent;
				indentAdded = commentIndent;
			}

			sb.Insert(insertPos, "//");
			insertPos += 2;

			if (indentRemoved > indentAdded)
			{
				sb.Insert(insertPos, new string(' ', indentRemoved - indentAdded));
			}

			line = sb.ToString();
		}

		private static readonly Regex _rxUncommentSingleLineStart = new Regex(@"^(\s*)//(.*)$");
		private static readonly Regex _rxUncommentMultiLineStart = new Regex(@"^(\s*)/\*(.*)$");
		private static readonly Regex _rxUncommentMultiLineEnd = new Regex(@"^(.*)\*/(\s*)$");

		private static void UncommentLine(ref string line)
		{
			if (string.IsNullOrWhiteSpace(line)) return;

			Match match;
			if ((match = _rxUncommentSingleLineStart.Match(line)).Success)
			{
				line = string.Concat(match.Groups[1].Value, match.Groups[2].Value);
			}
			else
			{
				if ((match = _rxUncommentMultiLineStart.Match(line)).Success)
				{
					line = string.Concat(match.Groups[1].Value, match.Groups[2].Value);
				}

				if ((match = _rxUncommentMultiLineEnd.Match(line)).Success)
				{
					line = string.Concat(match.Groups[1].Value, match.Groups[2].Value);
				}
			}
		}
	}
}
