using DkTools.SignatureHelp;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(Constants.TextContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    sealed class DkGenericTextViewListener : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView != null)
            {
                if (textView.TextBuffer.ContentType.TypeName != Constants.DkContentType)
                {
                    var filePath = VsTextUtil.TryGetDocumentFileName(textView.TextBuffer);
                    if (!string.IsNullOrEmpty(filePath) && IsDkFile(filePath))
                    {
                        var contentType = ContentTypeRegistryService.GetContentType(Constants.DkContentType);
                        textView.TextBuffer.ChangeContentType(contentType, null);
                    }
                }
            }
        }

        private bool IsDkFile(string filePath)
        {
            if (System.IO.Path.GetFileName(filePath).Equals("dict", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var ext = System.IO.Path.GetExtension(filePath).ToLower();
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            if (Constants.ProbeExtensions.Contains(ext) || Constants.IncludeExtensions.Contains(ext))
            {
                return true;
            }

            return false;
        }
    }
}
