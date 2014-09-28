using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.QuickInfo
{
	internal class QuickInfoSource : IQuickInfoSource
	{
		private QuickInfoSourceProvider _provider;
		private ITextBuffer _subjectBuffer;

		private struct TokenInfo
		{
			public CodeModel.Tokens.Token token;
			public UIElement infoElements;
		}

		public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
		{
			_provider = provider;
			_subjectBuffer = subjectBuffer;
		}

		void IQuickInfoSource.AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
		{
			applicableToSpan = null;

			var subjectTriggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
			if (!subjectTriggerPoint.HasValue) return;
			var snapshotPoint = subjectTriggerPoint.Value;
			var currentSnapshot = snapshotPoint.Snapshot;

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_subjectBuffer);
			if (fileStore != null)
			{
				var model = fileStore.Model;
				if (model != null && model.Snapshot != null)
				{
					var modelPos = model.AdjustPosition(snapshotPoint.Position, snapshotPoint.Snapshot);

					var tokens = model.FindTokens(modelPos).ToArray();
					var info = GetQuickInfoForTokens(tokens);
					if (info.HasValue)
					{
						quickInfoContent.Add(info.Value.infoElements);
						var tokenSpan = info.Value.token.Span;
						var snapSpan = new SnapshotSpan(model.Snapshot, tokenSpan.Start, tokenSpan.Length);
						applicableToSpan = model.Snapshot.CreateTrackingSpan(info.Value.token.Span.ToVsTextSpan(), SpanTrackingMode.EdgeInclusive);
					}

#if REPORT_ERRORS
					foreach (var error in model.PreprocessorModel.ErrorProvider.GetErrorsForPos(modelPos))
					{
						quickInfoContent.Add(error.Message);
						if (applicableToSpan == null)
						{
							var snapSpan = new SnapshotSpan(model.Snapshot, error.Span.Start, error.Span.Length);
							applicableToSpan = model.Snapshot.CreateTrackingSpan(snapSpan, SpanTrackingMode.EdgeInclusive);
						}
					}
#endif
				}
			}
		}

		private bool _disposed;
		void IDisposable.Dispose()
		{
			if (!_disposed)
			{
				GC.SuppressFinalize(this);
				_disposed = true;
			}
		}

		private TokenInfo? GetQuickInfoForTokens(CodeModel.Tokens.Token[] tokens)
		{
			if (tokens.Length == 0) return null;

			var lastToken = tokens.Last();
			var infoElements = lastToken.GetQuickInfoWpf();
			if (infoElements != null)
			{
				return new TokenInfo { infoElements = infoElements, token = lastToken };
			}

			return null;
		}
	}
}
