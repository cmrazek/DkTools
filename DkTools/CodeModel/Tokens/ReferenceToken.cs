﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class ReferenceToken : Token
	{
		public ReferenceToken(Scope scope, Span span)
			: base(scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Operator;
		}

		public override string Text
		{
			get { return "&"; }
		}
	}
}
