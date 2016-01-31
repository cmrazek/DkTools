﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class KeywordToken : WordToken
	{
		public KeywordToken(Scope scope, Span span, string text)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.Keyword;
		}
	}
}
