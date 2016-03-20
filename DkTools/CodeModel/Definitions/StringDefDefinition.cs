using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class StringDefDefinition : Definition
	{
		private DkDict.Stringdef _stringDef;

		public StringDefDefinition(DkDict.Stringdef stringDef)
			: base(stringDef.Name, null, -1, GetExternalRefId(stringDef.Name))
		{
			_stringDef = stringDef;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Constant; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr
		{
			get { return _stringDef.Text; }
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfMainLine(_stringDef.Text);
			}
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string name)
		{
			return string.Concat("stringdef:", name);
		}

		public override bool AllowsChild
		{
			get { return false; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			throw new NotSupportedException();
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}

		public override DataType DataType
		{
			get
			{
				return DataType.String;
			}
		}
	}
}
