using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace DkTools.StatementCompletion
{
	[Export(typeof(IAsyncCompletionSourceProvider))]
	[ContentType("DK")]
	[Name("DK Completion")]
	class ProbeAsyncCompletionSourceProvider : IAsyncCompletionSourceProvider
	{
		public IAsyncCompletionSource GetOrCreate(ITextView textView)
		{
			return new ProbeAsyncCompletionSource(textView);
		}
	}
}
