using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.StatementCompletion
{
	[Export(typeof(IAsyncCompletionCommitManagerProvider))]
	[Name("DK Commit Manager")]
	[ContentType("DK")]
	public class ProbeCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
	{
		IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
		{
			return new ProbeCompletionCommitManager(textView);
		}
	}
}
