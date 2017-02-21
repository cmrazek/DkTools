using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeAnalysis
{
	class RunScope
	{
		private Dictionary<string, Variable> _vars = new Dictionary<string, Variable>();
		private bool _returned;

		public RunScope()
		{
		}

		public RunScope Clone()
		{
			var scope = new RunScope()
			{
				_returned = _returned
			};

			foreach (var v in _vars)
			{
				scope._vars[v.Key] = v.Value.Clone();
			}

			return scope;
		}

		public void Merge(params RunScope[] scopes)
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
	}
}
