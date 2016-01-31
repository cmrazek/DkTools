﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class ConstantToken : WordToken
	{
		public ConstantToken(Scope scope, Span span, string text, Definition def)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.Constant;
			SourceDefinition = def;
		}
	}
}
