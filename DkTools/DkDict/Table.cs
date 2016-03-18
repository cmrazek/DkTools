using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.DkDict
{
	class Table
	{
		private string _name;
		private int _number;
		private int _number2;
		private List<Tag> _tags = new List<Tag>();
		private List<Column> _columns = new List<Column>();

		public bool Updates { get; set; }
		public int DatabaseNumber { get; set; }
		public bool Display { get; set; }
		public bool Modal { get; set; }
		public bool NoPick { get; set; }
		public int SnapshotFrequency { get; set; }
		public string Prompt { get; set; }
		public string Comment { get; set; }
		public string Image { get; set; }
		public string Description { get; set; }
		public string MasterTable { get; set; }

		public Table(string name, int number, int number2)
		{
			_name = name;
			_number = number;
			_number2 = number2;
		}

		public string Name
		{
			get { return _name; }
		}

		public void AddTag(Tag tag)
		{
			_tags.Add(tag);
		}

		public void AddColumn(Column col)
		{
			_columns.Add(col);
		}

		public void InsertColumn(int pos, Column col)
		{
			_columns.Insert(pos, col);
		}

		public Column GetColumn(string colName)
		{
			foreach (var col in _columns)
			{
				if (col.Name == colName) return col;
			}
			return null;
		}

		public int GetColumnPosition(string colName)
		{
			var index = 0;
			foreach (var col in _columns)
			{
				if (col.Name == colName) return index;
				index++;
			}
			return -1;
		}

		public bool DropColumn(string colName)
		{
			foreach (var col in _columns)
			{
				if (col.Name == colName)
				{
					return _columns.Remove(col);
				}
			}
			return false;
		}

		public bool MoveColumn(int colPos, string colName)
		{
			var pos = GetColumnPosition(colName);
			if (pos < 0) return false;

			var col = _columns[colPos];

			_columns.RemoveAt(pos);
			if (colPos > pos) colPos--;
			_columns.Insert(colPos, col);
			return true;
		}
	}
}
