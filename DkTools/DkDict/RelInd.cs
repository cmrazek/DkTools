using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.Classifier;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class RelInd : Table
	{
		public bool Unique { get; set; }
		public bool Primary { get; set; }

		/// <summary>
		/// For relationships only, a description of the relationship between the tables.
		/// For example: one cust to many dmd
		/// </summary>
		public ProbeClassifiedString LinkDesc { get; set; }

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

			var pcs = new ProbeClassifiedStringBuilder();
			if (Unique)
			{
				pcs.AddKeyword("unique");
				pcs.AddSpace();
			}
			if (Primary)
			{
				pcs.AddKeyword("primary");
				pcs.AddSpace();
			}
			if (NoPick)
			{
				pcs.AddKeyword("nopick");
				pcs.AddSpace();
			}

			switch (_type)
			{
				case RelIndType.Index:
					pcs.AddKeyword("index");
					break;
				case RelIndType.Relationship:
					pcs.AddKeyword("relationship");
					break;
				case RelIndType.TimeRelationship:
					pcs.AddKeyword("time");
					pcs.AddSpace();
					pcs.AddKeyword("relationship");
					break;
			}

			pcs.AddSpace();
			pcs.AddTableName(Name);

			if (_type == RelIndType.Index)
			{
				pcs.AddSpace();
				pcs.AddKeyword("on");
				pcs.AddSpace();
				pcs.AddTableName(TableName);
			}

			if (Number != 0)
			{
				pcs.AddSpace();
				pcs.AddNumber(Number.ToString());
			}

			if (LinkDesc != null && !LinkDesc.IsEmpty)
			{
				pcs.AddSpace();
				pcs.AddClassifiedString(LinkDesc);
			}

			if (_sortCols != null)
			{
				switch (_type)
				{
					case RelIndType.Index:
						{
							pcs.AddSpace();
							pcs.AddOperator("(");
							var first = true;
							foreach (var col in _sortCols)
							{
								if (first) first = false;
								else
								{
									pcs.AddDelimiter(",");
									pcs.AddSpace();
								}
								pcs.AddTableField(col);
							}
							pcs.AddOperator(")");
						}
						break;
					case RelIndType.Relationship:
					case RelIndType.TimeRelationship:
						pcs.AddSpace();
						pcs.AddKeyword("order");
						pcs.AddSpace();
						pcs.AddKeyword("by");
						if (Unique)
						{
							pcs.AddSpace();
							pcs.AddKeyword("unique");
						}
						foreach (var col in _sortCols)
						{
							pcs.AddSpace();
							pcs.AddTableField(col);
						}
						pcs.AddSpace();
						pcs.AddOperator("(");
						pcs.AddSpace();
						if (Columns.Any())
						{
							pcs.AddComment("... ");
						}
						pcs.AddOperator(")");
						break;
				}
			}

			_def = new RelIndDefinition(this, TableName, pcs.ToClassifiedString(), Description, FilePosition);
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
