using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	interface IBreakOwner
	{
		void OnBreakAttached(BreakStatement breakToken);
	}
}
