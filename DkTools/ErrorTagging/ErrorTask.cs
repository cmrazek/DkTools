using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using VsShell = Microsoft.VisualStudio.Shell;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.ErrorTagging
{
	class ErrorTask : VsShell.Task
	{
		private ErrorTaskSource _source;
		private string _sourceArg;
		private ErrorType _type;
		private VsText.ITextSnapshot _snapshot;
		private CodeModel.Span? _span;

		public ErrorTask(string fileName, int lineNum, int lineCol, string message, ErrorType type, ErrorTaskSource source,
			string sourceFileName, VsText.ITextSnapshot snapshot, CodeModel.Span? span = null)
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
			_snapshot = snapshot;
			_span = span;

			this.Navigate += ErrorTask_Navigate;
		}

		private void ErrorTask_Navigate(object sender, EventArgs e)
		{
			var task = sender as ErrorTask;
			if (task != null)
			{
				if (!string.IsNullOrEmpty(task.Document))
				{
					Shell.OpenDocumentAndLine(task.Document, task.Line);
				}
			}
		}

		public ErrorTaskSource Source
		{
			get { return _source; }
		}

		public string SourceArg
		{
			get { return _sourceArg; }
		}

		public ErrorType Type
		{
			get { return _type; }
		}

		public VsText.ITextSnapshot GetSnapshot(VsText.ITextSnapshot currentSnapshot)
		{
			if (_snapshot == null || _snapshot.TextBuffer != currentSnapshot.TextBuffer) _snapshot = currentSnapshot;
			return _snapshot;
		}

		public VsText.SnapshotSpan GetSnapshotSpan(VsText.ITextSnapshot currentSnapshot)
		{
			if (_span.HasValue)
			{
				if (_snapshot == null) _snapshot = currentSnapshot;
				return new VsText.SnapshotSpan(_snapshot, _span.Value.Start, _span.Value.Length);
			}

			var line = _snapshot.GetLineFromLineNumber(Line);
			var startPos = line.Start.Position;
			var endPos = line.End.Position;
			if (startPos < endPos)
			{
				var newStartPos = line.Start.Position + line.GetText().GetIndentOffset();
				if (newStartPos < endPos) startPos = newStartPos;
			}

			if (_snapshot == null) _snapshot = currentSnapshot;
			_span = new CodeModel.Span(startPos, endPos);
			return new VsText.SnapshotSpan(_snapshot, _span.Value.Start, _span.Value.Length);
		}

		public CodeModel.Span? Span
		{
			get { return _span; }
		}

		public object QuickInfoContent
		{
			get
			{
				System.Windows.Media.Brush brush;
				switch (_type)
				{
					case ErrorType.Warning:
						if (VSTheme.CurrentTheme == VSThemeMode.Light) brush = System.Windows.Media.Brushes.LimeGreen;
						else brush = System.Windows.Media.Brushes.Chartreuse;
						break;
					case ErrorType.CodeAnalysisError:
						if (VSTheme.CurrentTheme == VSThemeMode.Light) brush = System.Windows.Media.Brushes.Blue;
						else brush = System.Windows.Media.Brushes.SkyBlue;
						break;
					default:
						if (VSTheme.CurrentTheme == VSThemeMode.Light) brush = System.Windows.Media.Brushes.Red;
						else brush = System.Windows.Media.Brushes.OrangeRed;
						break;
				}

				return new System.Windows.Controls.TextBlock
				{
					Text = this.Text,
					Foreground = brush,
					FontWeight = System.Windows.FontWeights.Bold
				};
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
