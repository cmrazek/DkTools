using DK.Modeling.Tokens;
using DkTools.CodeModeling;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DkTools.QuickInfo
{
	internal class QuickInfoSource : IAsyncQuickInfoSource
	{
		private QuickInfoSourceProvider _provider;
		private ITextBuffer _subjectBuffer;

		private struct TokenInfo
		{
			public Token token;
			public object infoElements;
		}

		public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
		{
			_provider = provider;
			_subjectBuffer = subjectBuffer;
		}

		Task<QuickInfoItem> IAsyncQuickInfoSource.GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
		{
			// https://github.com/microsoft/VSSDK-Extensibility-Samples/blob/master/AsyncQuickInfo/src/LineAsyncQuickInfoSource.cs

			var subjectTriggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
			if (!subjectTriggerPoint.HasValue) return null;
			var snapshotPoint = subjectTriggerPoint.Value;

			var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_subjectBuffer);
			if (fileStore != null)
			{
				var model = fileStore.Model;
				var modelSnapshot = model?.Snapshot as ITextSnapshot;
				if (model != null && modelSnapshot != null)
				{
					var modelPos = model.AdjustPosition(snapshotPoint.Position, snapshotPoint.Snapshot);

					object elements = null;
					ITrackingSpan applicableToSpan = null;

					var tokens = model.FindTokens(modelPos).ToArray();
					var info = GetQuickInfoForTokens(tokens);
					if (info.HasValue)
					{
						elements = info.Value.infoElements;

						var tokenSpan = info.Value.token.Span;
						var snapSpan = new SnapshotSpan(modelSnapshot, tokenSpan.Start, tokenSpan.Length);
						applicableToSpan = modelSnapshot.CreateTrackingSpan(info.Value.token.Span.ToVsTextSpan(), SpanTrackingMode.EdgeInclusive);
					}


					if (elements != null && applicableToSpan != null)
					{
						return Task.FromResult(new QuickInfoItem(applicableToSpan, elements));
					}
				}
			}

			return Task.FromResult<QuickInfoItem>(null);
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

		private TokenInfo? GetQuickInfoForTokens(Token[] tokens)
		{
			if (tokens.Length == 0) return null;

			var lastToken = tokens.Last();
			var infoElements = lastToken.GetQuickInfoElements()?.GenerateElements_VS();
			if (infoElements != null)
			{
				return new TokenInfo { infoElements = infoElements, token = lastToken };
			}

			return null;
		}
	}
}
