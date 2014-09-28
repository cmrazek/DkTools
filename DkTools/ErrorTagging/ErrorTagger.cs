#if REPORT_ERRORS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using DkTools.CodeModel;

namespace DkTools.ErrorTagging
{
	internal class ErrorTagger : ITagger<ErrorTag>
	{
		private ITextView _view;
		private FileStore _store;
		private CodeModel.CodeModel _model;

		public const string CodeError = "Code Error";

		public ErrorTagger(ITextView view)
		{
			_view = view;
			_store = FileStore.GetOrCreateForTextBuffer(_view.TextBuffer);
			_store.ModelUpdated += OnModelUpdated;

			ProbeToolsPackage.Instance.EditorOptions.EditorRefreshRequired += EditorOptions_EditorRefreshRequired;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			_model = _store.GetMostRecentModel(_view.TextSnapshot, "ErrorTagger.GetTags()");

			if (!ProbeToolsPackage.Instance.EditorOptions.ShowErrors) yield break;

			foreach (var error in _model.PreprocessorModel.ErrorProvider.Errors)
			{
				foreach (var span in spans)
				{
					if (span.Contains(error.Span.Start) || span.Contains(error.Span.End))
					{
						var vsSpan = new SnapshotSpan(_model.Snapshot, error.Span.Start, error.Span.Length);
						yield return new TagSpan<ErrorTag>(vsSpan, new ErrorTag(CodeError, error.Message));
						break;
					}
				}
			}
		}

		private void OnModelUpdated(object sender, FileStore.ModelUpdatedEventArgs e)
		{
			var ev = TagsChanged;
			if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
		}

		void EditorOptions_EditorRefreshRequired(object sender, EventArgs e)
		{
			if (ProbeToolsPackage.Instance.EditorOptions.ShowErrors &&
				_model != null &&
				!_model.PreprocessorModel.ReportErrors)
			{
				_model.FileStore.RegenerateModel(_model.Snapshot.TextBuffer.CurrentSnapshot, "ErrorTagger detected ShowErrors switched on.");
			}

			var ev = TagsChanged;
			if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
		}
	}
}
#endif
