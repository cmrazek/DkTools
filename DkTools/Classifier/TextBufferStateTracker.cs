using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace DkTools.Classifier
{
	internal class TextBufferStateTracker
	{
		private string _data = Guid.NewGuid().ToString();
		private ITextBuffer _buffer;
		private ProbeClassifierScanner _scanner;
		private ITextSnapshot _snapshot;
		private List<int> _states = new List<int>();

		public TextBufferStateTracker(ITextBuffer buffer)
		{
			_buffer = buffer;
			_buffer.Changed += new EventHandler<TextContentChangedEventArgs>(Buffer_Changed);

			_scanner = new ProbeClassifierScanner();
		}

		public static TextBufferStateTracker GetTrackerForTextBuffer(ITextBuffer buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");

			TextBufferStateTracker tracker;
			buffer.Properties.TryGetProperty(typeof(TextBufferStateTracker), out tracker);
			if (tracker != null) return tracker;
			
			tracker = new TextBufferStateTracker(buffer);
			buffer.Properties.AddProperty(typeof(TextBufferStateTracker), tracker);
			return tracker;
		}

		private void Buffer_Changed(object sender, TextContentChangedEventArgs e)
		{
			var purgeLineNum = int.MaxValue;
			foreach (var change in e.Changes)
			{
				var oldLineNum = e.Before.GetLineFromPosition(change.OldPosition).LineNumber;
				var newLineNum = e.After.GetLineFromPosition(change.NewPosition).LineNumber;
				var changeLineNum = oldLineNum < newLineNum ? oldLineNum : newLineNum;
				if (changeLineNum < purgeLineNum) purgeLineNum = changeLineNum;
			}

			PurgeStatesOnOrAfterLine(purgeLineNum);
		}

		private void PurgeStatesOnOrAfterLine(int purgeLineNum)
		{
			if (purgeLineNum < _states.Count)
			{
				_states.RemoveRange(purgeLineNum, _states.Count - purgeLineNum);
			}
		}

		public int GetStateForPosition(int pos, ITextSnapshot snapshot)
		{
			_snapshot = snapshot;

			var line = _snapshot.GetLineFromPosition(pos);
			var state = GetStateForLine(line.LineNumber, snapshot);
			var lineStartPos = line.Start.Position;

			var model = CodeModelStore.GetModelForBuffer(snapshot.TextBuffer, null, true);

			if (lineStartPos < pos)
			{
				var lineText = line.GetTextIncludingLineBreak();
				if (pos - lineStartPos < lineText.Length) lineText = lineText.Substring(0, pos - lineStartPos);

				_scanner.SetSource(lineText, line.Start.Position, line.Snapshot, model);

				var tokenInfo = new ProbeClassifierScanner.TokenInfo();
				while (_scanner.ScanTokenAndProvideInfoAboutIt(tokenInfo, ref state)) ;
			}

			return state;
		}

		public int GetStateForLine(int lineNum, ITextSnapshot snapshot)
		{
			var state = 0;
			if (_states.Count <= lineNum)
			{
				var stateLineNum = _states.Count - 1;
				if (stateLineNum < 0)
				{
					stateLineNum = 0;
					state = 0;
				}
				else
				{
					state = _states[stateLineNum];
				}

				var model = CodeModelStore.GetModelForBuffer(snapshot.TextBuffer, null, true);
				_snapshot = snapshot;

				var tokenInfo = new ProbeClassifierScanner.TokenInfo();

				while (stateLineNum < lineNum)
				{
					var line = _snapshot.GetLineFromLineNumber(stateLineNum);
					if (line == null) break;

					_scanner.SetSource(line.GetTextIncludingLineBreak(), line.Start.Position, _snapshot, model);
					while (_scanner.ScanTokenAndProvideInfoAboutIt(tokenInfo, ref state)) ;

					_states.Add(state);
					stateLineNum++;
				}

				if (_states.Count <= lineNum) return 0;
			}

			return _states[lineNum];
		}
	}
}
