using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class DataTypeDefinition : Definition
	{
		private DataType _dataType;

		public DataTypeDefinition(string name, string fileName, int startPos, DataType dataType)
			: base(name, fileName, startPos)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public DataTypeDefinition(string name, DataType dataType)
			: base(name, null, -1)
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

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.DataType; }
		}

		public override string QuickInfoTextStr
		{
			get { return !string.IsNullOrEmpty(_dataType.InfoText) ? _dataType.InfoText : _dataType.Name; }
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return _dataType.QuickInfoWpf;
			}
		}
	}
}
