using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

namespace DkTools.Navigation
{
	internal sealed class Navigator
	{
		private IWpfTextView _view;

		/// <summary>
		/// The outlining manager for this view. (could potentially be null)
		/// </summary>
		private IOutliningManager _outliningManager;

		public Navigator(IWpfTextView wpfView, IOutliningManager outMgr)
		{
			_view = wpfView;
			_outliningManager = outMgr;
		}

		public static Navigator TryGetForView(IWpfTextView view)
		{
			Navigator nav;
			if (view.Properties.TryGetProperty(typeof(Navigator), out nav) && nav != null) return nav;
			return null;
		}

		public void MoveTo(SnapshotSpan span)
		{
			if (_outliningManager != null)
			{
				foreach (var region in _outliningManager.GetCollapsedRegions(span, false))
				{
					_outliningManager.Expand(region);
				}
			}

			if (span.Snapshot != _view.TextSnapshot)
			{
				span = span.TranslateTo(_view.TextSnapshot, SpanTrackingMode.EdgeExclusive);
			}

			_view.Caret.MoveTo(span.Start);
			_view.Selection.Select(span, false);
			_view.ViewScroller.EnsureSpanVisible(span);
		}

		public void MoveTo(SnapshotPoint pt)
		{
			MoveTo(new SnapshotSpan(pt.Snapshot, new Span(pt.Position, 0)));
		}

		public void GoToNextOrPrevReference(bool next)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_view.TextBuffer);
			if (fileStore == null)
			{
				Log.Debug("No file store available.");
				return;
			}
			var model = fileStore.GetMostRecentModel(_view.TextSnapshot, "ReferenceScroller.GoToNextReference()");

			// Get the caret position
			var caretPtTest = _view.Caret.Position.Point.GetPoint(buf => (!buf.ContentType.IsOfType("projection")), Microsoft.VisualStudio.Text.PositionAffinity.Predecessor);
			if (!caretPtTest.HasValue)
			{
				Log.Debug("Couldn't get caret point.");
				return;
			}
			var caretPt = caretPtTest.Value;
			var modelPos = caretPt.Snapshot.TranslateOffsetToSnapshot(caretPt.Position, model.Snapshot);

			// Get the token the cursor is currently on
			var token = model.File.FindDownward(modelPos).LastOrDefault(x => x.SourceDefinition != null);
			if (token == null)
			{
				Shell.SetStatusText("No reference found at this position.");
				return;
			}
			var def = token.SourceDefinition;

			// Find all references
			var refs = model.File.FindDownward(t => t.SourceDefinition == def).ToArray();
			if (refs.Length == 0)
			{
				Log.Debug("List of references is empty.");
				return;
			}

			// Find the current reference in the index
			var refIndex = -1;
			for (int i = 0; i < refs.Length; i++)
			{
				if (refs[i] == token)
				{
					refIndex = i;
					break;
				}
			}
			if (refIndex == -1)
			{
				Log.Debug("The current token couldn't be found in the reference list.");
				return;
			}

			var nextIndex = -1;
			if (next)
			{
				if (refIndex + 1 < refs.Length) nextIndex = refIndex + 1;
				else nextIndex = 0;
			}
			else
			{
				if (refIndex > 0) nextIndex = refIndex - 1;
				else nextIndex = refs.Length - 1;
			}
			var nextToken = refs[nextIndex];

			var snapStart = model.Snapshot.TranslateOffsetToSnapshot(nextToken.Span.Start, caretPt.Snapshot);
			var snapEnd = model.Snapshot.TranslateOffsetToSnapshot(nextToken.Span.End, caretPt.Snapshot);
			var snapSpan = new SnapshotSpan(caretPt.Snapshot, snapStart, snapEnd - snapStart);
			MoveTo(snapSpan);

			Shell.SetStatusText(string.Format("Reference {0} of {1}.", nextIndex + 1, refs.Length));
		}
	}
}
