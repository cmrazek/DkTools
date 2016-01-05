using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;

namespace DkTools.ErrorTagging
{
	internal class ErrorTag : Microsoft.VisualStudio.Text.Tagging.ErrorTag
	{
		public ErrorTag(string errorType, string toolTipContent)
			: base(errorType, toolTipContent)
		{ }
	}
}
