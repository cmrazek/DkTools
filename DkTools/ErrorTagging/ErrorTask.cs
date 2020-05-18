using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using VsShell = Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace DkTools.ErrorTagging
{
	class ErrorTask : VsShell.Task
	{
		private ErrorTaskSource _source;
		private string _sourceArg;
		private ErrorType _type;
		private CodeModel.Span? _reportedSpan;
		private SnapshotSpan? _snapshotSpan;

		public ErrorTask(string fileName, int lineNum, int lineCol, string message, ErrorType type, ErrorTaskSource source,
			string sourceFileName, CodeModel.Span? reportedSpan, SnapshotSpan? snapshotSpan)
		{
			this.Document = fileName;
			this.Line = lineNum;
			this.Column = lineCol;

			if (string.IsNullOrEmpty(sourceFileName) || string.Equals(sourceFileName, fileName, StringComparison.OrdinalIgnoreCase))
			{
				this.Text = message;
			}
			else
			{
				var ix = sourceFileName.LastIndexOf('\\');
				var sourceFileTitle = ix >= 0 ? sourceFileName.Substring(ix + 1) : sourceFileName;
				this.Text = string.Format("{0}: {1}", sourceFileTitle, message);
			}
			
			this.Priority = type == ErrorType.Warning || type == ErrorType.CodeAnalysisError ? TaskPriority.Normal : TaskPriority.High;
			this.Category = TaskCategory.BuildCompile;
			_source = source;
			_sourceArg = sourceFileName;
			_type = type;
			_reportedSpan = reportedSpan;
			_snapshotSpan = snapshotSpan;

			this.Navigate += ErrorTask_Navigate;
		}

		public ErrorTaskSource Source => _source;
		public string SourceArg => _sourceArg;
		public ErrorType Type => _type;

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

			if (_snapshotSpan.HasValue) return _snapshotSpan.Value;

			if (_reportedSpan.HasValue)
			{
				var clampedSpan = _reportedSpan.Value.Intersection(new CodeModel.Span(0, currentSnapshot.Length));
				if (clampedSpan.IsEmpty) return null;
				_snapshotSpan = clampedSpan.ToVsTextSnapshotSpan(currentSnapshot);
			}
			else
			{
				if (Line < 0 || Line >= currentSnapshot.LineCount) return null;
				var line = currentSnapshot.GetLineFromLineNumber(Line);
				_snapshotSpan = line.GetSnapshotSpan();
			}

			return _snapshotSpan;
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
