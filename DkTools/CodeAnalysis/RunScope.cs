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
		private CodeAnalyzer _ca;
		private Dictionary<string, Variable> _vars = new Dictionary<string, Variable>();
		private FunctionDefinition _funcDef;
		private int _funcOffset;
		private DataType _dataTypeContext;

		private TriState _returned;
		private TriState _breaked;
		private TriState _continued;
		private bool _canBreak;
		private bool _canContinue;
		private bool _suppressInitializedCheck;
		private bool _removeHeaderString;			// Does not inherit on clone

		public RunScope(CodeAnalyzer ca, FunctionDefinition funcDef, int funcOffset)
		{
			_ca = ca;
			_funcDef = funcDef;
			_funcOffset = funcOffset;
		}

		public RunScope Clone(DataType dataTypeContext = null, bool? canBreak = null, bool? canContinue = null)
		{
			var scope = new RunScope(_ca, _funcDef, _funcOffset)
			{
				_returned = _returned,
				_dataTypeContext = _dataTypeContext,
				_canBreak = _canBreak,
				_canContinue = _canContinue,
				_suppressInitializedCheck = _suppressInitializedCheck
			};

			if (dataTypeContext != null) scope._dataTypeContext = dataTypeContext;
			if (canBreak.HasValue) scope._canBreak = canBreak.Value;
			if (canContinue.HasValue) scope._canContinue = canContinue.Value;

			foreach (var v in _vars)
			{
				scope._vars[v.Key] = v.Value.Clone();
			}

			return scope;
		}

		public void Merge(RunScope scope, bool promoteBreak = true, bool promoteContinue = true)
		{
			if (scope.Returned > _returned) _returned = scope.Returned;
			if (promoteBreak && scope.Breaked > _breaked) _breaked = scope.Breaked;
			if (promoteContinue && scope.Continued > _continued) _continued = scope.Continued;

			
			foreach (var myVar in _vars.Values)
			{
				Variable otherVar;
				if (scope._vars.TryGetValue(myVar.Name, out otherVar))
				{
					if (scope.Returned != TriState.True)	// Don't initialize variables if the branch returned before this point
					{
						if (otherVar.IsInitialized > myVar.IsInitialized) myVar.IsInitialized = otherVar.IsInitialized;
					}

					if (otherVar.IsUsed) myVar.IsUsed = true;
				}
			}
		}

		public void Merge(IEnumerable<RunScope> scopes, bool promoteBreak = true, bool promoteContinue = true)
		{
			if (!scopes.Any(x => x != null)) return;

			if (_returned != TriState.True)
			{
				var returned = TriStateUtil.Combine(from s in scopes select s.Returned);
				if (returned > _returned) _returned = returned;
			}

			if (promoteBreak && _breaked != TriState.True)
			{
				var breaked = TriStateUtil.Combine(from s in scopes select s.Breaked);
				if (breaked > _breaked) _breaked = breaked;
			}

			if (promoteContinue && _continued != TriState.True)
			{
				var continued = TriStateUtil.Combine(from s in scopes select s.Continued);
				if (continued > _continued) _continued = continued;
			}

			foreach (var v in _vars)
			{
				if (v.Value.IsInitialized != TriState.True)
				{
					var numScopes = 0;
					var numInitScopes = 0;
					var numMaybeInitScopes = 0;
					foreach (var scope in scopes)
					{
						if (scope == null || scope.Returned == TriState.True) continue;
						numScopes++;
						switch (scope.GetVariable(v.Key).IsInitialized)
						{
							case TriState.True:
								numInitScopes++;
								break;
							case TriState.Indeterminate:
								numMaybeInitScopes++;
								break;
						}
					}

					if (numScopes > 0)
					{
						if (numInitScopes == numScopes) v.Value.IsInitialized = TriState.True;
						else if (numInitScopes > 0 || numMaybeInitScopes > 0) v.Value.IsInitialized = TriState.Indeterminate;
					}
				}

				if (!v.Value.IsUsed)
				{
					foreach (var scope in scopes)
					{
						if (scope == null) continue;
						if (scope.GetVariable(v.Key).IsUsed) v.Value.IsUsed = true;
					}
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

		public IEnumerable<Variable> Variables
		{
			get { return _vars.Values; }
		}

		public TriState Returned
		{
			get { return _returned; }
			set { _returned = value; }
		}

		public TriState Breaked
		{
			get { return _breaked; }
			set { _breaked = value; }
		}

		public TriState Continued
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

		public CodeAnalyzer CodeAnalyzer
		{
			get { return _ca; }
		}

		public bool SuppressInitializedCheck
		{
			get { return _suppressInitializedCheck; }
			set { _suppressInitializedCheck = value; }
		}

		public bool RemoveHeaderString
		{
			get { return _removeHeaderString; }
			set { _removeHeaderString = value; }
		}
	}
}
