using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	internal abstract class Definition
	{
		private string _name;
		private bool _global;
		private Token _sourceToken;
		private CodeFile _sourceFile;
		private string _sourceFileName;
		private Span _sourceSpan;

		private bool _gotLocalFileInfo;
		private string _localFileName;
		private Span _localFileSpan;

		public abstract bool CompletionVisible { get; }
		public abstract StatementCompletion.CompletionType CompletionType { get; }
		public abstract string CompletionDescription { get; }
		public abstract Classifier.ProbeClassifierType ClassifierType { get; }
		public abstract string QuickInfoText { get; }

		public Definition(string name, Token sourceToken, bool global)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
#endif
			_name = name;
			_global = global;

			if (sourceToken != null)
			{
				_sourceToken = sourceToken;
				_sourceFile = sourceToken.File;
				_sourceSpan = sourceToken.Span;

				if (_sourceFile != null)
				{
					_sourceFileName = _sourceFile.FileName;
				}
				else if (sourceToken is ExternalToken)
				{
					_sourceFileName = (sourceToken as ExternalToken).FileName;
				}
				else
				{
					throw new InvalidOperationException("Source token has no file object.");
				}
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public Token SourceToken
		{
			get { return _sourceToken; }
		}

		public bool Global
		{
			get { return _global; }
		}

		public CodeFile SourceFile
		{
			get { return _sourceFile; }
		}

		public string SourceFileName
		{
			get { return _sourceFileName; }
		}

		public Span SourceSpan
		{
			get { return _sourceSpan; }
			set { _sourceSpan = value; }
		}

		public string LocationText
		{
			get
			{
				return string.Concat(_sourceFileName, "(", _sourceSpan.Start.LineNum + 1, ")");
			}
		}

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement(GetType().Name);
			xml.WriteAttributeString("name", _name);
			xml.WriteAttributeString("global", _global.ToString());

			string fileName;
			Span span;
			GetLocalFileSpan(out fileName, out span);
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				xml.WriteAttributeString("localFileName", fileName);
				xml.WriteAttributeString("localOffset", span.Start.Offset.ToString());
			}

			xml.WriteEndElement();
		}

		/// <summary>
		/// Gets the location of the definition, in the file where it originated (not the preprocessed content).
		/// </summary>
		/// <param name="fileName">(out) file that contains the definition.</param>
		/// <param name="span">(out) Span of the definition, transformed to be in the originating file.</param>
		public void GetLocalFileSpan(out string fileName, out Span span)
		{
			if (_sourceFile == null)
			{
				fileName = null;
				span = Span.Empty;
				return;
			}

			if (!_gotLocalFileInfo)
			{
				_sourceFile.CodeSource.GetFileSpan(_sourceSpan, out _localFileName, out _localFileSpan);
				_gotLocalFileInfo = true;
			}
			fileName = _localFileName;
			span = _localFileSpan;
		}

#if DEBUG
		public string Dump()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Type [{0}]", GetType());
			sb.AppendFormat(" File [{0}]", SourceFile != null ? SourceFile.FileName : "(null)");
			sb.AppendFormat(" Offset [{0}]", SourceSpan.Start.Offset);
			sb.AppendFormat(" CompletionType [{0}]", CompletionType);
			sb.AppendFormat(" CompletionDescription [{0}]", CompletionDescription);
			return sb.ToString();
		}
#endif
	}

	internal class VariableDefinition : Definition
	{
		private DataType _dataType;

		public VariableDefinition(string name, Token sourceToken, DataType dataType)
			: base(name, sourceToken, false)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Variable; }
		}

		public override string CompletionDescription
		{
			get
			{
				if (_dataType != null) return string.Concat(_dataType.Name, " ", Name);
				return Name;
			}
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Normal; }
		}

		public override string QuickInfoText
		{
			get { return this.CompletionDescription; }
		}
	}

	internal class FunctionDefinition : Definition
	{
		private DataType _dataType;
		private string _signature;

		public FunctionDefinition(string name, Token sourceToken, DataType dataType, string signature)
			: base(name, sourceToken, true)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_dataType = dataType != null ? dataType : DataType.Int;
			_signature = signature;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public string Signature
		{
			get { return _signature; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Function; }
		}

		public override string CompletionDescription
		{
			get { return _signature; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
		}

		public override string QuickInfoText
		{
			get { return _signature; }
		}
	}

	internal class DataTypeDefinition : Definition
	{
		private DataType _dataType;

		public DataTypeDefinition(string name, Token sourceToken, DataType dataType)
			: base(name, sourceToken, true)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public DataTypeDefinition(string name, DataType dataType)
			: base(name, null, true)
		{
			_dataType = dataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.DataType; }
		}

		public override string CompletionDescription
		{
			get { return _dataType.InfoText; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.DataType; }
		}

		public override string QuickInfoText
		{
			get { return _dataType.InfoText; }
		}
	}

	internal class MacroDefinition : Definition
	{
		private string _signature;
		private string _body;

		public MacroDefinition(string name, Token sourceToken, string signature, string body)
			: base(name, sourceToken, true)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(signature)) throw new ArgumentNullException("signature");
#endif
			_signature = signature;
			_body = body;
		}

		public string Signature
		{
			get { return _signature; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Function; }
		}

		public override string CompletionDescription
		{
			get { return _signature; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Function; }
		}

		public override string QuickInfoText
		{
			get
			{
				return string.Concat(_signature, " ", _body).Trim();
			}
		}
	}

	internal class ConstantDefinition : Definition
	{
		private string _text;

		public ConstantDefinition(string name, Token sourceToken, string text)
			: base(name, sourceToken, true)
		{
			_text = text;
		}

		public string Text
		{
			get { return _text; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Constant; }
		}

		public override string CompletionDescription
		{
			get { return _text; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoText
		{
			get { return _text; }
		}
	}

	internal class TableDefinition : Definition
	{
		private string _desc;
		private string _prompt;
		private string _comment;
		private string _description;

		public TableDefinition(string name, Dict.DictTable table)
			: base(name, null, true)
		{
			_prompt = table.Prompt;
			_comment = table.Comment;
			_description = table.Description;
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Table; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override string CompletionDescription
		{
			get
			{
				if (_desc == null)
				{
					var sb = new StringBuilder();
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

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoText
		{
			get { return CompletionDescription; }
		}

		public void SetPromptComment(string prompt, string comment)
		{
			_prompt = prompt;
			_comment = comment;
			_desc = null;
		}
	}

	internal class TableFieldDefinition : Definition
	{
		private string _tableName;
		private string _fieldName;
		private string _prompt;
		private string _comment;
		private string _dataType;
		private string _desc;
		private string _repoDesc;

		public TableFieldDefinition(string tableName, string fieldName, string prompt, string comment, string dataType, string description)
			: base(fieldName, null, true)
		{
			_tableName = tableName;
			_fieldName = fieldName;
			_prompt = prompt;
			_comment = comment;
			_dataType = dataType;
			_repoDesc = description;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.TableField; }
		}

		public override string CompletionDescription
		{
			get
			{
				if (_desc == null)
				{
					var sb = new StringBuilder();
					if (!string.IsNullOrWhiteSpace(_dataType))
					{
						sb.Append(_dataType);
						sb.Append(" ");
					}
					sb.Append(_tableName);
					sb.Append(".");
					sb.Append(_fieldName);

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
					if (!string.IsNullOrWhiteSpace(_repoDesc))
					{
						sb.AppendLine();
						sb.Append("Description: ");
						sb.Append(_repoDesc);
					}
					_desc = sb.ToString();
				}
				return _desc;
			}
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableField; }
		}

		public override string QuickInfoText
		{
			get { return this.CompletionDescription; }
		}

		public string TableName
		{
			get { return _tableName; }
		}

		public string FieldName
		{
			get { return _fieldName; }
		}
	}

	internal class RelIndDefinition : Definition
	{
		private string _infoText;
		private string _baseTableName;

		public RelIndDefinition(string name, string baseTableName, string infoText)
			: base(name, null, true)
		{
			_infoText = infoText;
			_baseTableName = baseTableName;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override string CompletionDescription
		{
			get { return _infoText; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Table; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoText
		{
			get { return _infoText; }
		}

		public string BaseTableName
		{
			get { return _baseTableName; }
		}
	}

	internal class StringDefDefinition : Definition
	{
		private Dict.DictStringDef _stringDef;

		public StringDefDefinition(Dict.DictStringDef stringDef)
			: base(stringDef.Name, null, true)
		{
			_stringDef = stringDef;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Constant; }
		}

		public override string CompletionDescription
		{
			get { return _stringDef.Value; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoText
		{
			get { return _stringDef.Value; }
		}
	}
}
