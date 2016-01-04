using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace DkTools.ErrorTagging
{
	class ErrorTaskProvider : TaskProvider
	{
		public static ErrorTaskProvider Instance;

		public ErrorTaskProvider(IServiceProvider provider)
			: base(provider)
		{
			Instance = this;
		}

		public void Add(ErrorTask task)
		{
#if DEBUG
			if (task == null) throw new ArgumentNullException("task");
#endif
			Tasks.Add(task);
		}

		public void Clear()
		{
			Tasks.Clear();
		}

		public void RemoveAllForSource(ErrorTaskSource source, string sourceArg)
		{
			var tasksToRemove = (from t in Tasks.Cast<ErrorTask>() where t.Source == source && t.SourceArg == sourceArg select t).ToArray();
			foreach (var task in tasksToRemove) Tasks.Remove(task);
		}

		public void RemoveAllForFile(string fileName)
		{
			var tasksToRemove = (from t in Tasks.Cast<ErrorTask>() where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t).ToArray();
			foreach (var task in tasksToRemove) Tasks.Remove(task);
		}

		public void RemoveAllForSourceAndFile(ErrorTaskSource source, string sourceArg, string fileName)
		{
			var tasksToRemove = (from t in Tasks.Cast<ErrorTask>()
								 where t.Source == source && t.SourceArg == sourceArg &&
									 string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase)
								 select t).ToArray();
			foreach (var task in tasksToRemove) Tasks.Remove(task);
		}

		public void OnDocumentClosed(ITextView textView)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(textView.TextBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(textView.TextSnapshot, "ErrorTaskProvider.OnDocumentClosed()");
				RemoveAllForSource(ErrorTaskSource.BackgroundFec, model.FileName.ToLower());
			}
		}

		public IEnumerable<ITagSpan<ErrorTag>> GetErrorTagsForFile(string fileName, NormalizedSnapshotSpanCollection spans)
		{
			var firstSpan = spans.FirstOrDefault();
			if (firstSpan == null) yield break;
			var snapshot = firstSpan.Snapshot;

			foreach (var task in (from t in Tasks.Cast<ErrorTask>() where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t))
			{
				var line = task.GetSnapshot(snapshot).GetLineFromLineNumber(task.Line);
				foreach (var span in spans)
				{
					var spanLineStart = line.Start.TranslateTo(span.Snapshot, PointTrackingMode.Negative);
					if (span.Contains(spanLineStart.Position))
					{
						var errorCode = task.Type == ErrorType.Warning ? ErrorCode.Fec_Warning : ErrorCode.Fec_Error;

						var spanLine = span.Snapshot.GetLineFromPosition(spanLineStart);
						var startPos = spanLine.Start.Position;
						var endPos = spanLine.End.Position;
						if (startPos < endPos)
						{
							var newStartPos = spanLine.Start.Position + spanLine.GetText().GetIndentOffset();
							if (newStartPos < endPos) startPos = newStartPos;
						}

						yield return new TagSpan<ErrorTag>(new SnapshotSpan(span.Snapshot, startPos, endPos - startPos),
							new ErrorTag(task.Type == ErrorType.Warning ? ErrorTagger.CodeWarning : ErrorTagger.CodeError, task.Text));
						break;
					}
				}
			}
		}

		public IEnumerable<ErrorTask> GetErrorMessagesAtPoint(string fileName, SnapshotPoint pt)
		{
			foreach (var task in (from t in Tasks.Cast<ErrorTask>() where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t))
			{
				var taskLine = task.GetSnapshot(pt.Snapshot).GetLineFromLineNumber(task.Line);
				var taskSpan = new SnapshotSpan(taskLine.Start, taskLine.End);
				if (taskSpan.Contains(pt.TranslateTo(taskSpan.Snapshot, PointTrackingMode.Positive)))
				{
					yield return task;
				}
			}
		}
	}
}
