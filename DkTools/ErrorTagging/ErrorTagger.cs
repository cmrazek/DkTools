#if REPORT_ERRORS || BACKGROUND_FEC
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
#if REPORT_ERRORS
		private BackgroundDeferrer _analysisDeferrer;
#endif
		private BackgroundDeferrer _backgroundFecDeferrer;

		public const string CodeError = "Code Error";
		public const string CodeWarning = "Code Warning";

		public ErrorTagger(ITextView view)
		{
			_view = view;
			_store = FileStore.GetOrCreateForTextBuffer(_view.TextBuffer);
			_store.ModelUpdated += OnModelUpdated;

#if REPORT_ERRORS
			_analysisDeferrer = new BackgroundDeferrer();
			_analysisDeferrer.Idle += _analysisDeferrer_Idle;
			_analysisDeferrer.OnActivity();
#endif

			ProbeToolsPackage.Instance.EditorOptions.EditorRefreshRequired += EditorOptions_EditorRefreshRequired;
			Shell.FileSaved += Shell_FileSaved;

			_backgroundFecDeferrer = new BackgroundDeferrer(Constants.BackgroundFecDelay);
			_backgroundFecDeferrer.Idle += _backgroundFecDeferrer_Idle;
			_backgroundFecDeferrer.OnActivity();
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			_model = _store.GetMostRecentModel(_view.TextSnapshot, "ErrorTagger.GetTags()");

			if (!ProbeToolsPackage.Instance.EditorOptions.ShowErrors) yield break;

			// TODO: remove
			//foreach (var error in _model.PreprocessorModel.ErrorProvider.Errors)
			//{
			//	foreach (var span in spans)
			//	{
			//		if (span.Contains(error.Span.Start) || span.Contains(error.Span.End))
			//		{
			//			var vsSpan = new SnapshotSpan(_model.Snapshot, error.Span.Start, error.Span.Length);
			//			yield return new TagSpan<ErrorTag>(vsSpan, new ErrorTag(CodeError, error.Message));
			//			break;
			//		}
			//	}
			//}

#if REPORT_ERRORS
			var viewSnapshot = _view.TextSnapshot;

			var analysis = _model.Analysis;
			if (analysis != null)
			{
				foreach (var error in analysis.ErrorProvider.Errors)
				{
					var vsSpan = new SnapshotSpan(_model.Snapshot, error.Span.Start, error.Span.Length);
					if (_model.Snapshot != viewSnapshot) vsSpan = vsSpan.TranslateTo(viewSnapshot, SpanTrackingMode.EdgeExclusive);

					foreach (var span in spans)
					{
						if (span.OverlapsWith(vsSpan))
						{
							//Log.WriteDebug("Error at {0}: {1}", error.Span, error.Message);	// TODO: remove
							yield return new TagSpan<ErrorTag>(vsSpan, new ErrorTag(CodeError, error.Message));
							break;
						}
					}
				}
			}
#endif

			foreach (var tagSpan in ErrorTaskProvider.Instance.GetErrorTagsForFile(_model.FileName, spans))
			{
				yield return tagSpan;
			}
		}

		private void OnModelUpdated(object sender, FileStore.ModelUpdatedEventArgs e)
		{
			//var ev = TagsChanged;
			//if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
#if REPORT_ERRORS
			_analysisDeferrer.OnActivity();
#endif
		}

#if REPORT_ERRORS
		private void _analysisDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			_model = _store.GetMostRecentModel(_view.TextSnapshot, "ErrorTagger._analysisDeferrer_Idle()");

			if (_model.PerformCodeAnalysis())
			{
				var ev = TagsChanged;
				if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
			}
		}
#endif

		private void EditorOptions_EditorRefreshRequired(object sender, EventArgs e)
		{
#if REPORT_ERRORS
			if (ProbeToolsPackage.Instance.EditorOptions.ShowErrors &&
				_model != null &&
				!_model.PreprocessorModel.ReportErrors)
			{
				_model.FileStore.RegenerateModel(_model.Snapshot.TextBuffer.CurrentSnapshot, "ErrorTagger detected ShowErrors switched on.");
			}
#endif

			_backgroundFecDeferrer.OnActivity();

			var ev = TagsChanged;
			if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
		}

		void Shell_FileSaved(object sender, Shell.FileSavedEventArgs e)
		{
			_backgroundFecDeferrer.OnActivity();
		}

#if REPORT_ERRORS
		public void OnCodeAnalysisFinished(CodeModel.CodeModel model)
		{
			var ev = TagsChanged;
			if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
		}

		public BackgroundDeferrer AnalysisDeferrer
		{
			get { return _analysisDeferrer; }
		}
#endif

		private void _backgroundFecDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			try
			{
				if (_model.FileContext != FileContext.Include && ProbeToolsPackage.Instance.EditorOptions.ShowErrors)
				{
					Compiler.BackgroundFec.Run(_model.FileName, _model.Snapshot.TextBuffer.CurrentSnapshot);

					var ev = TagsChanged;
					if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}
	}
}
#endif
