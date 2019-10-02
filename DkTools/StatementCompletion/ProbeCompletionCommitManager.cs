using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace DkTools.StatementCompletion
{
	class ProbeCompletionCommitManager : IAsyncCompletionCommitManager
	{
		private ITextView _textView;
		private bool _startAbort;

		private static readonly char[] _commitChars = new char[]
		{
			' ', ';', '.', '(', ')', ',', '<', '>', '\"', '\'', '-',
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
		};
		private static readonly char[] _acceptSelectionCompletionChars = new char[]
		{
			'\n', '\t', ' '
		};

		public ProbeCompletionCommitManager(ITextView textView)
		{
			_textView = textView;
		}

		IEnumerable<char> IAsyncCompletionCommitManager.PotentialCommitCharacters
		{
			get { return _commitChars; }
		}

		bool IAsyncCompletionCommitManager.ShouldCommitCompletion(char typedChar, SnapshotPoint location, CancellationToken token)
		{
			// Runs synchronously on main thread

			_startAbort = false;

			if (_textView.Properties.TryGetProperty(typeof(IAsyncCompletionSession), out IAsyncCompletionSession session))
			{
				if (ProbeAsyncCompletionSource.NoCompletionChars.Contains(typedChar))
				{
					if (session.ApplicableToSpan.GetSpan(location.Snapshot).Length == 1 &&
						session.ApplicableToSpan.GetText(location.Snapshot) == typedChar.ToString())
					{
						_startAbort = true;
						return true;
					}
				}
			}

			if (char.IsDigit(typedChar)) return false;

			return true;
		}

		CommitResult IAsyncCompletionCommitManager.TryCommit(ITextView view, ITextBuffer buffer, CompletionItem item,
			ITrackingSpan applicableToSpan, char typedChar, CancellationToken token)
		{
			if (_startAbort)
			{
				var span = applicableToSpan.GetSpan(buffer.CurrentSnapshot);
				var text = applicableToSpan.GetText(buffer.CurrentSnapshot);

				if (view.Properties.TryGetProperty(typeof(IAsyncCompletionSession), out IAsyncCompletionSession session) &&
					session != null && !session.IsDismissed)
				{
					session.Dismiss();
				}

				return CommitResult.Handled;
			}
			else if (!_acceptSelectionCompletionChars.Contains(typedChar))
			{
				// Stick with what the user has typed so far, rather than the selection.
				if (view.Properties.TryGetProperty(typeof(IAsyncCompletionSession), out IAsyncCompletionSession session) &&
					session != null && !session.IsDismissed)
				{
					session.Dismiss();
				}

				return CommitResult.Handled;
			}
			else
			{
				// The selection should override what the user has typed.
				return CommitResult.Unhandled;  // Use default mechanism
			}
		}
	}
}
