using DkTools.CodeModeling;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using DK.Diagnostics;

namespace DkTools.Outlining
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification="This class is part of MEF.")]
	internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>
	{
		private ITextBuffer _buffer;
		private List<ModelRegion> _modelRegions = new List<ModelRegion>();

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		private class ModelRegion
		{
			public SnapshotSpan span;
			public bool isFunction;
			public string text;
			public string tooltipText;
		}

		public OutliningTagger(ITextBuffer buffer)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_buffer = buffer;

			var notifier = DkTextBufferNotifier.GetOrCreate(_buffer);
            notifier.NewModelAvailable += Notifier_NewModelAvailable;
		}

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0) yield break;

			foreach (var region in _modelRegions)
			{
				var snapSpan = region.span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);

				yield return new TagSpan<IOutliningRegionTag>(snapSpan, new OutliningRegionTag(false, region.isFunction, region.text, region.tooltipText));
			}
		}

		private void Notifier_NewModelAvailable(object sender, DkTextBufferNotifier.NewModelAvailableEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
					Reparse();
                }
                catch (Exception ex)
                {
					Log.Error(ex);
                }
			});
        }

		private void Reparse()
		{
			_modelRegions.Clear();
			var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_buffer);
			if (fileStore == null)
			{
				Log.Debug("Outlinging could not be reparsed because the file store is null.");
				return;
			}

			var model = fileStore.Model;
			if (model == null)
			{
				Log.Debug("Outlining could not be reparsed because the model is null.");
				return;
			}

			var modelSnapshot = model.Snapshot as ITextSnapshot;
			if (modelSnapshot == null)
			{
				Log.Debug("Outlinging could not be reparsed because the model has no snapshot.");
				return;
			}

			foreach (var region in model.OutliningRegions.OrderBy(r => r.Span.Start))
			{
				_modelRegions.Add(new ModelRegion
				{
					span = new SnapshotSpan(modelSnapshot, new Span(region.Span.Start, region.Span.End - region.Span.Start)),
					isFunction = region.CollapseToDefinition,
					text = region.Text,
					tooltipText = region.TooltipText
				});
			}

			var span = new SnapshotSpan(modelSnapshot, 0, modelSnapshot.Length);
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
		}
	}
}
