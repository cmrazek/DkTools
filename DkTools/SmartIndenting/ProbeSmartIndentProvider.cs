using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.SmartIndenting
{
	[Export(typeof(ISmartIndentProvider))]
	[Export(typeof(IVsTextViewCreationListener))]
	[ContentType(Constants.DkContentType)]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	public sealed class ProbeSmartIndentProvider : ISmartIndentProvider, IVsTextViewCreationListener
	{
		public ISmartIndent CreateSmartIndent(ITextView textView)
		{
			Func<ProbeSmartIndent> createObj = () => { return new ProbeSmartIndent(textView); };
			return textView.Properties.GetOrCreateSingletonProperty(createObj);
		}

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			var textView = Shell.VsTextViewToWpfTextView(textViewAdapter);

			// Create the smart indent now so that it's available before VS determines it's needed (e.g. when cleaning up snippets)
			CreateSmartIndent(textView);

			Func<ProbeSmartIndentCommandHandler> createObj = () => { return new ProbeSmartIndentCommandHandler(textViewAdapter, textView, this); };
			textView.Properties.GetOrCreateSingletonProperty(createObj);
		}
	}
}
