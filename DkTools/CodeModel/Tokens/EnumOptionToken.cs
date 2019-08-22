using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	class EnumOptionToken : WordToken
	{
		private DataType _dataType;

		public EnumOptionToken(Scope scope, Span span, string text, DataType dataType)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.Constant;
			_dataType = dataType;
		}

		public override DataType ValueDataType
		{
			get
			{
				return _dataType;
			}
		}

		public override object GetQuickInfoElements(Token token = null)
		{
			return Definition.QuickInfoStack(
				Definition.QuickInfoAttributeString("Name", Text),
				Definition.QuickInfoAttributeElement("Data Type", _dataType.QuickInfoElements)
			);
		}

		public void SetEnumDataType(DataType dataType)
		{
			_dataType = dataType;
		}
	}
}
