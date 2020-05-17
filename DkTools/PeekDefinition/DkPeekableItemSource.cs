using DkTools.Navigation;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.PeekDefinition
{
	public class DkPeekableItemSource : IPeekableItemSource
	{
		private IServiceProvider _serviceProvider;
		private IPeekResultFactory _peekResultFactory;
		private ITextBuffer _textBuffer;

		public DkPeekableItemSource(IServiceProvider serviceProvider, IPeekResultFactory peekResultFactory, ITextBuffer textBuffer)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_peekResultFactory = peekResultFactory ?? throw new ArgumentNullException(nameof(peekResultFactory));
			_textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
		}

		public void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var triggerPoint = session.GetTriggerPoint(_textBuffer).GetPoint(session.TextView.TextSnapshot);
			var def = GoToDefinitionHelper.GetDefinitionAtPoint(triggerPoint);
			if (def == null || def.FilePosition.IsEmpty)
			{
				ProbeToolsPackage.Instance.SetStatusText("No definition found at cursor.");
				return;
			}

			peekableItems.Add(new DkPeekableItem(_peekResultFactory, def));
		}

		public void Dispose()
		{
			_textBuffer = null;
		}
	}
}
