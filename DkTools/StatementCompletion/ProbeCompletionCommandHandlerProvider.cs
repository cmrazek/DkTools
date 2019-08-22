using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.StatementCompletion
{
    [Export(typeof(IVsTextViewCreationListener))]
	[Name("DK Completion Handler")]
	[ContentType("DK")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class ProbeCompletionCommandHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            Func<ProbeCompletionCommandHandler> createCommandHandler = delegate() { return new ProbeCompletionCommandHandler(textViewAdapter, textView, this); };
            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);

			textView.GotAggregateFocus += textView_GotAggregateFocus;
        }

		void textView_GotAggregateFocus(object sender, EventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				var view = sender as IWpfTextView;
				if (view != null)
				{
					Shell.OnTextViewActivated(view);
				}
			});
		}
    }
}
