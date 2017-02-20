﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class ExpressionNode : GroupNode
	{
		public ExpressionNode(Statement stmt)
			: base(stmt)
		{
		}

		public override int Precedence
		{
			get
			{
				return 0;
			}
		}
	}
}
