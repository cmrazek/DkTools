﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal class ClassAndFunctionToken : GroupToken
	{
		public ClassAndFunctionToken(GroupToken parent, Scope scope, ClassToken classToken, DotToken dotToken, FunctionCallToken funcToken)
			: base(parent, scope, new Token[] { classToken, dotToken, funcToken })
		{ }
	}
}