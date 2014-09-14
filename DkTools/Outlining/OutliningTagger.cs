using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace DkTools.Outlining
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification="This class is part of MEF.")]
	internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>
	{
		private ITextBuffer _buffer;
		private ITextSnapshot _snapshot;
		private List<ModelRegion> _modelRegions = new List<ModelRegion>();
		private BackgroundDeferrer _defer;

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
			_buffer = buffer;
			_snapshot = buffer.CurrentSnapshot;

			Reparse();
			
			_defer = new BackgroundDeferrer();
			_defer.Idle += new EventHandler<BackgroundDeferrer.IdleEventArgs>(_defer_Idle);

			_buffer.Changed += new EventHandler<TextContentChangedEventArgs>(BufferChanged);
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

		private void BufferChanged(object sender, TextContentChangedEventArgs e)
		{
			if (e.After != _buffer.CurrentSnapshot) return;
			_defer.OnActivity();
		}

		void _defer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			Reparse();

			var ev = TagsChanged;
			if (ev != null) ev(this, new SnapshotSpanEventArgs(new SnapshotSpan(_snapshot, 0, _snapshot.Length)));
		}

		private void Reparse()
		{
			_snapshot = _buffer.CurrentSnapshot;
			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_buffer).GetCurrentModel(_snapshot, "OutliningTagger.Reparse()");

			_modelRegions.Clear();
			foreach (var region in model.OutliningRegions.OrderBy(r => r.Span.Start))
			{
				_modelRegions.Add(new ModelRegion
				{
					span = new SnapshotSpan(model.Snapshot, new Span(region.Span.Start, region.Span.End - region.Span.Start)),
					isFunction = region.CollapseToDefinition,
					text = region.Text,
					tooltipText = region.TooltipText
				});
			}
		}
	}
}
