using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class ExtractFieldDefinition : Definition
	{
		private ExtractTableDefinition _ex;

		public ExtractFieldDefinition(string name, string sourceFileName, int sourceStartPos, ExtractTableDefinition tableDef)
			: base(name, sourceFileName, sourceStartPos, tableDef.Permanent ? GetExternalRefId(tableDef.Name, name) : null)
		{ }

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.TableField; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableField; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_ex != null) return string.Concat(_ex.Name, ".", Name);
				else return Name;
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfDivs(WpfMainLine(_ex != null ? string.Concat(_ex.Name, ".", Name) : Name),
					_ex != null ? WpfInfoLine(_ex.Permanent ? "Permanent extract" : "Temporary extract") : null);
			}
		}

		public ExtractTableDefinition ExtractDefinition
		{
			get { return _ex; }
			set { _ex = value; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string tableName, string fieldName)
		{
			return string.Concat("permx:", tableName, ".", fieldName);
		}
	}
}
