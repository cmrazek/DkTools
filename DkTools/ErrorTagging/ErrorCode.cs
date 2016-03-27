using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ErrorTagging
{
	internal enum ErrorCode
	{
		[Description("(none)")]
		None,

		[Description("FEC reported an error.")]
		[ErrorType(ErrorType.Error)]
		Fec_Error,

		[Description("FEC reported a warning.")]
		[ErrorType(ErrorType.Warning)]
		Fec_Warning,
	}
}
