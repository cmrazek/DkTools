#if REPORT_ERRORS
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.ErrorTagging
{
	[Export(typeof(IViewTaggerProvider))]
	[TagType(typeof(ErrorTag))]
	[ContentType("DK")]
	internal class ErrorTaggerProvider : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			if (textView.TextBuffer != buffer) return null;

			return new ErrorTagger(textView) as ITagger<T>;
		}
	}
}
#endif
