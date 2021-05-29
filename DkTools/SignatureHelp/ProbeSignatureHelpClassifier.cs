using DK.Syntax;
using DkTools.Classifier;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;

namespace DkTools.SignatureHelp
{
	public class ProbeSignatureHelpClassifier : IClassifier
	{
		private ITextBuffer _textBuffer;

		public ProbeSignatureHelpClassifier(ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
		}

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			if (!span.Snapshot.TextBuffer.Properties.TryGetProperty(typeof(ISignatureHelpSession), out ISignatureHelpSession session))
				return GetFallbackClassificationSpans(span);

			if (session.IsDismissed) return GetFallbackClassificationSpans(span);

			var sig = session.SelectedSignature as ProbeSignature;
			if (sig == null) return GetFallbackClassificationSpans(span);

			if (!span.Snapshot.TextBuffer.Properties.TryGetProperty("UsePrettyPrintedContent", out bool usePrettyPrintedContent))
				usePrettyPrintedContent = false;

			var spans = new List<ClassificationSpan>();
			int pos = span.Start.Position;
			foreach (var run in sig.ClassifiedContent.Runs)
			{
				var classificationType = ProbeClassifier.GetClassificationType(run.Type);
				if (classificationType != null)
				{
					spans.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot, pos, run.Length), classificationType));
				}
				pos += run.Length;
			}

			return spans;
		}

		private IList<ClassificationSpan> GetFallbackClassificationSpans(SnapshotSpan span)
		{
			var ret = new List<ClassificationSpan>();
			var classificationType = ProbeClassifier.GetClassificationType(ProbeClassifierType.Normal);
			if (classificationType != null)
			{
				var cs = new ClassificationSpan(span, classificationType);
				ret.Add(cs);
			}
			return ret;
		}

		private void FireClassificationChanged()
		{
			// This is only here to suppress the warning that the event is never used.
			// Classification should never change for a function signature.
			ClassificationChanged?.Invoke(this, null);
		}
	}
}
