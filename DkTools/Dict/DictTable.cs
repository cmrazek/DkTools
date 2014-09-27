using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal class DictTable
	{
		private int _number;
		private string _name;
		private string _prompt;
		private string _comment;
		private string _description;
		private Dictionary<string, DictField> _fields;
		private List<DictRelInd> _relInds;
		private TableDefinition[] _definitions;
		private string _baseTable;

		public DictTable(DICTSRVRLib.IPTable repoTable)
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

			_baseTable = _name;	// TODO: Do we need to retrieve this from somewhere?

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

		public string BaseTable
		{
			get { return _baseTable; }
		}

		public bool IsField(string fieldName)
		{
			return _fields.ContainsKey(fieldName);
		}

		public IEnumerable<DictField> Fields
		{
			get { return _fields.Values; }
		}

		public DictField GetField(string fieldName)
		{
			DictField field;
			if (_fields.TryGetValue(fieldName, out field)) return field;
			return null;
		}

		private void LoadFields(DICTSRVRLib.IPTable repoTable)
		{
			_fields = new Dictionary<string, DictField>();
			for (int c = 1, cc = repoTable.ColumnCount; c <= cc; c++)
			{
				var field = new DictField(_name, repoTable.Columns[c]);
				_fields[field.Name] = field;
			}

			_relInds = new List<DictRelInd>();
			for (int i = 1, ii = repoTable.IndexCount; i <= ii; i++)
			{
				var relind = new DictRelInd(this, repoTable.Indexes[i]);
				_relInds.Add(relind);
			}
		}

		public void AddRelInd(DictRelInd relInd)
		{
			_relInds.Add(relInd);
		}

		public IEnumerable<DictRelInd> RelInds
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
	}
}
