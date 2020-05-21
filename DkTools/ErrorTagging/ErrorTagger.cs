using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
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

		public const string CodeErrorLight = "DkCodeError.Light";
		public const string CodeErrorDark = "DkCodeError.Dark";
		public const string CodeWarningLight = "DkCodeWarning.Light";
		public const string CodeWarningDark = "DkCodeWarning.Dark";
		public const string CodeAnalysisErrorLight = "DkCodeAnalysisError.Light";
		public const string CodeAnalysisErrorDark = "DkCodeAnalysisError.Dark";

		public ErrorTagger(ITextView view)
		{
			_view = view;
			_store = FileStore.GetOrCreateForTextBuffer(_view.TextBuffer);

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
			ThreadHelper.ThrowIfNotOnUIThread();

			var appSettings = ProbeEnvironment.CurrentAppSettings;

			var fileName = VsTextUtil.TryGetDocumentFileName(_view.TextBuffer);
			_model = _store.GetMostRecentModel(appSettings, fileName, _view.TextSnapshot, "ErrorTagger.GetTags()");

			return ErrorTaskProvider.Instance.GetErrorTagsForFile(_model.FileName, spans);
		}

		private void EditorOptions_EditorRefreshRequired(object sender, EventArgs e)
		{
			try
			{
				if (_model != null &&
					_model.FileContext != FileContext.Include &&
					ProbeEnvironment.CurrentAppSettings.FileExistsInApp(_model.FileName))
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
			ThreadHelper.ThrowIfNotOnUIThread();
			try
			{
				if (_model == null) return;

				if (string.Equals(e.FileName, _model.FileName, StringComparison.OrdinalIgnoreCase))
				{
					if (_model.FileContext != FileContext.Include &&
						ProbeEnvironment.CurrentAppSettings.FileExistsInApp(_model.FileName))
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

		private void _backgroundFecDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					var fileName = VsTextUtil.TryGetDocumentFileName(_view.TextBuffer);
					if (string.IsNullOrEmpty(fileName)) return;

					if (_model != null &&
						_model.FileContext != FileContext.Include &&
						ProbeEnvironment.CurrentAppSettings.FileExistsInApp(_model.FileName))
					{
						if (ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave ||
							ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave)
						{
							System.Threading.ThreadPool.QueueUserWorkItem(state =>
							{
								try
								{
									if (ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave)
									{
										Compiler.BackgroundFec.RunSync(_model.FileName);
									}

									if (ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave)
									{
										var textBuffer = _model.Snapshot.TextBuffer;
										var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(textBuffer);
										if (fileStore == null) return;

										var preprocessedModel = fileStore.CreatePreprocessedModel(_model.AppSettings, fileName, textBuffer.CurrentSnapshot, false, "Background Code Analysis");

										var ca = new CodeAnalysis.CodeAnalyzer(null, preprocessedModel);
										ca.Run();
									}
								}
								catch (Exception ex)
								{
									Log.WriteEx(ex);
								}
							});
						}
					}
				}
				catch (Exception ex)
				{
					Log.WriteEx(ex);
				}
			});
		}
	}
}
