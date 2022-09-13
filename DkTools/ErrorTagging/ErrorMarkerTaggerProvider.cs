using DK;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace DkTools.ErrorTagging
{
	[Export(typeof(IViewTaggerProvider))]
	[TagType(typeof(ErrorMarkerTag))]
	[ContentType(Constants.DkContentType)]
	class ErrorMarkerTaggerProvider : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new ErrorMarkerTagger(textView) as ITagger<T>;
		}

		private static List<ErrorMarkerTag> _tags = new List<ErrorMarkerTag>();

		public static void ReplaceForSourceAndFile(ErrorTaskSource source, string filePath, IEnumerable<ErrorMarkerTag> tags)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_tags.RemoveAll(x => x.Source == source && x.FilePath.EqualsI(filePath));
				_tags.AddRange(tags);

				ErrorMarkerTagsChanged?.Invoke(null, new ErrorMarkerTagsChangedEventArgs { FilePath = filePath });
			});
		}

		public static event EventHandler<ErrorMarkerTagsChangedEventArgs> ErrorMarkerTagsChanged;

		public class ErrorMarkerTagsChangedEventArgs : EventArgs
		{
			public string FilePath { get; set; }
		}

		public static IEnumerable<ITagSpan<ErrorMarkerTag>> GetTags(string filePath, NormalizedSnapshotSpanCollection spans)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (var querySpan in spans)
			{
				foreach (var tag in _tags)
				{
					if (!tag.FilePath.EqualsI(filePath)) continue;

					var tagSnapshotSpan = tag.TryGetSnapshotSpan(querySpan.Snapshot);
					if (!tagSnapshotSpan.HasValue) continue;

					if (querySpan.IntersectsWith(tagSnapshotSpan.Value))
					{
						yield return new TagSpan<ErrorMarkerTag>(tagSnapshotSpan.Value, tag);
					}
				}
			}
		}
	}
}
