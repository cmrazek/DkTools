using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	interface IContinueOwner
	{
		void OnContinueAttached(ContinueStatement continueToken);
	}
}
