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
		private BackgroundDeferrer _backgroundFecDeferrer;

		// TODO: remove
		//#if REPORT_ERRORS
		//		private BackgroundDeferrer _analysisDeferrer;
		//#endif

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
			ErrorTaskProvider.Instance.ErrorTagsChangedForFile += Instance_ErrorTagsChangedForFile;

			_backgroundFecDeferrer = new BackgroundDeferrer(Constants.BackgroundFecDelay);
			_backgroundFecDeferrer.Idle += _backgroundFecDeferrer_Idle;
			_backgroundFecDeferrer.OnActivity();
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			_model = _store.GetMostRecentModel(_view.TextSnapshot, "ErrorTagger.GetTags()");

			//if (!ProbeToolsPackage.Instance.EditorOptions.ShowErrors) return new TagSpan<ErrorTag>[0];

			return ErrorTaskProvider.Instance.GetErrorTagsForFile(_model.FileName, spans);
		}

		private void OnModelUpdated(object sender, FileStore.ModelUpdatedEventArgs e)
		{
			//var ev = TagsChanged;
			//if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
#if REPORT_ERRORS
			_analysisDeferrer.OnActivity();
#endif
		}

		// TODO: remove
//#if REPORT_ERRORS
//		private void _analysisDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
//		{
//			_model = _store.GetMostRecentModel(_view.TextSnapshot, "ErrorTagger._analysisDeferrer_Idle()");

//			if (_model.PerformCodeAnalysis())
//			{
//				var ev = TagsChanged;
//				if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
//			}
//		}
//#endif

		private void EditorOptions_EditorRefreshRequired(object sender, EventArgs e)
		{
			try
			{
#if REPORT_ERRORS
				if (ProbeToolsPackage.Instance.EditorOptions.ShowErrors &&
					_model != null &&
					!_model.PreprocessorModel.ReportErrors)
				{
					_model.FileStore.RegenerateModel(_model.Snapshot.TextBuffer.CurrentSnapshot, "ErrorTagger detected ShowErrors switched on.");
				}
#endif

				if (_model != null &&
					_model.FileContext != FileContext.Include &&
					ProbeEnvironment.FileExistsInApp(_model.FileName))
				{
					_backgroundFecDeferrer.OnActivity();
				}

				var ev = TagsChanged;
				if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		void Shell_FileSaved(object sender, Shell.FileSavedEventArgs e)
		{
			try
			{
				if (_model == null) return;

				if (string.Equals(e.FileName, _model.FileName, StringComparison.OrdinalIgnoreCase))
				{
					if (_model.FileContext != FileContext.Include &&
						ProbeEnvironment.FileExistsInApp(_model.FileName))
					{
						_backgroundFecDeferrer.OnActivity();
					}
					else
					{
						foreach (var sourceFileName in ErrorTaskProvider.Instance.GetFilesForInclude(_model.FileName))
						{
							Shell.OnFileSaved(sourceFileName);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		void Instance_ErrorTagsChangedForFile(object sender, ErrorTaskProvider.ErrorTaskEventArgs e)
		{
			try
			{
				if (_model == null) return;

				if (string.Equals(e.FileName, _model.FileName, StringComparison.OrdinalIgnoreCase))
				{
					var ev = TagsChanged;
					if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

#if REPORT_ERRORS
		public void OnCodeAnalysisFinished(CodeModel.CodeModel model)
		{
			var ev = TagsChanged;
			if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
		}

		// TODO: remove
		//public BackgroundDeferrer AnalysisDeferrer
		//{
		//	get { return _analysisDeferrer; }
		//}
#endif

		private void _backgroundFecDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			try
			{
				if (_model != null &&
					_model.FileContext != FileContext.Include &&
					ProbeToolsPackage.Instance.EditorOptions.ShowErrors &&
					ProbeEnvironment.FileExistsInApp(_model.FileName))
				{
					Compiler.BackgroundFec.Run(_model.FileName, _model.Snapshot.TextBuffer.CurrentSnapshot);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}
	}
}
