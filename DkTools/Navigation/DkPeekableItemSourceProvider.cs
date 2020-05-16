using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Navigation
{
	[Export(typeof(IPeekableItemSourceProvider))]
	[ContentType("DK")]
	[Name("DK Peekable Item Source Provider")]
	public class DkPeekableItemSourceProvider : IPeekableItemSourceProvider
	{
		public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
		{
			return textBuffer.Properties.GetOrCreateSingletonProperty<DkPeekableItemSource>(() => new DkPeekableItemSource(textBuffer)) as IPeekableItemSource;
		}
	}
}
