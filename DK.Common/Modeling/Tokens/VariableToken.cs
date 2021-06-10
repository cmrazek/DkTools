using DK.Code;
using DK.Definitions;
using System;

namespace DK.Modeling.Tokens
{
	/// <summary>
	/// Usage of a previously defined variable.
	/// </summary>
	public class VariableToken : WordToken
	{
		private DataType _dataType;

		internal VariableToken(Scope scope, CodeSpan span, string text, VariableDefinition def)
			: base(scope, span, text)
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
