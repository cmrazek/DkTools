using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Navigation
{
	public class DkPeekableItemSource : IPeekableItemSource
	{
		private ITextBuffer _textBuffer;

		public DkPeekableItemSource(ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
		}

		public void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var triggerPoint = session.GetTriggerPoint(_textBuffer).GetPoint(session.TextView.TextSnapshot);
			var def = GoToDefinitionHelper.GetDefinitionAtPoint(triggerPoint);
			if (def == null)
			{
				ProbeToolsPackage.Instance.SetStatusText("No definition found at cursor.");
				return;
			}
		}

		public void Dispose()
		{
			_textBuffer = null;
		}
	}
}
