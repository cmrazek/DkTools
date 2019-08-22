using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.QuickInfo
{
	[Export(typeof(IAsyncQuickInfoSourceProvider))]
	[Name("ToolTip QuickInfo Source")]
	[Order(Before = "Default Quick Info Presenter")]
	[ContentType(Constants.DkContentType)]
	internal class QuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
	{
		IAsyncQuickInfoSource IAsyncQuickInfoSourceProvider.TryCreateQuickInfoSource(ITextBuffer textBuffer)
		{
			return new QuickInfoSource(this, textBuffer);
		}
	}
}
