using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling.Tokens;
using DK.Schema;
using DK.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DK.Modeling
{
    [Flags]
    public enum DataTypeFlags
    {
        None = 0x00,
        CompletionEnumOptions = 0x01,
        CompletionTables = 0x02,
        CompletionRelInds = 0x04,
        CompletionInterfaceMembers = 0x08,
        InterfaceArray = 0x10,
        InterfacePointer = 0x20,
        Signed = 0x40,

        CompletionOptionsMask = (CompletionEnumOptions | CompletionTables | CompletionRelInds | CompletionInterfaceMembers)
    }

    public class DataType
    {
        private string _name;
        private ProbeClassifiedString _source;
        private Definition[] _completionOptions;
        private ValType _valueType;
        private DataTypeFlags _flags;
        private Interface _intf;
        private int _scale; // For strings this is also the length
        private int _precision;

        public static readonly DataType Boolean_t = MakeEnum(new string[] { "FALSE", "TRUE" }, "Boolean_t");
        public static readonly DataType Char = MakeChar();
        public static readonly DataType Char8 = MakeString(8);
        public static readonly DataType Char30 = MakeString(30);
        public static readonly DataType Char255 = MakeString(255);
        public static readonly DataType Command = MakeCommand();
        public static readonly DataType Date = MakeDate();
        public static readonly DataType IndRel = MakeIndRel();
        public static readonly DataType Int = MakeInteger(4, signed: true);
        public static readonly DataType InterfaceType = MakeInterface(null);
        public static readonly DataType Numeric = MakeNumeric(
            scale: 0,
            precision: 0,
            signed: true,
            name: null,
            source: new ProbeClassifiedString(ProbeClassifierType.DataType, "numeric"));
        public static readonly DataType OleObject = new DataType(
            name: null,
            source: new ProbeClassifiedString(ProbeClassifierType.DataType, "oleobject"),
            valueType: ValType.Interface);
        public static readonly DataType String = MakeString(
            length: 0,
            name: null,
            source: new ProbeClassifiedString(ProbeClassifierType.DataType, "string"));
        public static readonly DataType StringVarying = MakeString(
            length: 0,
            name: null,
            source: new ProbeClassifiedString(ProbeClassifierType.DataType, "string varying"));
        public static readonly DataType Table = MakeTable();
        public static readonly DataType Unknown = MakeUnknown();
        public static readonly DataType Unsigned = MakeInteger(
            size: 4,
            signed: false,
            name: null,
            source: new ProbeClassifiedString(ProbeClassifierType.DataType, "unsigned"));
        public static readonly DataType Unsigned2 = MakeNumeric(
            scale: 2,
            precision: 0,
            signed: false,
            name: null,
            source: new ProbeClassifiedString(
                new ProbeClassifiedRun(ProbeClassifierType.DataType, "numeric"),
                new ProbeClassifiedRun(ProbeClassifierType.Operator, "("),
                new ProbeClassifiedRun(ProbeClassifierType.Number, "2"),
                new ProbeClassifiedRun(ProbeClassifierType.Operator, ")"),
                new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                new ProbeClassifiedRun(ProbeClassifierType.DataType, "unsigned")
            ));
        public static readonly DataType Unsigned9 = MakeNumeric(
            scale: 9,
            precision: 0,
            signed: false,
            name: null,
            source:  new ProbeClassifiedString(
                new ProbeClassifiedRun(ProbeClassifierType.DataType, "numeric"),
                new ProbeClassifiedRun(ProbeClassifierType.Operator, "("),
                new ProbeClassifiedRun(ProbeClassifierType.Number, "9"),
                new ProbeClassifiedRun(ProbeClassifierType.Operator, ")"),
                new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                new ProbeClassifiedRun(ProbeClassifierType.DataType, "unsigned")
            ));
        public static readonly DataType Variant = MakeVariant();
        public static readonly DataType Void = new DataType(
            name: null,
            source: new ProbeClassifiedString(ProbeClassifierType.DataType, "void"),
            valueType: ValType.Void);
        public static readonly DataType EnumNumeric = MakeInteger(2, signed: false, "enum_t",
            new ProbeClassifiedString(ProbeClassifierType.DataType, "enum_t"));
        public static readonly DataType CharNumeric = MakeInteger(2, signed: false, "char",
            new ProbeClassifiedString(ProbeClassifierType.DataType, "char"));

        internal delegate DataTypeDefinition GetDataTypeDelegate(string name);
        internal delegate VariableDefinition GetVariableDelegate(string name);
        internal delegate Definition[] GetTableFieldDelegate(string tableName, string fieldName);
        internal delegate void TokenCreateDelegate(Token token);
        internal delegate Interface GetInterfaceDelegate(string nameOrPlatformName);

        #region Construction
        /// <summary>
        /// Constructs a data type object.
        /// </summary>
        /// <param name="name">An optional name for the type (for example: balance_t)</param>
        /// <param name="source">The source for the type (for example: unsigned int)</param>
        /// <param name="valueType">The underlying type</param>
        private DataType(string name, ProbeClassifiedString source, ValType valueType)
        {
            _name = name;
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _valueType = valueType;
        }

        /// <summary>
        /// Clones a data type object.
        /// </summary>
        /// <param name="clone">The data type to be cloned.</param>
        public DataType(DataType clone)
        {
            _name = clone._name;
            _source = clone._source;
            _completionOptions = clone._completionOptions;
            _valueType = clone._valueType;
            _flags = clone._flags;
            _intf = clone._intf;
        }

        /// <summary>
        /// Creates a numeric data type (numeric(x,y) [unsigned])
        /// </summary>
        /// /// <param name="scale">Number of digits total.</param>
        /// <param name="precision">Number of digits below the decimal point.</param>
        /// <param name="signed">Signed or unsigned?</param>
        /// <param name="name">Optional name of the data type (e.g. balance_t)</param>
        /// <param name="source">Source code of the data type.</param>
        /// <returns>A new data type object.</returns>
        public static DataType MakeNumeric(int scale, int precision, bool signed, string name = null, ProbeClassifiedString source = null)
        {
            if (source == null)
            {
                if (scale == 0)
                {
                    source = new ProbeClassifiedString(ProbeClassifierType.DataType, "numeric");
                }
                else
                {
                    var sb = new ProbeClassifiedStringBuilder();
                    sb.AddDataType("numeric");
                    sb.AddSpace();
                    sb.AddOperator("(");
                    sb.AddNumber(scale.ToString());
                    sb.AddDelimiter(",");
                    sb.AddNumber(precision.ToString());
                    sb.AddOperator(")");
                    if (!signed)
                    {
                        sb.AddSpace();
                        sb.AddDataType("unsigned");
                    }
                    source = sb.ToClassifiedString();
                }
            }

            return new DataType(name, source, ValType.Numeric)
            {
                _scale = scale,
                _precision = precision,
                _flags = signed ? DataTypeFlags.Signed : DataTypeFlags.None
            };
        }

        public static DataType MakeInteger(int size, bool signed, string name = null, ProbeClassifiedString source = null)
        {
            switch (size)
            {
                case 1:
                    if (source == null)
                    {
                        if (signed) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "char");
                        else
                        {
                            source = new ProbeClassifiedString(
                                new ProbeClassifiedRun(ProbeClassifierType.DataType, "unsigned"),
                                ProbeClassifiedRun.Space,
                                new ProbeClassifiedRun(ProbeClassifierType.DataType, "char")
                            );
                        }
                    }
                    break;
                case 2:
                    if (source == null)
                    {
                        if (!signed) source = new ProbeClassifiedString
                        (
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "unsigned"),
                            ProbeClassifiedRun.Space,
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "short")
                        );
                        else source = new ProbeClassifiedString(ProbeClassifierType.DataType, "short");
                    }
                    break;
                default:
                    if (source == null)
                    {
                        if (!signed) source = new ProbeClassifiedString
                        (
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "unsigned"),
                            ProbeClassifiedRun.Space,
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "int")
                        );
                        else source = new ProbeClassifiedString(ProbeClassifierType.DataType, "int");
                    }
                    size = 4;
                    break;
            }

            return new DataType(name, source, ValType.Numeric)
            {
                _scale = -size,
                _flags = signed ? DataTypeFlags.Signed : DataTypeFlags.None
            };
        }

        public static DataType MakeEnum(IEnumerable<string> options, string name = null, ProbeClassifiedString source = null)
        {
            return MakeEnum(options?.Select(x => new EnumOptionDefinition(NormalizeEnumOption(x), dataType: null)), name, source);
        }

        public static DataType MakeEnum(IEnumerable<Definition> options, string name = null, ProbeClassifiedString source = null)
        {
            var optionsArray = options?.ToArray();

            if (source == null)
            {
                var sb = new ProbeClassifiedStringBuilder();
                if (name != null)
                {
                    sb.AddDataType(name);
                    sb.AddSpace();
                }
                sb.AddDataType("enum");
                sb.AddSpace();
                sb.AddOperator("{");

                if (optionsArray != null)
                {
                    var first = true;
                    foreach (var opt in optionsArray)
                    {
                        if (first) first = false;
                        else
                        {
                            sb.AddDelimiter(",");
                            sb.AddSpace();
                        }
                        if (opt.Name.StartsWith("\"")) sb.AddStringLiteral(opt.Name);
                        else sb.AddConstant(opt.Name);
                    }
                }

                sb.AddSpace();
                sb.AddOperator("}");
                source = sb.ToClassifiedString();
            }

            var dt = new DataType(name, source, ValType.Enum)
            {
                _flags = DataTypeFlags.CompletionEnumOptions,
                _completionOptions = optionsArray
            };

            foreach (var o in dt._completionOptions.Cast<EnumOptionDefinition>()) o.SetEnumDataType(dt);

            return dt;
        }

        public static DataType MakeChar(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null)
            {
                source = new ProbeClassifiedString(ProbeClassifierType.DataType, "char");
            }

            return new DataType(name, source, ValType.Char);
        }

        public static DataType MakeString(int length, string name = null, ProbeClassifiedString source = null)
        {
            if (source == null)
            {
                var sb = new ProbeClassifiedStringBuilder();
                sb.AddDataType("char");
                sb.AddOperator("(");
                sb.AddNumber(length.ToString());
                sb.AddOperator(")");
                source = sb.ToClassifiedString();
            }

            return new DataType(name, source, ValType.String)
            {
                _scale = length
            };
        }

        public static DataType MakeCommand(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "command");
            return new DataType(name, source, ValType.Command);
        }

        public static DataType MakeDate(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "date");
            return new DataType(name, source, ValType.Date);
        }

        public static DataType MakeTime(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "time");
            return new DataType(name, source, ValType.Time);
        }

        public static DataType MakeIndRel(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "indrel");
            return new DataType(name, source, ValType.IndRel)
            {
                _flags = DataTypeFlags.CompletionRelInds
            };
        }

        public static DataType MakeTable(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "table");
            return new DataType(name, source, ValType.Table) { _flags = DataTypeFlags.CompletionTables };
        }

        public static DataType MakeVariant(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.DataType, "variant");
            return new DataType(name, source, ValType.Variant);
        }

        public static DataType MakeUnknown(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString();
            return new DataType(name, source, ValType.Unknown);
        }

        public static DataType MakeSection(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.Normal, "section");
            return new DataType(name, source, ValType.Section);
        }

        public static DataType MakeScroll(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.Normal, "scroll");
            return new DataType(name, source, ValType.Scroll);
        }

        public static DataType MakeGraphic(string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.Normal, "graphic");
            return new DataType(name, source, ValType.Graphic);
        }

        public static DataType MakeInterface(Interface intf, string name = null, ProbeClassifiedString source = null)
        {
            if (source == null) source = new ProbeClassifiedString(ProbeClassifierType.Normal, "interface");
            return new DataType(name, source, ValType.Interface)
            {
                _intf = intf,
                _flags = DataTypeFlags.CompletionInterfaceMembers
            };
        }
        #endregion

        public bool IsReportable => _valueType != ValType.Void && _valueType != ValType.Unknown;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ProbeClassifiedString Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public string DisplayName => _name ?? _source.ToString();

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_name)) return string.Concat(_name, " (", _source, ")");
            else return _source.ToString();
        }

        public Interface Interface
        {
            get { return _intf; }
            set { _intf = value; }
        }

        public bool HasCompletionOptions => (_flags & DataTypeFlags.CompletionOptionsMask) != DataTypeFlags.None;

        public IEnumerable<Definition> GetCompletionOptions(DkAppSettings appSettings)
        {
            if (_flags.HasFlag(DataTypeFlags.CompletionEnumOptions))
            {
                if (_completionOptions != null)
                {
                    foreach (var opt in _completionOptions) yield return opt;
                }
            }
            else if (_flags.HasFlag(DataTypeFlags.CompletionTables))
            {
                foreach (var table in appSettings.Dict.Tables)
                {
                    foreach (var def in table.Definitions) yield return def;
                }
            }
            else if (_flags.HasFlag(DataTypeFlags.CompletionRelInds))
            {
                yield return RelIndDefinition.Physical;
                foreach (var r in appSettings.Dict.RelInds) yield return r.Definition;
            }
            else if (_intf != null)
            {
                foreach (var def in _intf.Definition.GetChildDefinitions(appSettings)) yield return def;
            }
        }

        public static string[] DataTypeStartingKeywords = new string[] { "char", "date", "enum", "int", "indrel", "like", "numeric", "string", "table", "time", "unsigned", "void" };

        [Flags]
        public enum ParseFlag
        {
            Strict
        }

        internal class ParseArgs
        {
            public ParseArgs(CodeParser code, DkAppSettings appSettings)
            {
                Code = code ?? throw new ArgumentNullException(nameof(code));
                AppSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            }

            /// <summary>
            /// The token parser to read from.
            /// </summary>
            public CodeParser Code { get; set; }

            /// <summary>
            /// The current DK profile.
            /// </summary>
            public DkAppSettings AppSettings { get; set; }

            /// <summary>
            /// (optional) Flags to control the parsing behaviour.
            /// </summary>
            public ParseFlag Flags { get; set; }

            /// <summary>
            /// (optional) A callback function used to look up existing data types.
            /// </summary>
            public GetDataTypeDelegate DataTypeCallback { get; set; }

            /// <summary>
            /// (optional) A callback function used to look up existing variables.
            /// </summary>
            public GetVariableDelegate VariableCallback { get; set; }

            /// <summary>
            /// (optional) A callback function used to look up table/fields (like extracts).
            /// The dict will automatically be searched first before calling this function.
            /// </summary>
            public GetTableFieldDelegate TableFieldCallback { get; set; }

            /// <summary>
            /// (optional) A name to be given to the data type. If null or blank, the actual text will be used as the name.
            /// </summary>
            public string TypeName { get; set; }

            /// <summary>
            /// (optional) A callback which triggers creation of tokens for use in a code model.
            /// </summary>
            public TokenCreateDelegate TokenCreateCallback { get; set; }

            /// <summary>
            /// (optional) A callback which can be used to look up an interface using alternate methods than just the dict.
            /// </summary>
            public GetInterfaceDelegate InterfaceCallback { get; set; }

            /// <summary>
            /// (optional) The scope to use when creating tokens.
            /// This is required if CreateTokens is true.
            /// </summary>
            public Scope Scope { get; set; }

            /// <summary>
            /// (optional) Set to true if this is for a visible model which is not preprocessed.
            /// </summary>
            public bool VisibleModel { get; set; }

            /// <summary>
            /// (out) The first token parsed by the data type
            /// </summary>
            public Token FirstToken { get; set; }

            /// <summary>
            /// (in) 
            /// </summary>
            public bool AllowTags { get; set; }

            public void OnKeyword(CodeSpan span, string text)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new KeywordToken(Scope, span, text);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnDataTypeKeyword(CodeSpan span, string text, Definition def)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new DataTypeKeywordToken(Scope, span, text, def);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnIdentifier(CodeSpan span, string text, Definition def)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new IdentifierToken(Scope, span, text, def);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnOperator(CodeSpan span, string text)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new OperatorToken(Scope, span, text);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnStringLiteral(CodeSpan span, string text)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new StringLiteralToken(Scope, span, text);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnNumber(CodeSpan span, string text)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new NumberToken(Scope, span, text);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnUnknown(CodeSpan span, string text)
            {
                if (TokenCreateCallback != null)
                {
                    var tok = new UnknownToken(Scope, span, text);
                    if (FirstToken == null) FirstToken = tok;
                    TokenCreateCallback(tok);
                }
            }

            public void OnToken(Token token)
            {
                if (FirstToken == null) FirstToken = token;
                if (TokenCreateCallback != null) TokenCreateCallback(token);
            }
        }

        /// <summary>
        /// Parses a data type from a string.
        /// </summary>
        /// <param name="a">Contains arguments that control the parsing.</param>
        /// <returns>A data type object, if a data type could be parsed; otherwise null.</returns>
        internal static DataType TryParse(ParseArgs a)
        {
            var code = a.Code;
            var startPos = code.Position;

            // Check if there is a data-type name embedded before the source.
            string name = null;
            if (code.ReadExact('@'))
            {
                var tildeSpan = code.Span;
                if (code.ReadWord()) name = code.Text;
                else if (code.ReadStringLiteral()) name = CodeParser.StringLiteralToString(code.Text);
                else
                {
                    code.Position = startPos;
                    return null;
                }
            }
            if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(a.TypeName)) a.TypeName = name;

            if (!code.ReadWord()) return null;
            var startWord = code.Text;

            DataType dataType = null;

            switch (code.Text)
            {
                case "void":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.Void;
                    break;

                case "numeric":
                case "decimal":
                case "NUMERIC":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessNumeric(a, code.Text);
                    break;

                case "unsigned":
                case "signed":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessSignedUnsigned(a, code.Text);
                    break;

                case "int":
                case "short":
                case "long":
                case "ulong":
                case "number":
                case "unumber":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessInt(a, code.Text);
                    break;

                case "char":
                case "character":
                case "varchar":
                case "CHAR":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessChar(a, code.Text);
                    break;

                case "string":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessString(a, code.Text);
                    break;

                case "date":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessDate(a, code.Text);
                    break;

                case "time":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessTime(a, code.Text);
                    break;

                case "enum":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessEnum(a);
                    break;

                case "like":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessLike(a);
                    break;

                case "table":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.Table;
                    break;

                case "indrel":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.IndRel;
                    break;

                case "command":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.Command;
                    break;

                case "Section":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessSection(a);
                    break;

                case "scroll":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessScroll(a);
                    break;

                case "graphic":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessGraphic(a);
                    break;

                case "interface":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = ProcessInterface(a);
                    break;

                case "variant":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.Variant;
                    break;

                case "oleobject":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.OleObject;
                    break;

                case "Boolean_t":
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    dataType = DataType.Boolean_t;
                    break;

                default:
                    if (a.DataTypeCallback != null)
                    {
                        var word = code.Text;
                        var wordSpan = code.Span;

                        var def = a.DataTypeCallback(word);
                        if (def != null)
                        {
                            a.OnDataTypeKeyword(wordSpan, word, def);
                            dataType = def.DataType;
                        }
                    }
                    break;
            }

            if (dataType == null)
            {
                code.Position = code.TokenStartPostion;
            }

            // Give the first keyword in the data type an inferred data type, to get casting to work
            var dtToken = a.FirstToken as DataTypeKeywordToken;
            if (dtToken != null)
            {
                dtToken.InferredDataType = dataType;
            }

            return dataType;
        }

        private static readonly string[] _bracketsEndTokens = new string[] { ")" };

        private static DataType ProcessNumeric(ParseArgs a, string tokenText)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType(tokenText);

            var code = a.Code;
            int scale = 0;
            int precision = 0;
            bool signed = true;

            if (code.ReadExact('('))
            {
                pcs.AddOperator("(");

                BracketsToken brackets = null;
                if (a.TokenCreateCallback != null)
                {
                    brackets = new BracketsToken(a.Scope);
                    brackets.AddOpen(code.Span);
                }

                if (code.ReadNumber())
                {
                    int.TryParse(code.Text, out scale);
                    pcs.AddNumber(code.Text);
                    if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));

                    if (code.ReadExact(','))
                    {
                        pcs.AddDelimiter(",");
                        if (brackets != null) brackets.AddToken(new DelimiterToken(a.Scope, code.Span));

                        if (code.ReadNumber())
                        {
                            int.TryParse(code.Text, out precision);
                            pcs.AddSpace();
                            pcs.AddNumber(code.Text);
                            if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));
                        }
                    }
                }
                else if (a.VisibleModel)
                {
                    var exp = ExpressionToken.TryParse(a.Scope, _bracketsEndTokens);
                    if (exp != null) a.OnToken(exp);
                }
                if (code.ReadExact(')'))
                {
                    pcs.AddOperator(")");
                    if (brackets != null)
                    {
                        brackets.AddClose(code.Span);
                        a.OnToken(brackets);
                    }
                }
                else return MakeNumeric(scale, precision, signed: true, a.TypeName, pcs.ToClassifiedString());
            }

            var done = false;
            var gotMask = false;
            while (!done && !code.EndOfFile)
            {
                if (code.ReadExactWholeWord("unsigned"))
                {
                    pcs.AddSpace();
                    pcs.AddDataType(code.Text);
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    signed = false;
                }
                else if (ReadAttribute(a, code, pcs, "currency", "local_currency")) { }
                else if (!gotMask && code.ReadStringLiteral())
                {
                    pcs.AddSpace();
                    pcs.AddDataType(code.Text);
                    a.OnStringLiteral(code.Span, code.Text);
                    gotMask = true;
                }
                else break;
            }

            return MakeNumeric(scale, precision, signed, a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessSignedUnsigned(ParseArgs a, string tokenText)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType(tokenText);

            var code = a.Code;
            var signed = tokenText == "signed";
            int scale = 0;
            int precision = 0;
            bool? isInteger = null;

            if (code.ReadNumber())
            {
                // width
                pcs.AddSpace();
                pcs.AddNumber(code.Text);
                a.OnNumber(code.Span, code.Text);
                isInteger = false;
            }
            else if (code.ReadExact('('))
            {
                pcs.AddOperator("(");

                BracketsToken brackets = null;
                if (a.TokenCreateCallback != null)
                {
                    brackets = new BracketsToken(a.Scope);
                    brackets.AddOpen(code.Span);
                }

                if (code.ReadNumber())
                {
                    int.TryParse(code.Text, out scale);
                    pcs.AddNumber(code.Text);
                    if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));

                    if (code.ReadExact(','))
                    {
                        pcs.AddDelimiter(",");
                        if (brackets != null) brackets.AddToken(new DelimiterToken(a.Scope, code.Span));

                        if (code.ReadNumber())
                        {
                            int.TryParse(code.Text, out precision);
                            pcs.AddNumber(code.Text);
                            if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));
                        }
                    }
                }
                else if (a.VisibleModel)
                {
                    var exp = ExpressionToken.TryParse(a.Scope, _bracketsEndTokens);
                    if (exp != null) a.OnToken(exp);
                }

                if (code.ReadExact(')'))
                {
                    pcs.AddOperator(")");
                    if (brackets != null)
                    {
                        brackets.AddClose(code.Span);
                        a.OnToken(brackets);
                    }
                }
                else return MakeNumeric(scale, precision, signed, a.TypeName, pcs.ToClassifiedString());

                isInteger = false;
            }

            if (isInteger == null)
            {
                if (code.ReadExactWholeWord("int"))
                {
                    pcs.AddSpace();
                    pcs.AddDataType("int");
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    scale = 4;
                    isInteger = true;
                }
                else if (code.ReadExactWholeWord("short"))
                {
                    pcs.AddSpace();
                    pcs.AddDataType("short");
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    scale = 2;
                    isInteger = true;
                }
                else if (code.ReadExactWholeWord("long"))
                {
                    pcs.AddSpace();
                    pcs.AddDataType("long");
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    scale = 4;
                    isInteger = true;
                }
                else if (code.ReadExactWholeWord("char"))
                {
                    pcs.AddSpace();
                    pcs.AddDataType("char");
                    a.OnDataTypeKeyword(code.Span, code.Text, null);
                    scale = 1;
                    isInteger = true;
                }
                else
                {
                    // Just the word 'unsigned' and that's it.
                    scale = 4;
                    isInteger = true;
                }
            }

            var gotMask = false;
            while (!code.EndOfFile)
            {
                if (ReadAttribute(a, code, pcs)) { }
                else if (!gotMask && code.ReadStringLiteral())
                {
                    gotMask = true;
                    pcs.AddSpace();
                    pcs.AddStringLiteral(code.Text);
                    a.OnStringLiteral(code.Span, code.Text);
                }
                else break;
            }

            if (isInteger == true) return MakeInteger(scale, signed, a.TypeName, pcs.ToClassifiedString());
            return MakeNumeric(scale, precision, signed, a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessInt(ParseArgs a, string tokenText)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType(tokenText);

            var code = a.Code;
            var signed = true;
            int scale = 4;

            if (code.ReadExact("unsigned"))
            {
                signed = false;
                pcs.AddSpace();
                pcs.AddDataType("unsigned");
                a.OnDataTypeKeyword(code.Span, code.Text, null);
            }
            else if (code.ReadExact("signed"))
            {
                signed = true;
                pcs.AddSpace();
                pcs.AddDataType("signed");
                a.OnDataTypeKeyword(code.Span, code.Text, null);
            }

            if (code.ReadNumber())
            {
                // width
                int.TryParse(code.Text, out scale);
                pcs.AddSpace();
                pcs.AddNumber(code.Text);
                a.OnNumber(code.Span, code.Text);
            }

            while (!code.EndOfFile)
            {
                if (!ReadAttribute(a, code, pcs)) break;
            }

            return MakeInteger(scale, signed, a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessChar(ParseArgs a, string tokenText)
        {
            var code = a.Code;
            if (!code.ReadExact('(')) return DataType.Char;

            BracketsToken brackets = null;
            if (a.TokenCreateCallback != null)
            {
                brackets = new BracketsToken(a.Scope);
                brackets.AddOpen(code.Span);
            }

            if (code.ReadNumber())
            {
                int.TryParse(code.Text, out var length);

                var pcs = new ProbeClassifiedStringBuilder();
                pcs.AddDataType(tokenText);
                pcs.AddOperator("(");
                pcs.AddNumber(code.Text);
                if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));
                if (code.ReadExact(')'))
                {
                    pcs.AddOperator(")");
                    if (brackets != null)
                    {
                        brackets.AddClose(code.Span);
                        a.OnToken(brackets);
                    }
                }
                else return MakeString(length, a.TypeName, pcs.ToClassifiedString());

                var done = false;
                var gotMask = false;
                while (!done && !code.EndOfFile)
                {
                    if (ReadAttribute(a, code, pcs)) { }
                    else if (!gotMask && code.ReadStringLiteral())
                    {
                        gotMask = true;
                        pcs.AddSpace();
                        pcs.AddStringLiteral(code.Text);
                        a.OnStringLiteral(code.Span, code.Text);
                    }
                    else break;
                }

                return MakeString(length, a.TypeName, pcs.ToClassifiedString());
            }
            else if (a.VisibleModel)
            {
                var exp = ExpressionToken.TryParse(a.Scope, _bracketsEndTokens);
                if (exp != null)
                {
                    var pcs = new ProbeClassifiedStringBuilder();
                    pcs.AddDataType(tokenText);
                    pcs.AddOperator("(");
                    pcs.AddNumber(code.Text);
                    if (brackets != null) brackets.AddToken(exp);
                    if (code.ReadExact(')'))
                    {
                        pcs.AddOperator(")");
                        if (brackets != null)
                        {
                            brackets.AddClose(code.Span);
                            a.OnToken(brackets);
                        }
                    }

                    return MakeString(0, a.TypeName, pcs.ToClassifiedString());
                }
                else
                {
                    return MakeString(0, a.TypeName, new ProbeClassifiedString(ProbeClassifierType.DataType, tokenText));
                }
            }
            else
            {
                return MakeString(0, a.TypeName, new ProbeClassifiedString(ProbeClassifierType.DataType, tokenText));
            }
        }

        private static DataType ProcessString(ParseArgs a, string tokenText)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType(tokenText);

            var code = a.Code;

            if (code.ReadExact("varying"))
            {
                pcs.AddSpace();
                pcs.AddDataType("varying");
                a.OnDataTypeKeyword(code.Span, code.Text, null);
            }
            else if (code.ReadNumber())
            {
                pcs.AddSpace();
                pcs.AddNumber(code.Text);
                a.OnNumber(code.Span, code.Text);
            }

            while (!code.EndOfFile)
            {
                if (ReadAttribute(a, code, pcs)) { }
                else if (code.ReadStringLiteral())
                {
                    pcs.AddSpace();
                    pcs.AddStringLiteral(code.Text);
                    a.OnStringLiteral(code.Span, code.Text);
                }
                else break;
            }

            return MakeString(0, a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessDate(ParseArgs a, string tokenText)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType(tokenText);

            var code = a.Code;

            if (code.ReadNumber())
            {
                // width
                pcs.AddSpace();
                pcs.AddNumber(code.Text);
                a.OnNumber(code.Span, code.Text);
            }

            var gotMask = false;
            var done = false;
            while (!done && !code.EndOfFile)
            {
                if (ReadAttribute(a, code, pcs, "shortform", "longform", "alternate")) { }
                else if (!gotMask && code.ReadStringLiteral())
                {
                    pcs.AddSpace();
                    pcs.AddStringLiteral(code.Text);
                    a.OnStringLiteral(code.Span, code.Text);
                }
                else break;
            }

            return MakeDate(a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessTime(ParseArgs a, string tokenText)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType(tokenText);

            var code = a.Code;

            var gotMask = false;
            while (!code.EndOfFile)
            {
                if (ReadAttribute(a, code, pcs)) { }
                else if (!gotMask && code.ReadNumber())
                {
                    gotMask = true;
                    pcs.AddSpace();
                    pcs.AddNumber(code.Text);
                    a.OnNumber(code.Span, code.Text);
                }
                else break;
            }

            return MakeTime(a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessEnum(ParseArgs a)
        {
            var options = new List<Definition>();

            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType("enum");

            var code = a.Code;
            BracesToken braces = null;

            // Read tokens before the option list
            var gotWidth = false;
            while (!code.EndOfFile)
            {
                if (code.ReadExact('{'))
                {
                    if (a.TokenCreateCallback != null)
                    {
                        braces = new BracesToken(a.Scope);
                        braces.AddOpen(code.Span);
                    }
                    break;
                }
                else if (ReadAttribute(a, code, pcs, "alterable", "required", "nowarn", "numeric")) { }
                else if (!gotWidth && code.ReadNumber())
                {
                    pcs.AddSpace();
                    pcs.AddNumber(code.Text);
                    a.OnNumber(code.Span, code.Text);
                    gotWidth = true;
                }
                else return MakeEnum((IEnumerable<EnumOptionDefinition>)null, a.TypeName, pcs.ToClassifiedString());
            }

            // Read the option list
            if ((a.Flags & ParseFlag.Strict) != 0)
            {
                var expectingComma = false;
                while (!code.EndOfFile)
                {
                    if (!code.Read()) break;

                    if (code.Type == CodeType.Operator)
                    {
                        if (code.Text == "}")
                        {
                            if (braces != null) braces.AddClose(code.Span);
                            break;
                        }
                        if (code.Text == ",")
                        {
                            if (braces != null) braces.AddToken(new DelimiterToken(a.Scope, code.Span));
                            if (expectingComma) expectingComma = false;
                        }
                    }
                    else if (code.Type == CodeType.StringLiteral || code.Type == CodeType.Word)
                    {
                        if (braces != null) braces.AddToken(new EnumOptionToken(a.Scope, code.Span, code.Text, null));

                        var str = NormalizeEnumOption(code.Text);
                        if (!expectingComma && !options.Any(x => x.Name == str))
                        {
                            options.Add(new EnumOptionDefinition(str, null));
                        }
                    }
                }
            }
            else
            {
                while (!code.EndOfFile)
                {
                    if (!code.Read()) break;
                    if (code.Text == "}")
                    {
                        if (braces != null)
                        {
                            braces.AddClose(code.Span);
                            a.OnToken(braces);
                        }
                        break;
                    }
                    if (code.Text == ",")
                    {
                        if (braces != null) braces.AddToken(new DelimiterToken(a.Scope, code.Span));
                        continue;
                    }
                    switch (code.Type)
                    {
                        case CodeType.Word:
                        case CodeType.StringLiteral:
                            if (braces != null) braces.AddToken(new EnumOptionToken(a.Scope, code.Span, code.Text, null));
                            options.Add(new EnumOptionDefinition(NormalizeEnumOption(code.Text), null));
                            break;
                    }
                }
            }

            pcs.AddSpace();
            pcs.AddOperator("{");
            var first = true;
            foreach (var option in options)
            {
                if (first)
                {
                    first = false;
                    pcs.AddSpace();
                }
                else
                {
                    pcs.AddDelimiter(",");
                    pcs.AddSpace();
                }
                if (option.Name.StartsWith("\"")) pcs.AddStringLiteral(option.Name);
                else pcs.AddConstant(option.Name);
            }
            pcs.AddSpace();
            pcs.AddOperator("}");

            while (ReadAttribute(a, code, pcs)) ;

            var dataType = MakeEnum(options, a.TypeName, pcs.ToClassifiedString());

            if (braces != null)
            {
                foreach (var token in braces.FindDownward<EnumOptionToken>())
                {
                    token.SetEnumDataType(dataType);
                }
            }

            return dataType;
        }

        private static DataType ProcessLike(ParseArgs a)
        {
            var code = a.Code;
            if (code.ReadWord())
            {
                var word1 = code.Text;
                var word1Span = code.Span;

                if (code.ReadExact('.'))
                {
                    var dotSpan = code.Span;

                    if (code.ReadWord())
                    {
                        var word2 = code.Text;
                        var word2Span = code.Span;

                        var table = a.AppSettings.Dict.GetTable(word1);
                        if (table != null)
                        {
                            var field = table.GetColumn(word2);
                            if (field != null)
                            {
                                if (a.TokenCreateCallback != null)
                                {
                                    var tableToken = new TableToken(a.Scope, word1Span, word1, table.Definition);
                                    var dotToken = new DotToken(a.Scope, dotSpan);
                                    var fieldToken = new TableFieldToken(a.Scope, word2Span, word2, field);
                                    var tableAndFieldToken = new TableAndFieldToken(a.Scope, tableToken, dotToken, fieldToken);
                                    a.OnToken(tableAndFieldToken);
                                }
                                return field.DataType;
                            }
                        }
                        else if (a.TableFieldCallback != null)
                        {
                            var tfDefs = a.TableFieldCallback(word1, word2);
                            if (tfDefs != null && tfDefs.Length == 2)
                            {
                                if (a.TokenCreateCallback != null)
                                {
                                    a.OnToken(new IdentifierToken(a.Scope, word1Span, word1, tfDefs[0]));
                                    a.OnToken(new DotToken(a.Scope, dotSpan));
                                    a.OnToken(new IdentifierToken(a.Scope, word2Span, word2, tfDefs[1]));
                                }
                                if (tfDefs[1].DataType != null) return tfDefs[1].DataType;
                                else return MakeUnknown(a.TypeName, new ProbeClassifiedString(
                                    new ProbeClassifiedRun(ProbeClassifierType.DataType, "like"),
                                    new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                                    new ProbeClassifiedRun(ProbeClassifierType.TableName, word1),
                                    new ProbeClassifiedRun(ProbeClassifierType.Delimiter, "."),
                                    new ProbeClassifiedRun(ProbeClassifierType.TableField, word2)
                                ));
                            }
                        }

                        if (a.TokenCreateCallback != null)
                        {
                            a.OnToken(new UnknownToken(a.Scope, word1Span, word1));
                            a.OnToken(new UnknownToken(a.Scope, dotSpan, "."));
                            a.OnToken(new UnknownToken(a.Scope, word2Span, word2));
                        }

                        return MakeUnknown(a.TypeName, new ProbeClassifiedString(
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "like"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.TableName, word1),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, "."),
                            new ProbeClassifiedRun(ProbeClassifierType.TableField, word2)
                        ));
                    }
                    else
                    {
                        if (a.TokenCreateCallback != null)
                        {
                            a.OnToken(new UnknownToken(a.Scope, word1Span, word1));
                            a.OnToken(new UnknownToken(a.Scope, dotSpan, "."));
                        }

                        return MakeUnknown(a.TypeName, new ProbeClassifiedString(
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "like"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.TableName, word1),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ".")
                        ));
                    }
                }

                if (a.VariableCallback != null)
                {
                    var def = a.VariableCallback(word1);
                    if (def != null)
                    {
                        a.OnIdentifier(code.Span, code.Text, def);
                        return def.DataType;
                    }
                }

                if (a.TokenCreateCallback != null) a.OnToken(new UnknownToken(a.Scope, word1Span, word1));

                return MakeUnknown(a.TypeName, new ProbeClassifiedString(
                    new ProbeClassifiedRun(ProbeClassifierType.DataType, "like"),
                    new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                    new ProbeClassifiedRun(ProbeClassifierType.TableName, word1)
                ));
            }
            else return null;
        }

        private static DataType ProcessSection(ParseArgs a)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType("Section");

            var code = a.Code;

            if (code.ReadExact("Level"))
            {
                pcs.AddSpace();
                pcs.AddDataType("Level");
                a.OnDataTypeKeyword(code.Span, code.Text, null);
                if (code.ReadNumber())
                {
                    pcs.AddSpace();
                    pcs.AddNumber(code.Text);
                    a.OnNumber(code.Span, code.Text);
                }
            }

            while (ReadAttribute(a, code, pcs)) ;

            return MakeSection(a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessScroll(ParseArgs a)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType("scroll");

            var code = a.Code;

            if (code.ReadNumber())
            {
                pcs.AddSpace();
                pcs.AddNumber(code.Text);
                a.OnNumber(code.Span, code.Text);
            }

            while (ReadAttribute(a, code, pcs)) ;

            return MakeScroll(a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessGraphic(ParseArgs a)
        {
            var pcs = new ProbeClassifiedStringBuilder();
            pcs.AddDataType("graphic");

            var code = a.Code;

            if (code.ReadNumber())	// rows
            {
                pcs.AddSpace();
                pcs.AddNumber(code.Text);
                a.OnNumber(code.Span, code.Text);

                if (code.ReadNumber())	// columns
                {
                    pcs.AddSpace();
                    pcs.AddNumber(code.Text);
                    a.OnNumber(code.Span, code.Text);

                    if (code.ReadNumber())	// bytes
                    {
                        pcs.AddSpace();
                        pcs.AddNumber(code.Text);
                        a.OnNumber(code.Span, code.Text);
                    }
                }
            }

            while (ReadAttribute(a, code, pcs)) ;

            return MakeGraphic(a.TypeName, pcs.ToClassifiedString());
        }

        private static DataType ProcessInterface(ParseArgs a)
        {
            var code = a.Code;
            if (code.ReadWord())
            {
                var intfName = code.Text;
                var resetPos = code.Position;

                while (a.InterfaceCallback != null && code.ReadExact('.') && code.ReadWord())
                {
                    intfName = $"{intfName}.{code.Text}";
                    resetPos = code.Position;
                }
                code.Position = resetPos;

                // Check for array brackets []
                var array = false;
                resetPos = code.Position;
                var arraySpan1 = CodeSpan.Empty;
                var arraySpan2 = CodeSpan.Empty;
                if (code.ReadExact('['))
                {
                    arraySpan1 = code.Span;
                    if (code.ReadExact(']'))
                    {
                        arraySpan2 = code.Span;
                        array = true;
                    }
                }
                if (!array) code.Position = resetPos;

                // Check for pointer *
                var pointer = code.ReadExact('*');
                var pointerSpan = code.Span;

                Interface intf = null;
                if (a.InterfaceCallback != null) intf = a.InterfaceCallback(intfName);
                if (intf == null) intf = a.AppSettings.Dict.GetInterface(code.Text);
                if (intf != null)
                {
                    if (a.TokenCreateCallback != null)
                    {
                        a.OnToken(new IdentifierToken(a.Scope, code.Span, code.Text, intf.Definition));
                        if (array)
                        {
                            a.OnOperator(arraySpan1, "[");
                            a.OnOperator(arraySpan2, "]");
                        }
                        if (pointer) a.OnOperator(pointerSpan, "*");
                    }

                    if (array) return intf.MakeArrayDataType();
                    if (pointer) return intf.MakePointerDataType();
                    return intf.DataType;
                }
            }

            return MakeInterface(null, a.TypeName, new ProbeClassifiedString(ProbeClassifierType.Interface, "interface"));
        }

        private static bool ReadAttribute(ParseArgs a, CodeParser code, ProbeClassifiedStringBuilder pcs, params string[] extraTokens)
        {
            var startPos = code.Position;
            if (code.ReadWord())
            {
                var word = code.Text;
                switch (code.Text)
                {
                    case "ALLCAPS":
                    case "AUTOCAPS":
                    case "LEADINGZEROS":
                    case "NOCHANGE":
                    case "NODISPLAY":
                    case "NOECHO":
                    case "NOINPUT":
                    case "NOPICK":
                    case "NOUSE":
                    case "REQUIRED":
                    case "PROBE":
                        a.OnDataTypeKeyword(code.Span, code.Text, null);
                        pcs.AddSpace();
                        pcs.AddDataType(word);
                        return true;

                    case "tag":
                        if (a.AllowTags)
                        {
                            pcs.AddSpace();
                            pcs.AddKeyword("tag");
                            a.OnDataTypeKeyword(code.Span, code.Text, null);

                            var resetPos = code.Position;
                            if (code.ReadTagName())
                            {
                                if (DkEnvironment.IsValidTagName(code.Text))
                                {
                                    pcs.AddSpace();
                                    pcs.AddKeyword(code.Text);
                                    a.OnKeyword(code.Span, code.Text);
                                    if (code.ReadStringLiteral())
                                    {
                                        pcs.AddSpace();
                                        pcs.AddStringLiteral(code.Text);
                                        a.OnStringLiteral(code.Span, code.Text);
                                    }
                                    else if (code.ReadWord())
                                    {
                                        pcs.AddSpace();
                                        pcs.AddStringLiteral(code.Text);
                                        a.OnDataTypeKeyword(code.Span, code.Text, null);
                                    }
                                }
                                else
                                {
                                    code.Position = resetPos;
                                }
                            }

                            return true;
                        }
                        else
                        {
                            code.Position = startPos;
                            return false;
                        }

                    default:
                        if (word.StartsWith("INTENSITY_") || extraTokens.Contains(word))
                        {
                            pcs.AddSpace();
                            pcs.AddDataType(word);
                            a.OnDataTypeKeyword(code.Span, code.Text, null);
                            return true;
                        }
                        else
                        {
                            code.Position = startPos;
                            return false;
                        }
                }
            }
            else if (code.ReadExact("@neutral"))
            {
                pcs.AddSpace();
                pcs.AddDataType("@neutral");
                a.OnDataTypeKeyword(code.Span, code.Text, null);
                return true;
            }

            return false;
        }

        private static readonly Regex _rxRepoWords = new Regex(@"\G((nomask|(NO)?PROBE|[%@]undefined|@neutral|(NO)?PICK)\b|&)");

        public void DumpTree(System.Xml.XmlWriter xml)
        {
            xml.WriteStartElement("dataType");
            xml.WriteAttributeString("name", _name);
            if (_completionOptions != null && _completionOptions.Length > 0)
            {
                var sb = new StringBuilder();
                foreach (var opt in _completionOptions)
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(opt);
                }
                xml.WriteAttributeString("completionOptions", sb.ToString());
            }
            xml.WriteEndElement();
        }

        public string InfoText
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                {
                    return string.Concat(_name, ": ", _source);
                }
                else
                {
                    return _source.ToString();
                }
            }
        }

        public ProbeClassifiedString GetClassifiedString(bool shortVersion)
        {
            if (!string.IsNullOrWhiteSpace(_name) && shortVersion)
            {
                return new ProbeClassifiedString(ProbeClassifierType.DataType, _name);
            }
            else
            {
                return _source;
            }
        }

        public static string NormalizeEnumOption(string option, bool strict = false)
        {
            if (strict)
            {
                if (option == null) return "\"\"";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(option)) return "\" \"";
            }

            if (option.IsWord()) return option;

            if (option.StartsWith("\"") && option.EndsWith("\""))
            {
                var inner = CodeParser.StringLiteralToString(option);
                if (string.IsNullOrWhiteSpace(inner) || inner.Trim() != inner || !inner.IsWord()) return option;
                return inner;
            }

            return CodeParser.StringToStringLiteral(option);
        }

        public bool HasEnumOptions => _flags.HasFlag(DataTypeFlags.CompletionEnumOptions);

        public bool IsValidEnumOption(string optionText, bool strict = false)
        {
            if (!_flags.HasFlag(DataTypeFlags.CompletionEnumOptions) || _completionOptions == null) return false;

            optionText = NormalizeEnumOption(optionText, strict);

            foreach (var opt in _completionOptions)
            {
                if (opt.Name == optionText) return true;
            }

            return false;
        }

        public EnumOptionDefinition GetEnumOption(int index)
        {
            if (!_flags.HasFlag(DataTypeFlags.CompletionEnumOptions) || _completionOptions == null) return null;
            if (index < 0 || index >= _completionOptions.Length) return null;
            return _completionOptions[index] as EnumOptionDefinition;
        }

        public EnumOptionDefinition GetEnumOption(string name)
        {
            if (!_flags.HasFlag(DataTypeFlags.CompletionEnumOptions) || _completionOptions == null) return null;

            name = NormalizeEnumOption(name);

            foreach (var opt in _completionOptions)
            {
                if (opt.Name == name) return opt as EnumOptionDefinition;
            }

            return null;
        }

        public int? GetEnumOrdinal(string value)
        {
            if (!_flags.HasFlag(DataTypeFlags.CompletionEnumOptions) || _completionOptions == null) return null;
            if (string.IsNullOrEmpty(value)) return null;
            value = NormalizeEnumOption(value);

            var index = 0;
            foreach (var option in _completionOptions)
            {
                if (NormalizeEnumOption(option.Name) == value) return index;
                index++;
            }

            return null;
        }

        public int NumEnumOptions => _flags.HasFlag(DataTypeFlags.CompletionEnumOptions) ? (_completionOptions?.Length ?? 0) : 0;

        public ValType ValueType
        {
            get { return _valueType; }
        }

        /// <summary>
        /// Returns a string of code with extra info embedded to be saved to the database.
        /// This string is normally not displayed to the user.
        /// </summary>
        public string ToCodeString()
        {
            if (!string.IsNullOrEmpty(_name))
            {
                return string.Concat("@", NormalizeEnumOption(_name), " ", _source);
            }
            else
            {
                return _source.ToString();
            }
        }

        public static float CalcArgumentCompatibility(DataType argType, DataType passType)
        {
            if (argType == null) return 1.0f;
            if (passType == null) return .5f;

            switch (argType.ValueType)
            {
                case ValType.Unknown:
                case ValType.Void:
                    return 1.0f;
                case ValType.Numeric:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Numeric:	return 1.0f;
                        case ValType.String:	return .9f;
                        case ValType.Char:		return .75f;
                        case ValType.Enum:		return .75f;
                        case ValType.Date:		return .75f;
                        case ValType.Time:		return .75f;
                        default:				return .2f;
                    }
                case ValType.String:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Numeric:	return .9f;
                        case ValType.String:	return 1.0f;
                        case ValType.Char:		return .8f;
                        case ValType.Enum:		return .9f;
                        case ValType.Date:		return .9f;
                        case ValType.Time:		return .9f;
                        default:				return .2f;
                    }
                case ValType.Char:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Numeric:	return .7f;
                        case ValType.String:	return .7f;
                        case ValType.Char:		return 1.0f;
                        case ValType.Enum:		return .8f;
                        case ValType.Date:		return .7f;
                        case ValType.Time:		return .7f;
                        default:				return .2f;
                    }
                case ValType.Enum:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Numeric:	return .8f;
                        case ValType.String:	return .9f;
                        case ValType.Char:		return .9f;
                        case ValType.Enum:		return 1.0f;
                        case ValType.Date:		return .7f;
                        case ValType.Time:		return .7f;
                        default:				return .2f;
                    }
                case ValType.Date:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Numeric:	return .9f;
                        case ValType.String:	return .9f;
                        case ValType.Char:		return .7f;
                        case ValType.Enum:		return .7f;
                        case ValType.Date:		return 1.0f;
                        case ValType.Time:		return .7f;
                        default:				return .2f;
                    }
                case ValType.Time:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Numeric:	return .9f;
                        case ValType.String:	return .9f;
                        case ValType.Char:		return .7f;
                        case ValType.Enum:		return .7f;
                        case ValType.Date:		return .7f;
                        case ValType.Time:		return 1.0f;
                        default:				return .2f;
                    }
                case ValType.Table:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Table:		return 1.0f;
                        case ValType.IndRel:	return .9f;
                        default:				return .2f;
                    }
                case ValType.IndRel:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Table:		return .9f;
                        case ValType.IndRel:	return 1.0f;
                        default:				return .2f;
                    }
                case ValType.Interface:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Interface:
                            if (argType.InterfaceArray != passType.InterfaceArray) return .5f;
                            if (argType.InterfacePointer != passType.InterfacePointer) return .5f;
                            return argType._source == passType._source ? 1.0f : .7f;
                        default:				return .2f;
                    }
                case ValType.Command:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Command:	return 1.0f;
                        default:				return .2f;
                    }
                case ValType.Section:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Section:	return 1.0f;
                        default:				return .2f;
                    }
                case ValType.Scroll:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Scroll:	return 1.0f;
                        default:				return .2f;
                    }
                case ValType.Graphic:
                    switch (passType.ValueType)
                    {
                        case ValType.Unknown:	return .5f;
                        case ValType.Void:		return .5f;
                        case ValType.Graphic:	return 1.0f;
                        default:				return .2f;
                    }
                case ValType.Variant:
                    switch (passType.ValueType)
                    {
                        case ValType.Variant:	return 1.0f;
                        default:				return .2f;
                    }
                default:
                    return .2f;
            }
        }

        public static float CalcArgumentListCompatibility(IEnumerable<ArgumentDescriptor> sigArguments, IEnumerable<DataType> passedDataTypes)
        {
            if (sigArguments.Count() == 0 && passedDataTypes.Count() == 0) return 1.0f;

            float score = 1.0f;
            int scoreCount = 1;

            for (int a = 0; a < passedDataTypes.Count() && a < sigArguments.Count(); a++)
            {
                var passedDataType = passedDataTypes.ElementAt(a);
                var sigDataType = sigArguments.ElementAt(a).DataType;
                score += DataType.CalcArgumentCompatibility(sigDataType, passedDataType);
                scoreCount++;
            }

            if (scoreCount > 0) score /= (float)scoreCount;

            if (sigArguments.Count() != passedDataTypes.Count()) score *= .25f; // Penalty if number of args don't match.

            return score;
        }

        public bool IsVoid
        {
            get { return _valueType == ValType.Void || _valueType == ValType.Unknown; }
        }

        public bool AllowsSubscript
        {
            get { return _valueType == ValType.String; }
        }

        public DataType GetSubscriptDataType(int numArgs)
        {
            if (_valueType == ValType.String)
            {
                if (numArgs == 1) return DataType.Char;
                else if (numArgs == 2) return DataType.String;
                else return DataType.Unknown;
            }

            return DataType.Unknown;
        }

        public bool InterfaceArray
        {
            get => _flags.HasFlag(DataTypeFlags.InterfaceArray);
            set => _flags = value ? (_flags | DataTypeFlags.InterfaceArray) : (_flags & ~DataTypeFlags.InterfaceArray);
        }

        public bool InterfacePointer
        {
            get => _flags.HasFlag(DataTypeFlags.InterfacePointer);
            set => _flags = value ? (_flags | DataTypeFlags.InterfacePointer) : (_flags & ~DataTypeFlags.InterfacePointer);
        }

        public bool IsVariableInitializedAutomatically => _valueType == ValType.Variant;

        #region Numeric Attributes
        public int Scale => _scale;
        public int Precision => _precision;
        public int Length => _scale;
        public bool Signed => _flags.HasFlag(DataTypeFlags.Signed);
        public bool IsNumeric => _valueType == ValType.Numeric;
        public bool IsInteger => _valueType == ValType.Numeric && _scale < 0;
        public bool IsString => _valueType == ValType.String;

        public decimal MaxNumericValue
        {
            get
            {
                if (_valueType != ValType.Numeric) return 0M;
                if (_scale == 0) return 0M;
                if (_scale > 0)
                {
                    decimal value = 0M;
                    decimal delta = 9M;
                    for (int i = 0; i < _scale; i++)
                    {
                        value += delta;
                        delta *= 10M;
                    }

                    for (int i = 0; i < _precision; i++)
                    {
                        value /= 10M;
                    }

                    return value;
                }
                else // integer
                {
                    switch (_scale)
                    {
                        case -1: return _flags.HasFlag(DataTypeFlags.Signed) ? (decimal)short.MaxValue : (decimal)ushort.MaxValue;  // Char is actually 2 bytes
                        case -2: return _flags.HasFlag(DataTypeFlags.Signed) ? (decimal)short.MaxValue : (decimal)ushort.MaxValue;
                        default: return _flags.HasFlag(DataTypeFlags.Signed) ? (decimal)int.MaxValue : (decimal)uint.MaxValue;
                    }
                }

                
            }
        }

        public decimal MinNumericValue
        {
            get
            {
                if (_valueType != ValType.Numeric) return 0M;
                if (_scale == 0) return 0M;
                if (!_flags.HasFlag(DataTypeFlags.Signed)) return 0M;
                if (_scale > 0)
                {
                    decimal value = 0M;
                    decimal delta = 9M;
                    for (int i = 0; i < _scale; i++)
                    {
                        value += delta;
                        delta *= 10M;
                    }

                    for (int i = 0; i < _precision; i++)
                    {
                        value /= 10M;
                    }

                    return -value;
                }
                else // integer
                {
                    switch (_scale)
                    {
                        case -1: return _flags.HasFlag(DataTypeFlags.Signed) ? (decimal)short.MinValue : (decimal)ushort.MinValue;  // Char is actually 2 bytes
                        case -2: return _flags.HasFlag(DataTypeFlags.Signed) ? (decimal)short.MinValue : (decimal)ushort.MinValue;
                        default: return _flags.HasFlag(DataTypeFlags.Signed) ? (decimal)int.MinValue : (decimal)uint.MinValue;
                    }
                }
            }
        }
        #endregion

    }
}
