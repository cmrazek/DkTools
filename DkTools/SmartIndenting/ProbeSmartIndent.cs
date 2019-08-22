using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using DkTools.Classifier;

namespace DkTools.SmartIndenting
{
	internal class ProbeSmartIndent : ISmartIndent
	{
		private ITextView _view;
		private static int _tabSize = 4;
		private static bool _keepTabs = true;

		private static readonly Regex _rxPreprocessorLine = new Regex(@"^\s*#(?:if|ifdef|ifndef|elif|else|endif|define|undef)");
		private static readonly Regex _rxCaseLine = new Regex(@"^\s*(case\s+.*\:|default\:|before\s+group.*\:|after\s+group.*\:|for\s+each\s*\:)");

		public ProbeSmartIndent(ITextView view)
		{
			_view = view;
		}

		public void Dispose()
		{
		}

		public int? GetDesiredIndentation(ITextSnapshotLine line)
		{
			_tabSize = _view.GetTabSize();
			_keepTabs = _view.GetKeepTabs();

			return GetDesiredIndentation(line.Snapshot.TextBuffer, line, _tabSize, _keepTabs);
		}

		public static int? GetDesiredIndentation(ITextBuffer buffer, ITextSnapshotLine line, int tabSize, bool keepTabs)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (line.LineNumber == 0) return 0;

			var lineText = line.GetText();
			var lineTextTrim = lineText.Trim();

			if (lineTextTrim == "}")
			{
				// User is ending a set of braces.

				var bracePos = line.Start.Position + GetNumWhiteSpacePrefixChars(lineText);
				var braceParser = new BraceHighlighting.BraceHighlightParser();

				foreach (var span in braceParser.FindMatchingBraces(line.Snapshot, bracePos))
				{
					if (span.End < bracePos)
					{
						return span.Start.GetContainingLine().GetText().GetIndentCount(tabSize);
					}
				}
			}
			else if (lineTextTrim == "#")
			{
				// Preprocessor statements always at the very beginning of the line.
				return 0;
			}
			else if (_rxCaseLine.IsMatch(lineTextTrim))
			{
				// User is typing a 'case' inside a switch.

				// Try to find the braces that contain the 'case'.
				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(buffer);
				if (fileStore != null)
				{
					var fileName = VsTextUtil.TryGetDocumentFileName(buffer);
					var model = fileStore.GetCurrentModel(fileName, buffer.CurrentSnapshot, "Smart indenting - case inside switch");
					var offset = line.Snapshot.TranslateOffsetToSnapshot(line.Start.Position, model.Snapshot);
					var bracesToken = model.File.FindDownward(offset, t => t is CodeModel.Tokens.BracesToken).LastOrDefault() as CodeModel.Tokens.BracesToken;
					if (bracesToken != null)
					{
						// Get the indent of the line where the opening brace resides.
						var openOffset = bracesToken.OpenToken.Span.Start;
						openOffset = model.Snapshot.TranslateOffsetToSnapshot(openOffset, line.Snapshot);
						var openLine = line.Snapshot.GetLineFromPosition(openOffset);
						return openLine.GetText().GetIndentCount(tabSize);
					}
				}
			}

			// If we got to this point, then the default smart indenting is to be used.
			{
				var prevLine = GetPreviousCodeLine(line);
				if (prevLine != null)
				{
					var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(buffer);
					if (tracker != null)
					{
						var lineNumber = prevLine.LineNumber;
						var prevState = tracker.GetStateForLineStart(lineNumber, tracker.Snapshot);
						while (State.IsInsideMultiLineComment(prevState))
						{
							if (lineNumber == 0) break;	// At start of file. In theory, this should never happen as the state for the start of the file is always zero.
							prevState = tracker.GetStateForLineStart(--lineNumber, tracker.Snapshot);
						}

						if (prevLine.LineNumber != lineNumber) prevLine = prevLine.Snapshot.GetLineFromLineNumber(lineNumber);
					}

					var prevLineText = prevLine.Snapshot.GetText(prevLine.Start.Position, line.Start.Position - prevLine.Start.Position);

					//var tracker = ;
					//var state = tracker != null ? tracker.GetStateForLine(prevLine.LineNumber, _view.TextSnapshot) : 0;

					//if (prevLineText.EndsWith("{") || _rxCaseLine.IsMatch(prevLineText))
					if (PrevLineTextWarrantsIndent(prevLineText))
					{
						return prevLineText.GetIndentCount(tabSize).AddIndentTab(tabSize);
					}
					else
					{
						return prevLineText.GetIndentCount(tabSize);
					}
				}
				return 0;
			}
		}

		private static ITextSnapshotLine GetPreviousCodeLine(ITextSnapshotLine line)
		{
			var prevLineNum = line.LineNumber - 1;
			ITextSnapshotLine prevLine = null;
			while (prevLineNum >= 0 && prevLine == null)
			{
				prevLine = line.Snapshot.GetLineFromLineNumber(prevLineNum);

				var prevLineText = prevLine.GetText();
				if (!string.IsNullOrWhiteSpace(prevLineText) &&
					!_rxPreprocessorLine.IsMatch(prevLineText))
				{
					return prevLine;
				}

				prevLine = null;
				prevLineNum--;
			}

			return null;
		}

		public static int GetNumWhiteSpacePrefixChars(string lineText)
		{
			var pos = 0;
			var length = lineText.Length;

			while (pos < length && char.IsWhiteSpace(lineText[pos])) pos++;

			return pos;
		}

		public int TabSize
		{
			get { return _tabSize; }
		}

		public bool KeepTabs
		{
			get { return _keepTabs; }
		}

		public void FixIndentingBetweenLines(int startLineNumber, int endLineNumber)
		{
			using (var tran = _view.TextBuffer.CreateUndoTransaction("Indentation fix"))
			{
				for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
				{
					var line = _view.TextSnapshot.GetLineFromLineNumber(lineNumber);
					var indent = GetDesiredIndentation(line);
					if (!indent.HasValue) continue;
					var desiredIndent = indent.Value;

					var lineText = line.GetText();
					var actualIndent = lineText.GetIndentCount(_tabSize);

					if (desiredIndent != actualIndent)
					{
						lineText = lineText.AdjustIndent(desiredIndent, _tabSize, _keepTabs);
						_view.TextBuffer.Replace(new SnapshotSpan(line.Start, line.End), lineText);
					}
				}

				tran.Complete();
			}
		}

		public static void FixIndentingBetweenLines(ITextBuffer buffer, int startLineNumber, int endLineNumber, int tabSize, bool keepTabs)
		{
			for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
			{
				var line = buffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);
				var indent = GetDesiredIndentation(buffer, line, tabSize, keepTabs);
				if (!indent.HasValue) continue;
				var desiredIndent = indent.Value;

				var lineText = line.GetText();
				var actualIndent = lineText.GetIndentCount(tabSize);

				if (desiredIndent != actualIndent)
				{
					var oldIndent = lineText.GetIndentText();
					lineText = lineText.AdjustIndent(desiredIndent, tabSize, keepTabs);
					var newIndent = lineText.GetIndentText();
					buffer.Replace(new SnapshotSpan(line.Snapshot, line.Start.Position, oldIndent.Length), newIndent);
				}
			}
		}

		public static bool PrevLineTextWarrantsIndent(string lineText)
		{
			var code = new CodeParser(lineText);
			var indent = 0;

			if (_rxCaseLine.IsMatch(lineText)) return true;

			while (code.Read())
			{
				if (code.Type == CodeType.Operator)
				{
					switch (code.Text)
					{
						case "{":
						case "(":
						case "[":
							indent++;
							break;
						case "}":
						case ")":
						case "]":
							indent--;
							break;
					}
				}
			}

			return indent > 0;
		}
	}
}
