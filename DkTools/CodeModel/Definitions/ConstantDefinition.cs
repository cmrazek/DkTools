﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class ConstantDefinition : Definition
	{
		private string _text;

		public ConstantDefinition(Scope scope, string name, Token sourceToken, string text)
			: base(scope, name, sourceToken, true)
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
}