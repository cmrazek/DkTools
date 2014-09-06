using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
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
			public string infoText;
			public CodeModel.Tokens.Token token;
		}

		public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
		{
			_provider = provider;
			_subjectBuffer = subjectBuffer;
		}

		void IQuickInfoSource.AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
		{
			var subjectTriggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
			if (!subjectTriggerPoint.HasValue)
			{
				applicableToSpan = null;
				return;
			}
			var snapshotPoint = subjectTriggerPoint.Value;
			var currentSnapshot = snapshotPoint.Snapshot;

			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_subjectBuffer).Model;
			if (model != null && model.Snapshot != null)
			{
				var modelPos = model.GetPosition(snapshotPoint.Position, snapshotPoint.Snapshot);
				var tokens = model.FindTokens(modelPos).ToArray();
				var info = GetQuickInfoForTokens(tokens);
				if (info.HasValue)
				{
					quickInfoContent.Add(info.Value.infoText);
					applicableToSpan = model.Snapshot.CreateTrackingSpan(info.Value.token.Span.ToVsTextSpan(), SpanTrackingMode.EdgeInclusive);
					return;
				}
			}

			applicableToSpan = null;
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
			var infoText = lastToken.GetQuickInfo();
			if (!string.IsNullOrEmpty(infoText))
			{
				return new TokenInfo { infoText = infoText, token = lastToken };
			}

			return null;
		}
	}
}
