using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis
{
	class RunScope
	{
		private Dictionary<string, Variable> _vars = new Dictionary<string, Variable>();
		private FunctionDefinition _funcDef;
		private int _funcOffset;
		private DataType _dataTypeContext;

		private bool _returned;
		private bool _breaked;
		private bool _continued;
		private bool _canBreak;
		private bool _canContinue;

		public RunScope(FunctionDefinition funcDef, int funcOffset)
		{
			_funcDef = funcDef;
			_funcOffset = funcOffset;
		}

		public RunScope Clone(DataType dataTypeContext = null)
		{
			var scope = new RunScope(_funcDef, _funcOffset)
			{
				_returned = _returned,
				_dataTypeContext = _dataTypeContext,
				_canBreak = _canBreak,
				_canContinue = _canContinue
			};

			if (dataTypeContext != null) scope._dataTypeContext = dataTypeContext;

			foreach (var v in _vars)
			{
				scope._vars[v.Key] = v.Value.Clone();
			}

			return scope;
		}

		public void Merge(params RunScope[] scopes)
		{
			MergeChildScopes(scopes);
		}

		public void Merge(IEnumerable<RunScope> scopes)
		{
			MergeChildScopes(scopes);
		}

		private void MergeChildScopes(IEnumerable<RunScope> scopes)
		{
			if (!scopes.Any(x => x != null)) return;

			if (!_returned) _returned = scopes.All(x => x == null || x._returned);

			foreach (var v in _vars)
			{
				if (!v.Value.IsInitialized)
				{
					v.Value.IsInitialized = scopes.All(x => x == null || x.GetVariable(v.Key).IsInitialized);
				}
			}
		}

		public void AddVariable(Variable v)
		{
			_vars[v.Name] = v;
		}

		public Variable GetVariable(string name)
		{
			Variable v;
			if (_vars.TryGetValue(name, out v)) return v;
			return null;
		}

		public bool Returned
		{
			get { return _returned; }
			set { _returned = value; }
		}

		public bool Breaked
		{
			get { return _breaked; }
			set { _breaked = value; }
		}

		public bool Continued
		{
			get { return _continued; }
			set { _continued = value; }
		}

		public bool CanBreak
		{
			get { return _canBreak; }
			set { _canBreak = value; }
		}

		public bool CanContinue
		{
			get { return _canContinue; }
			set { _canContinue = value; }
		}

		public int FuncOffset
		{
			get { return _funcOffset; }
		}

		public DataType DataTypeContext
		{
			get { return _dataTypeContext; }
		}

		public FunctionDefinition FunctionDefinition
		{
			get { return _funcDef; }
		}
	}
}
