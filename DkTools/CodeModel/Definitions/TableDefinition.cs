using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class TableDefinition : Definition
	{
		private string _desc;
		private string _prompt;
		private string _comment;
		private string _description;

		public TableDefinition(Scope scope, string name, Dict.DictTable table)
			: base(scope, name, null, true)
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
}
