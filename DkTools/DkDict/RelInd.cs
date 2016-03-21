using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class RelInd : Table
	{
		public string Description { get; set; }
		public bool Unique { get; set; }
		public bool Primary { get; set; }
		// TODO: remove
		//public bool NoPick { get; set; }
		//public int Number { get; set; }
		//public string Prompt { get; set; }
		//public string Comment { get; set; }
		//public bool Updates { get; set; }
		public string LinkDesc { get; set; }

		RelIndType _type;
		private string _tableName;
		private List<string> _sortCols;
		private string _sortColString;
		private RelIndDefinition _def;

		public RelInd(RelIndType type, string name, int number, string tableName, FilePosition filePos)
			: base(name, number, 0, filePos)
		{
			_type = type;
			_tableName = tableName;
		}

		public void AddSortColumn(string colName)
		{
			if (_sortCols == null) _sortCols = new List<string>();
			_sortCols.Add(colName);
		}

		public string SortedColumnString
		{
			get
			{
				if (_sortColString == null)
				{
					if (_sortCols != null)
					{
						var sb = new StringBuilder();
						foreach (var col in _sortCols)
						{
							if (sb.Length > 0) sb.Append(", ");
							sb.Append(col);
						}
						_sortColString = sb.ToString();
					}
					else
					{
						_sortColString = string.Empty;
					}
				}

				return _sortColString;
			}
		}

		private void CreateDefinition()
		{
			/*
			create [unique] [primary | NOPICK] index IndexName on TableName [description "Line1" ... "LineN"] (ColumnName [,...] )
					 
			create relationship RelationshipName SchemaNumber [updates] [prompt "TablePrompt"] [comment "RelationshipComment"] [image "FileName"] [description "Line1" ... "LineN"]
			<one TableNameA to one TableNameB | one TableNameA to many TableNameB | many TableNameA to many TableNameB[2] > [order by [unique] ColumnName [...]] ( [ColumnDefinitions] )

			create time relationship RelationshipName SchemaNumber [prompt "ChildTablePrompt"] [comment "RelationshipComment"] [description "Line1" ... "LineN"] 
			MasterTableName to HistoryTableName
			[order by effective ColumnName [...]] ( )
			*/

			var sb = new StringBuilder();
			if (Unique) sb.Append("unique ");
			if (Primary) sb.Append("primary ");
			if (NoPick) sb.Append("nopick ");

			switch (_type)
			{
				case RelIndType.Index:
					sb.Append("index");
					break;
				case RelIndType.Relationship:
					sb.Append("relationship");
					break;
				case RelIndType.TimeRelationship:
					sb.Append("time relationship");
					break;
			}

			sb.Append(' ');
			sb.Append(Name);

			if (_type == RelIndType.Index)
			{
				sb.Append(" on ");
				sb.Append(TableName);
			}

			if (Number != 0)
			{
				sb.Append(' ');
				sb.Append(Number);
			}

			if (!string.IsNullOrEmpty(LinkDesc))
			{
				sb.Append(' ');
				sb.Append(LinkDesc);
			}

			if (_sortCols != null)
			{
				switch (_type)
				{
					case RelIndType.Index:
						{
							sb.Append(" (");
							var first = true;
							foreach (var col in _sortCols)
							{
								if (first) first = false;
								else sb.Append(", ");
								sb.Append(col);
							}
							sb.Append(')');
						}
						break;
					case RelIndType.Relationship:
					case RelIndType.TimeRelationship:
						sb.Append(" order by");
						if (Unique) sb.Append(" unique");
						foreach (var col in _sortCols)
						{
							sb.Append(' ');
							sb.Append(col);
						}
						sb.Append(" ( ");
						if (Columns.Any() != null) sb.Append("... ");
						sb.Append(')');
						break;
				}
			}

			_def = new RelIndDefinition(this, TableName, sb.ToString(), Description, FilePosition);
		}

		public override Definition Definition
		{
			get
			{
				if (_def == null) CreateDefinition();
				return _def;
			}
		}

		public override IEnumerable<Definition> Definitions
		{
			get
			{
				yield return Definition;
			}
		}

		public string TableName
		{
			get { return _tableName; }
			set
			{
				if (!string.IsNullOrEmpty(value) && _tableName != value)
				{
					var table = Dict.GetTable(value);
					if (table != null) table.AddRelInd(this);
				}
			}
		}

		public RelIndType Type
		{
			get { return _type; }
		}
	}

	enum RelIndType
	{
		Index,
		Relationship,
		TimeRelationship
	}
}
