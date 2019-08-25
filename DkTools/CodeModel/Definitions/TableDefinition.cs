﻿using System;
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

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Table; }
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

		public override object QuickInfoElements => QuickInfoStack(
			QuickInfoAttributeString("Table", Name),
			string.IsNullOrWhiteSpace(_prompt) ? null : QuickInfoAttributeString("Prompt", _prompt),
			string.IsNullOrWhiteSpace(_comment) ? null : QuickInfoAttributeString("Comment", _comment),
			string.IsNullOrWhiteSpace(_description) ? null : QuickInfoDescription(_description)
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

		public override IEnumerable<Definition> GetChildDefinitions(string name)
		{
			var col = _table.GetColumn(name);
			if (col != null) yield return col.Definition;
		}

		public override IEnumerable<Definition> ChildDefinitions
		{
			get
			{
				return _table.ColumnDefinitions;
			}
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
	}
}
