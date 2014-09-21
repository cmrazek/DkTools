using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	internal class DefinitionProvider
	{
		private static Definition[] _builtInDefs;

		public DefinitionProvider(string fileName)
		{
			// Add definitions from external sources
			if (_builtInDefs == null)
			{
				_builtInDefs = new Definition[]
				{
					new FunctionDefinition(null, "diag", null, -1, DataType.Void, "void diag(expressions ...)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "gofield", null, -1, DataType.Void, "void gofield(TableName.ColumnName)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "makestring", null, -1, DataType.FromString("char(255)"), "char(255) makestring(expressions ...)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "oldvalue", null, -1, DataType.Void, "oldvalue(TableName.ColumnName)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "qcolsend", null, -1, DataType.Void, "void qcolsend(TableName.ColumnName ...)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "SetMessage", null, -1, DataType.Int, "int SetMessage(MessageControlString, expressions ...)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "STRINGIZE", null, -1, DataType.FromString("char(255)"), "STRINGIZE(x)", 0, 0, Span.Empty, FunctionPrivacy.Public, true),
					new FunctionDefinition(null, "UNREFERENCED_PARAMETER", null, -1, DataType.Void, "UNREFERENCED_PARAMETER(parameter)", 0, 0, Span.Empty, FunctionPrivacy.Public, true)
				};
			}
			AddGlobal(_builtInDefs);
			AddGlobal(ProbeEnvironment.DictDefinitions);
			if (string.IsNullOrEmpty(fileName) || !System.IO.Path.GetFileName(fileName).Equals("stdlib.i", StringComparison.OrdinalIgnoreCase)) AddGlobal(FileStore.StdLibModel.PreprocessorModel.DefinitionProvider.Globals);
			AddGlobal(ProbeToolsPackage.Instance.FunctionFileScanner.GlobalDefinitions);
		}

		#region Global Definitions
		private DefinitionCollection _globalDefs = new DefinitionCollection();

		public IEnumerable<Definition> Globals
		{
			get { return _globalDefs.All; }
		}

		public void AddGlobal(Definition def)
		{
			_globalDefs.Add(def);
		}

		public void AddGlobal(IEnumerable<Definition> defs)
		{
			_globalDefs.Add(defs);
		}

		public IEnumerable<Definition> GetGlobal(string name)
		{
			return _globalDefs.Get(name);
		}

		public IEnumerable<T> GetGlobal<T>(string name) where T : Definition
		{
			return _globalDefs.Get<T>(name);
		}

		public IEnumerable<T> GetGlobal<T>() where T : Definition
		{
			return _globalDefs.Get<T>();
		}
		#endregion

		#region Local Definitions
		private Dictionary<Span, DefinitionCollection> _localDefs = new Dictionary<Span, DefinitionCollection>();

		public void AddLocal(Span span, Definition def)
		{
			DefinitionCollection list;
			if (!_localDefs.TryGetValue(span, out list))
			{
				list = new DefinitionCollection();
				_localDefs[span] = list;
			}

			list.Add(def);
		}

		public void AddLocal(Span span, IEnumerable<Definition> defs)
		{
			DefinitionCollection list;
			if (!_localDefs.TryGetValue(span, out list))
			{
				list = new DefinitionCollection();
				_localDefs[span] = list;
			}

			list.Add(defs);
		}

		public IEnumerable<Definition> GetLocal(int pos, string name)
		{
			foreach (var node in _localDefs)
			{
				if (node.Key.Contains(pos))
				{
					foreach (var def in node.Value[name]) yield return def;
				}
			}
		}

		public IEnumerable<Definition> GetLocal(int pos)
		{
			foreach (var node in _localDefs)
			{
				if (node.Key.Contains(pos))
				{
					foreach (var def in node.Value.All) yield return def;
				}
			}
		}

		public IEnumerable<T> GetLocal<T>(int pos, string name) where T: Definition
		{
			foreach (var node in _localDefs)
			{
				if (node.Key.Contains(pos))
				{
					foreach (var def in node.Value.Get<T>(name))
					{
						yield return def;
					}
				}
			}
		}
		#endregion

#if DEBUG
		public string DumpDefinitions()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Global Definitions:");
			foreach (var def in _globalDefs.All)
			{
				sb.AppendLine(def.Dump());
			}

			sb.AppendLine();
			sb.AppendLine("Local Definitions:");
			foreach (var offset in _localDefs.Keys)
			{
				foreach (var def in _localDefs[offset].All)
				{
					sb.AppendFormat("Span [{0}]  ", offset);
					sb.AppendLine(def.Dump());
				}
			}

			return sb.ToString();
		}
#endif
	}
}
