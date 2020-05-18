﻿using System;
using System.Collections.Concurrent;
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

		private ConcurrentQueue<ErrorTask> _newTasks = new ConcurrentQueue<ErrorTask>();
		private ConcurrentQueue<ErrorTask> _delTasks = new ConcurrentQueue<ErrorTask>();

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

		private async System.Threading.Tasks.Task OnTasksChangedAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var fileNames = new List<string>();

			ErrorTask task;
			while (_newTasks.TryDequeue(out task))
			{
				Tasks.Add(task);
				if (!fileNames.Contains(task.Document)) fileNames.Add(task.Document);
			}
			while (_delTasks.TryDequeue(out task))
			{
				Tasks.Remove(task);
				if (!fileNames.Contains(task.Document)) fileNames.Add(task.Document);
			}

			foreach (var fileName in fileNames)
			{
				ErrorTagsChangedForFile?.Invoke(this, new ErrorTaskEventArgs { FileName = fileName });
			}
		}

		public void Add(ErrorTask task, bool dontSignalTagsChanged = false)
		{
			if (task == null) throw new ArgumentNullException("task");

			_newTasks.Enqueue(task);

			if (!dontSignalTagsChanged)
			{
				FireTagsChanged();
			}
		}

		public void FireTagsChanged()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(OnTasksChangedAsync);
		}

		private async System.Threading.Tasks.Task OnClearTasksAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var fileNames = new List<string>();

			foreach (var task in Tasks.Cast<ErrorTask>())
			{
				if (!fileNames.Contains(task.Document)) fileNames.Add(task.Document);
			}
			Tasks.Clear();

			while (_newTasks.TryDequeue(out var task))
			{
				if (!fileNames.Contains(task.Document)) fileNames.Add(task.Document);
			}

			while (_delTasks.TryDequeue(out var task))
			{
				if (!fileNames.Contains(task.Document)) fileNames.Add(task.Document);
			}

			foreach (var fileName in fileNames)
			{
				ErrorTagsChangedForFile?.Invoke(this, new ErrorTaskEventArgs { FileName = fileName });
			}
		}

		public void Clear()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(OnClearTasksAsync);
		}

		public void RemoveAllForSource(ErrorTaskSource source, string sourceFileName)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				var filesToNotify = new List<string>();

				var tasksToRemove = (from t in Tasks.Cast<ErrorTask>()
									 where t.Source == source && string.Equals(t.SourceArg, sourceFileName, StringComparison.OrdinalIgnoreCase)
									 select t).ToArray();
				foreach (var task in tasksToRemove)
				{
					Tasks.Remove(task);
					if (!filesToNotify.Contains(task.Document)) filesToNotify.Add(task.Document);
				}

				foreach (var file in filesToNotify)
				{
					ErrorTagsChangedForFile?.Invoke(this, new ErrorTaskEventArgs { FileName = file });
				}
			});
		}

		public void OnDocumentClosed(ITextView textView)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(textView.TextBuffer);
				if (fileStore != null)
				{
					var appSettings = ProbeEnvironment.CurrentAppSettings;
					var fileName = VsTextUtil.TryGetDocumentFileName(textView.TextBuffer);
					var model = fileStore.GetMostRecentModel(appSettings, fileName, textView.TextSnapshot, "ErrorTaskProvider.OnDocumentClosed()");
					RemoveAllForSource(ErrorTaskSource.BackgroundFec, model.FileName);
				}
			});
		}

		public IEnumerable<ITagSpan<ErrorTag>> GetErrorTagsForFile(string fileName, NormalizedSnapshotSpanCollection docSpans)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var tags = new List<TagSpan<ErrorTag>>();

			var firstSpan = docSpans.FirstOrDefault();
			if (firstSpan == null) return tags;
			var snapshot = firstSpan.Snapshot;

			ErrorTask[] tasks = Tasks.Cast<ErrorTask>().ToArray();

			foreach (var task in (from t in tasks where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t))
			{
				var taskSpan = task.TryGetSnapshotSpan(snapshot);
				if (!taskSpan.HasValue) continue;

				foreach (var docSpan in docSpans)
				{
					var mappedTaskSpan = taskSpan.Value.TranslateTo(docSpan.Snapshot, SpanTrackingMode.EdgeExclusive);
					if (docSpan.Contains(mappedTaskSpan))
					{
						string tagType;
						switch (task.Type)
						{
							case ErrorType.Warning:
								tagType = VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.CodeWarningLight : ErrorTagger.CodeWarningDark;
								break;
							case ErrorType.CodeAnalysisError:
								tagType = VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.CodeAnalysisErrorLight : ErrorTagger.CodeAnalysisErrorDark;
								break;
							default:
								tagType = VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.CodeErrorLight : ErrorTagger.CodeErrorDark;
								break;
						}
						tags.Add(new TagSpan<ErrorTag>(taskSpan.Value, new ErrorTag(tagType, task.Text)));
						break;
					}
				}
			}

			return tags;
		}

		public async Task<IEnumerable<ErrorTask>> GetErrorMessagesAtPointAsync(string fileName, SnapshotPoint pt)
		{
			var tasks = new List<ErrorTask>();
			await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				foreach (var task in (from t in Tasks.Cast<ErrorTask>() where string.Equals(t.Document, fileName, StringComparison.OrdinalIgnoreCase) select t))
				{
					var taskSpan = task.TryGetSnapshotSpan(pt.Snapshot);
					if (taskSpan.HasValue)
					{
						var mappedPt = pt.TranslateTo(taskSpan.Value.Snapshot, PointTrackingMode.Positive);
						if (taskSpan.Value.Contains(mappedPt))
						{
							tasks.Add(task);
						}
					}
				}
			});

			return tasks;
		}

		public IEnumerable<string> GetFilesForInclude(string fileName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

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
