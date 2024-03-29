﻿using DK.Modeling.Tokens;
using DkTools.CodeModeling;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System.Threading;

namespace DkTools.Navigation
{
	internal class GoToBraceHelper
	{
		public static void Trigger(ITextView view, bool extend, CancellationToken cancel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var caretPtTest = view.Caret.Position.Point.GetPoint(buf => (!buf.ContentType.IsOfType("projection")), Microsoft.VisualStudio.Text.PositionAffinity.Predecessor);
			if (!caretPtTest.HasValue) return;
			var caretPt = caretPtTest.Value;

			var store = FileStoreHelper.GetOrCreateForTextBuffer(view.TextBuffer);
			if (store == null) return;

			var fileName = VsTextUtil.TryGetDocumentFileName(view.TextBuffer);
			var model = store.Model;
			if (model == null) return;

			var modelPos = model.AdjustPosition(caretPt.Position, caretPt.Snapshot);
			var selTokens = model.File.FindDownwardTouching(modelPos).ToArray();
			if (selTokens.Length == 0) return;

			if (!extend)
			{
				Token selToken = null;
				var token = selTokens.LastOrDefault(t => t is BraceToken || t is BracesToken);
				if (token is BracesToken)
				{
					selToken = (token as BracesToken).OpenToken;
				}
				else if (token is BraceToken)
				{
					var bracesToken = ((token as BraceToken).Parent as BracesToken);
					if (bracesToken != null)
					{
						selToken = token == bracesToken.OpenToken ? bracesToken.CloseToken : bracesToken.OpenToken;
					}
				}

				if (selToken != null)
				{
					var modelSnapshot = model.Snapshot as ITextSnapshot;
					if (modelSnapshot != null)
					{
						var snapPos = modelSnapshot.TranslateOffsetToSnapshot(selToken.Span.Start, caretPt.Snapshot);

						var snapPt = new SnapshotPoint(caretPt.Snapshot, snapPos);
						view.Caret.MoveTo(snapPt);
						view.Selection.Select(new SnapshotSpan(snapPt, 0), false);
						view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(snapPt, 0));
					}
				}
			}
			else
			{
				var token = selTokens.LastOrDefault(t => t is BracesToken) as BracesToken;
				if (token != null)
				{
					var modelSnapshot = model.Snapshot as ITextSnapshot;
					if (modelSnapshot != null)
					{
						var startPos = modelSnapshot.TranslateOffsetToSnapshot(token.OpenToken.Span.Start, caretPt.Snapshot);
						var endPos = modelSnapshot.TranslateOffsetToSnapshot(token.CloseToken.Span.End, caretPt.Snapshot);

						var snapPt = new SnapshotPoint(caretPt.Snapshot, startPos);
						view.Caret.MoveTo(snapPt);

						var snapSpan = new SnapshotSpan(caretPt.Snapshot, startPos, endPos - startPos);
						view.Selection.Select(snapSpan, false);
						view.ViewScroller.EnsureSpanVisible(snapSpan);
					}
				}
			}
		}
	}
}
