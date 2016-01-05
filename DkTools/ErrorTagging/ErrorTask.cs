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

		public ErrorTask(string fileName, int lineNum, string message, ErrorType type, ErrorTaskSource source, string sourceFileName, VsText.ITextSnapshot snapshot)
		{
			this.Document = fileName;
			this.Line = lineNum;

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
			
			this.Priority = type == ErrorType.Warning ? TaskPriority.Normal : TaskPriority.High;
			this.Category = TaskCategory.BuildCompile;
			_source = source;
			_sourceArg = sourceFileName;
			_type = type;
			_snapshot = snapshot;

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
			if (_snapshot == null || _snapshot.TextBuffer != currentSnapshot.TextBuffer) _snapshot = currentSnapshot;

			var line = _snapshot.GetLineFromLineNumber(Line);
			var startPos = line.Start.Position;
			var endPos = line.End.Position;
			if (startPos < endPos)
			{
				var newStartPos = line.Start.Position + line.GetText().GetIndentOffset();
				if (newStartPos < endPos) startPos = newStartPos;
			}

			return new VsText.SnapshotSpan(_snapshot, new VsText.Span(startPos, endPos - startPos));
		}
	}

	enum ErrorTaskSource
	{
		Compile,
		BackgroundFec
	}
}
