using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DkTools.Navigation
{
	public class DkPeekResultSource : IPeekResultSource
	{
		private DkPeekableItem _item;

		internal DkPeekResultSource(DkPeekableItem item)
		{
			_item = item ?? throw new ArgumentNullException(nameof(item));
		}

		public void FindResults(string relationshipName, IPeekResultCollection resultCollection,
			CancellationToken cancellationToken, IFindPeekResultsCallback callback)
		{
			resultCollection.Add(new DkPeekResult(_item.Definition));
		}
	}
}
