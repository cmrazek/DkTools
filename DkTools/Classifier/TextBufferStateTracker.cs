using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace DkTools.Classifier
{
	internal class TextBufferStateTracker
	{
		private string _data = Guid.NewGuid().ToString();
		private ITextBuffer _buffer;
		private ProbeClassifierScanner _scanner;
		private ITextSnapshot _snapshot;
		private List<long> _states = new List<long>();

		public TextBufferStateTracker(ITextBuffer buffer)
		{
			_buffer = buffer;
			_buffer.Changed += new EventHandler<TextContentChangedEventArgs>(Buffer_Changed);

			_scanner = new ProbeClassifierScanner();

			// First line always has a zero state.
			_states.Add(0);
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
			lock (this)
			{
				if (purgeLineNum < _states.Count)
				{
					_states.RemoveRange(purgeLineNum, _states.Count - purgeLineNum);
				}
			}
		}

		public long GetStateForPosition(SnapshotPoint snapPt, string fileName, ProbeAppSettings appSettings)
		{
			return GetStateForPosition(snapPt.Position, snapPt.Snapshot, fileName, appSettings);
		}

		public long GetStateForPosition(int pos, ITextSnapshot snapshot, string fileName, ProbeAppSettings appSettings)
		{
			lock (this)
			{
				_snapshot = snapshot;

				if (pos < 0) pos = 0;
				if (pos > snapshot.Length) pos = snapshot.Length;

				var line = _snapshot.GetLineFromPosition(pos);
				var state = GetStateForLineStart(line.LineNumber, snapshot, fileName, appSettings);
				var lineStartPos = line.Start.Position;

				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(snapshot.TextBuffer);
				if (fileStore == null) return 0;

				var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "GetStateForPosition");

				if (lineStartPos <= pos)
				{
					var lineText = line.GetTextIncludingLineBreak();
					if (pos - lineStartPos < lineText.Length) lineText = lineText.Substring(0, pos - lineStartPos);

					_scanner.SetSource(lineText, line.Start.Position, line.Snapshot, model);

					var tokenInfo = new ProbeClassifierScanner.TokenInfo();
					while (_scanner.ScanTokenAndProvideInfoAboutIt(tokenInfo, ref state)) ;
				}

				return state;
			}
		}

		public long GetStateForLineStart(int lineNum, ITextSnapshot snapshot, string fileName, ProbeAppSettings appSettings)
		{
			lock (this)
			{
				if (_states.Count <= lineNum)
				{
					if (_states.Count == 0)
					{
						_states.Add(0);
						if (lineNum == 0) return 0;
					}

					var stateLineNum = _states.Count - 1;
					var state = _states[stateLineNum];

					var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(snapshot.TextBuffer);
					if (fileStore == null) return 0;

					var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "GetStateForLine()");
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

		public ITextSnapshot Snapshot
		{
			get
			{
				lock (this)
				{
					return _snapshot;
				}
			}
		}

		/// <summary>
		/// Returns true if the position is not inside a comment, string literal or disabled code.
		/// </summary>
		public bool IsPositionInLiveCode(int pos, ITextSnapshot snapshot, string fileName, ProbeAppSettings appSettings)
		{
            return State.IsInLiveCode(GetStateForPosition(pos, snapshot, fileName, appSettings));
		}
	}
}
