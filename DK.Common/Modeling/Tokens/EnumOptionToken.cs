using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
	public class EnumOptionToken : WordToken
	{
		private DataType _dataType;

		internal EnumOptionToken(Scope scope, CodeSpan span, string text, DataType dataType)
			: base(scope, span, text)
		{
			ClassifierType = ProbeClassifierType.Constant;
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
			new QuickInfoAttribute("Data Type", _dataType.GetClassifiedString(shortVersion: false))
		);

		public void SetEnumDataType(DataType dataType)
		{
			_dataType = dataType;
		}
	}
}
