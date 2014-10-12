using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace DkTools
{
	[Export(typeof(IVsTextViewCreationListener))]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	[ContentType("plaintext")]
	internal sealed class DictTextViewListener : IVsTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

		[Import]
		internal IContentTypeRegistryService ContentTypeRegistryService = null;

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			ITextView view = AdapterService.GetWpfTextView(textViewAdapter);
			if (view == null) return;

			var buf = view.TextBuffer;
			var fileName = buf.TryGetFileName();
			if (string.IsNullOrEmpty(fileName)) return;

			var titleExt = System.IO.Path.GetFileName(fileName);
			if (titleExt.Equals("dict", StringComparison.OrdinalIgnoreCase) ||
				titleExt.Equals("dict+", StringComparison.OrdinalIgnoreCase) ||
				titleExt.Equals("dict&", StringComparison.OrdinalIgnoreCase))
			{
				var contentType = ContentTypeRegistryService.GetContentType(Constants.DkContentType);
				if (contentType == null) return;

				Log.WriteDebug("Changing content type of DICT file to DK.");
				buf.ChangeContentType(contentType, null);
			}
		}
	}
}
