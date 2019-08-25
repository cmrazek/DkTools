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
		private static readonly char[] _commitChars = new char[] { ' ', ';', '.', '(', ')', ',' };

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

			return true;	// All commit chars should trigger a commit
		}

		CommitResult IAsyncCompletionCommitManager.TryCommit(ITextView view, ITextBuffer buffer, CompletionItem item,
			ITrackingSpan applicableToSpan, char typedChar, CancellationToken token)
		{
			return CommitResult.Unhandled;	// Use default mechanism
		}

	}
}
