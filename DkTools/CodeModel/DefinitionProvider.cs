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
					// Functions
					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "abs",
						"Calculates the absolute value of an expression.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression to be evaluated", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "avg",
						"Calculates the running average of an expression for a set of rows in a select statement.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "count",
						"Keeps a running count of the number of rows selected in a select statement that satisfy a condition.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("* , where expression, group TableName.ColumnName | all, in SelectName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "createobject",
						"Instantiates a COM object. The parameter iObj becomes a handle to the instance. The interface type of iObj determines what methods and properties the handle can call.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "diag",
						"Outputs specified expressions to a diagnostic device.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expressions ...", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "FormatString",
						"Generates a message from a format string containing placeholder substrings '%1', '%2', '%3' etc., in any order, along with other optional user-specified substrings.",
						new ArgumentDescriptor[] {
							new ArgumentDescriptor("FormatControlString", DataType.Char255, PassByMethod.Value),
							new ArgumentDescriptor("expression1, expression2, ...", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "gofield",
						"Puts the focus on the requested field on the form.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "invokeerror",
						"Determines whether an instantiated COM or .NET object has encountered an error. If an error, returns the error code of the object.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "invokeerrorstring",
						"Returns the text of the last error invoked on the object.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "isinstance",
						"Determines whether a variable points to a valid instance of the variable interface type. ",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "makestring", 
						"Creates a string by concatenating a list of expressions.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expressions ...", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "max",
						"Determines the running maximum of an expression for a set of rows in a select statement.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "min",
						"Determines the running minimum of an expression for a set of rows in a select statement.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "oldvalue",
						"Returns the value of a column in the old row buffer.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "qcolsend",
						"Sends columns of the client's current row buffer to SAM or from SAM to the client. Only the current row buffer (not the old buffer) of the recepient is overwritten.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName ...", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "releaseobject",
						"Releases the object identified by iObj, and automatically disconnects all events associated with iObj.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "SetMessage",
						"Writes to the error message buffer. CAM displays the contents of that buffer when a trigger encounters an error. " +
						"In code, you can read that buffer using the getmsg function.\r\n\r\n" +
						"Provides similar functionality to setmsg, but allows you to maintain one source code for all languages " +
						"(with one set of resource files per language).",
						new ArgumentDescriptor[] {
							new ArgumentDescriptor("MessageControlString", DataType.Char255, PassByMethod.Value),
							new ArgumentDescriptor("expressions ...", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "STRINGIZE",
						"Converts macro parameters to strings.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "sum",
						"Calculates the running total of an expression for a set of rows in a select statement.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "UNREFERENCED_PARAMETER",
						"Prevents a compiler warning if a parameter passed to a function is not used.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("parameter", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.StringVarying, null, "vstring",
						"Creates a string of varying length by concatenating a list of expressions. ",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, ...", null) })),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "widthof",
						"Returns the displayable width of a variable or column.",
						new ArgumentDescriptor[] { new ArgumentDescriptor("name", null) })),

					// Data types
					new DataTypeDefinition("int", DataType.Int),

					// Interfaces
					new InterfaceTypeDefinition("oleobject", FilePosition.Empty),

					// Constants
					new ConstantDefinition("_WINDOWS", FilePosition.Empty, string.Empty),

					// Indexes / Relationships
					RelIndDefinition.Physical
				};
			}
			AddGlobalFromAnywhere(_builtInDefs);
			AddGlobalFromAnywhere(DkDict.Dict.AllDictDefinitions);
			if (string.IsNullOrEmpty(fileName) || !System.IO.Path.GetFileName(fileName).Equals("stdlib.i", StringComparison.OrdinalIgnoreCase))
			{
				AddGlobalFromAnywhere(FileStore.StdLibModel.PreprocessorModel.DefinitionProvider.GlobalsFromFile);
			}

			var ffApp = ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp;
			AddGlobalFromAnywhere(ffApp.GlobalDefinitions);
		}

		#region Global Definitions
		private DefinitionCollection _fileGlobalDefs = new DefinitionCollection();
		private DefinitionCollection _anywhereGlobalDefs = new DefinitionCollection();

		public IEnumerable<Definition> GlobalsFromAnywhere
		{
			get
			{
				foreach (var def in _fileGlobalDefs.All)
				{
					yield return def;
				}

				foreach (var def in _anywhereGlobalDefs.All)
				{
					yield return def;
				}
			}
		}

		public IEnumerable<Definition> GlobalsFromFile
		{
			get
			{
				return _fileGlobalDefs.All;
			}
		}

		public void AddGlobalFromAnywhere(Definition def)
		{
			_anywhereGlobalDefs.Add(def);
		}

		public void AddGlobalFromAnywhere(IEnumerable<Definition> defs)
		{
			_anywhereGlobalDefs.Add(defs);
		}

		public void AddGlobalFromFile(Definition def)
		{
			_fileGlobalDefs.Add(def);
		}

		public void AddGlobalFromFile(IEnumerable<Definition> defs)
		{
			_fileGlobalDefs.Add(defs);
		}

		public IEnumerable<Definition> GetGlobalFromAnywhere(string name)
		{
			foreach (var def in _fileGlobalDefs.Get(name))
			{
				yield return def;
			}

			foreach (var def in _anywhereGlobalDefs.Get(name))
			{
				yield return def;
			}
		}

		public IEnumerable<T> GetGlobalFromAnywhere<T>(string name) where T : Definition
		{
			foreach (var def in _fileGlobalDefs.Get<T>(name))
			{
				yield return def;
			}

			foreach (var def in _anywhereGlobalDefs.Get<T>(name))
			{
				yield return def;
			}
		}

		public IEnumerable<T> GetGlobalFromAnywhere<T>() where T : Definition
		{
			foreach (var def in _fileGlobalDefs.Get<T>())
			{
				yield return def;
			}

			foreach (var def in _anywhereGlobalDefs.Get<T>())
			{
				yield return def;
			}
		}

		public IEnumerable<T> GetGlobalFromFile<T>() where T : Definition
		{
			return _fileGlobalDefs.Get<T>();
		}

		public IEnumerable<T> GetGlobalFromFile<T>(string name) where T : Definition
		{
			return _fileGlobalDefs.Get<T>(name);
		}

		public IEnumerable<Definition> GetGlobalFromFile(string name)
		{
			return _fileGlobalDefs.Get(name);
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

		#region Global/Local Combined
		public IEnumerable<Definition> GetAny(int pos, string name)
		{
			foreach (var def in GetLocal(pos, name)) yield return def;
			foreach (var def in GetGlobalFromAnywhere(name)) yield return def;
		}

		public IEnumerable<T> GetAny<T>(int pos, string name) where T : Definition
		{
			foreach (var def in GetLocal<T>(pos, name)) yield return def;
			foreach (var def in GetGlobalFromAnywhere<T>(name)) yield return def;
		}
		#endregion

#if DEBUG
		public string DumpDefinitions()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Global Definitions (from file):");
			foreach (var def in _fileGlobalDefs.All)
			{
				sb.AppendLine(def.Dump());
			}

			sb.AppendLine();
			sb.AppendLine("Global Definitions (anywhere):");
			foreach (var def in _anywhereGlobalDefs.All)
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
