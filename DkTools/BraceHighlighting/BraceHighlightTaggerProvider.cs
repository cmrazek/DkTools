using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
//using Microsoft.VisualStudio.TextManager.Interop;

namespace DkTools.BraceHighlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(BraceHighlightTag))]
    [ContentType("DK")]
    internal class BraceHighlightTaggerProvider : IViewTaggerProvider
    {
		[Import]
		public IOutliningManagerService OutliningManagerService = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
			//provide highlighting only on the top buffer 
			if (textView.TextBuffer != buffer) return null;

			var wpfView = textView as IWpfTextView;
			if (wpfView != null)
			{
				var outMgr = OutliningManagerService.GetOutliningManager(textView);
				Func<Navigation.Navigator> createHandler = () => { return new Navigation.Navigator(wpfView, outMgr); };
				wpfView.Properties.GetOrCreateSingletonProperty(createHandler);
			}

            return new BraceHighlightTagger(textView, buffer) as ITagger<T>;
        }
    }
}
