using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class Variable
	{
		private string _name;
		private DataType _dataType;
		private Value _value;
		private bool _isArg;

		public Variable(string name, DataType dataType, Value value, bool isArg)
		{
			_name = name;
			_dataType = dataType;
			_value = value;
			_isArg = isArg;
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
	}
}
