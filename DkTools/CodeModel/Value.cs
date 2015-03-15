using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	struct Value
	{
		private ValType _type;
		private object _knownValue;

		public static readonly Value Unknown = new Value(ValType.Unknown, null);
		public static readonly Value Void = new Value(ValType.Void, null);
		public static readonly Value Table = new Value(ValType.Table, null);

		private Value(ValType type, object knownValue)
		{
			_type = type;
			_knownValue = knownValue;
		}

		public Value(DataType dataType)
		{
			_type = dataType != null ? dataType.ValueType : ValType.Unknown;
			_knownValue = null;
		}

		public static Value CreateEnum(string optionText)
		{
			return new Value(ValType.Enum, optionText);
		}

		public static Value CreateFromDataType(DataType dataType)
		{
			return new Value(dataType != null ? dataType.ValueType : ValType.Unknown, null);
		}

		public ValType Type
		{
			get { return _type; }
		}

		public static Value CreateStringLiteral(string text)
		{
			return new Value(ValType.String, text);
		}

		public static Value CreateNumber(string numberText)
		{
			decimal dec;
			if (decimal.TryParse(numberText, out dec))
			{
				return new Value(ValType.Numeric, dec);
			}
			else
			{
				return new Value(ValType.Numeric, null);
			}
		}
	}

	enum ValType
	{
		//			// _knownValue type:
		Unknown,	// null
		Void,		// null
		Numeric,	// decimal
		String,		// string
		Char,		// char
		Enum,		// string
		Date,		// DateTime
		Time,		// DateTime
		Table,		// null
		IndRel,		// null
		Interface,	// null
		Command,	// null
		Section,	// null
		Scroll,		// null
		Graphic		// null
	}
}
