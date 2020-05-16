using DkTools.CodeModel.Definitions;
using EnvDTE;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DkTools.Navigation
{
	public class DkPeekableItem : IPeekableItem
	{
		private Definition _def;
		private DkPeekResultSource _resultSource;

		internal DkPeekableItem(Definition definition)
		{
			_def = definition ?? throw new ArgumentNullException(nameof(definition));
		}

		public string DisplayName => _def.Name;
		internal Definition Definition => _def;

		public IEnumerable<IPeekRelationship> Relationships
		{
			get
			{
				return new IPeekRelationship[] { new DkPeekRelationship(this) };
			}
		}

		public IPeekResultSource GetOrCreateResultSource(string relationshipName)
		{
			if (_resultSource == null) _resultSource = new DkPeekResultSource(this);
			return _resultSource;
		}
	}
}
