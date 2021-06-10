using DK.AppEnvironment;
using DK.Code;
using DK.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.Definitions
{
	public class ClassDefinition : Definition
	{
		private List<FunctionDefinition> _funcs = new List<FunctionDefinition>();

		public ClassDefinition(string name, string fileName)
			: base(name, new FilePosition(fileName, 0, true), GetExternalRefId(name))
		{
		}

		public IEnumerable<FunctionDefinition> Functions => _funcs;
		public override string ToString() => $"Class: {Name}";

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Class; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get { return string.Concat("Class: ", Name); }
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoAttribute("Class", new ProbeClassifiedString(ProbeClassifierType.TableName, Name));

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool RequiresChild
		{
			get { return true; }
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override IEnumerable<Definition> GetChildDefinitions(string name, DkAppSettings appSettings)
		{
			foreach (var func in _funcs)
			{
				if (func.Name == name) yield return func;
			}
		}

		public override IEnumerable<Definition> GetChildDefinitions(DkAppSettings appSettings) => _funcs.Cast<Definition>();

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override bool CaseSensitive
		{
			get { return false; }
		}

		public void AddFunction(FunctionDefinition func)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));

			var ix = _funcs.FindIndex(x => x.Name == func.Name);
			if (ix >= 0) _funcs.RemoveAt(ix);

			_funcs.Add(func);
		}

		public void ClearFunctions()
		{
			_funcs.Clear();
		}

		public static string GetExternalRefId(string className)
		{
			return string.Concat("class:", className);
		}
	}
}
