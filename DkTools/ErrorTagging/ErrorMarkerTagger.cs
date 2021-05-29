using DK;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace DkTools.ErrorTagging
{
	class ErrorMarkerTagger : ITagger<ErrorMarkerTag>
	{
		private ITextView _textView;

		public ErrorMarkerTagger(ITextView textView)
		{
			_textView = textView ?? throw new ArgumentNullException(nameof(textView));

			ErrorMarkerTaggerProvider.ErrorMarkerTagsChanged += ErrorMarkerTaggerProvider_ErrorMarkerTagsChanged;
		}

		~ErrorMarkerTagger()
		{
			ErrorMarkerTaggerProvider.ErrorMarkerTagsChanged -= ErrorMarkerTaggerProvider_ErrorMarkerTagsChanged;
		}

		private void ErrorMarkerTaggerProvider_ErrorMarkerTagsChanged(object sender, ErrorMarkerTaggerProvider.ErrorMarkerTagsChangedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileName = VsTextUtil.TryGetDocumentFileName(_textView.TextBuffer);
			if (e.FilePath.EqualsI(fileName))
			{
				var ss = new SnapshotSpan(_textView.TextBuffer.CurrentSnapshot, new Span(0, _textView.TextBuffer.CurrentSnapshot.Length));
				TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(ss));
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<ErrorMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileName = VsTextUtil.TryGetDocumentFileName(_textView.TextBuffer);
			if (string.IsNullOrEmpty(fileName)) return new ITagSpan<ErrorMarkerTag>[0];

			return ErrorMarkerTaggerProvider.GetTags(fileName, spans);
		}
	}
}
