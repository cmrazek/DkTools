using DK.Code;
using DK.CodeAnalysis.Values;
using DK.Definitions;
using DK.Modeling;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis
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
		private CodeSpan _rawSpan;

		public Variable(Definition def, string name, DataType dataType, Value value, bool isArg, TriState isInitialized, bool isUsed, CodeSpan rawSpan)
		{
			_def = def;
			_name = name;
			_dataType = dataType;
			_value = value;
			_isArg = isArg;
			_isInitialized = isInitialized;
			_isUsed = isUsed;
			_rawSpan = rawSpan;
		}

		public Variable Clone()
		{
			return new Variable(_def, _name, _dataType, _value, _isArg, _isInitialized, _isUsed, _rawSpan);
		}

		public DataType DataType => _dataType;
		public Definition Definition => _def;
		public bool IsArgument => _isArg;
		public TriState IsInitialized { get => _isInitialized; set => _isInitialized = value; }
		public bool IsUsed { get => _isUsed; set => _isUsed = value; }
		public string Name => _name;
		public CodeSpan RawSpan => _rawSpan;
		public override string ToString() => _name;
		public Value Value => _value;

		public void AssignValue(Value value)
		{
			_value = value;
		}

		public void Merge(Variable other)
		{
			_value = other._value;
		}

		public void Merge(IEnumerable<Variable> otherVars)
		{
			var others = otherVars.Where(x => x != null).ToArray();
			if (others.Length == 0) return;

			_value = Value.Combine(_value.DataType, others.Select(x => x._value));
		}
	}
}
