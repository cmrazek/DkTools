using DK.AppEnvironment;
using DK.Code;
using DK.Modeling;
using DK.Schema;
using DK.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Definitions
{
	public class TableDefinition : Definition
	{
		private string _desc;
		private string _prompt;
		private string _comment;
		private string _description;
		private bool _orig;
		private Table _table;

		public TableDefinition(string name, Table table, bool orig, FilePosition filePos)
			: base(name, filePos, Table.GetExternalRefId(name))
		{
#if DEBUG
			if (table == null) throw new ArgumentNullException("table");
#endif
			_prompt = table.Prompt;
			_comment = table.Comment;
			_description = table.Description;
			_orig = orig;
			_table = table;
		}

		public override ServerContext ServerContext => ServerContext.Neutral;

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Table; }
		}

		public override bool CompletionVisible
		{
			get { return _orig; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_desc == null)
				{
					var sb = new StringBuilder();
					sb.Append("Table: ");
					sb.Append(Name);
					if (!string.IsNullOrWhiteSpace(_prompt))
					{
						sb.AppendLine();
						sb.Append("Prompt: ");
						sb.Append(_prompt);
					}
					if (!string.IsNullOrWhiteSpace(_comment))
					{
						sb.AppendLine();
						sb.Append("Comment: ");
						sb.Append(_comment);
					}
					if (!string.IsNullOrWhiteSpace(_description))
					{
						sb.AppendLine();
						sb.Append("Description: ");
						sb.Append(_description);
					}
					_desc = sb.ToString();
				}
				return _desc;
			}
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoAttribute("Table", Name),
			string.IsNullOrWhiteSpace(_prompt) ? null : new QuickInfoAttribute("Prompt", _prompt),
			string.IsNullOrWhiteSpace(_comment) ? null : new QuickInfoAttribute("Comment", _comment),
			string.IsNullOrWhiteSpace(_description) ? null : new QuickInfoDescription(_description)
		);

		public void SetPromptComment(string prompt, string comment)
		{
			_prompt = prompt;
			_comment = comment;
			_desc = null;
		}

		public override string PickText
		{
			get { return Name; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override IEnumerable<Definition> GetChildDefinitions(string name, DkAppSettings appSettings)
		{
			var col = _table.GetColumn(name);
			if (col != null) yield return col.Definition;
		}

		public override bool AllowsDollarChild => true;

        public override bool AllowsDoubleDollarChild => true;

        public override IEnumerable<Definition> GetChildDefinitions(DkAppSettings appSettings) => _table.ColumnDefinitions;

        public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override DataType DataType
		{
			get
			{
				return DataType.Table;
			}
		}

		public override bool RequiresRefDataType
		{
			get { return true; }
		}

        #region AFS Children
        public override IEnumerable<Definition> GetDollarChildDefinitions(string name, DkAppSettings appSettings)
        {
            switch (name)
            {
                case "Action": yield return ActionDefinition; break;
                case "Clear": yield return ClearDefinition; break;
                case "ConnectErrors": yield return ConnectErrorsDefinition; break;
                case "FieldCount": yield return FieldCountDefinition; break;
                case "FieldDatatype": yield return FieldDatatypeDefinition; break;
                case "FieldFK": yield return FieldFKDefinition; break;
                case "FieldIntensity": yield return FieldIntensityDefinition; break;
                case "FieldItype": yield return FieldItypeDefinition; break;
                case "Fields": yield return FieldsDefinition; break;
                case "FieldValue": yield return FieldValueDefinition; break;
                case "File": yield return FileDefinition; break;
                case "FocusName": yield return FocusNameDefinition; break;
                case "FocusNo": yield return FocusNoDefinition; break;
                case "FocusValue": yield return FocusValueDefinition; break;
                case "Form": yield return FormDefinition; break;
                case "Insert": yield return InsertDefinition; break;
                case "Listing": yield return ListingDefinition; break;
                case "Navigate": yield return NavigateDefinition; break;
                case "ReleaseErrors": yield return ReleaseErrorsDefinition; break;
                case "Search": yield return SearchDefinition; break;
                case "SearchIndex": yield return SearchIndexDefinition; break;
                case "Table": yield return TableDefinition_; break;
                case "Update": yield return UpdateDefinition; break;
                default:
                    {
                        var col = _table.GetColumn(name);
                        if (col != null) yield return col.Definition;
                    }
                    break;
            }
        }

        public override IEnumerable<Definition> GetDollarChildDefinitions(DkAppSettings appSettings)
        {
            foreach (var cd in _table.ColumnDefinitions) yield return cd;

            yield return ActionDefinition;
            yield return ClearDefinition;
            yield return ConnectErrorsDefinition;
            yield return FieldCountDefinition;
            yield return FieldDatatypeDefinition;
            yield return FieldFKDefinition;
            yield return FieldIntensityDefinition;
            yield return FieldItypeDefinition;
            yield return FieldsDefinition;
            yield return FieldValueDefinition;
            yield return FileDefinition;
            yield return FocusNameDefinition;
            yield return FocusNoDefinition;
            yield return FocusValueDefinition;
            yield return FormDefinition;
            yield return InsertDefinition;
            yield return ListingDefinition;
            yield return NavigateDefinition;
            yield return ReleaseErrorsDefinition;
            yield return SearchDefinition;
            yield return SearchIndexDefinition;
            yield return TableDefinition_;
            yield return UpdateDefinition;
        }

        public override IEnumerable<Definition> GetDoubleDollarChildDefinitions(string name, DkAppSettings appSettings)
        {
            var col = _table.GetColumn(name);
            if (col != null) yield return col.Definition.FieldNumberDefinition;
        }

        public override IEnumerable<Definition> GetDoubleDollarChildDefinitions(DkAppSettings appSettings)
        {
            foreach (var cd in _table.ColumnDefinitions) yield return cd.FieldNumberDefinition;
        }

        Definition _actionDef;
        Definition ActionDefinition => _actionDef ?? (_actionDef = new AfsMethodDefinition(Name, "Action",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "Action",
                devDesc: "Executes the client action or server action event, depending on where the action is defined.",
                args: new ArgumentDescriptor[] {
                    new ArgumentDescriptor("ActionName", DataType.Char255)
                },
                serverContext: ServerContext.Neutral)));

        Definition _clearDef;
        Definition ClearDefinition => _clearDef ?? (_clearDef = new AfsMethodDefinition(Name, "Clear",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "Clear",
                devDesc: "Executes the Clear form event.",
                args: ArgumentDescriptor.EmptyArray,
                serverContext: ServerContext.Neutral)));

        Definition _connectErrorsDef;
        Definition ConnectErrorsDefinition => _connectErrorsDef ?? (_connectErrorsDef = new AfsMethodDefinition(Name, "ConnectErrors",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Void,
                className: Name,
                funcName: "ConnectErrors",
                devDesc: "Notifies the application, via events, of errors that occur while performing AFSH methods on the TableName form.",
                args: new ArgumentDescriptor[] {
					new ArgumentDescriptor("SuffixName", DataType.Char255)
				},
                serverContext: ServerContext.Neutral)));

        Definition _fieldCountDef;
        Definition FieldCountDefinition => _fieldCountDef ?? (_fieldCountDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "FieldCount",
            dataType: DataType.Int,
            devDesc: "Retrieves the number of fields on a form, including foreign keys and hidden fields. The count does not include zoom columns.",
            readOnly: true));

        Definition _fieldDatatypeDef;
		Definition FieldDatatypeDefinition => _fieldDatatypeDef ?? (_fieldDatatypeDef = new AfsMethodDefinition(Name, "FieldDatatype",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "FieldDatatype",
                devDesc: "Returns the data type number of a field. ",
                args: new ArgumentDescriptor[] {
                    new ArgumentDescriptor("FieldNumber ", DataType.Int)
                },
                serverContext: ServerContext.Neutral)));

        Definition _fieldFKDef;
        Definition FieldFKDefinition => _fieldFKDef ?? (_fieldFKDef = new AfsMethodDefinition(Name, "FieldFK",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "FieldFK",
                devDesc: "Returns 0 if the field is a foreign key, and -1, otherwise.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("FieldNumber", DataType.Int)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _fieldIntensityDef;
        Definition FieldIntensityDefinition => _fieldIntensityDef ?? (_fieldIntensityDef = new AfsMethodDefinition(Name, "FieldIntensity",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "FieldIntensity",
                devDesc: "Returns the INTENSITY number of a field. ",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("FieldNumber", DataType.Int)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _fieldITypeDef;
        Definition FieldItypeDefinition => _fieldITypeDef ?? (_fieldITypeDef = new AfsMethodDefinition(Name, "FieldItype",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "FieldItype",
                devDesc: "Returns the data attribute number of the field.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("FieldNumber", DataType.Int)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _fieldsDef;
        Definition FieldsDefinition => _fieldsDef ?? (_fieldsDef = new AfsMethodDefinition(Name, "Fields",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.OleObject,
                className: Name,
                funcName: "Fields",
                devDesc: "Returns an IPColumn object: DictSrvr model.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("FieldNumber", DataType.Int)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _fieldValueDef;
        Definition FieldValueDefinition => _fieldValueDef ?? (_fieldValueDef = new AfsMethodDefinition(Name, "FieldValue",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Variant,
                className: Name,
                funcName: "FieldValue",
                devDesc: "Returns the value of the field as a variant.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("FieldNumber", DataType.Int)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _fileDef;
        Definition FileDefinition => _fileDef ?? (_fileDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "File",
            dataType: DataType.OleObject,
            devDesc: "Returns an IPFile object (DictSrvr model) for the TableName.",
            readOnly: true));

        Definition _focusNameDef;
        Definition FocusNameDefinition => _focusNameDef ?? (_focusNameDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "FocusName",
            dataType: DataType.Char255,
            devDesc: "Retrieves the name of the field with focus, or sets focus to the specified field name.",
            readOnly: false));

        Definition _focusNoDef;
        Definition FocusNoDefinition => _focusNoDef ?? (_focusNoDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "FocusNo",
            dataType: DataType.Int,
            devDesc: "Retrieves the field number of the field with focus, or sets focus to the specified field number.",
            readOnly: false));

        Definition _focusValueDef;
        Definition FocusValueDefinition => _focusValueDef ?? (_focusValueDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "FocusValue",
            dataType: DataType.Variant,
            devDesc: "Retrieves or sets the value of the field that presently has focus.",
            readOnly: false));

        Definition _formDef;
        Definition FormDefinition => _formDef ?? (_formDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "Form",
            dataType: DataType.OleObject,
            devDesc: "Retrieves an IPForm object (ActiveFormSrvr model) for the TableName.",
            readOnly: true));

        Definition _insertDef;
        Definition InsertDefinition => _insertDef ?? (_insertDef = new AfsMethodDefinition(Name, "Insert",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "Insert",
                devDesc: "Executes all but the first step (Exit field) in the Add event.",
                args: ArgumentDescriptor.EmptyArray,
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _listingDef;
        Definition ListingDefinition => _listingDef ?? (_listingDef = new AfsMethodDefinition(Name, "Listing",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.OleObject,
                className: Name,
                funcName: "Listing",
                devDesc: "Executes the client listing or server listing event, depending on where the listing is defined.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("ListingName", DataType.Char255)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _navigateDef;
        Definition NavigateDefinition
        {
            get
            {
                if (_navigateDef == null)
                {
                    var enumOptions = new EnumOptionDefinition[]
                    {
                        new EnumOptionDefinition("FIRST", null),
                        new EnumOptionDefinition("NEXT", null),
                        new EnumOptionDefinition("PREV", null),
                        new EnumOptionDefinition("LAST", null),
                        new EnumOptionDefinition("EXIT", null),
                    };

                    var enumSource = new ProbeClassifiedString(
                        new ProbeClassifiedRun[]
                        {
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "enum"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Operator, "{"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "FIRST"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "NEXT"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "PREV"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "LAST"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "EXIT"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Operator, "}")
                        });

                    var enumDataType = new DataType(ValType.Enum, "afsnavigate_t", enumSource,
                        enumOptions, DataType.CompletionOptionsType.EnumOptionsList);

                    foreach (var o in enumOptions) o.SetEnumDataType(enumDataType);

                    _navigateDef = new AfsMethodDefinition(Name, "Navigate",
                        new FunctionSignature(
                            isExtern: false,
                            privacy: FunctionPrivacy.Public,
                            returnDataType: DataType.Int,
                            className: Name,
                            funcName: "Navigate",
                            devDesc: "Shifts focus to another field and calls the Exit field event, or, if type = EXIT and the column value has changed, calls the Change field value event without switching focus elsewhere.",
                            args: new ArgumentDescriptor[]
                            {
                                new ArgumentDescriptor("type", enumDataType)
                            },
                            serverContext: ServerContext.Neutral));
                }
                return _navigateDef;
            }
        }

        Definition _releaseErrorsDef;
        Definition ReleaseErrorsDefinition => _releaseErrorsDef ?? (_releaseErrorsDef = new AfsMethodDefinition(Name, "ReleaseErrors",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Void,
                className: Name,
                funcName: "ReleaseErrors",
                devDesc: "Deactivates an error connection created by $ConnectErrors.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("SuffixName", DataType.Char255)
                },
                serverContext: ServerContext.Neutral)
            )
        );

        Definition _searchDef;
        Definition SearchDefinition => _searchDef ?? (_searchDef = new AfsMethodDefinition(Name, "Search",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "Search",
                devDesc: "Executes all but the first step (Exit field) in the Search event.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("type", afssearch_t)
                },
                serverContext: ServerContext.Neutral)));

        Definition _searchIndexDef;
        Definition SearchIndexDefinition => _searchIndexDef ?? (_searchIndexDef = new AfsMethodDefinition(Name, "SearchIndex",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "SearchIndex",
                devDesc: "Executes all but the first step (Exit field) in the Search event, using a specific index.",
                args: new ArgumentDescriptor[]
                {
                    new ArgumentDescriptor("type", afssearch_t),
                    new ArgumentDescriptor("IndexName", DataType.Char255)
                },
                serverContext: ServerContext.Neutral)));

        Definition _tableDef;
        Definition TableDefinition_ => _tableDef ?? (_tableDef = new AfsPropertyDefinition(
            tableName: Name,
            name: "Table",
            dataType: DataType.OleObject,
            devDesc: "Returns an IPTable object (DictSrvr model) for the TableName.",
            readOnly: true));

        Definition _updateDef;
        Definition UpdateDefinition => _updateDef ?? (_updateDef = new AfsMethodDefinition(Name, "Update",
            new FunctionSignature(
                isExtern: false,
                privacy: FunctionPrivacy.Public,
                returnDataType: DataType.Int,
                className: Name,
                funcName: "Update",
                devDesc: "Executes all but the first step (Exit field) in the Update event.",
                args: ArgumentDescriptor.EmptyArray,
                serverContext: ServerContext.Neutral)));

        static DataType _afssearch_t;
        static DataType afssearch_t
        {
            get
            {
                if (_afssearch_t == null)
                {
                    var enumOptions = new EnumOptionDefinition[]
                        {
                        new EnumOptionDefinition("FIRST", null),
                        new EnumOptionDefinition("NEXT", null),
                        new EnumOptionDefinition("PREV", null),
                        new EnumOptionDefinition("LAST", null),
                        new EnumOptionDefinition("EQUAL", null),
                        };

                    var enumSource = new ProbeClassifiedString(
                        new ProbeClassifiedRun[]
                        {
                            new ProbeClassifiedRun(ProbeClassifierType.DataType, "enum"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Operator, "{"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "FIRST"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "NEXT"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "PREV"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "LAST"),
                            new ProbeClassifiedRun(ProbeClassifierType.Delimiter, ","),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Constant, "EQUAL"),
                            new ProbeClassifiedRun(ProbeClassifierType.Normal, " "),
                            new ProbeClassifiedRun(ProbeClassifierType.Operator, "}")
                        });

                    _afssearch_t = new DataType(ValType.Enum, "afssearch_t", enumSource,
                        enumOptions, DataType.CompletionOptionsType.EnumOptionsList);

                    foreach (var o in enumOptions) o.SetEnumDataType(_afssearch_t);
                }
                return _afssearch_t;
            }
        }
        #endregion
    }
}
