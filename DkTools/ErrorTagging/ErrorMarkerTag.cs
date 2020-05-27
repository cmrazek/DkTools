using DkTools.CodeModel;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ErrorTagging
{
	class ErrorMarkerTag : TextMarkerTag
	{
		private ErrorTaskSource _source;
		private string _filePath;
		private CodeModel.Span _span;
		private Dictionary<ITextBuffer, SnapshotSpan> _snapshotSpans;

		public ErrorMarkerTag(ErrorTaskSource source, string tagType, string filePath, CodeModel.Span span)
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

			var clampedSpan = _span.Intersection(new CodeModel.Span(0, currentSnapshot.Length));
			if (clampedSpan.IsEmpty) return null;
			taskSnapshotSpan = clampedSpan.ToVsTextSnapshotSpan(currentSnapshot);

			_snapshotSpans[currentSnapshot.TextBuffer] = taskSnapshotSpan;
			return taskSnapshotSpan;
		}
	}
}
