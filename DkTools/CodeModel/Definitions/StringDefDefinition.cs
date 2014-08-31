using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class StringDefDefinition : Definition
	{
		private Dict.DictStringDef _stringDef;

		public StringDefDefinition(Scope scope, Dict.DictStringDef stringDef)
			: base(scope, stringDef.Name, null, true)
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

		public override string CompletionDescription
		{
			get { return _stringDef.Value; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoText
		{
			get { return _stringDef.Value; }
		}
	}
}
