using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	/// <summary>
	/// Usage of a previously defined variable.
	/// </summary>
	internal class VariableToken : WordToken
	{
		private DataType _dataType;

		public VariableToken(GroupToken parent, Scope scope, Span span, string text, VariableDefinition def)
			: base(parent, scope, span, text)
		{
#if DEBUG
			if (def == null) throw new ArgumentNullException();
#endif
			SourceDefinition = def;
			_dataType = def.DataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public override DataType ValueDataType
		{
			get
			{
				return _dataType;
			}
		}
	}
}
