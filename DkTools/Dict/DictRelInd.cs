using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal enum DictRelIndType
	{
		Index,
		Relationship
	}

	internal class DictRelInd
	{
		private DictRelIndType _type;
		private string _name;
		private CodeModel.Definitions.RelIndDefinition _def;
		private string _repoDesc;
		private string _prompt;
		private string _comment;
		private string _columns;
		//private int? _autoSequence;
		//private int? _autoSequenceBase;
		//private int? _autoSequenceIncrement;
		//private int? _clustered;
		//private int? _descending;
		//private int? _noPick;
		//private int? _number;
		//private int? _primary;
		//private int? _unique;
		private Dictionary<string, DictField> _fields = new Dictionary<string, DictField>();
		private object _repoObj;

		public DictRelInd(DictTable table, DICTSRVRLib.IPIndex repoIndex)
		{
			_type = DictRelIndType.Index;
			_name = repoIndex.Name;
			//_autoSequence = repoIndex.AutoSequence;
			//_autoSequenceBase = repoIndex.AutoSequenceBase;
			//_autoSequenceIncrement = repoIndex.AutoSequenceIncrement;
			//_clustered = repoIndex.Clustered;
			//_descending = repoIndex.Descending;
			//_noPick = repoIndex.NoPick;
			//_number = repoIndex.Number;
			//_primary = repoIndex.Primary;
			//_unique = repoIndex.Unique;
			_repoObj = repoIndex;

			var dev = repoIndex as DICTSRVRLib.IPDictObj;
			if (dev != null)
			{
				var devInfo = dev.DevInfo;
				if (!string.IsNullOrWhiteSpace(devInfo)) _repoDesc = devInfo;
			}

			var desc = repoIndex as DICTSRVRLib.IPDObjDesc;
			if (desc != null)
			{
				_prompt = desc.Prompt[0];
				_comment = desc.Comment[0];
			}

			// Info text will be the list of columns in the index.
			var info = new StringBuilder();
			if (repoIndex.Unique != 0) info.Append("unique ");
			if (repoIndex.Primary != 0) info.Append("primary ");
			if (repoIndex.NoPick != 0) info.Append("NOPICK ");
			info.Append("index ");
			info.Append(_name);
			info.Append(" on ");
			info.Append(table.Name);
			info.Append(" (");

			var first = true;
			var cols = new StringBuilder();
			for (int c = 1, cc = repoIndex.ColumnCount; c <= cc; c++)
			{
				var colName = repoIndex.Columns[c].Name;

				if (first) first = false;
				else
				{
					info.Append(", ");
					cols.Append(", ");
				}

				info.Append(colName);
				cols.Append(colName);
			}
			info.Append(')');
			_columns = cols.ToString();

			//if (!string.IsNullOrWhiteSpace(_repoDesc))
			//{
			//	info.AppendLine();
			//	info.AppendFormat("Description: {0}", _repoDesc);
			//}

			_def = new CodeModel.Definitions.RelIndDefinition(_name, table.Name, info.ToString(), _repoDesc);
		}

		public DictRelInd(DictTable table, DICTSRVRLib.IPRelationship repoRel)
		{
			_type = DictRelIndType.Relationship;
			_name = repoRel.Name;
			_prompt = repoRel.Prompt[0];
			_comment = repoRel.Comment[0];
			_repoObj = repoRel;

			var dev = repoRel as DICTSRVRLib.IPDictObj;
			if (dev != null)
			{
				var devInfo = dev.DevInfo;
				if (!string.IsNullOrWhiteSpace(devInfo)) _repoDesc = devInfo;
			}

			var repoTable = repoRel.Child;
			if (repoTable != null)
			{
				for (int c = 1, cc = repoTable.ColumnCount; c <= cc; c++)
				{
					var col = repoTable.Columns[c];
					if (col != null)
					{
						var field = new DictField(_name, col);
						_fields[field.Name] = field;
					}
				}
			}

			string relText = string.Empty;
			switch (repoRel.Type)
			{
				case DICTSRVRLib.PDS_Relationship.Relationship_ONEONE:
					relText = string.Format("relationship {2} one {0} to one {1}", repoRel.Parent.Name, repoRel.Child.Name, _name);
					break;
				case DICTSRVRLib.PDS_Relationship.Relationship_ONEMANY:
					relText = string.Format("relationship {2} one {0} to many {1}", repoRel.Parent.Name, repoRel.Child.Name, _name);
					break;
				case DICTSRVRLib.PDS_Relationship.Relationship_MANYMANY:
					relText = string.Format("relationship {2} many {0} to many {1}", repoRel.Parent.Name, repoRel.Child.Name, _name);
					break;
				case DICTSRVRLib.PDS_Relationship.Relationship_TIME:
					relText = string.Format("time relationship {2} {0} to {1}", repoRel.Parent.Name, repoRel.Child.Name, _name);
					break;
			}

			_def = new RelIndDefinition(_name, table.Name, relText, _repoDesc);
		}

		public DictRelIndType Type
		{
			get { return _type; }
		}

		public string Name
		{
			get { return _name; }
		}

		public CodeModel.Definitions.RelIndDefinition Definition
		{
			get { return _def; }
		}

		public string Prompt
		{
			get { return _prompt; }
		}

		public string Comment
		{
			get { return _comment; }
		}

		public string Columns
		{
			get { return _columns; }
		}

		public string Description
		{
			get { return _repoDesc; }
		}

		public DictField GetField(string fieldName)
		{
			DictField field;
			if (_fields.TryGetValue(fieldName, out field)) return field;
			return null;
		}

		public IEnumerable<TableFieldDefinition> FieldDefinitions
		{
			get
			{
				foreach (var field in _fields.Values) yield return field.Definition;
			}
		}

		public object RepoObj
		{
			get { return _repoObj; }
		}
	}
}
