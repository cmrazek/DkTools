using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class TableAndFieldToken : GroupToken
	{
		private TableToken _tableToken;
		private TableFieldToken _fieldToken;
		private DataType _dataType;	// Could be null

		public TableAndFieldToken(GroupToken parent, Scope scope, TableToken tableToken, DotToken dotToken, TableFieldToken fieldToken, Dict.DictField field)
			: base(parent, scope, new Token[] { tableToken, dotToken, fieldToken })
		{
#if DEBUG
			if (tableToken == null || dotToken == null || fieldToken == null || field == null) throw new ArgumentNullException();
#endif
			_tableToken = tableToken;
			_fieldToken = fieldToken;
			_dataType = field.DataType;
		}

		// TODO: remove
		//public static TableAndFieldToken TryParse(GroupToken parent, Scope scope)
		//{
		//	var startPos = scope.File.Position;

		//	var tableToken = TableToken.TryParse(parent, scope);
		//	if (tableToken == null) return null;

		//	var ret = TryParse(parent, scope, tableToken);
		//	if (ret != null) return ret;

		//	scope.File.Position = startPos;
		//	return null;
		//}

		//public static TableAndFieldToken TryParse(GroupToken parent, Scope scope, TableToken tableToken)
		//{
		//	var file = scope.File;
		//	if (!file.SkipWhiteSpaceAndComments(scope) || file.PeekChar() != '.') return null;

		//	var table = ProbeEnvironment.GetTable(tableToken.Text);
		//	if (table == null) return null;

		//	var dotToken = new DotToken(parent, scope, file.MoveNextSpan());

		//	if (!file.SkipWhiteSpaceAndComments(scope)) return null;

		//	var fieldName = file.PeekWord();
		//	var field = table.GetField(fieldName);
		//	if (field == null) return null;
		//	var fieldToken = new TableFieldToken(parent, scope, file.MoveNextSpan(fieldName.Length), fieldName, tableToken);

		//	return new TableAndFieldToken(parent, scope, tableToken, dotToken, fieldToken, field);
		//}

		public override DataType ValueDataType
		{
			get
			{
				return _dataType;
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
