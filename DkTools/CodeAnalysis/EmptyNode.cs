﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class EmptyNode : Node
	{
		public EmptyNode(Statement stmt)
			: base(stmt, Span.Empty)
		{
		}
	}
}
