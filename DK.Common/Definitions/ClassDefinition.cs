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
		private ServerContext _serverContext;
		private List<FunctionDefinition> _funcs = new List<FunctionDefinition>();

		public ClassDefinition(string name, string fileName, ServerContext serverContext)
			: base(name, new FilePosition(fileName, 0, true), GetExternalRefId(name))
		{
			_serverContext = serverContext;
		}

		public IEnumerable<FunctionDefinition> Functions => _funcs;
		public override string ToString() => $"Class: {Name}";
		public override bool CompletionVisible => true;
		public override ProbeCompletionType CompletionType => ProbeCompletionType.Class;
		public override ProbeClassifierType ClassifierType => ProbeClassifierType.TableName;
		public override string QuickInfoTextStr => string.Concat("Class: ", Name);
		public override QuickInfoLayout QuickInfo => new QuickInfoAttribute("Class", new ProbeClassifiedString(ProbeClassifierType.TableName, Name));
		public override string PickText => QuickInfoTextStr;
		public override bool RequiresChild => true;
		public override bool AllowsChild => true;
		public override bool ArgumentsRequired => false;
		public override bool CaseSensitive => false;
		public static string GetExternalRefId(string className) => string.Concat("class:", className);
		public override ServerContext ServerContext => _serverContext;

		public override IEnumerable<Definition> GetChildDefinitions(string name, DkAppSettings appSettings)
		{
			foreach (var func in _funcs)
			{
				if (func.Name == name) yield return func;
			}
		}

		public override IEnumerable<Definition> GetChildDefinitions(DkAppSettings appSettings) => _funcs.Cast<Definition>();

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
	}
}
