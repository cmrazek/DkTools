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
		private Dictionary<ProbeClassifierType, IClassificationType> _lightTokenTypes;
		private Dictionary<ProbeClassifierType, IClassificationType> _darkTokenTypes;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		public ProbeClassifier(IClassificationTypeRegistryService registry)
		{
			_lightTokenTypes = new Dictionary<ProbeClassifierType, IClassificationType>();
			_lightTokenTypes[ProbeClassifierType.Normal] = registry.GetClassificationType("DK.Normal.Light");
			_lightTokenTypes[ProbeClassifierType.Comment] = registry.GetClassificationType("DK.Comment.Light");
			_lightTokenTypes[ProbeClassifierType.Keyword] = registry.GetClassificationType("DK.Keyword.Light");
			_lightTokenTypes[ProbeClassifierType.Number] = registry.GetClassificationType("DK.Number.Light");
			_lightTokenTypes[ProbeClassifierType.StringLiteral] = registry.GetClassificationType("DK.StringLiteral.Light");
			_lightTokenTypes[ProbeClassifierType.Preprocessor] = registry.GetClassificationType("DK.Preprocessor.Light");
			_lightTokenTypes[ProbeClassifierType.Inactive] = registry.GetClassificationType("DK.Inactive.Light");
			_lightTokenTypes[ProbeClassifierType.TableName] = registry.GetClassificationType("DK.TableName.Light");
			_lightTokenTypes[ProbeClassifierType.TableField] = registry.GetClassificationType("DK.TableField.Light");
			_lightTokenTypes[ProbeClassifierType.Constant] = registry.GetClassificationType("DK.Constant.Light");
			_lightTokenTypes[ProbeClassifierType.DataType] = registry.GetClassificationType("DK.DataType.Light");
			_lightTokenTypes[ProbeClassifierType.Function] = registry.GetClassificationType("DK.Function.Light");
			_lightTokenTypes[ProbeClassifierType.Delimiter] = registry.GetClassificationType("DK.Delimiter.Light");
			_lightTokenTypes[ProbeClassifierType.Operator] = registry.GetClassificationType("DK.Operator.Light");
			_lightTokenTypes[ProbeClassifierType.Variable] = registry.GetClassificationType("DK.Variable.Light");
			_lightTokenTypes[ProbeClassifierType.Interface] = registry.GetClassificationType("DK.Interface.Light");

			_darkTokenTypes = new Dictionary<ProbeClassifierType, IClassificationType>();
			_darkTokenTypes[ProbeClassifierType.Normal] = registry.GetClassificationType("DK.Normal.Dark");
			_darkTokenTypes[ProbeClassifierType.Comment] = registry.GetClassificationType("DK.Comment.Dark");
			_darkTokenTypes[ProbeClassifierType.Keyword] = registry.GetClassificationType("DK.Keyword.Dark");
			_darkTokenTypes[ProbeClassifierType.Number] = registry.GetClassificationType("DK.Number.Dark");
			_darkTokenTypes[ProbeClassifierType.StringLiteral] = registry.GetClassificationType("DK.StringLiteral.Dark");
			_darkTokenTypes[ProbeClassifierType.Preprocessor] = registry.GetClassificationType("DK.Preprocessor.Dark");
			_darkTokenTypes[ProbeClassifierType.Inactive] = registry.GetClassificationType("DK.Inactive.Dark");
			_darkTokenTypes[ProbeClassifierType.TableName] = registry.GetClassificationType("DK.TableName.Dark");
			_darkTokenTypes[ProbeClassifierType.TableField] = registry.GetClassificationType("DK.TableField.Dark");
			_darkTokenTypes[ProbeClassifierType.Constant] = registry.GetClassificationType("DK.Constant.Dark");
			_darkTokenTypes[ProbeClassifierType.DataType] = registry.GetClassificationType("DK.DataType.Dark");
			_darkTokenTypes[ProbeClassifierType.Function] = registry.GetClassificationType("DK.Function.Dark");
			_darkTokenTypes[ProbeClassifierType.Delimiter] = registry.GetClassificationType("DK.Delimiter.Dark");
			_darkTokenTypes[ProbeClassifierType.Operator] = registry.GetClassificationType("DK.Operator.Dark");
			_darkTokenTypes[ProbeClassifierType.Variable] = registry.GetClassificationType("DK.Variable.Dark");
			_darkTokenTypes[ProbeClassifierType.Interface] = registry.GetClassificationType("DK.Interface.Dark");

			_scanner = new ProbeClassifierScanner();

			ProbeEnvironment.AppChanged += new EventHandler(ProbeEnvironment_AppChanged);
			ProbeToolsPackage.Instance.EditorOptions.EditorRefreshRequired += EditorOptions_ClassifierRefreshRequired;

			VSTheme.ThemeChanged += VSTheme_ThemeChanged;
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			_snapshot = span.Snapshot;

			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(span.Snapshot.TextBuffer);
			var spans = new List<ClassificationSpan>();
			var state = tracker.GetStateForPosition(span.Start.Position, span.Snapshot);
			var tokenInfo = new ProbeClassifierScanner.TokenInfo();

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(span.Snapshot.TextBuffer);
			if (fileStore == null) return new List<ClassificationSpan>();
			var model = fileStore.GetMostRecentModel(span.Snapshot, "GetClassificationSpans");
			_scanner.SetSource(span.GetText(), span.Start.Position, span.Snapshot, model);

			var disableDeadCode = ProbeToolsPackage.Instance.EditorOptions.DisableDeadCode;

			DisabledSectionTracker disabledSectionTracker = null;
			if (disableDeadCode)
			{
				disabledSectionTracker = new DisabledSectionTracker(model.DisabledSections);
				if (disabledSectionTracker.SetOffset(_scanner.PositionOffset)) state |= State.Disabled;
				else state &= ~State.Disabled;
			}
			else
			{
				state &= ~State.Disabled;
			}

			while (_scanner.ScanTokenAndProvideInfoAboutIt(tokenInfo, ref state))
			{
				IClassificationType classificationType = null;
				if (VSTheme.CurrentTheme == VSThemeMode.Light)
				{
					_lightTokenTypes.TryGetValue(tokenInfo.Type, out classificationType);
				}
				else
				{
					_darkTokenTypes.TryGetValue(tokenInfo.Type, out classificationType);
				}

				if (classificationType != null)
				{
					spans.Add(new ClassificationSpan(new SnapshotSpan(_snapshot, new Span(span.Start.Position + tokenInfo.StartIndex, tokenInfo.Length)), classificationType));
				}

				if (disableDeadCode)
				{
					if (disabledSectionTracker.Advance(_scanner.PositionOffset + _scanner.Position)) state |= State.Disabled;
					else state &= ~State.Disabled;
				}
				else
				{
					state &= ~State.Disabled;
				}
			}

			return spans;
		}

		private void UpdateClassification()
		{
			if (_snapshot != null)
			{
				var ev = ClassificationChanged;
				if (ev != null) ev(this, new ClassificationChangedEventArgs(new SnapshotSpan(_snapshot, new Span(0, _snapshot.Length))));
			}
		}

		private void ProbeEnvironment_AppChanged(object sender, EventArgs e)
		{
			UpdateClassification();
		}

		void EditorOptions_ClassifierRefreshRequired(object sender, EventArgs e)
		{
			UpdateClassification();
		}

		void VSTheme_ThemeChanged(object sender, EventArgs e)
		{
			try
			{
				UpdateClassification();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception on classifier theme changed.");
			}
		}
	}
}
