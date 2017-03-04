using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	enum ValType
	{
		//			// _knownValue type:
		Unknown,	// null
		Void,		// null
		Numeric,	// decimal
		String,		// string
		Char,		// char
		Enum,		// string
		Date,		// DateTime
		Time,		// DateTime
		Table,		// null
		IndRel,		// null
		Interface,	// null
		Command,	// null
		Section,	// null
		Scroll,		// null
		Graphic		// null
	}
}
