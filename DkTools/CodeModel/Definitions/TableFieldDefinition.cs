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
		private string _dataType;
		private string _desc;
		private string _repoDesc;

		public TableFieldDefinition(Scope scope, string tableName, string fieldName, string prompt, string comment, string dataType, string description)
			: base(scope, fieldName, null, true)
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
}
