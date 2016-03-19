using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.DkDict
{
	class RelInd
	{
		public bool Index { get; private set; }
		public string Name { get; private set; }
		public string TableName { get; set; }
		public string Description { get; set; }
		public bool Unique { get; set; }
		public bool Primary { get; set; }
		public bool NoPick { get; set; }
		public int Number { get; set; }
		public string Prompt { get; set; }
		public string Comment { get; set; }
		public bool Updates { get; set; }
		public string LinkDesc { get; set; }

		private List<Tag> _tags;
		private List<string> _sortCols;
		private List<Column> _cols;

		public RelInd(bool index, string name, string tableName)
		{
			Name = name;
			TableName = tableName;
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}

		public void AddSortColumn(string colName)
		{
			if (_sortCols == null) _sortCols = new List<string>();
			_sortCols.Add(colName);
		}

		public void AddColumn(Column col)
		{
			if (_cols == null) _cols = new List<Column>();
			_cols.Add(col);
		}
	}
}
