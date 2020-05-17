using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
namespace DkTools.SignatureHelp
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("Probe Signature Help Controller")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    //[TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
	[ContentType(Constants.DkContentType)]
    [ContentType(Constants.TextContentType)]
    [Order(After = nameof(DkGenericTextViewListener))]
    internal sealed class ProbeSignatureHelpCommandProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ISignatureHelpBroker SignatureHelpBroker { get; set; }

		[Import]
		internal SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        internal IPeekBroker PeekBroker = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null) return;
            if (textView.TextBuffer.ContentType.TypeName != Constants.DkContentType) return;

            textView.Properties.GetOrCreateSingletonProperty(
                 () => new ProbeSignatureHelpCommandHandler(textViewAdapter,
                    textView,
                    NavigatorService.GetTextStructureNavigator(textView.TextBuffer),
                    SignatureHelpBroker,
					this));

            textView.Properties.GetOrCreateSingletonProperty(typeof(IPeekBroker), () => PeekBroker);
        }
    }
}
