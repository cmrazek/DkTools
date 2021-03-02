using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using DkTools.CodeModel;
using DkTools.CodeModel.Tokens;

namespace DkTools.Navigation
{
	internal class GoToBraceHelper
	{
		public static void Trigger(ITextView view, bool extend)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var caretPtTest = view.Caret.Position.Point.GetPoint(buf => (!buf.ContentType.IsOfType("projection")), Microsoft.VisualStudio.Text.PositionAffinity.Predecessor);
			if (!caretPtTest.HasValue)
			{
				Log.Debug("Couldn't get caret point.");
				return;
			}
			var caretPt = caretPtTest.Value;

			var store = FileStore.GetOrCreateForTextBuffer(view.TextBuffer);
			if (store == null) return;

			var appSettings = DkEnvironment.CurrentAppSettings;
			var fileName = VsTextUtil.TryGetDocumentFileName(view.TextBuffer);
			var model = store.GetMostRecentModel(appSettings, fileName, view.TextSnapshot, "GoToBraceHelper.Trigger()");

			var modelPos = model.AdjustPosition(caretPt.Position, caretPt.Snapshot);
			var selTokens = model.File.FindDownwardTouching(modelPos).ToArray();
			if (selTokens.Length == 0)
			{
				Log.Debug("Touching no tokens.");
				return;
			}

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
					var snapPos = model.Snapshot.TranslateOffsetToSnapshot(selToken.Span.Start, caretPt.Snapshot);

					var snapPt = new SnapshotPoint(caretPt.Snapshot, snapPos);
					view.Caret.MoveTo(snapPt);
					view.Selection.Select(new SnapshotSpan(snapPt, 0), false);
					view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(snapPt, 0));
				}
			}
			else
			{
				var token = selTokens.LastOrDefault(t => t is BracesToken) as BracesToken;
				if (token != null)
				{
					var startPos = model.Snapshot.TranslateOffsetToSnapshot(token.OpenToken.Span.Start, caretPt.Snapshot);
					var endPos = model.Snapshot.TranslateOffsetToSnapshot(token.CloseToken.Span.End, caretPt.Snapshot);

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
