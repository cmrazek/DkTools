using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class ExtractTableDefinition : Definition
	{
		private bool _permanent;
		private List<ExtractFieldDefinition> _fields = new List<ExtractFieldDefinition>();

		public ExtractTableDefinition(string name, string sourceFileName, int sourceStartPos, bool permanent)
			: base(name, sourceFileName, sourceStartPos, permanent ? string.Concat("permx:", name) : null)
		{
			_permanent = permanent;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Table; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_permanent) return string.Concat("extract permanent ", Name);
				else return string.Concat("extract ", Name);
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfDivs(WpfMainLine(Name),
					WpfInfoLine(_permanent ? "Permanent extract" : "Temporary extract"));
			}
		}

		public void AddField(ExtractFieldDefinition field)
		{
			_fields.Add(field);
			field.ExtractDefinition = this;
		}

		public ExtractFieldDefinition GetField(string fieldName)
		{
			foreach (var field in _fields)
			{
				if (field.Name == fieldName) return field;
			}
			return null;
		}

		public IEnumerable<ExtractFieldDefinition> Fields
		{
			get { return _fields; }
		}

		public bool Permanent
		{
			get { return _permanent; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}
	}
}
