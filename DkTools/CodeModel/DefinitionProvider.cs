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
		private List<Definition> _globalDefs = new List<Definition>();

		/// <summary>
		/// Local definitions for the primary file, grouped by file offset in the primary file.
		/// </summary>
		private Dictionary<int, List<Definition>> _localDefs = new Dictionary<int, List<Definition>>();

		private bool _preprocessor;

		/// <summary>
		/// Get or sets a flag that the model can use to determine if it should be creating or consuming definitions.
		/// The preprocessor model creates definitions, and the visible model consumes them.
		/// </summary>
		public bool Preprocessor
		{
			get { return _preprocessor; }
			set { _preprocessor = value; }
		}

		public void AddGlobalDefinition(Definition def)
		{
			_globalDefs.Add(def);
		}

		public IEnumerable<Definition> GlobalDefinitions
		{
			get { return _globalDefs; }
		}

		public void AddLocalDefinition(int offset, Definition def)
		{
			List<Definition> list;
			if (!_localDefs.TryGetValue(offset, out list))
			{
				list = new List<Definition>();
				_localDefs[offset] = list;
			}

			list.Add(def);
		}

		public IEnumerable<Definition> GetLocalDefinitionsForOffset(int offset)
		{
			List<Definition> list;
			if (!_localDefs.TryGetValue(offset, out list)) return new Definition[0];
			
			return list;
		}

#if DEBUG
		public string DumpDefinitions()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Global Definitions:");
			foreach (var def in _globalDefs)
			{
				sb.AppendLine(def.Dump());
			}

			sb.AppendLine();
			sb.AppendLine("Local Definitions:");
			foreach (var offset in _localDefs.Keys)
			{
				foreach (var def in _localDefs[offset])
				{
					sb.AppendFormat("DictOffset [{0}]  ", offset);
					sb.AppendLine(def.Dump());
				}
			}

			return sb.ToString();
		}
#endif
	}
}
