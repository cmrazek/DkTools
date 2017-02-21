using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	struct Value
	{
		private DataType _dataType;

		public static readonly Value Empty = new Value();

		public Value(DataType dataType)
		{
			_dataType = dataType;
		}

		public Value(Value val)
		{
			_dataType = val._dataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public bool IsVoid
		{
			get
			{
				if (_dataType == null) return true;
				if (_dataType.ValueType == ValType.Void) return true;
				return false;
			}
		}
	}
}
