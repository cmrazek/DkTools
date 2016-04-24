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
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression to be evaluated", null) }),
						"Calculates the absolute value of an expression."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "avg",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) }),
						"Calculates the running average of an expression for a set of rows in a select statement."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "count",
						new ArgumentDescriptor[] { new ArgumentDescriptor("* , where expression, group TableName.ColumnName | all, in SelectName", null) }),
						"Keeps a running count of the number of rows selected in a select statement that satisfy a condition."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "createobject",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }),
						"Instantiates a COM object. The parameter iObj becomes a handle to the instance. The interface type of iObj determines what methods and properties the handle can call."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "diag",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expressions ...", null) }),
						"Outputs specified expressions to a diagnostic device."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "FormatString",
						new ArgumentDescriptor[] {
							new ArgumentDescriptor("FormatControlString", DataType.Char255, PassByMethod.Value),
							new ArgumentDescriptor("expression1, expression2, ...", null) }),
						"Generates a message from a format string containing placeholder substrings '%1', '%2', '%3' etc., in any order, along with other optional user-specified substrings."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "gofield",
						new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName", null) }),
						"Puts the focus on the requested field on the form."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "invokeerror",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }),
						"Determines whether an instantiated COM or .NET object has encountered an error. If an error, returns the error code of the object."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "invokeerrorstring",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }),
						"Returns the text of the last error invoked on the object."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "isinstance",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }),
						"Determines whether a variable points to a valid instance of the variable interface type. "),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "makestring", 
						new ArgumentDescriptor[] { new ArgumentDescriptor("expressions ...", null) }),
						"Creates a string by concatenating a list of expressions."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "max",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) }),
						"Determines the running maximum of an expression for a set of rows in a select statement."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "min",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) }),
						"Determines the running minimum of an expression for a set of rows in a select statement."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "oldvalue",
						new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName", null) }),
						"Returns the value of a column in the old row buffer."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "qcolsend",
						new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName ...", null) }),
						"Sends columns of the client's current row buffer to SAM or from SAM to the client. Only the current row buffer (not the old buffer) of the recepient is overwritten."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "releaseobject",
						new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }),
						"Releases the object identified by iObj, and automatically disconnects all events associated with iObj."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "SetMessage",
						new ArgumentDescriptor[] {
							new ArgumentDescriptor("MessageControlString", DataType.Char255, PassByMethod.Value),
							new ArgumentDescriptor("expressions ...", null) }),
						"Writes to the error message buffer. CAM displays the contents of that buffer when a trigger encounters an error. In code, you can read that buffer using the getmsg function.\r\n\r\n" +
						"Provides similar functionality to setmsg, but allows you to maintain one source code for all languages (with one set of resource files per language)."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "STRINGIZE",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression", null) }),
						"Converts macro parameters to strings."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "sum",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) }),
						"Calculates the running total of an expression for a set of rows in a select statement."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "UNREFERENCED_PARAMETER",
						new ArgumentDescriptor[] { new ArgumentDescriptor("parameter", null) }),
						"Prevents a compiler warning if a parameter passed to a function is not used."),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.StringVarying, null, "vstring",
						new ArgumentDescriptor[] { new ArgumentDescriptor("expression, ...", null) }),
						"Creates a string of varying length by concatenating a list of expressions. "),

					new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "widthof",
						new ArgumentDescriptor[] { new ArgumentDescriptor("name", null) }),
						"Returns the displayable width of a variable or column."),

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
