// TODO: remove this file
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.Text.Editor;
//using VsText = Microsoft.VisualStudio.Text;

//namespace DkTools.Navigation
//{
//	internal class ReferenceScroller
//	{
//		public static void GoToNextOrPrevReference(ITextView view, bool next)
//		{
//			if (view == null) return;

//			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(view.TextBuffer);
//			if (fileStore == null)
//			{
//				Log.WriteDebug("No file store available.");
//				return;
//			}
//			var model = fileStore.GetMostRecentModel(view.TextSnapshot, "ReferenceScroller.GoToNextReference()");

//			// Get the caret position
//			var caretPtTest = view.Caret.Position.Point.GetPoint(buf => (!buf.ContentType.IsOfType("projection")), Microsoft.VisualStudio.Text.PositionAffinity.Predecessor);
//			if (!caretPtTest.HasValue)
//			{
//				Log.WriteDebug("Couldn't get caret point.");
//				return;
//			}
//			var caretPt = caretPtTest.Value;
//			var modelPos = caretPt.Snapshot.TranslateOffsetToSnapshot(caretPt.Position, model.Snapshot);

//			// Get the token the cursor is currently on
//			var token = model.File.FindDownward(modelPos).LastOrDefault(x => x.SourceDefinition != null);
//			if (token == null)
//			{
//				Shell.SetStatusText("No reference found at this position.");
//				return;
//			}
//			var def = token.SourceDefinition;

//			// Find all references
//			var refs = model.File.FindDownward(t => t.SourceDefinition == def).ToArray();
//			if (refs.Length == 0)
//			{
//				Log.WriteDebug("List of references is empty.");
//				return;
//			}

//			// Find the current reference in the index
//			var refIndex = -1;
//			for (int i = 0; i < refs.Length; i++)
//			{
//				if (refs[i] == token)
//				{
//					refIndex = i;
//					break;
//				}
//			}
//			if (refIndex == -1)
//			{
//				Log.WriteDebug("The current token couldn't be found in the reference list.");
//				return;
//			}

//			var nextIndex = -1;
//			if (next)
//			{
//				if (refIndex + 1 < refs.Length) nextIndex = refIndex + 1;
//				else nextIndex = 0;
//			}
//			else
//			{
//				if (refIndex > 0) nextIndex = refIndex - 1;
//				else nextIndex = refs.Length - 1;
//			}
//			var nextToken = refs[nextIndex];

//			var snapStart = model.Snapshot.TranslateOffsetToSnapshot(nextToken.Span.Start, caretPt.Snapshot);
//			var snapEnd = model.Snapshot.TranslateOffsetToSnapshot(nextToken.Span.End, caretPt.Snapshot);
//			var snapSpan = new VsText.SnapshotSpan(caretPt.Snapshot, snapStart, snapEnd - snapStart);
//			Shell.MoveTo(view, snapSpan);

//			Shell.SetStatusText(string.Format("Reference {0} of {1}.", nextIndex + 1, refs.Length));
//		}
//	}
//}
