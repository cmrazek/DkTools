using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using DkTools.Compiler;

namespace DkTools
{
	internal enum CommandId
	{
		ShowProbeExplorer = 0x0100,
		probeMenuId = 0x0101,
		Compile = 0x0102,
		KillCompile = 0x0103,
		FecFile = 0x0104,
		FecFileToVisualC = 0x0105,
		PstTable = 0x0106,
		MergeFile = 0x0107,
		TableListing = 0x0108,
		FindInProbeFiles = 0x0109,
		Settings = 0x010a,
		Run = 0x010b,
		InsertFileHeader = 0x010c,
		InsertTag = 0x010d,
		InsertDiag = 0x010e,
		InsertDate = 0x010f,
		TaggingOptions = 0x0114,
		PSelect = 0x011a,
		ShowCodeModel = 0x011b,
		Dccmp = 0x011c,
		Credelix = 0x011d,
		ShowHelp = 0x011e,
		ShowDrv = 0x011f,
		DisableDeadCode = 0x0120,
		ShowProbeNV = 0x0123,
		ShowErrors = 0x0124,
		GoToNextReference = 0x0125,
		GoToPrevReference = 0x0126,
		ShowFunctions = 0x0127,
		RunCodeAnalysis = 0x0128,
		ShowCodeAnalysis = 0x0129,
		ClearErrors = 0x012a,
		ShowDict = 0x012b
	}

	internal static class Commands
	{
		private static List<CommandInstance> _cmds = new List<CommandInstance>();

		public static void InitCommands(OleMenuCommandService mcs)
		{
			AddCommand(mcs, CommandId.ShowProbeExplorer, ShowProbeExplorer);
			AddCommand(mcs, CommandId.Compile, Compile);
			AddCommand(mcs, CommandId.KillCompile, KillCompile);
			AddCommand(mcs, CommandId.FecFile, FecFile);
			AddCommand(mcs, CommandId.FecFileToVisualC, FecFileToVisualC);
			AddCommand(mcs, CommandId.MergeFile, MergeFile);
			AddCommand(mcs, CommandId.TableListing, TableListing);
			AddCommand(mcs, CommandId.FindInProbeFiles, FindInProbeFiles);
			AddCommand(mcs, CommandId.Settings, ProbeSettings);
			AddCommand(mcs, CommandId.Run, RunSamCam);
			AddCommand(mcs, CommandId.InsertFileHeader, InsertFileHeader);
			AddCommand(mcs, CommandId.InsertTag, InsertTag);
			AddCommand(mcs, CommandId.InsertDiag, InsertDiag);
			AddCommand(mcs, CommandId.InsertDate, InsertDate);
			AddCommand(mcs, CommandId.TaggingOptions, ShowTaggingOptions);
			AddCommand(mcs, CommandId.PSelect, LaunchPSelect);
			AddCommand(mcs, CommandId.Dccmp, Compile_Dccmp);
			AddCommand(mcs, CommandId.Credelix, Compile_Credelix);
			AddCommand(mcs, CommandId.ShowHelp, ShowHelp);
			AddCommand(mcs, CommandId.ShowDrv, ShowDrv);
			AddCommand(mcs, CommandId.DisableDeadCode, DisableDeadCode, checkedCallback: DisableDeadCode_Checked);
			AddCommand(mcs, CommandId.ShowProbeNV, ShowProbeNV);
			AddCommand(mcs, CommandId.ShowErrors, ShowErrors, checkedCallback: ShowErrors_Checked);
			AddCommand(mcs, CommandId.GoToNextReference, GoToNextReference);
			AddCommand(mcs, CommandId.GoToPrevReference, GoToPrevReference);
			AddCommand(mcs, CommandId.ShowFunctions, ShowFunctions);
			AddCommand(mcs, CommandId.RunCodeAnalysis, RunCodeAnalysis);
			AddCommand(mcs, CommandId.ShowCodeAnalysis, ShowCodeAnalysis, checkedCallback: ShowCodeAnalysis_Checked);
			AddCommand(mcs, CommandId.ClearErrors, ClearErrors);
			AddCommand(mcs, CommandId.ShowDict, ShowDict);
		}

		private class CommandInstance
		{
			public CommandId id;
			public OleMenuCommand cmdObject;
			public EventHandler handler;
			public QueryStatusDelegate visibleCallback;
			public QueryStatusDelegate enabledCallback;
			public QueryStatusDelegate checkedCallback;
		}

		public delegate bool QueryStatusDelegate(CommandId id);

		private static void AddCommand(OleMenuCommandService mcs, CommandId id, EventHandler handler, QueryStatusDelegate visibleCallback = null, QueryStatusDelegate enabledCallback = null, QueryStatusDelegate checkedCallback = null)
		{
			var cmdId = new CommandID(GuidList.guidProbeToolsCmdSet, (int)id);
			var cmd = new OleMenuCommand(handler, cmdId);
			mcs.AddCommand(cmd);

			if (visibleCallback != null || enabledCallback != null || checkedCallback != null)
			{
				cmd.BeforeQueryStatus += (sender, e) => { UpdateCommandStatus(id); };
			}

			_cmds.Add(new CommandInstance
			{
				id = id,
				cmdObject = cmd,
				handler = handler,
				visibleCallback = visibleCallback,
				enabledCallback = enabledCallback,
				checkedCallback = checkedCallback
			});

			if (visibleCallback == null) cmd.Visible = true;
			if (enabledCallback == null) cmd.Enabled = true;
			if (checkedCallback == null) cmd.Checked = false;

			if (visibleCallback != null || enabledCallback != null | checkedCallback != null)
			{
				UpdateCommandStatus(id);
			}
		}

		public static void UpdateCommandStatus(CommandId id)
		{
			var cmd = _cmds.FirstOrDefault(x => x.id == id);
			if (cmd != null)
			{
				var visible = true;
				if (cmd.visibleCallback != null)
				{
					try
					{
						visible = cmd.visibleCallback(id);
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Error when querying visible status for command ID {0}", id);
					}
				}
				cmd.cmdObject.Visible = visible;

				var enabled = true;
				if (cmd.enabledCallback != null)
				{
					try
					{
						enabled = cmd.enabledCallback(id);
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Error when querying enabled status for command ID {0}", id);
					}
				}
				cmd.cmdObject.Enabled = enabled;

				var chkd = true;
				if (cmd.checkedCallback != null)
				{
					try
					{
						chkd = cmd.checkedCallback(id);
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Error when querying checked status for command ID {0}", id);
					}
				}
				cmd.cmdObject.Checked = chkd;
			}
		}

		private static void ShowProbeExplorer(object sender, EventArgs e)
		{
			var window = Shell.ShowProbeExplorerToolWindow();
			window.FocusFileFilter();
		}

		internal static void SaveProbeFiles()
		{
			List<string> srcDirs = null;

			foreach (EnvDTE.Document doc in Shell.DTE.Documents)
			{
				if (!doc.Saved)
				{
					Log.Debug(string.Concat("Doc Path: ", Path.GetFullPath(doc.Path)));

					var docFileName = doc.FullName;
					var saveFile = false;

					if (srcDirs == null) srcDirs = ProbeEnvironment.SourceIncludeDirs.ToList();
					foreach (var dir in srcDirs)
					{
						var srcDirPath = dir;
						if (!srcDirPath.EndsWith("\\")) srcDirPath += "\\";

						if (docFileName.Length >= srcDirPath.Length &&
							docFileName.Substring(0, srcDirPath.Length).Equals(srcDirPath, StringComparison.OrdinalIgnoreCase))
						{
							saveFile = true;
							break;
						}
					}

					if (saveFile)
					{
						try
						{
							Log.Debug("Saving document: {0}", doc.FullName);
							doc.Save();
						}
						catch (Exception ex)
						{
							Log.Error(ex, string.Format("Unable to save document '{0}'.", doc.FullName));
						}
					}
				}
			}
		}

		private static void Compile(object sender, EventArgs e)
		{
			ProbeCompiler.Instance.Compile(ProbeCompiler.CompileMethod.Compile);
		}

		private static void Compile_Dccmp(object sender, EventArgs e)
		{
			ProbeCompiler.Instance.Compile(ProbeCompiler.CompileMethod.Dccmp);
		}

		private static void Compile_Credelix(object sender, EventArgs e)
		{
			ProbeCompiler.Instance.Compile(ProbeCompiler.CompileMethod.Credelix);
		}

		private static void KillCompile(object sender, EventArgs e)
		{
			ProbeCompiler.Instance.Kill();
		}

		private static void ClearErrors(object sender, EventArgs e)
		{
			ErrorTagging.ErrorTaskProvider.Instance.Clear();
		}

		private static void FecFile(object sender, EventArgs e)
		{
			try
			{
				var activeDoc = Shell.DTE.ActiveDocument;
				if (activeDoc == null)
				{
					Shell.ShowError("No file is open.");
					return;
				}

				string baseFileName = ProbeEnvironment.FindBaseFile(activeDoc.FullName);
				if (string.IsNullOrEmpty(baseFileName))
				{
					Shell.ShowError("Base file could not be found.");
					return;
				}

				string fileName;
				using (TempFileOutput output = new TempFileOutput(
					Path.GetFileNameWithoutExtension(baseFileName) + "_fec",
					Path.GetExtension(baseFileName)))
				{
					using (ProcessRunner pr = new ProcessRunner())
					{
						int exitCode = pr.CaptureProcess("fec.exe", "/p \"" + baseFileName + "\"",
							Path.GetDirectoryName(baseFileName), output);

						if (exitCode != 0)
						{
							Shell.ShowError(string.Format("FEC returned exit code {0}.", exitCode));
							return;
						}
					}

					fileName = output.FileName;
				}

				Shell.OpenDocument(fileName);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void FecFileToVisualC(object sender, EventArgs e)
		{
			try
			{
				var activeDoc = Shell.DTE.ActiveDocument;
				if (activeDoc == null)
				{
					Shell.ShowError("No file is open.");
					return;
				}

				string baseFileName = ProbeEnvironment.FindBaseFile(activeDoc.FullName);
				if (string.IsNullOrEmpty(baseFileName))
				{
					Shell.ShowError("Base file could not be found.");
					return;
				}

				using (var pr = new ProcessRunner())
				{
					var args = string.Concat("\"", baseFileName, "\"");

					var output = new StringOutput();

					var exitCode = pr.CaptureProcess("fec.exe", args, Path.GetDirectoryName(baseFileName), output);

					if (exitCode != 0)
					{
						Shell.ShowError(string.Format("FEC returned exit code {0}\r\n\r\n{1}", exitCode, output.Text));
						return;
					}
				}

				var cFileName = Path.Combine(Path.GetDirectoryName(baseFileName),
					string.Concat(Path.GetFileNameWithoutExtension(baseFileName), ".c"));
				if (!File.Exists(cFileName))
				{
					Shell.ShowError("Unable to find .c file produced by FEC.");
					return;
				}

				Shell.OpenDocument(cFileName);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void MergeFile(object sender, EventArgs e)
		{
			try
			{
				var activeDoc = Shell.DTE.ActiveDocument;
				if (activeDoc == null)
				{
					Shell.ShowError("No file is open.");
					return;
				}
				var fileName = activeDoc.FullName;

				var cp = new CodeProcessing.CodeProcessor();
				cp.ShowMergeComments = true;
				cp.ProcessFile(fileName);

				string tempFileName = string.Empty;
				using (var tempFileOutput = new TempFileOutput(string.Concat(Path.GetFileNameWithoutExtension(fileName), "_merge"),
					Path.GetExtension(fileName)))
				{
					var errors = cp.Errors;
					if (errors.Any())
					{
						tempFileOutput.WriteLine("// Errors encountered during processing:");
						foreach (var error in errors)
						{
							if (error.Line != null && error.Line.File != null) tempFileOutput.WriteLine(string.Format("// {0}({1}): {2}", error.Line.FileName, error.Line.LineNum, error.Message));
							else tempFileOutput.WriteLine(error.Message);
						}
						tempFileOutput.WriteLine(string.Empty);
					}

					foreach (var line in cp.Lines)
					{
						tempFileOutput.WriteLine(line.Text);
					}

					tempFileName = tempFileOutput.FileName;
				}

				Shell.OpenDocument(tempFileName);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void TableListing(object sender, EventArgs e)
		{
			try
			{
				var output = new StringOutput();
				int exitCode;

				using (var pr = new ProcessRunner())
				{
					exitCode = pr.CaptureProcess("ptd.exe", "", ProbeEnvironment.TempDir, output);
				}
				if (exitCode != 0)
				{
					Shell.ShowError(string.Format("PTD returned exit code {0}.", exitCode));
				}
				else
				{
					var tempFileName = TempManager.GetNewTempFileName("ptd", ".txt");
					File.WriteAllText(tempFileName, output.Text);
					Shell.OpenDocument(tempFileName);
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void FindInProbeFiles(object sender, EventArgs e)
		{
			try
			{
				var dirs = new StringBuilder();
				var dirList = new List<string>();

				foreach (var sourceDir in ProbeEnvironment.SourceDirs) dirList.Add(sourceDir);
				foreach (var includeDir in ProbeEnvironment.IncludeDirs) dirList.Add(includeDir);

				Shell.ShowFindInFiles(dirList);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ProbeSettings(object sender, EventArgs e)
		{
			try
			{
				Shell.DTE.ExecuteCommand("Tools.Options", GuidList.strProbeExplorerOptions);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void RunSamCam(object sender, EventArgs e)
		{
			try
			{
				using (var form = new Run.RunForm())
				{
					form.ShowDialog();
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void InsertFileHeader(object sender, EventArgs e)
		{
			try
			{
				Tagging.Tagger.InsertFileHeader();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void InsertTag(object sender, EventArgs e)
		{
			try
			{
				Tagging.Tagger.InsertTag();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void InsertDiag(object sender, EventArgs e)
		{
			try
			{
				Tagging.Tagger.InsertDiag();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void InsertDate(object sender, EventArgs e)
		{
			try
			{
				Tagging.Tagger.InsertDate();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ShowTaggingOptions(object sender, EventArgs e)
		{
			try
			{
				Shell.DTE.ExecuteCommand("Tools.Options", GuidList.strTaggingOptions);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void LaunchPSelect(object sender, EventArgs e)
		{
			try
			{
				if (!string.IsNullOrEmpty(ProbeEnvironment.PlatformPath))
				{
					var pathName = Path.Combine(ProbeEnvironment.PlatformPath, "ACM.msc");
					if (File.Exists(pathName))
					{
						Process.Start(pathName);
					}
					else
					{
						Shell.ShowError("Unable to locate 'ACM.msc'");
					}
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ShowHelp(object sender, EventArgs e)
		{
			try
			{
				if (!string.IsNullOrEmpty(ProbeEnvironment.PlatformPath))
				{
					var pathName = Path.Combine(ProbeEnvironment.PlatformPath, "..\\Documentation\\platform.chm");
					if (File.Exists(pathName))
					{
						Process.Start(pathName);
					}
					else
					{
						Shell.ShowError("Unable to locate 'platform.chm'");
					}
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ShowDrv(object sender, EventArgs e)
		{
			try
			{
				if (!string.IsNullOrEmpty(ProbeEnvironment.PlatformPath))
				{
					var pathName = Path.Combine(ProbeEnvironment.PlatformPath, "DRV.msc");
					if (File.Exists(pathName))
					{
						Process.Start(pathName);
					}
					else
					{
						Shell.ShowError("Unable to locate 'DRV.msc'");
					}
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ShowProbeNV(object sender, EventArgs e)
		{
			try
			{
				if (!string.IsNullOrEmpty(ProbeEnvironment.PlatformPath))
				{
					var pathName = Path.Combine(ProbeEnvironment.PlatformPath, "probenv.exe");
					if (File.Exists(pathName))
					{
						Process.Start(pathName);
					}
					else
					{
						Shell.ShowError("Unable to locate 'ProbeNV.exe'");
					}
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void DisableDeadCode(object sender, EventArgs e)
		{
			try
			{
				var options = ProbeToolsPackage.Instance.EditorOptions;
				options.DisableDeadCode = !options.DisableDeadCode;
				options.SaveSettingsToStorage();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static bool DisableDeadCode_Checked(CommandId id)
		{
			return ProbeToolsPackage.Instance.EditorOptions.DisableDeadCode;
		}

		private static void ShowFunctions(object sender, EventArgs e)
		{
			try
			{
				var window = Shell.ShowProbeExplorerToolWindow();
				window.FocusFunctionFilter();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ShowDict(object sender, EventArgs e)
		{
			try
			{
				var window = Shell.ShowProbeExplorerToolWindow();
				window.FocusDictFilter();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void ShowErrors(object sender, EventArgs e)
		{
			try
			{
				var options = ProbeToolsPackage.Instance.EditorOptions;
				options.RunBackgroundFecOnSave = !options.RunBackgroundFecOnSave;
				options.SaveSettingsToStorage();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static bool ShowErrors_Checked(CommandId id)
		{
			return ProbeToolsPackage.Instance.EditorOptions.RunBackgroundFecOnSave;
		}

		private static void ShowCodeAnalysis(object sender, EventArgs e)
		{
			try
			{
				var options = ProbeToolsPackage.Instance.EditorOptions;
				options.RunCodeAnalysisOnSave = !options.RunCodeAnalysisOnSave;
				options.SaveSettingsToStorage();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static bool ShowCodeAnalysis_Checked(CommandId id)
		{
			return ProbeToolsPackage.Instance.EditorOptions.RunCodeAnalysisOnSave;
		}

		private static void GoToNextReference(object sender, EventArgs e)
		{
			try
			{
				var nav = Navigation.Navigator.TryGetForView(Shell.ActiveView);
				if (nav != null) nav.GoToNextOrPrevReference(true);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void GoToPrevReference(object sender, EventArgs e)
		{
			try
			{
				var nav = Navigation.Navigator.TryGetForView(Shell.ActiveView);
				if (nav != null) nav.GoToNextOrPrevReference(false);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		private static void RunCodeAnalysis(object sender, EventArgs e)
		{
			try
			{
				var view = Shell.ActiveView;
				if (view == null) return;

				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(view.TextBuffer);
				if (fileStore == null) return;
				var model = fileStore.CreatePreprocessedModel(view.TextSnapshot, false, "Code Analysis");

				var pane = Shell.CreateOutputPane(GuidList.guidCodeAnalysisPane, "DK Code Analysis");
				pane.Clear();
				pane.Show();

				var ca = new CodeAnalysis.CodeAnalyzer(pane, model);
				ca.Run();
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

#if DEBUG
		internal static class DebugCommands
		{
			private static CodeModel.CodeModel GetModelForActiveDoc()
			{
				var view = Shell.ActiveView;
				if (view == null) return null;

				var store = CodeModel.FileStore.GetOrCreateForTextBuffer(view.TextBuffer);
				if (store == null) return null;
				return store.GetMostRecentModel(view.TextSnapshot, "Debug Commands");
			}

			public static void ShowCodeModelDump()
			{
				var view = Shell.ActiveView;
				if (view == null) return;

				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(view.TextBuffer);
				if (fileStore == null) return;
				var model = fileStore.GetCurrentModel(view.TextSnapshot, "Debug:ShowCodeModelDump");

				Shell.OpenTempContent(model.DumpTree(), Path.GetFileName(model.FileName), ".model.xml");
			}

			public static void ShowStdLibCodeModelDump()
			{
				var model = CodeModel.FileStore.StdLibModel;

				Shell.OpenTempContent(model.DumpTree(), "stdlib", ".model.xml");
			}

			public static void ShowDefinitions()
			{
				var model = GetModelForActiveDoc();
				if (model != null)
				{
					Shell.OpenTempContent(model.DefinitionProvider.DumpDefinitions(), Path.GetFileName(model.FileName), ".txt");
				}
			}

			private static CodeModel.CodeSource GetCodeSourceForActiveView(out string fileName)
			{
				var view = Shell.ActiveView;
				if (view != null)
				{
					fileName = VsTextUtil.TryGetFileName(view.TextBuffer);
					var content = view.TextBuffer.CurrentSnapshot.GetText();

					try
					{
						var merger = new CodeModel.FileMerger();
						merger.MergeFile(fileName, content, true, true);
						return merger.MergedContent;
					}
					catch (Exception ex)
					{
						Log.WriteEx(ex);

						var codeSource = new CodeModel.CodeSource();
						codeSource.Append(content, new CodeModel.CodeAttributes(fileName, 0, content.Length, true, true, false));
						codeSource.Flush();

						return codeSource;
					}
				}

				fileName = null;
				return null;
			}

			public static void ShowPreprocessor()
			{
				string fileName;
				var codeSource = GetCodeSourceForActiveView(out fileName);
				if (codeSource == null) return;

				var store = new CodeModel.FileStore();
				var model = store.CreatePreprocessedModel(codeSource, fileName, false, "Commands.Debug.ShowPreprocessor()", null);
				
				Shell.OpenTempContent(model.Source.Text, Path.GetFileName(fileName), ".preprocessor.txt");
			}

			public static void ShowPreprocessorDump()
			{
				var model = GetModelForActiveDoc();
				if (model == null)
				{
					Shell.ShowError("No model found.");
					return;
				}

				var prepModel = model.PreprocessorModel;
				if (prepModel == null)
				{
					Shell.ShowError("No preprocessor model found.");
					return;
				}

				//Shell.OpenTempContent(prepModel.File.CodeSource.Dump(), Path.GetFileName(model.FileName), ".ppsegs.txt");
				Shell.OpenTempContent(prepModel.Dump(), Path.GetFileName(model.FileName), ".prep.txt");
			}

			public static void ShowPreprocessorFullModelDump()
			{
				var view = Shell.ActiveView;
				if (view == null) return;

				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(view.TextBuffer);
				if (fileStore == null) return;
				var model = fileStore.CreatePreprocessedModel(view.TextSnapshot, false, "Debug:ShowPreprocessorFullModelDump");

				Shell.OpenTempContent(model.DumpTree(), Path.GetFileName(model.FileName), ".prepmodel.xml");
			}

			public static void ShowStateAtCaret()
			{
				var view = Shell.ActiveView;
				if (view == null) return;

				var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(view.TextBuffer);
				var state = tracker.GetStateForPosition(view.Caret.Position.BufferPosition, view.TextSnapshot);
				Log.Debug("State at caret: 0x{0:X8}", state);
			}
		}
#endif
	}
}
