﻿using DK.AppEnvironment;
using DkTools.CodeModeling;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DkTools.BraceHighlighting
{
	internal class BraceHighlightTagger : ITagger<BraceHighlightTag>
	{
		private ITextView _view;
		private ITextBuffer _sourceBuffer;
		private NormalizedSnapshotSpanCollection _braceSpans;
		private NormalizedSnapshotSpanCollection _wordSpans;
		private SnapshotSpan? _updateSpan;
		private object _updateLock = new object();
		private BackgroundDeferrer _wordSelectDeferrer;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public BraceHighlightTagger(ITextView view, ITextBuffer sourceBuffer)
		{
			_view = view;
			_sourceBuffer = sourceBuffer;

			_view.Caret.PositionChanged += new EventHandler<CaretPositionChangedEventArgs>(Caret_PositionChanged);
			_view.LayoutChanged += new EventHandler<TextViewLayoutChangedEventArgs>(_view_LayoutChanged);

			_wordSelectDeferrer = new BackgroundDeferrer(Constants.WordSelectDelay);
			_wordSelectDeferrer.Idle += new EventHandler<BackgroundDeferrer.IdleEventArgs>(WordSelectDeferrer_Idle);
		}

		private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			UpdateAtCaretPosition(_view.Caret.Position);
		}

		private void _view_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (e.NewSnapshot != e.OldSnapshot)
			{
				UpdateAtCaretPosition(_view.Caret.Position);
			}
		}

		private void UpdateAtCaretPosition(CaretPosition caretPosition)
		{
			var snapshotPoint = caretPosition.Point.GetPoint(_sourceBuffer, caretPosition.Affinity);
			if (!snapshotPoint.HasValue) return;

			Update(snapshotPoint.Value, null);

			_wordSelectDeferrer.OnActivity(snapshotPoint.Value);
		}

		public IEnumerable<ITagSpan<BraceHighlightTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (_braceSpans != null)
			{
				foreach (var span in _braceSpans) yield return new TagSpan<BraceHighlightTag>(span, new BraceHighlightTag());
			}

			if (_wordSpans != null)
			{
				foreach (var span in _wordSpans) yield return new TagSpan<BraceHighlightTag>(span, new BraceHighlightTag());
			}
		}

		private void Update(SnapshotPoint snapPt, NormalizedSnapshotSpanCollection wordSpans)
		{
			var parser = new BraceHighlightParser();
			var spans = parser.FindMatchingBraces(snapPt.Snapshot, snapPt.Position).ToArray();
			var changed = false;

			var oldBraceSpans = _braceSpans;
			var oldWordSpans = _wordSpans;
			SnapshotSpan? updateSpan = null;

			lock (_updateLock)
			{
				if (wordSpans != _wordSpans)
				{
					_wordSpans = wordSpans;
					changed = true;
				}

				if (spans.Length < 2)
				{
					if (_braceSpans != null)
					{
						_braceSpans = null;
						changed = true;
					}
				}
				else
				{
					_braceSpans = new NormalizedSnapshotSpanCollection((from s in spans select new SnapshotSpan(snapPt.Snapshot, s.Start.Position, s.End.Position - s.Start.Position)));
					changed = true;
				}

				if (changed)
				{
					updateSpan = null;
					if (oldBraceSpans != null) updateSpan = oldBraceSpans.EncompassingSpan();
					if (oldWordSpans != null) updateSpan = updateSpan.HasValue ? updateSpan.EncompassingSpan(oldWordSpans) : oldWordSpans.EncompassingSpan();
					if (_braceSpans != null) updateSpan = updateSpan.HasValue ? updateSpan.EncompassingSpan(_braceSpans) : _braceSpans.EncompassingSpan();
					if (_wordSpans != null) updateSpan = updateSpan.HasValue ? updateSpan.EncompassingSpan(_wordSpans) : _wordSpans.EncompassingSpan();

					_updateSpan = null;
					if (_braceSpans != null) _updateSpan = _braceSpans.EncompassingSpan();
					if (_wordSpans != null) _updateSpan = _updateSpan.HasValue ? _updateSpan.EncompassingSpan(_wordSpans) : _wordSpans.EncompassingSpan();
				}
			}

			if (changed && updateSpan.HasValue)
			{
				var ev = TagsChanged;
				if (ev != null) ev(this, new SnapshotSpanEventArgs(updateSpan.Value));
			}
		}

		private void WordSelectDeferrer_Idle(object sender, BackgroundDeferrer.IdleEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				var snapPt = (SnapshotPoint)e.Value;
				if (_sourceBuffer.CurrentSnapshot != snapPt.Snapshot)
				{
					Update(snapPt, null);
					return;
				}

				var snapshot = snapPt.Snapshot;

				var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_sourceBuffer);
				if (fileStore != null)
				{
					var model = fileStore.Model;
					if (model != null)
					{
						var modelPos = model.AdjustPosition(snapPt);
						var caretToken = model.File.FindDownwardTouching(modelPos).LastOrDefault(t => t.SourceDefinition != null);
						if (caretToken == null)
						{
							Update(snapPt, null);
							return;
						}

						var modelSnapshot = model.Snapshot as ITextSnapshot;
						if (modelSnapshot != null)
						{
							var sourceDef = caretToken.SourceDefinition;
							var file = model.File;
							var matchingTokens = file.FindDownward(t => t.SourceDefinition == sourceDef);

							var wordSpans = new NormalizedSnapshotSpanCollection(from t in matchingTokens select new SnapshotSpan(snapshot, VsTextUtil.ModelSpanToVsSnapshotSpan(modelSnapshot, t.Span, snapshot)));
							if (!wordSpans.Any()) wordSpans = null;

							Update(snapPt, wordSpans);
						}
					}
				}
			});
		}
	}
}
