using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace DkTools.ProbeExplorer
{
	[Guid(GuidList.strProbeExplorerOptions)]
	class ProbeExplorerOptions : DialogPage
	{
		[Category("Probe Explorer Options")]
		[DisplayName("Show Files in Tree")]
		[Description("Show all the files when expanding folders in the tree-view. The tree list can get slow if there are many files to display. If turned off, files are still accessible through the filter list.")]
		public bool ShowFilesInTree { get; set; }

		public ProbeExplorerOptions()
		{
			ShowFilesInTree = false;
		}
	}
}
