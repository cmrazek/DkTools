using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
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
			' ', ';', '.', '(', ')', ',', '<', '>', '\"', '\'', '-', ':',
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
		};

		public ProbeCompletionCommitManager(ITextView textView)
		{
			_textView = textView;
		}

		IEnumerable<char> IAsyncCompletionCommitManager.PotentialCommitCharacters
		{
			get { return _commitChars; }
		}

		bool IAsyncCompletionCommitManager.ShouldCommitCompletion(IAsyncCompletionSession session, SnapshotPoint location,
			char typedChar, CancellationToken token)
		{
			// Runs synchronously on main thread

			_startAbort = false;

			if (ProbeAsyncCompletionSource.NoCompletionChars.Contains(typedChar))
			{
				if (session.ApplicableToSpan.GetSpan(location.Snapshot).Length == 1 &&
					session.ApplicableToSpan.GetText(location.Snapshot) == typedChar.ToString())
				{
					_startAbort = true;
					return true;
				}
			}

			if (char.IsDigit(typedChar)) return false;

			// For #include, if we're typing a char but the file has '.' between name and extension, then don't commit the completion.
			if (typedChar == '.')
			{
				if (session.Properties.ContainsProperty(ProbeAsyncCompletionSource.CompletionTypeProperty_Include))
                {
					return false;
                }
			}

			return true;
		}

		CommitResult IAsyncCompletionCommitManager.TryCommit(IAsyncCompletionSession session,
			ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
		{
			if (_startAbort)
			{
				if (session != null && !session.IsDismissed)
				{
					session.Dismiss();
				}

				return CommitResult.Handled;
			}
			else if (!ShouldAcceptCompletion(typedChar))
			{
				// Stick with what the user has typed so far, rather than the selection.
				if (session != null && !session.IsDismissed)
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

		private bool ShouldAcceptCompletion(char typedChar)
		{
			if (typedChar == '\n') return true;

			switch (typedChar)
			{
				case ' ':
					return ProbeToolsPackage.Instance.EditorOptions.AutoCompleteOnSpace;
				case '\t':
					return ProbeToolsPackage.Instance.EditorOptions.AutoCompleteOnTab;
				case '.':
					return ProbeToolsPackage.Instance.EditorOptions.AutoCompleteOnDot;
				default:
					return false;
			}
		}
	}
}
