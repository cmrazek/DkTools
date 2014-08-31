using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class DataTypeDefinition : Definition
	{
		private DataType _dataType;

		public DataTypeDefinition(Scope scope, string name, Token sourceToken, DataType dataType)
			: base(scope, name, sourceToken, true)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public DataTypeDefinition(Scope scope, string name, DataType dataType)
			: base(scope, name, null, true)
		{
			_dataType = dataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.DataType; }
		}

		public override string CompletionDescription
		{
			get { return _dataType.InfoText; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.DataType; }
		}

		public override string QuickInfoText
		{
			get { return _dataType.InfoText; }
		}
	}
}
