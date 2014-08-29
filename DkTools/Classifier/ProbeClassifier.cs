using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace DkTools.Classifier
{
	internal class ProbeClassifier : IClassifier
	{
		private ITextSnapshot _snapshot;
		private ProbeClassifierScanner _scanner;
		private Dictionary<ProbeClassifierType, IClassificationType> _tokenTypes;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		public ProbeClassifier(IClassificationTypeRegistryService registry)
		{
			_tokenTypes = new Dictionary<ProbeClassifierType, IClassificationType>();

			_tokenTypes[ProbeClassifierType.Normal] = registry.GetClassificationType("DK.Normal");
			_tokenTypes[ProbeClassifierType.Comment] = registry.GetClassificationType("DK.Comment");
			_tokenTypes[ProbeClassifierType.Keyword] = registry.GetClassificationType("DK.Keyword");
			_tokenTypes[ProbeClassifierType.Number] = registry.GetClassificationType("DK.Number");
			_tokenTypes[ProbeClassifierType.StringLiteral] = registry.GetClassificationType("DK.StringLiteral");
			_tokenTypes[ProbeClassifierType.Preprocessor] = registry.GetClassificationType("DK.Preprocessor");
			_tokenTypes[ProbeClassifierType.Inactive] = registry.GetClassificationType("DK.Inactive");
			_tokenTypes[ProbeClassifierType.TableName] = registry.GetClassificationType("DK.TableName");
			_tokenTypes[ProbeClassifierType.TableField] = registry.GetClassificationType("DK.TableField");
			_tokenTypes[ProbeClassifierType.Constant] = registry.GetClassificationType("DK.Constant");
			_tokenTypes[ProbeClassifierType.DataType] = registry.GetClassificationType("DK.DataType");
			_tokenTypes[ProbeClassifierType.Function] = registry.GetClassificationType("DK.Function");
			_tokenTypes[ProbeClassifierType.Delimiter] = registry.GetClassificationType("DK.Delimiter");
			_tokenTypes[ProbeClassifierType.Operator] = registry.GetClassificationType("DK.Operator");

			_scanner = new ProbeClassifierScanner();

			ProbeEnvironment.AppChanged += new EventHandler(ProbeEnvironment_AppChanged);
		}

		private void ProbeEnvironment_AppChanged(object sender, EventArgs e)
		{
			if (_snapshot != null)
			{
				var ev = ClassificationChanged;
				if (ev != null) ev(this, new ClassificationChangedEventArgs(new SnapshotSpan(_snapshot, new Span(0, _snapshot.Length))));
			}
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			_snapshot = span.Snapshot;

			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(span.Snapshot.TextBuffer);
			var spans = new List<ClassificationSpan>();
			var state = tracker.GetStateForPosition(span.Start.Position, span.Snapshot);
			var tokenInfo = new ProbeClassifierScanner.TokenInfo();

			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(span.Snapshot.TextBuffer).GetMostRecentModelOrCreate(span.Snapshot, "GetClassificationSpans");
			_scanner.SetSource(span.GetText(), span.Start.Position, span.Snapshot, model);

			while (_scanner.ScanTokenAndProvideInfoAboutIt(tokenInfo, ref state))
			{
				IClassificationType classificationType;
				if (_tokenTypes.TryGetValue(tokenInfo.Type, out classificationType))
				{
					spans.Add(new ClassificationSpan(new SnapshotSpan(_snapshot, new Span(span.Start.Position + tokenInfo.StartIndex, tokenInfo.Length)), classificationType));
				}
			}

			return spans;
		}
	}
}
