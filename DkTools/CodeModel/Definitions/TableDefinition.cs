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
		private bool _orig;
		private DkDict.Table _table;

		public TableDefinition(string name, DkDict.Table table, bool orig, FilePosition filePos)
			: base(name, filePos, DkDict.Table.GetExternalRefId(name))
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

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Table; }
		}

		public override bool CompletionVisible
		{
			get { return _orig; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
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

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				var items = new List<System.Windows.UIElement>();
				items.Add(WpfAttribute("Table", Name));
				if (!string.IsNullOrWhiteSpace(_prompt)) items.Add(WpfAttribute("Prompt", _prompt));
				if (!string.IsNullOrWhiteSpace(_comment)) items.Add(WpfAttribute("Comment", _comment));
				if (!string.IsNullOrWhiteSpace(_description)) items.Add(WpfInfoLine(_description));
				return WpfDivs(items);
			}
		}

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

		public override Definition GetChildDefinition(string name)
		{
			var col = _table.GetColumn(name);
			if (col != null) return col.Definition;
			return null;
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}
	}
}
