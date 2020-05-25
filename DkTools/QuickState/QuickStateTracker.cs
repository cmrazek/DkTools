using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace DkTools
{
	class QuickStateTracker
	{
		private ITextBuffer _textBuffer;
		private List<int> _lines = new List<int>();

		public QuickStateTracker(ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
			_textBuffer.Changed += TextBuffer_Changed;
		}

		private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
		{
			// Find the earliest position that changed
			var startPos = -1;
			foreach (var change in e.Changes)
			{
				if (change.NewPosition < startPos || startPos == -1)
				{
					startPos = change.NewPosition;
				}
			}

			if (startPos >= 0 && startPos <= e.After.Length)
			{
				var snapLine = e.After.GetLineFromPosition(startPos);
				Invalidate(snapLine.LineNumber);
			}
		}

		private int GetStateForLine(int lineNumber)
		{
			if (_lines.Count >= lineNumber)
			{
				if (lineNumber == 0)
				{
					return QuickState.Normal;
				}
				else
				{
					return _lines[lineNumber - 1];
				}
			}

			var state = _lines.Count == 0 ? QuickState.Normal : _lines[_lines.Count - 1];
			var snapshot = _textBuffer.CurrentSnapshot;
			while (_lines.Count < lineNumber)
			{
				var snapLine = snapshot.GetLineFromLineNumber(_lines.Count);
				_lines.Add(state = ComputeState(snapLine.GetText(), state));
			}

			return _lines[lineNumber - 1];
		}

		public int GetStateForPosition(SnapshotPoint snapPt)
		{
			if (snapPt.Snapshot != _textBuffer.CurrentSnapshot)
			{
				snapPt = snapPt.TranslateTo(_textBuffer.CurrentSnapshot, PointTrackingMode.Positive);
			}

			var snapLine = snapPt.GetContainingLine();
			int state = GetStateForLine(snapLine.LineNumber);
			return ComputeState(snapLine.GetTextUpToPosition(snapPt), state);
		}

		private void Invalidate(int lineNumber)
		{
			if (_lines.Count >= lineNumber)
			{
				_lines.RemoveRange(lineNumber, _lines.Count - lineNumber);
			}
		}

		private int ComputeState(string source, int state, int stopPos = -1)
		{
			state &= ~(QuickState.SingleLineComment | QuickState.StringLiteral | QuickState.CharLiteral | QuickState.EscapeChar);

			if (stopPos < 0) stopPos = source.Length;
			for (int pos = 0; pos < stopPos; ++pos)
			{
				char ch = source[pos];
				if ((state & QuickState.StringLiteral) != 0)
				{
					if ((state & QuickState.EscapeChar) != 0) state &= ~QuickState.EscapeChar;
					else if (ch == '\"') state &= ~QuickState.StringLiteral;
					else if (ch == '\\') state |= QuickState.EscapeChar;
				}
				else if ((state & QuickState.CharLiteral) != 0)
				{
					if ((state & QuickState.EscapeChar) != 0) state &= ~QuickState.EscapeChar;
					else if (ch == '\'') state &= ~QuickState.StringLiteral;
					else if (ch == '\\') state |= QuickState.EscapeChar;
				}
				else if ((state & QuickState.SingleLineComment) != 0)
				{ }
				else if ((state & QuickState.MultiLineMask) != 0)
				{
					if (ch == '/' && pos > 0 && source[pos - 1] == '*')
					{
						int level = (state & QuickState.MultiLineMask) >> QuickState.MultiLineShift;
						level--;
						state = (state & ~QuickState.MultiLineMask) | (level << QuickState.MultiLineShift);
					}
					else if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '*')
					{
						int level = (state & QuickState.MultiLineMask) >> QuickState.MultiLineShift;
						level++;
						state = (state & ~QuickState.MultiLineMask) | ((level << QuickState.MultiLineShift) & QuickState.MultiLineMask);
					}
				}
				else // Normal text
				{
					if (ch == '\"') state |= QuickState.StringLiteral;
					else if (ch == '\'') state |= QuickState.CharLiteral;
					else if (ch == '/' && pos + 1 < source.Length)
					{
						ch = source[pos + 1];
						if (ch == '/') state |= QuickState.SingleLineComment;
						else if (ch == '*') state = (state & ~QuickState.MultiLineMask) | (1 << QuickState.MultiLineShift);
					}
				}
			}

			return state;
		}
	}

	static class QuickState
	{
		public const int Normal = 0x00;
		public const int StringLiteral = 0x01;
		public const int CharLiteral = 0x02;
		public const int EscapeChar = 0x04;
		public const int SingleLineComment = 0x08;
		public const int MultiLineShift = 16;
		public const int MultiLineMask = 0x00ff0000;

		public static QuickStateTracker GetQuickStateTracker(this ITextBuffer textBuffer)
		{
			if (textBuffer.Properties.TryGetProperty(typeof(QuickStateTracker), out QuickStateTracker tracker))
			{
				return tracker;
			}

			var newTracker = new QuickStateTracker(textBuffer);
			textBuffer.Properties.AddProperty(typeof(QuickStateTracker), newTracker);
			return newTracker;
		}

		public static int GetQuickState(this SnapshotPoint snapPt)
		{
			var tracker = snapPt.Snapshot.TextBuffer.GetQuickStateTracker();
			return tracker.GetStateForPosition(snapPt);
		}

		public static bool IsInLiveCode(this SnapshotPoint snapPt)
		{
			var tracker = snapPt.Snapshot.TextBuffer.GetQuickStateTracker();
			return IsInLiveCode(tracker.GetStateForPosition(snapPt));
		}

		public static bool IsInLiveCode(int state)
		{
			return (state & (StringLiteral | CharLiteral | SingleLineComment | MultiLineMask)) == 0;
		}

		public static bool IsInMultiLineComment(int state)
		{
			return (state & MultiLineMask) != 0;
		}
	}
}
