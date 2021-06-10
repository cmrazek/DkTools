using DK.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace DkTools.ErrorTagging
{
	[Export(typeof(IViewTaggerProvider))]
	[TagType(typeof(ErrorTag))]
	[ContentType(Constants.DkContentType)]
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
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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
			});
		}
	}
}
