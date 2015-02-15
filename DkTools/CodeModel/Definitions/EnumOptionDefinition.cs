﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class EnumOptionDefinition : Definition
	{
		public EnumOptionDefinition(string text)
			: base(text, null, -1, null)
		{ }

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Constant; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr
		{
			get { return Name; }
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfMainLine(Name);
			}
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}
	}
}
