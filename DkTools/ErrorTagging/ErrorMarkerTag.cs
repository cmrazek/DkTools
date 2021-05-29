using DK.Code;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace DkTools.ErrorTagging
{
	class ErrorMarkerTag : TextMarkerTag
	{
		private ErrorTaskSource _source;
		private string _filePath;
		private CodeSpan _span;
		private Dictionary<ITextBuffer, SnapshotSpan> _snapshotSpans;

		public ErrorMarkerTag(ErrorTaskSource source, string tagType, string filePath, CodeSpan span)
			: base(tagType)
		{
			_source = source;
			_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
			_span = span;
		}

		public string FilePath => _filePath;
		public ErrorTaskSource Source => _source;

		public SnapshotSpan? TryGetSnapshotSpan(ITextSnapshot currentSnapshot)
		{
			if (currentSnapshot == null) throw new ArgumentNullException(nameof(currentSnapshot));

			if (_snapshotSpans == null) _snapshotSpans = new Dictionary<ITextBuffer, SnapshotSpan>();

			if (_snapshotSpans.TryGetValue(currentSnapshot.TextBuffer, out var taskSnapshotSpan))
			{
				if (taskSnapshotSpan.Snapshot.Version != currentSnapshot.Version)
				{
					taskSnapshotSpan = taskSnapshotSpan.TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
					_snapshotSpans[currentSnapshot.TextBuffer] = taskSnapshotSpan;
				}

				return taskSnapshotSpan;
			}

			var clampedSpan = _span.Intersection(new CodeSpan(0, currentSnapshot.Length));
			if (clampedSpan.IsEmpty) return null;
			taskSnapshotSpan = clampedSpan.ToVsTextSnapshotSpan(currentSnapshot);

			_snapshotSpans[currentSnapshot.TextBuffer] = taskSnapshotSpan;
			return taskSnapshotSpan;
		}
	}
}
