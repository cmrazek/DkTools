using DK.Code;
using DK.Modeling;
using DK.Schema;
using DK.Syntax;
using System.Text;

namespace DK.Definitions
{
	public class ColumnDefinition : Definition
	{
		private string _tableName;
		private string _fieldName;
		private string _prompt;
		private string _comment;
		private DataType _dataType;
		private string _desc;
		private string _repoDesc;

		public ColumnDefinition(string tableName, string fieldName, string prompt, string comment, DataType dataType, string description, FilePosition filePos)
			: base(fieldName, filePos, Column.GetTableFieldExternalRefId(tableName, fieldName))
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

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.TableField; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.TableField; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_desc == null)
				{
					var sb = new StringBuilder();
					sb.Append(_tableName);
					sb.Append('.');
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
					if (_dataType != null)
					{
						sb.AppendLine();
						sb.Append("Data Type: ");
						sb.Append(_dataType.Name);
						if (!string.IsNullOrEmpty(_dataType.InfoText))
						{
							sb.AppendLine();
							sb.Append(_dataType.InfoText);
						}
					}
					_desc = sb.ToString();
				}
				return _desc;
			}
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoStack(
			new QuickInfoClassifiedString(new ProbeClassifiedString[]
			{
				new ProbeClassifiedString(ProbeClassifierType.TableName, _tableName),
				new ProbeClassifiedString(ProbeClassifierType.Delimiter, "."),
				new ProbeClassifiedString(ProbeClassifierType.TableField, _fieldName)
			}),
			string.IsNullOrWhiteSpace(_prompt) ? null : new QuickInfoAttribute("Prompt", _prompt),
			string.IsNullOrWhiteSpace(_comment) ? null : new QuickInfoAttribute("Comment", _comment),
			_dataType != null ? new QuickInfoAttribute("Data Type", _dataType.GetClassifiedString(shortVersion: false)) : null,
			string.IsNullOrWhiteSpace(_repoDesc) ? null : new QuickInfoDescription(_repoDesc)
		);

		public string TableName
		{
			get { return _tableName; }
		}

		public string FieldName
		{
			get { return _fieldName; }
		}

		public override string PickText
		{
			get { return string.Concat(_tableName, ".", _fieldName); }
		}

		public override DataType DataType
		{
			get { return _dataType; }
		}

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

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override int SelectionOrder
		{
			get
			{
				return 10;
			}
		}

		public override bool RequiresParent(string curClassName)
		{
			return true;
		}
	}
}
