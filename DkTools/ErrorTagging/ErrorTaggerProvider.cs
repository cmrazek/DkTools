#if REPORT_ERRORS || BACKGROUND_FEC
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

			textView.Closed += textView_Closed;

			Func<ErrorTagger> creator = () => { return new ErrorTagger(textView); };
			return textView.Properties.GetOrCreateSingletonProperty<ErrorTagger>(creator) as ITagger<T>;
		}

		void textView_Closed(object sender, EventArgs e)
		{
			try
			{
				var textView = sender as ITextView;
				if (textView != null)
				{
					ErrorTaskProvider.Instance.OnDocumentClosed(textView);
				}
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}
	}
}
#endif
