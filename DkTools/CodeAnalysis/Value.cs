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
		private bool _initialized;

		public static readonly Value Empty = new Value();

		public Value(DataType dataType, bool initialized)
		{
			_dataType = dataType;
			_initialized = initialized;
		}

		public Value(Value val)
		{
			_dataType = val._dataType;
			_initialized = val._initialized;
		}

		public Value(Value a, Value b)
		{
			_dataType = a._dataType;
			_initialized = a._initialized && b._initialized;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public bool Initialized
		{
			get { return _initialized; }
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
