using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.PeekDefinition
{
	public class DkPeekRelationship : IPeekRelationship
	{
		private DkPeekableItem _item;

		public const string RelationshipName = "DkPeekRelationship";

		public DkPeekRelationship(DkPeekableItem item)
		{
			_item = item ?? throw new ArgumentNullException(nameof(item));
		}

		public string Name => RelationshipName;

		public string DisplayName => "Definition";
	}
}
