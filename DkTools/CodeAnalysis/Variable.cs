using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis
{
	class Variable
	{
		private Definition _def;
		private string _name;
		private DataType _dataType;
		private Value _value;
		private bool _isArg;
		private TriState _isInitialized;
		private bool _isUsed;

		public Variable(Definition def, string name, DataType dataType, Value value, bool isArg, TriState isInitialized, bool isUsed)
		{
			_def = def;
			_name = name;
			_dataType = dataType;
			_value = value;
			_isArg = isArg;
			_isInitialized = isInitialized;
			_isUsed = isUsed;
		}

		public Variable Clone()
		{
			return new Variable(_def, _name, _dataType, _value, _isArg, _isInitialized, _isUsed);
		}

		public Definition Definition
		{
			get { return _def; }
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
			get
			{
				return _value;
			}
			set
			{
				_value = value;
			}
		}

		public bool IsArgument
		{
			get { return _isArg; }
		}

		public TriState IsInitialized
		{
			get { return _isInitialized; }
			set { _isInitialized = value; }
		}

		public bool IsUsed
		{
			get { return _isUsed; }
			set { _isUsed = value; }
		}
	}
}
