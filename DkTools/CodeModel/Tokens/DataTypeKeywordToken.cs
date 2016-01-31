﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class DataTypeKeywordToken : WordToken
	{
		public DataTypeKeywordToken(Scope scope, Span span, string text)
			: base(scope, span, text)
		{
			ClassifierType = Classifier.ProbeClassifierType.DataType;
		}
	}
}
