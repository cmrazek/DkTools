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
	[ContentType("DK")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	public class ProbeSmartIndentProvider : ISmartIndentProvider, IVsTextViewCreationListener
	{
		ISmartIndent ISmartIndentProvider.CreateSmartIndent(ITextView textView)
		{
			Func<ProbeSmartIndent> createObj = () => { return new ProbeSmartIndent(textView); };
			return textView.Properties.GetOrCreateSingletonProperty(createObj);
		}

		void IVsTextViewCreationListener.VsTextViewCreated(IVsTextView textViewAdapter)
		{
			var textView = Shell.VsTextViewToWpfTextView(textViewAdapter);

			Func<ProbeSmartIndentCommandHandler> createObj = () => { return new ProbeSmartIndentCommandHandler(textViewAdapter, textView, this); };
			textView.Properties.GetOrCreateSingletonProperty(createObj);
		}
	}
}
