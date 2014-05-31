using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace DkTools
{
	internal static class VsTextUtil
	{
		public static string GetLineTextUpToPosition(this ITextSnapshot snapshot, int pos)
		{
			var line = snapshot.GetLineFromPosition(pos);
			var lineStart = line.Start.Position;
			var lineText = line.GetText();
			if (pos - lineStart <= lineText.Length) return lineText.Substring(0, pos - lineStart);
			return string.Empty;
		}

		public static int TranslateOffsetToSnapshot(this ITextSnapshot fromSnap, int offset, ITextSnapshot toSnap)
		{
			if (fromSnap != toSnap)
			{
				var pt = new Microsoft.VisualStudio.Text.SnapshotPoint(fromSnap, offset);
				return pt.TranslateTo(toSnap, Microsoft.VisualStudio.Text.PointTrackingMode.Positive).Position;
			}
			else
			{
				return offset;
			}
		}

		public static SnapshotSpan ModelSpanToVsSnapshotSpan(ITextSnapshot modelSnapshot, CodeModel.Span modelSpan, ITextSnapshot targetSnapshot, SpanTrackingMode trackingMode = SpanTrackingMode.EdgeExclusive)
		{
#if DEBUG
			if (modelSnapshot == null) throw new ArgumentNullException("modelSnapshot");
			if (targetSnapshot == null) throw new ArgumentNullException("targetSnapshot");
#endif

			var snapSpan = new SnapshotSpan(modelSnapshot, new Span(modelSpan.Start.Offset, modelSpan.End.Offset - modelSpan.Start.Offset));
			if (modelSnapshot != targetSnapshot) snapSpan = snapSpan.TranslateTo(targetSnapshot, trackingMode);
			return snapSpan;
		}

		public static SnapshotSpan? EncompassingSpan(this IEnumerable<SnapshotSpan> spans)
		{
			if (!spans.Any()) return null;

			SnapshotPoint? start = null;
			SnapshotPoint? end = null;

			foreach (var span in spans)
			{
				if (!start.HasValue || span.Start.Position < start.Value.Position)
				{
					start = span.Start;
				}

				if (!end.HasValue || span.End.Position > end.Value.Position)
				{
					end = span.End;
				}
			}

			return new SnapshotSpan(start.Value, end.Value);
		}

		public static SnapshotSpan EncompassingSpan(this SnapshotSpan first, SnapshotSpan second, SpanTrackingMode spanTrackingMode = SpanTrackingMode.EdgeExclusive)
		{
			var secondTrans = second.TranslateTo(first.Snapshot, SpanTrackingMode.EdgeExclusive);

			return new SnapshotSpan(first.Start.Position < secondTrans.Start.Position ? first.Start : secondTrans.Start,
				first.End.Position > secondTrans.End.Position ? first.End : secondTrans.End);
		}

		public static SnapshotSpan EncompassingSpan(this SnapshotSpan thisSpan, IEnumerable<SnapshotSpan> spans, SpanTrackingMode spanTrackingMode = SpanTrackingMode.EdgeExclusive)
		{
			var ret = thisSpan;
			foreach (var span in spans) ret = ret.EncompassingSpan(span, spanTrackingMode);
			return ret;
		}

		public static SnapshotSpan? EncompassingSpan(this SnapshotSpan? thisSpan, IEnumerable<SnapshotSpan> spans, SpanTrackingMode spanTrackingMode = SpanTrackingMode.EdgeExclusive)
		{
			if (thisSpan.HasValue) return thisSpan.Value.EncompassingSpan(spans, spanTrackingMode);
			return spans.EncompassingSpan();
		}
	}
}
