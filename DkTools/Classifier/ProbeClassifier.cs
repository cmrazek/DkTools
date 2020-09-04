using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace DkTools.Classifier
{
	internal class ProbeClassifier : IClassifier
	{
		private ITextSnapshot _snapshot;
		private ProbeClassifierScanner _scanner;
		private static Dictionary<ProbeClassifierType, IClassificationType> _lightTokenTypes;
		private static Dictionary<ProbeClassifierType, IClassificationType> _darkTokenTypes;
		private static Dictionary<ProbeClassifierType, Brush> _brushes = new Dictionary<ProbeClassifierType, Brush>();

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		public ProbeClassifier(IClassificationTypeRegistryService registry)
		{
			InitializeClassifierTypes(registry);

			_scanner = new ProbeClassifierScanner();

			ProbeToolsPackage.RefreshAllDocumentsRequired += OnRefreshAllDocumentsRequired;
			ProbeToolsPackage.RefreshDocumentRequired += OnRefreshDocumentRequired;

			VSTheme.ThemeChanged += VSTheme_ThemeChanged;
		}

		~ProbeClassifier()
		{
			ProbeToolsPackage.RefreshAllDocumentsRequired -= OnRefreshAllDocumentsRequired;
			ProbeToolsPackage.RefreshDocumentRequired -= OnRefreshDocumentRequired;
		}

		private static void InitializeClassifierTypes(IClassificationTypeRegistryService registry)
		{
			if (_lightTokenTypes == null)
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
			}

			if (_darkTokenTypes == null)
			{
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
			}
		}

		public static string GetClassificationTypeName(ProbeClassifierType type)
		{
			switch (type)
			{
				case ProbeClassifierType.Normal:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Normal.Dark" : "DK.Normal.Light";
				case ProbeClassifierType.Comment:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Comment.Dark" : "DK.Comment.Light";
				case ProbeClassifierType.Keyword:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Keyword.Dark" : "DK.Keyword.Light";
				case ProbeClassifierType.Number:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Number.Dark" : "DK.Number.Light";
				case ProbeClassifierType.StringLiteral:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.StringLiteral.Dark" : "DK.StringLiteral.Light";
				case ProbeClassifierType.Preprocessor:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Preprocessor.Dark" : "DK.Preprocessor.Light";
				case ProbeClassifierType.Inactive:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Inactive.Dark" : "DK.Inactive.Light";
				case ProbeClassifierType.TableName:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.TableName.Dark" : "DK.TableName.Light";
				case ProbeClassifierType.TableField:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.TableField.Dark" : "DK.TableField.Light";
				case ProbeClassifierType.Constant:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Constant.Dark" : "DK.Constant.Light";
				case ProbeClassifierType.DataType:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.DataType.Dark" : "DK.DataType.Light";
				case ProbeClassifierType.Function:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Function.Dark" : "DK.Function.Light";
				case ProbeClassifierType.Delimiter:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Delimiter.Dark" : "DK.Delimiter.Light";
				case ProbeClassifierType.Operator:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Operator.Dark" : "DK.Operator.Light";
				case ProbeClassifierType.Variable:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Variable.Dark" : "DK.Variable.Light";
				case ProbeClassifierType.Interface:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Interface.Dark" : "DK.Interface.Light";
				default:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? "DK.Normal.Dark" : "DK.Normal.Light";
			}
		}

		public static Color GetClassificationColor(ProbeClassifierType type)
		{
			switch (type)
			{
				case ProbeClassifierType.Normal:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Silver : Colors.Black;
				case ProbeClassifierType.Comment:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.LimeGreen : Colors.DarkGreen;
				case ProbeClassifierType.Keyword:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.DeepSkyBlue : Colors.Blue;
				case ProbeClassifierType.Number:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Tomato : Colors.DarkRed;
				case ProbeClassifierType.StringLiteral:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Tomato : Colors.DarkRed;
				case ProbeClassifierType.Preprocessor:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Gray : Colors.Gray;
				case ProbeClassifierType.Inactive:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.DimGray : Colors.LightGray;
				case ProbeClassifierType.TableName:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.SkyBlue : Colors.SteelBlue;
				case ProbeClassifierType.TableField:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.SkyBlue : Colors.SteelBlue;
				case ProbeClassifierType.Constant:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.DodgerBlue : Colors.DarkBlue;
				case ProbeClassifierType.DataType:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.LightSeaGreen : Colors.Teal;
				case ProbeClassifierType.Function:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Orchid : Colors.DarkMagenta;
				case ProbeClassifierType.Delimiter:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.LightSlateGray : Colors.DimGray;
				case ProbeClassifierType.Operator:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.LightSlateGray : Colors.DimGray;
				case ProbeClassifierType.Variable:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Azure : Colors.DarkSlateGray;
				case ProbeClassifierType.Interface:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.DarkOrange : Colors.DarkOrange;
				default:
					return VSTheme.CurrentTheme == VSThemeMode.Dark ? Colors.Silver : Colors.Black;
			}
		}

		public static Brush GetClassificationBrush(ProbeClassifierType type)
		{
			if (_brushes.TryGetValue(type, out var brush)) return brush;

			brush = new SolidColorBrush(GetClassificationColor(type));
			_brushes[type] = brush;
			return brush;
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)  // from IClassifier
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_snapshot = span.Snapshot;

			var appSettings = ProbeEnvironment.CurrentAppSettings;
			var fileName = VsTextUtil.TryGetDocumentFileName(span.Snapshot.TextBuffer);

			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(span.Snapshot.TextBuffer);
			var spans = new List<ClassificationSpan>();
			var state = tracker.GetStateForPosition(span.Start.Position, span.Snapshot, fileName, appSettings);
			var tokenInfo = new ProbeClassifierScanner.TokenInfo();

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(span.Snapshot.TextBuffer);
			if (fileStore == null) return new List<ClassificationSpan>();

			var model = fileStore.GetMostRecentModel(appSettings, fileName, span.Snapshot, "GetClassificationSpans");
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
				var classificationType = GetClassificationType(tokenInfo.Type);
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

		private void OnRefreshAllDocumentsRequired(object sender, EventArgs e)
		{
			try
			{
				UpdateClassification();
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		private void OnRefreshDocumentRequired(object sender, ProbeToolsPackage.RefreshDocumentEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (_snapshot == null) return;

				var filePath = VsTextUtil.TryGetDocumentFileName(_snapshot.TextBuffer);
				if (string.Equals(filePath, e.FilePath, StringComparison.OrdinalIgnoreCase))
				{
					UpdateClassification();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		void VSTheme_ThemeChanged(object sender, EventArgs e)
		{
			try
			{
				UpdateClassification();
				_brushes.Clear();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception on classifier theme changed.");
			}
		}

		public static IClassificationType GetClassificationType(ProbeClassifierType type)
		{
			if (VSTheme.CurrentTheme == VSThemeMode.Light)
			{
				if (_lightTokenTypes != null && _lightTokenTypes.TryGetValue(type, out IClassificationType classificationType))
				{
					return classificationType;
				}
			}
			else
			{
				if (_darkTokenTypes != null && _darkTokenTypes.TryGetValue(type, out IClassificationType classificationType))
				{
					return classificationType;
				}
			}

			return ProbeToolsPackage.Instance.DefaultClassificationType;
		}
	}
}
