using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.Snippets
{
	[Export(typeof(IVsTextViewCreationListener))]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	[ContentType(Constants.DkContentType)]
	internal sealed class SnippetCommandProvider : IVsTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			ITextView view = AdapterService.GetWpfTextView(textViewAdapter);
			if (view == null) return;

			view.Properties.GetOrCreateSingletonProperty(() => new SnippetCommandHandler(view, textViewAdapter));
		}
	}
}
