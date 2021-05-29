using DK.Code;
using DK.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using VsShell = Microsoft.VisualStudio.Shell;

namespace DkTools.ErrorTagging
{
	class ErrorTask : VsShell.Task
	{
		private ErrorTaskSource _source;
		private string _invokingFilePath;
		private ErrorType _type;
		private CodeSpan? _reportedSpan;
		private Dictionary<ITextBuffer, SnapshotSpan> _snapshotSpans;
		private CAError? _errorCode;

		/// <summary>
		/// Creates a new error/warning task.
		/// </summary>
		/// <param name="invokingFilePath">The file which caused this error to be found.
		/// For example, if doing code analysis on a .nc file but the error was found in an include file,
		/// this parameter would be the .nc file.</param>
		/// <param name="filePath">The file in which the error actually shows up.</param>
		/// <param name="lineNum">Line number in filePath where the error exists.</param>
		/// <param name="lineCol">Line column in filePath where the error exists.</param>
		/// <param name="message">Error message.</param>
		/// <param name="type">Type of task (error or warning)</param>
		/// <param name="source">Source of the task (compiler, background fec, or code analysis)</param>
		/// <param name="reportedSpan">Optional span in filePath where the error exists.</param>
		/// <param name="snapshotSpan">Optional snapshot span for the error. Will be generated later if not specified.</param>
		public ErrorTask(
			string invokingFilePath,
			string filePath,
			int lineNum,
			int lineCol,
			string message,
			ErrorType type,
			ErrorTaskSource source,
			CodeSpan? reportedSpan,
			CAError? errorCode = null)
		{
			_invokingFilePath = invokingFilePath;
			Document = filePath;
			Line = lineNum;
			Column = lineCol;

			if (string.IsNullOrEmpty(invokingFilePath) ||
				string.Equals(invokingFilePath, filePath, StringComparison.OrdinalIgnoreCase))
			{
				Text = message;
			}
			else
			{
				var ix = invokingFilePath.LastIndexOf('\\');
				var invokingFileName = ix >= 0 ? invokingFilePath.Substring(ix + 1) : invokingFilePath;
				Text = string.Format("{0}: {1}", invokingFileName, message);
			}
			
			Priority = type == ErrorType.Warning || type == ErrorType.CodeAnalysisError ? TaskPriority.Normal : TaskPriority.High;
			Category = TaskCategory.BuildCompile;
			_source = source;
			_type = type;
			_reportedSpan = reportedSpan;
			_errorCode = errorCode;

			Navigate += ErrorTask_Navigate;
		}

		/// <summary>
		/// The file that was being analyzed when this error was found.
		/// For include files, this will be the base file which included that file.
		/// </summary>
		public string InvokingFilePath => _invokingFilePath;
		public ErrorTaskSource Source => _source;
		public override string ToString() => string.Concat(Document, "(", Line, ", ", Column, ") ", Type, ": ", Text);
		public ErrorType Type => _type;
		public CodeSpan? ReportedSpan => _reportedSpan;
		public CAError? ErrorCode => _errorCode;

		private void ErrorTask_Navigate(object sender, EventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				var task = sender as ErrorTask;
				if (task != null)
				{
					if (!string.IsNullOrEmpty(task.Document))
					{
						Shell.OpenDocumentAndLine(task.Document, task.Line);
					}
				}
			});
		}

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

			if (_reportedSpan.HasValue)
			{
				var clampedSpan = _reportedSpan.Value.Intersection(new CodeSpan(0, currentSnapshot.Length));
				if (clampedSpan.IsEmpty) return null;
				taskSnapshotSpan = clampedSpan.ToVsTextSnapshotSpan(currentSnapshot);
			}
			else
			{
				if (Line < 0 || Line >= currentSnapshot.LineCount) return null;
				var line = currentSnapshot.GetLineFromLineNumber(Line);
				taskSnapshotSpan = line.GetTrimmedSnapshotSpan();
			}

			_snapshotSpans[currentSnapshot.TextBuffer] = taskSnapshotSpan;
			return taskSnapshotSpan;
		}

		public object QuickInfoContent
		{
			get
			{
				string type;
				switch (_type)
				{
					case ErrorType.Warning:
						type = PredefinedErrorTypeNames.Warning;
						break;
					case ErrorType.CodeAnalysisError:
						type = PredefinedErrorTypeNames.OtherError;
						break;
					default:
						type = PredefinedErrorTypeNames.CompilerError;
						break;
				}

				return new ClassifiedTextElement(new ClassifiedTextRun(type, this.Text));
			}
		}
	}

	enum ErrorTaskSource
	{
		Compile,
		BackgroundFec,
		CodeAnalysis
	}
}
