using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class Column
	{
		public string Accel { get; set; }
		public bool NoAudit { get; set; }
		public PersistMode Persist { get; set; }
		public string Image { get; set; }
		public bool Tool { get; set; }
		public string Prompt { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string Group { get; set; }
		public bool EndGroup { get; set; }
		public string CustomProgId { get; set; }
		public string CustomLicense { get; set; }

		private string _tableName;
		private string _name;
		private DataType _dataType;
		private List<Tag> _tags;
		private ColumnDefinition _def;
		private string _fullName;
		private FilePosition _filePos;

		public enum PersistMode
		{
			Form,
			FormOnly,
			Zoom,
			ZoomNoPersist
		}

		public Column(string tableName, string colName, DataType dataType, FilePosition filePos)
		{
			_tableName = tableName;
			_name = colName;
			_dataType = dataType;
			_fullName = string.Concat(tableName, ".", colName);
			_filePos = filePos;
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}

		public ColumnDefinition Definition
		{
			get
			{
				if (_def == null)
				{
					_def = new ColumnDefinition(TableName, Name, Prompt, Comment, DataType, Description, _filePos);
				}
				return _def;
			}
		}

		public static string GetTableFieldExternalRefId(string tableName, string fieldName)
		{
			return string.Concat("tableCol:", tableName, ".", fieldName);
		}

		public string Name
		{
			get { return _name; }
		}

		public string TableName
		{
			get { return _tableName; }
		}

		public DataType DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}

		public string FullName
		{
			get { return _fullName; }
		}
	}
}
