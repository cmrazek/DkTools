using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class CharLiteralNode : TextNode
	{
		private char? _char;

		public CharLiteralNode(Statement stmt, Span span, string text)
			: base(stmt, DataType.Char, span, text)
		{
			if (text != null && text.Length >= 1) _char = text[0];
		}

		public override bool IsReportable => true;
		public override Value ReadValue(RunScope scope) => new CharValue(DataType.Char, _char);
		public override string ToString() => string.Concat("'", _char.ToString(), "'");
	}
}
