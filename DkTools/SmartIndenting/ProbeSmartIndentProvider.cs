using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.SmartIndenting
{
	[Export(typeof(ISmartIndentProvider))]
	[Export(typeof(IVsTextViewCreationListener))]
	[ContentType(Constants.DkContentType)]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	[Order(After = nameof(DkGenericTextViewListener))]
	public sealed class ProbeSmartIndentProvider : ISmartIndentProvider, IVsTextViewCreationListener
	{
		public ISmartIndent CreateSmartIndent(ITextView textView)
		{
			Func<ProbeSmartIndent> createObj = () => { return new ProbeSmartIndent(textView); };
			return textView.Properties.GetOrCreateSingletonProperty(createObj);
		}

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var textView = Shell.VsTextViewToWpfTextView(textViewAdapter);
			if (textView == null) return;
			if (textView.TextBuffer.ContentType.TypeName != Constants.DkContentType) return;

			// Create the smart indent now so that it's available before VS determines it's needed (e.g. when cleaning up snippets)
			CreateSmartIndent(textView);

			Func<ProbeSmartIndentCommandHandler> createObj = () => { return new ProbeSmartIndentCommandHandler(textViewAdapter, textView, this); };
			textView.Properties.GetOrCreateSingletonProperty(createObj);
		}
	}
}
