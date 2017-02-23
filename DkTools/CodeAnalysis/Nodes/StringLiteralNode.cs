﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Nodes
{
	class StringLiteralNode : TextNode
	{
		public StringLiteralNode(Statement stmt, Span span, string text)
			: base(stmt, span, text)
		{
		}

		public override Value ReadValue(RunScope scope)
		{
			return new Value(DataType.String);
		}
	}
}