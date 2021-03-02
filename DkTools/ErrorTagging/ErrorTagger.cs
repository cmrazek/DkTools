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
using System.CodeDom;

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
		public const string ReportOutputTagLight = "DkReportOutputTag.Light";
		public const string ReportOutputTagDark = "DkReportOutputTag.Dark";

		private const int DeferPriority_UserInput = 1;
		private const int DeferPriority_DocumentRefresh = 2;

		public ErrorTagger(ITextView view)
		{
			_view = view;
			_store = FileStore.GetOrCreateForTextBuffer(_view.TextBuffer);

			ProbeToolsPackage.RefreshAllDocumentsRequired += OnRefreshAllDocumentsRequired;
			ProbeToolsPackage.RefreshDocumentRequired += OnRefreshDocumentRequired;
			ErrorTaskProvider.ErrorTagsChangedForFile += Instance_ErrorTagsChangedForFile;

			_backgroundFecDeferrer = new BackgroundDeferrer(Constants.BackgroundFecDelay);
			_backgroundFecDeferrer.Idle += _backgroundFecDeferrer_Idle;
			_backgroundFecDeferrer.OnActivity(priority: DeferPriority_DocumentRefresh);

			_view.TextBuffer.Changed += (sender, e) =>
			{
				_backgroundFecDeferrer.OnActivity(priority: DeferPriority_UserInput);
			};
		}

		~ErrorTagger()
		{
			ProbeToolsPackage.RefreshAllDocumentsRequired -= OnRefreshAllDocumentsRequired;
			ProbeToolsPackage.RefreshDocumentRequired -= OnRefreshDocumentRequired;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var appSettings = DkEnvironment.CurrentAppSettings;

			var fileName = VsTextUtil.TryGetDocumentFileName(_view.TextBuffer);
			_model = _store.GetMostRecentModel(appSettings, fileName, _view.TextSnapshot, "ErrorTagger.GetTags()");

			return ErrorTaskProvider.Instance.GetErrorTagsForFile(_model.FilePath, spans);
		}

		private void OnRefreshAllDocumentsRequired(object sender, EventArgs e)
		{
			try
			{
				if (_model != null && _model.FileContext != FileContext.Include)
				{
					_backgroundFecDeferrer.OnActivity(priority: DeferPriority_DocumentRefresh);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		private void OnRefreshDocumentRequired(object sender, ProbeToolsPackage.RefreshDocumentEventArgs e)
		{
			try
			{
				if (_model != null && _model.FileContext != FileContext.Include &&
					e.FilePath.EqualsI(_model.FilePath))
				{
					_backgroundFecDeferrer.OnActivity(priority: DeferPriority_DocumentRefresh);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		void Instance_ErrorTagsChangedForFile(object sender, ErrorTaskProvider.ErrorTaskEventArgs e)
		{
			try
			{
				if (_model == null) return;

				if (string.Equals(e.FileName, _model.FilePath, StringComparison.OrdinalIgnoreCase))
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
					if ((e.Priority == DeferPriority_DocumentRefresh && ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave) ||
						(e.Priority == DeferPriority_DocumentRefresh && ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave) ||
						(e.Priority == DeferPriority_UserInput && ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput))
					{
						var fileName = VsTextUtil.TryGetDocumentFileName(_view.TextBuffer);
						if (string.IsNullOrEmpty(fileName)) return;

						if (_model != null &&
							_model.FileContext != FileContext.Include &&
							DkEnvironment.CurrentAppSettings.FileExistsInApp(_model.FilePath))
						{
							System.Threading.ThreadPool.QueueUserWorkItem(state =>
							{
								try
								{
									if ((e.Priority == DeferPriority_DocumentRefresh && ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave))
									{
										Shell.Status($"FEC: {_model.FilePath} (running)");
										Compiler.BackgroundFec.RunSync(_model.FilePath);
										Shell.Status($"FEC: {_model.FilePath} (complete)");
									}

									if ((e.Priority == DeferPriority_DocumentRefresh && ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave) ||
										(e.Priority == DeferPriority_UserInput && ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput))
									{
										var textBuffer = _model.Snapshot.TextBuffer;
										var fileStore = FileStore.GetOrCreateForTextBuffer(textBuffer);
										if (fileStore != null)
										{
											var preprocessedModel = fileStore.CreatePreprocessedModel(_model.AppSettings, fileName,
												textBuffer.CurrentSnapshot, false, "Background Code Analysis");

											var ca = new CodeAnalysis.CodeAnalyzer(null, preprocessedModel);
											ca.Run();
										}
									}
								}
								catch (Exception ex)
								{
									Log.WriteEx(ex);
								}
							});
						}

						if (!ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave)
						{
							ErrorTaskProvider.Instance.RemoveAllForSource(ErrorTaskSource.BackgroundFec);
						}
						if (!ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave &&
							!ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput)
						{
							ErrorTaskProvider.Instance.RemoveAllForSource(ErrorTaskSource.CodeAnalysis);
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
