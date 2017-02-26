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

		private object _tasksLock = new object();

		public event EventHandler<ErrorTaskEventArgs> ErrorTagsChangedForFile;
		public class ErrorTaskEventArgs : EventArgs
		{
			public string FileName { get; set; }
		}

		public ErrorTaskProvider(IServiceProvider provider)
			: base(provider)
		{
			Instance = this;
		}

		public void Add(ErrorTask task, bool dontSignalTagsChanged = false)
		{
#if DEBUG
			if (task == null) throw new ArgumentNullException("task");
#endif

			var taskLine = task.Line;
			var taskColumn = task.Column;
			var taskDocument = task.Document;
			var taskText = task.Text;
			if (Tasks.Cast<ErrorTask>().Any(t => t.Line == taskLine && t.Column == taskColumn &&
				string.Equals(t.Document, taskDocument, StringComparison.OrdinalIgnoreCase) &&
				t.Text == taskText))
			{
				return;
			}

			lock (_tasksLock)
			{
				Tasks.Add(task);
			}

			if (!dontSignalTagsChanged)
			{
				var ev = ErrorTagsChangedForFile;
				if (ev != null) ev(this, new ErrorTaskEventArgs { FileName = task.Document });
			}
		}

		public void Clear()
		{
			var tasks = Tasks.Cast<ErrorTask>().ToArray();

			lock (_tasksLock)
			{
				Tasks.Clear();
			}

			foreach (ErrorTask task in tasks)
			{
				var ev = ErrorTagsChangedForFile;
				if (ev != null) ev(this, new ErrorTaskEventArgs { FileName = task.Document });
			}
		}

		public void FireTagsChangedEvent()
		{
			var fileNames = new List<string>();

			foreach (var task in Tasks)
			{
				if (!(task is ErrorTask)) continue;
				var errorTask = task as ErrorTask;


				if (!fileNames.Contains(errorTask.Document))
				{
					var ev = ErrorTagsChangedForFile;
					if (ev != null) ev(this, new ErrorTaskEventArgs { FileName = errorTask.Document });

					fileNames.Add(errorTask.Document);
				}
			}
		}

		public void RemoveTask(ErrorTask task)
		{
			lock (_tasksLock)
			{
				Tasks.Remove(task);
			}

			var ev = ErrorTagsChangedForFile;
			if (ev != null) ev(this, new ErrorTaskEventArgs { FileName = task.Document });
		}

		public void RemoveAllForSource(ErrorTaskSource source, string sourceFileName)
		{
			var tasksToRemove = (from t in Tasks.Cast<ErrorTask>()
								 where t.Source == source && string.Equals(t.SourceArg, sourceFileName, StringComparison.OrdinalIgnoreCase)
								 select t).ToArray();
			foreach (var task in tasksToRemove)
			{
				RemoveTask(task);
			}
		}

		public void RemoveAllForFile(string fileName)
		{
			var tasksToRemove = (from t in Tasks.Cast<ErrorTask>()
								 where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase)
								 select t).ToArray();
			foreach (var task in tasksToRemove)
			{
				RemoveTask(task);
			}
		}

		public void RemoveAllForSourceAndFile(ErrorTaskSource source, string sourceFileName, string fileName)
		{
			var tasksToRemove = (from t in Tasks.Cast<ErrorTask>()
								 where t.Source == source && string.Equals(t.SourceArg, sourceFileName, StringComparison.OrdinalIgnoreCase) &&
									 string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase)
								 select t).ToArray();
			foreach (var task in tasksToRemove)
			{
				RemoveTask(task);
			}
		}

		public void OnDocumentClosed(ITextView textView)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(textView.TextBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(textView.TextSnapshot, "ErrorTaskProvider.OnDocumentClosed()");
				RemoveAllForSource(ErrorTaskSource.BackgroundFec, model.FileName);
			}
		}

		public IEnumerable<ITagSpan<ErrorTag>> GetErrorTagsForFile(string fileName, NormalizedSnapshotSpanCollection docSpans)
		{
			var tags = new List<TagSpan<ErrorTag>>();

			var firstSpan = docSpans.FirstOrDefault();
			if (firstSpan == null) return tags;
			var snapshot = firstSpan.Snapshot;

			//// TODO: remove
			//Log.Debug("Getting tags for spans:");
			//foreach (var span in docSpans) Log.Debug(span.ToString());

			ErrorTask[] tasks;
			lock (_tasksLock)
			{
				tasks = Tasks.Cast<ErrorTask>().ToArray();
			}

			foreach (var task in (from t in tasks where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t))
			{
				var span = task.Span;
				if (span.HasValue)
				{
					//var reported = false;	// TODO: remove
					foreach (var docSpan in docSpans)
					{
						var spanT = span.Value.TranslateTo(docSpan.Snapshot, SpanTrackingMode.EdgeExclusive);
						if (docSpan.Contains(spanT.Start))
						{
							tags.Add(new TagSpan<ErrorTag>(spanT, new ErrorTag(task.Type == ErrorType.Warning ? ErrorTagger.CodeWarning : ErrorTagger.CodeError, task.Text)));
							//reported = true;
							break;
						}
					}

					//if (!reported)
					//{
					//	Log.Debug("Error task not reported because it's not in any spans: {0}", task.Text);	// TODO: remove
					//}
				}
				else
				{
					var line = task.GetSnapshot(snapshot).GetLineFromLineNumber(task.Line);
					foreach (var docSpan in docSpans)
					{
						var spanLineStart = line.Start.TranslateTo(docSpan.Snapshot, PointTrackingMode.Negative);
						if (docSpan.Contains(spanLineStart.Position))
						{
							var errorCode = task.Type == ErrorType.Warning ? ErrorCode.Fec_Warning : ErrorCode.Fec_Error;

							var spanLine = docSpan.Snapshot.GetLineFromPosition(spanLineStart);
							var startPos = spanLine.Start.Position;
							var endPos = spanLine.End.Position;
							if (startPos < endPos)
							{
								var newStartPos = spanLine.Start.Position + spanLine.GetText().GetIndentOffset();
								if (newStartPos < endPos) startPos = newStartPos;
							}

							tags.Add(new TagSpan<ErrorTag>(new SnapshotSpan(docSpan.Snapshot, startPos, endPos - startPos),
								new ErrorTag(task.Type == ErrorType.Warning ? ErrorTagger.CodeWarning : ErrorTagger.CodeError, task.Text)));
							break;
						}
					}
				}
			}

			return tags;
		}

		public IEnumerable<ErrorTask> GetErrorMessagesAtPoint(string fileName, SnapshotPoint pt)
		{
			foreach (var task in (from t in Tasks.Cast<ErrorTask>() where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t))
			{
				var span = task.Span;
				if (span.HasValue)
				{
					if (span.Value.Contains(pt.TranslateTo(span.Value.Snapshot, PointTrackingMode.Positive)))
					{
						yield return task;
					}
				}
				else
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

		public IEnumerable<string> GetFilesForInclude(string fileName)
		{
			var ret = new List<string>();

			foreach (ErrorTask task in Tasks)
			{
				if (string.Equals(task.Document, fileName))
				{
					if (task.Source == ErrorTaskSource.BackgroundFec)
					{
						var sourceFileName = task.SourceArg;
						if (!string.IsNullOrEmpty(sourceFileName))
						{
							if (!ret.Any(x => string.Equals(x, sourceFileName))) ret.Add(sourceFileName);
						}
					}
				}
			}

			return ret;
		}
	}
}
