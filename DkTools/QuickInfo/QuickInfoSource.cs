using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.QuickInfo
{
	internal class QuickInfoSource : IAsyncQuickInfoSource
	{
		private QuickInfoSourceProvider _provider;
		private ITextBuffer _subjectBuffer;

		private struct TokenInfo
		{
			public CodeModel.Tokens.Token token;
			public object infoElements;
		}

		public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
		{
			_provider = provider;
			_subjectBuffer = subjectBuffer;
		}

		async Task<QuickInfoItem> IAsyncQuickInfoSource.GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
		{
			// https://github.com/microsoft/VSSDK-Extensibility-Samples/blob/master/AsyncQuickInfo/src/LineAsyncQuickInfoSource.cs

			var subjectTriggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
			if (!subjectTriggerPoint.HasValue) return null;
			var snapshotPoint = subjectTriggerPoint.Value;
			var currentSnapshot = snapshotPoint.Snapshot;

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_subjectBuffer);
			if (fileStore != null)
			{
				var model = fileStore.Model;
				if (model != null && model.Snapshot != null)
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
						var snapSpan = new SnapshotSpan(model.Snapshot, tokenSpan.Start, tokenSpan.Length);
						applicableToSpan = model.Snapshot.CreateTrackingSpan(info.Value.token.Span.ToVsTextSpan(), SpanTrackingMode.EdgeInclusive);
					}

					var tasks = await ErrorTagging.ErrorTaskProvider.Instance.GetErrorMessagesAtPointAsync(model.FilePath, snapshotPoint);
					foreach (var task in tasks)
					{
						if (elements != null)
						{
							elements = new ContainerElement(ContainerElementStyle.Stacked, elements, task.QuickInfoContent);
						}
						else
						{
							elements = task.QuickInfoContent;
						}

						if (applicableToSpan == null)
						{
							var snapshotSpan = task.TryGetSnapshotSpan(snapshotPoint.Snapshot);
							if (snapshotSpan.HasValue)
							{
								applicableToSpan = model.Snapshot.CreateTrackingSpan(snapshotSpan.Value, SpanTrackingMode.EdgeInclusive);
							}
						}
					}

					if (elements != null && applicableToSpan != null)
					{
						return new QuickInfoItem(applicableToSpan, elements);
					}
				}
			}

			return null;
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
			var infoElements = lastToken.GetQuickInfoElements()?.GenerateElements_VS();
			if (infoElements != null)
			{
				return new TokenInfo { infoElements = infoElements, token = lastToken };
			}

			return null;
		}
	}
}
