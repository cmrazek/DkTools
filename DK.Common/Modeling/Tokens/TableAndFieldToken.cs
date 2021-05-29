using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DK.Modeling.Tokens
{
	public class TableAndFieldToken : GroupToken
	{
		private TableToken _tableToken;
		private TableFieldToken _fieldToken;

		internal TableAndFieldToken(Scope scope, TableToken tableToken, DotToken dotToken, TableFieldToken fieldToken)
			: base(scope)
		{
#if DEBUG
			if (tableToken == null || dotToken == null || fieldToken == null) throw new ArgumentNullException();
#endif
			AddToken(_tableToken = tableToken);
			AddToken(dotToken);
			AddToken(_fieldToken = fieldToken);
		}

		public override DataType ValueDataType
		{
			get
			{
				return _fieldToken.ValueDataType;
			}
		}

		public TableToken TableToken
		{
			get { return _tableToken; }
		}

		public TableFieldToken FieldToken
		{
			get { return _fieldToken; }
		}
	}
}
