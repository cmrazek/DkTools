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
		[Category("DK Explorer Options")]
		[DisplayName("Show Files in Tree")]
		[Description("Show all the files when expanding folders in the tree-view. The tree list can get slow if there are many files to display. If turned off, files are still accessible through the filter list.")]
		public bool ShowFilesInTree { get; set; }

		[Category("Compile")]
		[DisplayName("Show Error List After Compile")]
		[Description("When a compile is complete, if there are errors or warnings, then display Error List tool window.")]
		public bool ShowErrorListAfterCompile { get; set; }

		public ProbeExplorerOptions()
		{
			ShowFilesInTree = false;
			ShowErrorListAfterCompile = false;
		}
	}
}
