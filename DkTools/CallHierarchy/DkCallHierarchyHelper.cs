using DkTools.CodeModel;
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
				var def = Navigation.GoToDefinitionHelper.GetDefinitionAtPoint(triggerPt, realCodeOnly: true);
				if (!(def is FunctionDefinition))
				{
					Shell.Status("No function definition at cursor.");
					return;
				}

				ViewCallHierarchy(def as FunctionDefinition);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		public static void ViewCallHierarchy(FunctionDefinition funcDef)
		{
			try
			{
				var chWindow = ProbeToolsPackage.GetGlobalService(typeof(SCallHierarchy)) as ICallHierarchy;
				if (chWindow == null)
				{
					Shell.Status("Unable to access Call Hierarchy tool window.");
					return;
				}

				chWindow.ShowToolWindow();

				chWindow.AddRootItem(new DkCallHierarchyMemberItem(
					funcDef.FilePosition.FileName,
					functionFullName: funcDef.FullName,
					className: funcDef.ClassName,
					functionName: funcDef.Name,
					filePos: funcDef.FilePosition,
					extRefId: funcDef.ExternalRefId));
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}
	}
}
