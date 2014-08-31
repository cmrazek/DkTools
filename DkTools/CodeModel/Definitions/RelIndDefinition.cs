using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class RelIndDefinition : Definition
	{
		private string _infoText;
		private string _baseTableName;

		public RelIndDefinition(string name, string baseTableName, string infoText)
			: base(name, null, true)
		{
			_infoText = infoText;
			_baseTableName = baseTableName;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override string CompletionDescription
		{
			get { return _infoText; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Table; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoText
		{
			get { return _infoText; }
		}

		public string BaseTableName
		{
			get { return _baseTableName; }
		}
	}
}
