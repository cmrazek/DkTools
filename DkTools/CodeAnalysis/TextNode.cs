﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	abstract class TextNode : Node
	{
		private string _text;

		public TextNode(Statement stmt, Span span, string text)
			: base(stmt, span)
		{
			_text = text;
		}

		public string Text
		{
			get { return _text; }
		}
	}
}
