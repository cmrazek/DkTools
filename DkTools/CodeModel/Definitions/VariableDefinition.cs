using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class VariableDefinition : Definition
	{
		private DataType _dataType;
		private bool _arg;

		public VariableDefinition(Scope scope, string name, Token sourceToken, DataType dataType, bool arg)
			: base(scope, name, sourceToken, false)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
			_arg = arg;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public bool Argument
		{
			get { return _arg; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Variable; }
		}

		public override string CompletionDescription
		{
			get
			{
				if (_dataType != null) return string.Concat(_dataType.Name, " ", Name);
				return Name;
			}
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Normal; }
		}

		public override string QuickInfoText
		{
			get { return this.CompletionDescription; }
		}
	}
}
