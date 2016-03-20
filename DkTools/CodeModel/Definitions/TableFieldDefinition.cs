using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class TableFieldDefinition : Definition
	{
		private string _tableName;
		private string _fieldName;
		private string _prompt;
		private string _comment;
		private DataType _dataType;
		private string _desc;
		private string _repoDesc;

		public TableFieldDefinition(string tableName, string fieldName, string prompt, string comment, DataType dataType, string description)
			: base(fieldName, null, -1, DkDict.Column.GetTableFieldExternalRefId(tableName, fieldName))
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

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableField; }
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

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				var items = new List<System.Windows.UIElement>();
				items.Add(WpfMainLine(string.Concat(_tableName, ".", _fieldName)));
				if (!string.IsNullOrWhiteSpace(_prompt)) items.Add(WpfAttribute("Prompt", _prompt));
				if (!string.IsNullOrWhiteSpace(_comment)) items.Add(WpfAttribute("Comment", _comment));
				if (_dataType != null) items.Add(WpfAttribute("Data Type", _dataType.QuickInfoWpf));
				if (!string.IsNullOrWhiteSpace(_repoDesc)) items.Add(WpfInfoLine(_repoDesc));

				return WpfDivs(items);
			}
		}

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

		public override bool AllowsChild
		{
			get { return false; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			throw new NotSupportedException();
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}
	}
}
