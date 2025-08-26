using DK.AppEnvironment;
using DK.Code;
using DK.Modeling;
using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Definitions
{
    public class DefinitionProvider
    {
        private static Definition[] _builtInDefs;

        public DefinitionProvider(DkAppSettings appSettings, string fileName)
        {
            // Add definitions from external sources
            if (_builtInDefs == null)
            {
                _builtInDefs = new Definition[]
                {
                    // Functions
                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "abs",
                        "Calculates the absolute value of an expression.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression to be evaluated", null) }, ServerContext.Neutral,
                        flags: 0),
                        hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "avg",
                        "Calculates the running average of an expression for a set of rows in a select statement.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) },
                        ServerContext.Server, flags: 0),
                        hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "changevariant",
                        "Changes a variant's value to another subtype.",
                        new ArgumentDescriptor[]
                        {
                            new ArgumentDescriptor("vvalue", DataType.Variant),
                            new ArgumentDescriptor("vtype", DataType.Int)
                        },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "clearvariant",
                        "Releases the data held by the variant and sets the type to VT_EMPTY (0).",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("vvalue", DataType.Variant) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "count",
                        "Keeps a running count of the number of rows selected in a select statement that satisfy a condition.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("* , where expression, group TableName.ColumnName | all, in SelectName", null) },
                        ServerContext.Server, flags: 0),
                        hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "createobject",
                        "Instantiates a COM object. The parameter iObj becomes a handle to the instance. The interface type of iObj determines what methods and properties the handle can call.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) },
                        ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "decodevariant",
                        "Converts a byte array to a string using 'codepage'. Variant 'v' can be either a VT_UI1 or VT_I1, and must be either a VT_ARRAY or VT_BYREF. The result is placed in variant 'v'. See variant for a list of the types.",
                        new ArgumentDescriptor[]
                        {
                            new ArgumentDescriptor("vvalue", DataType.Variant),
                            new ArgumentDescriptor("codepage", DataType.Unsigned)
                        },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "diag",
                        "Outputs specified expressions to a diagnostic device.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expressions ...", null) },
                        ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "encodevariant",
                        "Converts the variant 'v' to a string, encodes the string using 'codepage', and puts the result into the variant 'v'. The result is always a VT_UI1 | VT_ARRAY in the variant variable. See variant for a list of the types.",
                        new ArgumentDescriptor[]
                        {
                            new ArgumentDescriptor("vvalue", DataType.Variant),
                            new ArgumentDescriptor("codepage", DataType.Unsigned)
                        },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "FormatString",
                        "Generates a message from a format string containing placeholder substrings '%1', '%2', '%3' etc., in any order, along with other optional user-specified substrings.",
                        new ArgumentDescriptor[] {
                            new ArgumentDescriptor("FormatControlString", DataType.Char255, PassByMethod.Value),
                            new ArgumentDescriptor("expression1, expression2, ...", null) },
                        ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "gofield",
                        "Puts the focus on the requested field on the form.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName", null) }, ServerContext.Client,
                        flags: 0),
                        hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "invokeerror",
                        "Determines whether an instantiated COM or .NET object has encountered an error. If an error, returns the error code of the object.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }, ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "invokeerrorstring",
                        "Returns the text of the last error invoked on the object.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }, ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "isinstance",
                        "Determines whether a variable points to a valid instance of the variable interface type. ",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) }, ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "makestring", 
                        "Creates a string by concatenating a list of expressions.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expressions ...", null) }, ServerContext.Neutral, flags: 0),
                        hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "max",
                        "Determines the running maximum of an expression for a set of rows in a select statement.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) },
                        ServerContext.Server, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "min",
                        "Determines the running minimum of an expression for a set of rows in a select statement.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) },
                        ServerContext.Server, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "oldvalue",
                        "Returns the value of a column in the old row buffer.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "qcolsend",
                        "Sends columns of the client's current row buffer to SAM or from SAM to the client. Only the current row buffer (not the old buffer) of the recepient is overwritten.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("TableName.ColumnName ...", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "releaseobject",
                        "Releases the object identified by iObj, and automatically disconnects all events associated with iObj.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("iObj", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "SetMessage",
                        "Writes to the error message buffer. CAM displays the contents of that buffer when a trigger encounters an error. " +
                        "In code, you can read that buffer using the getmsg function.\r\n\r\n" +
                        "Provides similar functionality to setmsg, but allows you to maintain one source code for all languages " +
                        "(with one set of resource files per language).",
                        new ArgumentDescriptor[] {
                            new ArgumentDescriptor("MessageControlString", DataType.Char255, PassByMethod.Value),
                            new ArgumentDescriptor("expressions ...", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "strcatvariant",
                        "Concatenates two variant strings.",
                        new ArgumentDescriptor[]
                        {
                            new ArgumentDescriptor("Dst", DataType.Variant),
                            new ArgumentDescriptor("Src", DataType.Variant)
                        },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "STRINGIZE",
                        "Converts macro parameters to strings.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "strlenvariant",
                        "Returns the number of characters in vStr.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("vStr", DataType.Variant) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "substrvariant",
                        "Extracts a variant string from another.",
                        new ArgumentDescriptor[]
                        {
                            new ArgumentDescriptor("Dst", DataType.Variant),
                            new ArgumentDescriptor("Src", DataType.Variant),
                            new ArgumentDescriptor("Pos", DataType.Int),
                            new ArgumentDescriptor("Cnt", DataType.Int)
                        },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "sum",
                        "Calculates the running total of an expression for a set of rows in a select statement.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression, where expression, group TableName.ColumnName | all, in SelectName", null) },
                        ServerContext.Server, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "UNREFERENCED_PARAMETER",
                        "Prevents a compiler warning if a parameter passed to a function is not used.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("parameter", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "varianttype",
                        "Returns the subtype of the variant.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("vvalue", DataType.Variant) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.StringVarying, null, "vstring",
                        "Creates a string of varying length by concatenating a list of expressions. ",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("expression, ...", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: true),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Int, null, "widthof",
                        "Returns the displayable width of a variable or column.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("name", null) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "$ConnectEvents",
                        "Notifies the application of any special event while performing AFSH methods on any form.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("SuffixName", DataType.Char255) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Char255, null, "$ErrorItems",
                        "Returns the string of the error message, identified by the item.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("item", DataType.Int) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "$InsertGermaneKey",
                        "Inserts a germane key into the AFS context.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("key", DataType.Char255) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "$ReleaseEvents",
                        "Deactivates an event connection created by $ConnectEvents.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("SuffixName", DataType.Char255) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    new FunctionDefinition(new FunctionSignature(true, FunctionPrivacy.Public, DataType.Void, null, "$RemoveGermaneKey",
                        "Removes a germane key from the AFS context.",
                        new ArgumentDescriptor[] { new ArgumentDescriptor("key", DataType.Char255) },
                        ServerContext.Neutral, flags: 0), hasVariableArgumentCount: false),

                    // Data types
                    new DataTypeDefinition("int", DataType.Int),

                    // Interfaces
                    new InterfaceTypeDefinition("oleobject", FilePosition.Empty),

                    // Constants
                    new ConstantDefinition("_WINDOWS", FilePosition.Empty, string.Empty),

                    // Indexes / Relationships
                    RelIndDefinition.Physical,

                    // Global properties
                    new VariableDefinition("$ErrorCount", FilePosition.Empty, DataType.Int, arg: false, null, VariableType.Global, argPassByMethod: null)
                };
            }
            AddGlobalFromAnywhere(_builtInDefs);
            AddGlobalFromAnywhere(appSettings.Dict.AllDictDefinitions);
            if (string.IsNullOrEmpty(fileName) || !PathUtil.GetFileName(fileName).Equals("stdlib.i", StringComparison.OrdinalIgnoreCase))
            {
                AddGlobalFromAnywhere(FileStore.GetStdLibModel(appSettings.Context, CodeScanMode.Global).PreprocessorModel.DefinitionProvider.GlobalsFromFile);
            }

            AddGlobalFromAnywhere(appSettings.Repo.GetGlobalDefinitions());
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

        /// <summary>
        /// Gets definitions that could be from the global scope, or global within the current file. Local definitions not included.
        /// </summary>
        /// <param name="name">Name of the definitions to find.</param>
        /// <returns>Found definitions.</returns>
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

        /// <summary>
        /// Gets definitions that could be from the global scope, or global within the current file. Local definitions not included.
        /// </summary>
        /// <typeparam name="T">The type of definition to find.</typeparam>
        /// <param name="name">Name of the definitions to find.</param>
        /// <returns>Found definitions.</returns>
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

        /// <summary>
        /// Gets definitions that could be from the global scope, or global within the current file. Local definitions not included.
        /// </summary>
        /// <typeparam name="T">Type of definition to find.</typeparam>
        /// <returns>Found definitions.</returns>
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
        private Dictionary<CodeSpan, DefinitionCollection> _localDefs = new Dictionary<CodeSpan, DefinitionCollection>();

        public void AddLocal(CodeSpan span, Definition def)
        {
            DefinitionCollection list;
            if (!_localDefs.TryGetValue(span, out list))
            {
                list = new DefinitionCollection();
                _localDefs[span] = list;
            }

            list.Add(def);
        }

        public void AddLocal(CodeSpan span, IEnumerable<Definition> defs)
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
