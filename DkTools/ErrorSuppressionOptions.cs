using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace DkTools
{
	[Guid(GuidList.strErrorSuppressionOptions)]
	class ErrorSuppressionOptions : DialogPage
	{
		[Category("Environment")]
		[DisplayName("DK App Change Admin Failure")]
		[Description("Displayed when the default DK application can't be changed because Visual Studio isn't running as an administrator.")]
		public bool DkAppChangeAdminFailure { get; set; }
	}
}
