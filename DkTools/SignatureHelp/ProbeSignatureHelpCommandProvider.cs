using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace DkTools.SignatureHelp
{
	[Export(typeof(IVsTextViewCreationListener))]
	[Name("Probe Signature Help Controller")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
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
				() => new ProbeSignatureHelpCommandHandler(
					textViewAdapter: textViewAdapter,
					textView: textView,
					textStructureNavigator: NavigatorService.GetTextStructureNavigator(textView.TextBuffer),
					signatureHelpBroker: SignatureHelpBroker,
					signatureHelpCommandProvider: this));

			textView.Properties.GetOrCreateSingletonProperty(typeof(IPeekBroker), () => PeekBroker);
		}
	}
}
