using DK.CodeAnalysis;
using DK.Diagnostics;
using DkTools.CodeModeling;
using DkTools.ErrorTagging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Threading;

namespace DkTools.Compiler
{
    class CompileCoordinator
    {
        private ITextBuffer _textBuffer;
        private string _fileName;
        private bool _fecScanned;
        private bool _codeAnalysisScanned;
        private BackgroundDeferrer _scannerDefer;
        private static Mutex _mutex;
        private CancellationTokenSource _cancel;

        public static CompileCoordinator GetOrCreateForTextBuffer(ITextBuffer textBuffer)
        {
            if (textBuffer == null) throw new ArgumentNullException(nameof(textBuffer));

            if (textBuffer.Properties.TryGetProperty<CompileCoordinator>(typeof(CompileCoordinator), out var cc))
                return cc;

            cc = new CompileCoordinator(textBuffer);
            textBuffer.Properties.AddProperty(typeof(CompileCoordinator), cc);
            return cc;
        }

        public CompileCoordinator(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
            _fileName = _textBuffer.TryGetDocumentFileName();
            _mutex = new Mutex();

            if (!ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave)
                _fecScanned = true;

            if (!ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave && !ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput)
                _codeAnalysisScanned = true;

            _scannerDefer = new BackgroundDeferrer(Constants.BackgroundFecDelay);
            _scannerDefer.Idle += ScannerDefer_Idle;

            _textBuffer.Changed += TextBuffer_Changed;
            ProbeToolsPackage.Instance.App.FileChanged += App_FileChanged;
            ProbeToolsPackage.Instance.App.RefreshAllDocumentsRequired += App_RefreshAllDocumentsRequired;
            ProbeToolsPackage.Instance.App.RefreshDocumentRequired += App_RefreshDocumentRequired;

            if (_fileName != null) _scannerDefer.OnActivity();
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            if (ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput)
            {
                _cancel?.Cancel();
                _cancel = new CancellationTokenSource();

                _codeAnalysisScanned = false;
                _scannerDefer.OnActivity();
            }
        }

        private void App_FileChanged(object sender, DK.AppEnvironment.FileEventArgs e)
        {
            if (e.FilePath.Equals(_fileName, StringComparison.OrdinalIgnoreCase))
            {
                var actionRequired = false;

                if (ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave)
                {
                    _codeAnalysisScanned = false;
                    actionRequired = true;

                }
                if (ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave)
                {
                    _fecScanned = false;
                    actionRequired = true;
                }

                if (actionRequired)
                {
                    _cancel?.Cancel();
                    _cancel = new CancellationTokenSource();
                    _scannerDefer.OnActivity();
                }
            }
        }

        private void App_RefreshAllDocumentsRequired(object sender, EventArgs e)
        {
            var actionRequired = false;

            if (ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave)
            {
                _fecScanned = false;
                actionRequired = true;
            }

            if (ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave || ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput)
            {
                _codeAnalysisScanned = false;
                actionRequired = true;
            }

            if (actionRequired)
            {
                _cancel?.Cancel();
                _cancel = new CancellationTokenSource();
                _scannerDefer.OnActivity();
            }
        }

        private void App_RefreshDocumentRequired(object sender, DK.AppEnvironment.RefreshDocumentEventArgs e)
        {
            if (e.FilePath.Equals(_fileName, StringComparison.OrdinalIgnoreCase))
            {
                var actionRequired = false;

                if (ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave)
                {
                    _fecScanned = false;
                    actionRequired = true;
                }

                if (ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave || ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnUserInput)
                {
                    _codeAnalysisScanned = false;
                    actionRequired = true;
                }

                if (actionRequired)
                {
                    _cancel?.Cancel();
                    _cancel = new CancellationTokenSource();
                    _scannerDefer.OnActivity();
                }
            }
        }

        private void ScannerDefer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
        {
            if (_fileName == null) return;
            if (_fecScanned && _codeAnalysisScanned) return;

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_cancel == null) _cancel = new CancellationTokenSource();

                var attempts = 10;
                while (attempts > 0)
                {
                    if (!_mutex.WaitOne(1000))
                    {
                        ProbeToolsPackage.Log.Info("Waiting for exclusive access to compiler.");
                        attempts--;
                    }
                    else break;
                }

                try
                {
                    if (!_fecScanned)
                    {
                        try
                        {
                            RunBackgroundFec();
                        }
                        catch (OperationCanceledException ex)
                        {
                            ProbeToolsPackage.Log.Debug(ex);
                        }
                        catch (Exception ex)
                        {
                            ProbeToolsPackage.Log.Error(ex);
                        }
                        finally
                        {
                            _fecScanned = true;
                        }
                    }

                    if (!_codeAnalysisScanned)
                    {
                        try
                        {
                            RunCodeAnalysis();
                        }
                        catch (OperationCanceledException ex)
                        {
                            ProbeToolsPackage.Log.Debug(ex);
                        }
                        catch (Exception ex)
                        {
                            ProbeToolsPackage.Log.Error(ex);
                        }
                        finally
                        {
                            _codeAnalysisScanned = true;
                        }
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            });
        }

        private void RunBackgroundFec()
        {
            Shell.Status($"FEC: {_fileName} (running)");

            BackgroundFec.RunSync(_fileName, _cancel.Token);

            Shell.Status($"FEC: {_fileName} (complete)");
        }

        private void RunCodeAnalysis()
        {
            var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_textBuffer);
            if (fileStore == null)
            {
                ProbeToolsPackage.Log.Warning("Unable to get file store for text buffer: {0}", _fileName);
                return;
            }

            Shell.Status($"Code Analysis: {_fileName} (running)");

            var appSettings = ProbeToolsPackage.Instance.App.Settings;
            var model = fileStore.GetMostRecentModelSync(appSettings, _fileName, _textBuffer.CurrentSnapshot, "Code Analysis", _cancel.Token);

            if (_cancel.IsCancellationRequested) return;
            
            var preprocessedModel = fileStore.CreatePreprocessedModelSync(
                appSettings: appSettings,
                fileName: _fileName,
                snapshot: _textBuffer.CurrentSnapshot,
                visible: false,
                cancel: _cancel.Token,
                reason: "Background Code Analysis");

            if (_cancel.IsCancellationRequested) return;

            var ca = new CodeAnalyzer(appSettings.Context, preprocessedModel);
            var caResults = ca.Run(_cancel.Token);

            if (_cancel.IsCancellationRequested) return;

            ErrorTaskProvider.Instance.ReplaceForSourceAndInvokingFile(ErrorTaskSource.CodeAnalysis,
                ca.CodeModel.FilePath, caResults.Tasks.Select(x => x.ToErrorTask()));

            ErrorMarkerTaggerProvider.ReplaceForSourceAndFile(ErrorTaskSource.CodeAnalysis,
                ca.CodeModel.FilePath, caResults.Markers.Select(x => x.ToErrorMarkerTag()));

            Shell.Status($"Code Analysis: {_fileName} (complete)");
        }

        public string FileName => _fileName;
    }
}
