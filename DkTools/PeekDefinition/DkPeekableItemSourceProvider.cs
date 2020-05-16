using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.PeekDefinition
{
	[Export(typeof(IPeekableItemSourceProvider))]
	[ContentType("DK")]
	[Name("DK Peekable Item Source Provider")]
	[SupportsPeekRelationship(DkPeekRelationship.RelationshipName)]
	public class DkPeekableItemSourceProvider : IPeekableItemSourceProvider
	{
		private IServiceProvider _serviceProvider;
		private IPeekResultFactory _peekResultFactory;

		[ImportingConstructor]
		public DkPeekableItemSourceProvider(
			[Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider,
			IPeekResultFactory peekResultFactory)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_peekResultFactory = peekResultFactory ?? throw new ArgumentNullException(nameof(peekResultFactory));
		}

		public IPeekableItemSource TryCreatePeekableItemSource(ITextBuffer textBuffer)
		{
			return textBuffer.Properties.GetOrCreateSingletonProperty(typeof(DkPeekableItemSource), () =>
				new DkPeekableItemSource(_serviceProvider, _peekResultFactory, textBuffer));
		}
	}
}
