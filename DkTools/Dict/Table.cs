using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal sealed class Table : IDictObj
	{
		private int _number;
		private string _name;
		private string _prompt;
		private string _comment;
		private string _description;
		private Dictionary<string, Field> _fields;
		private List<RelInd> _relInds;
		private TableDefinition[] _definitions;
		//private DICTSRVRLib.IPTable _repoTable;

		public Table(DICTSRVRLib.IPTable repoTable)
		{
			if (repoTable == null) throw new ArgumentNullException("repoTable");

			_number = repoTable.Number;
			_name = repoTable.Name;

			var desc = repoTable as DICTSRVRLib.IPDObjDesc;
			if (desc != null)
			{
				_prompt = desc.Prompt[0];
				_comment = desc.Comment[0];
			}

			var dev = repoTable as DICTSRVRLib.IPDictObj;
			if (dev != null)
			{
				var devInfo = dev.DevInfo;
				if (!string.IsNullOrEmpty(devInfo)) _description = devInfo;
			}

			_definitions = new TableDefinition[11];
			_definitions[0] = new TableDefinition(_name, this, true);
			for (int i = 0; i < 10; i++) _definitions[i + 1] = new TableDefinition(string.Concat(_name, i), this, false);

			LoadFields(repoTable);
		}

		public int Number
		{
			get { return _number; }
		}

		public string Name
		{
			get { return _name; }
		}

		public string Prompt
		{
			get { return _prompt; }
		}

		public string Comment
		{
			get { return _comment; }
		}

		public string Description
		{
			get { return _description; }
		}

		public bool IsField(string fieldName)
		{
			return _fields.ContainsKey(fieldName);
		}

		public IEnumerable<Field> Fields
		{
			get { return _fields.Values; }
		}

		public Field GetField(string fieldName)
		{
			Field field;
			if (_fields.TryGetValue(fieldName, out field)) return field;
			return null;
		}

		private void LoadFields(DICTSRVRLib.IPTable repoTable)
		{
			_fields = new Dictionary<string, Field>();
			for (int c = 1, cc = repoTable.ColumnCount; c <= cc; c++)
			{
				var field = new Field(FieldParentType.Table, _name, repoTable.Columns[c]);
				_fields[field.Name] = field;
			}

			_relInds = new List<RelInd>();
			for (int i = 1, ii = repoTable.IndexCount; i <= ii; i++)
			{
				var relind = new RelInd(this, repoTable.Indexes[i]);
				_relInds.Add(relind);
			}
		}

		public void AddRelInd(RelInd relInd)
		{
			_relInds.Add(relInd);
		}

		public IEnumerable<RelInd> RelInds
		{
			get { return _relInds; }
		}

		public IEnumerable<TableDefinition> Definitions
		{
			get { return _definitions; }
		}

		public TableDefinition BaseDefinition
		{
			get { return _definitions[0]; }
		}

		public IEnumerable<CodeModel.Definitions.TableFieldDefinition> FieldDefinitions
		{
			get
			{
				foreach (var field in _fields.Values) yield return field.Definition;
			}
		}

		public object CreateRepoObject(Dict dict)
		{
			return dict.GetTable(_name);
		}

		public static string GetExternalRefId(string tableName)
		{
			return string.Concat("table:", tableName);
		}
	}
}
