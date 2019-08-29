using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

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

		public static string GetLineTextAfterPosition(this ITextSnapshot snapshot, int pos)
		{
			var line = snapshot.GetLineFromPosition(pos);
			var lineStart = line.Start.Position;
			var lineText = line.GetText();
			if (pos - lineStart <= lineText.Length) return lineText.Substring(pos - lineStart);
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

			var snapSpan = new SnapshotSpan(modelSnapshot, new Span(modelSpan.Start, modelSpan.End - modelSpan.Start));
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

		/// <summary>
		/// Gets the file name for the text buffer.
		/// This must be run on the UI thread.
		/// </summary>
		/// <param name="buffer">The text buffer for the document.</param>
		/// <returns>The file name if it could be retrieved; otherwise null.</returns>
		public static string TryGetDocumentFileName(this ITextBuffer buffer)
		{
			// http://social.msdn.microsoft.com/Forums/vstudio/en-US/ef5cd137-56e4-4077-8e31-6d282668e8ad/filename-from-itextbuffer-or-itextbuffer-from-projectitem

			ThreadHelper.ThrowIfNotOnUIThread();

			IVsTextBuffer bufferAdapter;
			if (!buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out bufferAdapter) || bufferAdapter == null) return null;

			var persistFileFormat = bufferAdapter as IPersistFileFormat;
			if (persistFileFormat == null) return null;

			string fileName = null;
			uint formatIndex = 0;
			persistFileFormat.GetCurFile(out fileName, out formatIndex);

			return fileName;
		}

		public static ITextUndoTransaction CreateUndoTransaction(this ITextBuffer buf, string description)
		{
			if (buf == null) return null;

			ITextBufferUndoManager manager;
			if (!buf.Properties.TryGetProperty(typeof(ITextBufferUndoManager), out manager) || manager == null) return null;

			return manager.TextBufferUndoHistory.CreateTransaction(description);
		}

		public static int GetTabSize(this ITextView view)
		{
			return view.Options.GetOptionValue<int>(DefaultOptions.TabSizeOptionId);
		}

		public static bool GetKeepTabs(this ITextView view)
		{
			return !view.Options.GetOptionValue<bool>(DefaultOptions.ConvertTabsToSpacesOptionId);
		}

		public static Span GetSpan(this ITextSnapshotLine line)
		{
			return new Span(line.Start.Position, line.End.Position - line.Start.Position);
		}
	}

	internal static class SnapshotPointEx
	{
		/// <summary>
		/// Returns true if the point is not inside a comment, string literal or disabled code.
		/// </summary>
		public static bool IsInLiveCode(this SnapshotPoint pt, string fileName, ProbeAppSettings appSettings)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

			var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(pt.Snapshot.TextBuffer);
			return Classifier.State.IsInLiveCode(tracker.GetStateForPosition(pt.Position, pt.Snapshot, fileName, appSettings));
		}

		/// <summary>
		/// Gets the text on the line up to this point.
		/// </summary>
		public static string GetPrecedingLineText(this SnapshotPoint pt)
		{
			return pt.Snapshot.GetLineTextUpToPosition(pt.Position);
		}

		/// <summary>
		/// Gets the classifier state for this position.
		/// </summary>
		public static int GetState(this SnapshotPoint pt, string fileName, ProbeAppSettings appSettings)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

			var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(pt.Snapshot.TextBuffer);
			return tracker.GetStateForPosition(pt.Position, pt.Snapshot, fileName, appSettings);
		}

		public static SnapshotSpan ToSnapshotSpan(this SnapshotPoint pt)
		{
			return new SnapshotSpan(pt, 0);
		}
	}

	internal static class ITextSnapshotLineEx
	{
		public static string GetTextUpToPosition(this ITextSnapshotLine line, SnapshotPoint position)
		{
			var matchPos = position.TranslateTo(line.Snapshot, PointTrackingMode.Positive);
			if (matchPos.Position < line.Start.Position || matchPos.Position > line.End.Position) throw new ArgumentOutOfRangeException(nameof(position));
			return line.Snapshot.GetText(line.Start.Position, matchPos.Position - line.Start.Position);
		}
	}
}
