using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.StatementCompletion
{
	[Export(typeof(IAsyncCompletionCommitManagerProvider))]
	[Name("DK Commit Manager")]
	[ContentType(Constants.DkContentType)]
	public class ProbeCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
	{
		IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
		{
			return new ProbeCompletionCommitManager(textView);
		}
	}
}
