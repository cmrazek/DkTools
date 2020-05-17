using DkTools.CodeModel.Definitions;
using EnvDTE;
using Microsoft.VisualStudio.CallHierarchy.Package.Definitions;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CallHierarchy
{
	static class DkCallHierarchyHelper
	{
		public static void ViewCallHierarchy(SnapshotPoint triggerPt)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				var chWindow = ProbeToolsPackage.GetGlobalService(typeof(SCallHierarchy)) as ICallHierarchy;
				if (chWindow == null)
				{
					Shell.Status("Unable to access Call Hierarchy tool window.");
					return;
				}

				var def = Navigation.GoToDefinitionHelper.GetDefinitionAtPoint(triggerPt, realCodeOnly: true);
				if (!(def is FunctionDefinition))
				{
					Shell.Status("No function definition at cursor.");
					return;
				}

				chWindow.ShowToolWindow();

				var funcDef = def as FunctionDefinition;
				chWindow.AddRootItem(new DkCallHierarchyMemberItem(
					textBuffer: triggerPt.Snapshot.TextBuffer,
					functionFullName: funcDef.FullName,
					className: funcDef.ClassName,
					functionName: funcDef.Name,
					filePos: funcDef.FilePosition,
					extRefId: funcDef.ExternalRefId));
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}
	}
}
