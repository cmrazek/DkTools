using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class Variable
	{
		private string _name;
		private DataType _dataType;
		private Value _value;
		private bool _isArg;
		private bool _isInitialized;

		public Variable(string name, DataType dataType, Value value, bool isArg, bool isInitialized)
		{
			_name = name;
			_dataType = dataType;
			_value = value;
			_isArg = isArg;
			_isInitialized = isInitialized;
		}

		public Variable Clone()
		{
			return new Variable(_name, _dataType, _value, _isArg, _isInitialized);
		}

		public string Name
		{
			get { return _name; }
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public Value Value
		{
			get { return _value; }
			set { _value = value; }
		}

		public bool IsArgument
		{
			get { return _isArg; }
		}

		public bool IsInitialized
		{
			get { return _isInitialized; }
			set { _isInitialized = value; }
		}
	}
}
