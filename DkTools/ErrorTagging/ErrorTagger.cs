using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.CodeAnalysis;
using DK.Diagnostics;
using DK.Modeling;
using DkTools.CodeModeling;
using DkTools.Compiler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DkTools.ErrorTagging
{
    internal class ErrorTagger : ITagger<ErrorTag>
    {
        private ITextView _view;
        private CompileCoordinator _compileCoordinator;

        public const string CodeErrorLight = "DkCodeError.Light";
        public const string CodeErrorDark = "DkCodeError.Dark";
        public const string CodeWarningLight = "DkCodeWarning.Light";
        public const string CodeWarningDark = "DkCodeWarning.Dark";
        public const string CodeAnalysisErrorLight = "DkCodeAnalysisError.Light";
        public const string CodeAnalysisErrorDark = "DkCodeAnalysisError.Dark";
        public const string ReportOutputTagLight = "DkReportOutputTag.Light";
        public const string ReportOutputTagDark = "DkReportOutputTag.Dark";

        public ErrorTagger(ITextView view)
        {
            _view = view;
            _compileCoordinator = CompileCoordinator.GetOrCreateForTextBuffer(_view.TextBuffer);

            ErrorTaskProvider.ErrorTagsChangedForFile += Instance_ErrorTagsChangedForFile;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var fileName = _compileCoordinator.FileName;
            if (fileName != null)
            {
                return ErrorTaskProvider.Instance.GetErrorTagsForFile(fileName, spans);
            }

            return new TagSpan<ErrorTag>[0];
        }

        void Instance_ErrorTagsChangedForFile(object sender, ErrorTaskProvider.ErrorTaskEventArgs e)
        {
            try
            {
                if (string.Equals(e.FileName, _compileCoordinator.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(_view.TextSnapshot, 0, _view.TextSnapshot.Length)));
                }
            }
            catch (Exception ex)
            {
                ProbeToolsPackage.Instance.App.Log.Error(ex);
            }
        }
    }
}
