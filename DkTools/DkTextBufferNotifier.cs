using DK.AppEnvironment;
using DK.Diagnostics;
using DK.Modeling;
using DkTools.CodeModeling;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Threading;

namespace DkTools
{
    /// <summary>
    /// Provides centralized notifications for VS text buffers.
    /// </summary>
    internal class DkTextBufferNotifier
    {
        private ITextBuffer _textBuffer;
        private BackgroundDeferrer _modelRebuildDeferrer;
        private CancellationTokenSource _modelRebuildCancellationSource;

        public DkTextBufferNotifier(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));

            _modelRebuildDeferrer = new BackgroundDeferrer(Constants.ModelRebuildDelay);

            _textBuffer.Changed += TextBuffer_Changed;
            _modelRebuildDeferrer.Idle += ModelRebuildDeferrer_Idle;

            ProbeToolsPackage.Instance.App.RefreshDocumentRequired += GlobalEvents_RefreshDocumentRequired;
            ProbeToolsPackage.Instance.App.RefreshAllDocumentsRequired += GlobalEvents_RefreshAllDocumentsRequired;
            VSTheme.ThemeChanged += VSTheme_ThemeChanged;

            _modelRebuildDeferrer.OnActivity();
        }

        ~DkTextBufferNotifier()
        {
            _textBuffer.Changed -= TextBuffer_Changed;
            ProbeToolsPackage.Instance.App.RefreshDocumentRequired -= GlobalEvents_RefreshDocumentRequired;
            ProbeToolsPackage.Instance.App.RefreshAllDocumentsRequired -= GlobalEvents_RefreshAllDocumentsRequired;
        }

        /// <summary>
        /// Used when components are initializing on the main thread to get the notifier, or create it if it doesn't exist.
        /// </summary>
        public static DkTextBufferNotifier GetOrCreate(ITextBuffer textBuffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!textBuffer.Properties.TryGetProperty<DkTextBufferNotifier>(Constants.TextBufferProperties.TextBufferNotifier, out var notifier))
            {
                notifier = new DkTextBufferNotifier(textBuffer);
                textBuffer.Properties.AddProperty(Constants.TextBufferProperties.TextBufferNotifier, notifier);
                textBuffer.Properties.AddProperty(Constants.TextBufferProperties.FileName, textBuffer.TryGetDocumentFileName());
            }

            return notifier;
        }

        public class NewModelAvailableEventArgs : EventArgs
        {
            public CodeModel CodeModel { get; private set; }

            public NewModelAvailableEventArgs(CodeModel model)
            {
                CodeModel = model ?? throw new ArgumentNullException(nameof(model));
            }
        }

        public event EventHandler<NewModelAvailableEventArgs> NewModelAvailable;

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            _modelRebuildCancellationSource?.Cancel();
            _modelRebuildDeferrer.OnActivity();
        }

        private void ModelRebuildDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
					var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_textBuffer);
					if (fileStore != null)
                    {
						var appSettings = ProbeToolsPackage.Instance.App.Settings;
						var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
						var snapshot = _textBuffer.CurrentSnapshot;

						_modelRebuildCancellationSource?.Cancel();
						_modelRebuildCancellationSource = new CancellationTokenSource();
						var cancel = _modelRebuildCancellationSource.Token;

						ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
								cancel.ThrowIfCancellationRequested();

								var model = fileStore.GetCurrentModelSync(
									appSettings: appSettings,
									fileName: fileName,
									snapshot: snapshot,
									reason: "Model rebuild",
									cancel: cancel);
                                fileStore.Model = model;

								cancel.ThrowIfCancellationRequested();

                                NewModelAvailable?.Invoke(this, new NewModelAvailableEventArgs(model));
                            }
							catch (OperationCanceledException ex)
                            {
								ProbeToolsPackage.Log.Debug(ex);
                            }
                            catch (Exception ex)
                            {
								ProbeToolsPackage.Log.Error(ex);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
					ProbeToolsPackage.Log.Error(ex);
                }
			});
        }

        private void GlobalEvents_RefreshDocumentRequired(object sender, RefreshDocumentEventArgs e)
        {
            if (string.Equals(e.FilePath, _textBuffer.Properties[Constants.TextBufferProperties.FileName] as string, StringComparison.OrdinalIgnoreCase))
            {
                _modelRebuildCancellationSource?.Cancel();
                _modelRebuildDeferrer.OnActivity();
            }
        }

        private void GlobalEvents_RefreshAllDocumentsRequired(object sender, EventArgs e)
        {
            _modelRebuildCancellationSource?.Cancel();
            _modelRebuildDeferrer.OnActivity();
        }

        private void VSTheme_ThemeChanged(object sender, EventArgs e)
        {
            _modelRebuildCancellationSource?.Cancel();
            _modelRebuildDeferrer.OnActivity();
        }
    }
}
