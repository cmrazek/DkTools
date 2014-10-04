using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.SmartIndenting
{
	internal class ProbeSmartIndent : ISmartIndent
	{
		private ITextView _view;
		private int _tabSize = 4;
		private bool _keepTabs = true;

		private Regex _rxPreprocessorLine = new Regex(@"^\s*#(?:if|ifdef|ifndef|elif|else|endif|define|undef)");
		private Regex _rxCaseLine = new Regex(@"^\s*(case\s+.*\:|default\:|before\s+group.*\:|after\s+group.*\:|for\s+each\s*\:)");

		public ProbeSmartIndent(ITextView view)
		{
			_view = view;
		}

		public void Dispose()
		{
		}

		int? ISmartIndent.GetDesiredIndentation(ITextSnapshotLine line)
		{
			if (line.LineNumber == 0) return 0;

			_tabSize = _view.Options.GetOptionValue<int>(DefaultOptions.TabSizeOptionId);
			_keepTabs = !_view.Options.GetOptionValue<bool>(DefaultOptions.ConvertTabsToSpacesOptionId);

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
						return span.Start.GetContainingLine().GetText().GetIndentCount(_tabSize);
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
				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_view.TextBuffer);
				if (fileStore != null)
				{
					var model = fileStore.GetCurrentModel(_view.TextBuffer.CurrentSnapshot, "Smart indenting - case inside switch");
					var offset = line.Snapshot.TranslateOffsetToSnapshot(line.Start.Position, model.Snapshot);
					var bracesToken = model.File.FindDownward(offset, t => t is CodeModel.Tokens.BracesToken).LastOrDefault() as CodeModel.Tokens.BracesToken;
					if (bracesToken != null)
					{
						// Get the indent of the line where the opening brace resides.
						var openOffset = bracesToken.OpenToken.Span.Start;
						openOffset = model.Snapshot.TranslateOffsetToSnapshot(openOffset, line.Snapshot);
						var openLine = line.Snapshot.GetLineFromPosition(openOffset);
						return openLine.GetText().GetIndentCount(_tabSize);
					}
				}
			}

			// If we got to this point, then the default smart indenting is to be used.
			{
				var prevLine = GetPreviousCodeLine(line);
				if (prevLine != null)
				{
					Classifier.TextBufferStateTracker tracker;
					if (prevLine.Snapshot.TextBuffer.Properties.TryGetProperty<Classifier.TextBufferStateTracker>(typeof(Classifier.TextBufferStateTracker), out tracker))
					{
						var lineNumber = prevLine.LineNumber;
						var state = tracker.GetStateForLine(lineNumber, tracker.Snapshot);
						while (Classifier.ProbeClassifierScanner.StateInsideComment(state))
						{
							if (lineNumber == 0) break;	// At start of file. In theory, this should never happen as the state for the start of the file is always zero.
							state = tracker.GetStateForLine(--lineNumber, tracker.Snapshot);
						}

						if (prevLine.LineNumber != lineNumber) prevLine = prevLine.Snapshot.GetLineFromLineNumber(lineNumber);
					}

					var prevLineText = prevLine.GetText().TrimEnd();
					if (prevLineText.EndsWith("{") || _rxCaseLine.IsMatch(prevLineText))
					{
						return prevLineText.GetIndentCount(_tabSize).AddIndentTab(_tabSize);
					}
					else
					{
						return prevLineText.GetIndentCount(_tabSize);
					}
				}
				return 0;
			}
		}

		private ITextSnapshotLine GetPreviousCodeLine(ITextSnapshotLine line)
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
	}
}
