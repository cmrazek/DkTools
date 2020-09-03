using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;
using DkTools.QuickInfo;

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

		public override QuickInfoLayout GetQuickInfoElements(Token token = null) => new QuickInfoStack(
			new QuickInfoAttribute("Name", Text),
			new QuickInfoAttribute("Data Type", _dataType.ClassifiedString)
		);

		public void SetEnumDataType(DataType dataType)
		{
			_dataType = dataType;
		}
	}
}
