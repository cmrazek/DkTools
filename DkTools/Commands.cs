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
using DkTools.Compiler;

namespace DkTools
{
	internal enum CommandIds
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
	}

	internal static class Commands
	{
		private static OleMenuCommand _toolWindowCmd;
		private static OleMenuCommand _compileCmd;
		private static OleMenuCommand _killCompileCmd;
		private static OleMenuCommand _fecFileCmd;
		private static OleMenuCommand _fecFileToVisualCCmd;
		private static OleMenuCommand _mergeFileCmd;
		private static OleMenuCommand _tableListingCmd;
		private static OleMenuCommand _findInProbeFilesCmd;
		private static OleMenuCommand _settingsCmd;
		private static OleMenuCommand _runCmd;
		private static OleMenuCommand _insertFileHeaderCmd;
		private static OleMenuCommand _insertTag;
		private static OleMenuCommand _insertDiag;
		private static OleMenuCommand _insertDate;
		private static OleMenuCommand _taggingOptions;
		private static OleMenuCommand _pSelect;
		private static OleMenuCommand _dccmp;
		private static OleMenuCommand _credelix;
		private static OleMenuCommand _showHelp;

		public static void InitCommands(OleMenuCommandService mcs)
		{
			_toolWindowCmd = AddCommand(mcs, ShowProbeExplorer, CommandIds.ShowProbeExplorer);
			_compileCmd = AddCommand(mcs, Compile, CommandIds.Compile);
			_killCompileCmd = AddCommand(mcs, KillCompile, CommandIds.KillCompile);
			_fecFileCmd = AddCommand(mcs, FecFile, CommandIds.FecFile);
			_fecFileToVisualCCmd = AddCommand(mcs, FecFileToVisualC, CommandIds.FecFileToVisualC);
			_mergeFileCmd = AddCommand(mcs, MergeFile, CommandIds.MergeFile);
			_tableListingCmd = AddCommand(mcs, TableListing, CommandIds.TableListing);
			_findInProbeFilesCmd = AddCommand(mcs, FindInProbeFiles, CommandIds.FindInProbeFiles);
			_settingsCmd = AddCommand(mcs, ProbeSettings, CommandIds.Settings);
			_runCmd = AddCommand(mcs, RunSamCam, CommandIds.Run);
			_insertFileHeaderCmd = AddCommand(mcs, InsertFileHeader, CommandIds.InsertFileHeader);
			_insertTag = AddCommand(mcs, InsertTag, CommandIds.InsertTag);
			_insertDiag = AddCommand(mcs, InsertDiag, CommandIds.InsertDiag);
			_insertDate = AddCommand(mcs, InsertDate, CommandIds.InsertDate);
			_taggingOptions = AddCommand(mcs, ShowTaggingOptions, CommandIds.TaggingOptions);
			_pSelect = AddCommand(mcs, LaunchPSelect, CommandIds.PSelect);
			_dccmp = AddCommand(mcs, Compile_Dccmp, CommandIds.Dccmp);
			_credelix = AddCommand(mcs, Compile_Credelix, CommandIds.Credelix);
			_showHelp = AddCommand(mcs, ShowHelp, CommandIds.ShowHelp);
		}

		private static OleMenuCommand AddCommand(OleMenuCommandService mcs, EventHandler handler, CommandIds id)
		{
			var cmdId = new CommandID(GuidList.guidProbeToolsCmdSet, (int)id);
			var cmd = new OleMenuCommand(handler, cmdId);
			mcs.AddCommand(cmd);
			cmd.Visible = true;
			cmd.Enabled = true;

			return cmd;
		}

		private static void ShowProbeExplorer(object sender, EventArgs e)
		{
			var window = ProbeToolsPackage.Instance.FindToolWindow(typeof(ProbeExplorer.ProbeExplorerToolWindow), 0, true) as ProbeExplorer.ProbeExplorerToolWindow;
			if (window == null || window.Frame == null)
			{
				throw new NotSupportedException("Unable to create Probe Explorer tool window.");
			}

			ErrorHandler.ThrowOnFailure((window.Frame as IVsWindowFrame).Show());
			window.FocusFileFilter();
		}

		internal static void SaveProbeFiles()
		{
			List<string> srcDirs = null;

			foreach (EnvDTE.Document doc in Shell.DTE.Documents)
			{
				if (!doc.Saved)
				{
					Log.WriteDebug(string.Concat("Doc Path: ", Path.GetFullPath(doc.Path)));

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
							doc.Save();
						}
						catch (Exception ex)
						{
							Log.WriteEx(ex, string.Format("Unable to save document '{0}'.", doc.FullName));
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
					int exitCode = pr.ExecuteProcess("fec.exe", string.Concat("\"", baseFileName, "\""),
						Path.GetDirectoryName(baseFileName), true);

					if (exitCode != 0)
					{
						Shell.ShowError(string.Format("FEC returned exit code {0}.", exitCode));
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
						Shell.ShowError("Unable to locate platform 'ACM.msc'");
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

#if DEBUG
		private static CodeModel.CodeModel GetModelForActiveDoc()
		{
			var activeDoc = Shell.DTE.ActiveDocument;
			if (activeDoc != null)
			{
				var source = File.ReadAllText(activeDoc.FullName);
				var model = new CodeModel.CodeModel(source, null, activeDoc.FullName);

				return model;
			}

			return null;
		}

		public static void ShowCodeModelDump()
		{
			var model = GetModelForActiveDoc();
			if (model != null)
			{
				var fileName = "";
				using (var tempFile = new TempFileOutput(Path.GetFileName(model.FileName), ".xml"))
				{
					tempFile.WriteLine(model.DumpTree());
					fileName = tempFile.FileName;
				}
				Shell.OpenDocument(fileName);
			}
		}

		public static void ShowDefinitions()
		{
			var model = GetModelForActiveDoc();
			if (model != null)
			{
				var fileName = "";
				using (var tempFile = new TempFileOutput(Path.GetFileName(model.FileName), ".txt"))
				{
					tempFile.WriteLine(model.File.DumpDefinitionsText());
					fileName = tempFile.FileName;
				}
				Shell.OpenDocument(fileName);
			}
		}
#endif
	}
}
