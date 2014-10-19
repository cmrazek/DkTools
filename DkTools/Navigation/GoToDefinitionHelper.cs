using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using DkTools.CodeModel.Definitions;

namespace DkTools.Navigation
{
	public static class GoToDefinitionHelper
	{
		public static void TriggerGoToDefinition(ITextView textView)
		{
			// Get caret point
			var caretPtTest = textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
			if (!caretPtTest.HasValue) return;
			var caretPt = caretPtTest.Value;

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(textView.TextBuffer);
			if (fileStore == null) return;

			var model = fileStore.GetCurrentModel(caretPt.Snapshot, "Go to definition");
			var modelPos = model.AdjustPosition(caretPt.Position, caretPt.Snapshot);
			var selTokens = model.File.FindDownwardTouching(modelPos).ToArray();
			if (selTokens.Length == 0)
			{
				Log.WriteDebug("Nothing selected.");
				return;
			}

			var defToken = selTokens.LastOrDefault(t => t.SourceDefinition != null);
			if (defToken != null && defToken.SourceDefinition != null)
			{
				Log.WriteDebug("Got token with SourceDefinition.");
				BrowseToDefinition(defToken.SourceDefinition);
				return;
			}

			var includeToken = selTokens.LastOrDefault(t => t is CodeModel.Tokens.IncludeToken) as CodeModel.Tokens.IncludeToken;
			if (includeToken != null)
			{
				Log.WriteDebug("Found include token.");

				var pathName = includeToken.FullPathName;
				if (!string.IsNullOrEmpty(pathName))
				{
					Shell.OpenDocument(pathName);
					return;
				}
				else
				{
					Shell.SetStatusText("Include file not found.");
					return;
				}
			}

			Log.WriteDebug("Found no definitions.");
			Shell.SetStatusText("Definition not found.");
		}

		private static void BrowseToDefinition(CodeModel.Definitions.Definition def)
		{
			if (def is TableDefinition)
			{
				var table = ProbeEnvironment.GetTable((def as TableDefinition).Name);
				if (table != null)
				{
					var window = Shell.ShowProbeExplorerToolWindow();
					window.FocusTable(table.Name);
				}
				else Shell.SetStatusText("Table not found.");
			}
			else if (def is TableFieldDefinition)
			{
				var tfdef = def as TableFieldDefinition;

				var table = ProbeEnvironment.GetTable(tfdef.TableName);
				if (table != null)
				{
					var field = table.GetField(tfdef.FieldName);
					if (field != null)
					{
						var window = Shell.ShowProbeExplorerToolWindow();
						window.FocusTableField(table.Name, field.Name);
					}
					else Shell.SetStatusText("Field not found.");
				}
				else Shell.SetStatusText("Table not found.");
			}
			else if (def is RelIndDefinition)
			{
				var relIndDef = def as RelIndDefinition;
				var table = ProbeEnvironment.GetTable(relIndDef.BaseTableName);
				if (table != null)
				{
					var window = Shell.ShowProbeExplorerToolWindow();
					window.FocusTableRelInd(table.Name, relIndDef.Name);
				}
				else Shell.SetStatusText("Table not found.");
			}
			else if (def is FunctionDefinition)
			{
				var funcDef = def as FunctionDefinition;

				var funcList = new List<FunctionDefinition>();
				funcList.Add(funcDef);

				if (funcDef.Extern)
				{
					using (var searcher = ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp.CreateSearcher())
					{
						if (searcher != null)
						{
							foreach (var def2 in searcher.SearchForFunctionDefinitions(funcDef.Name))
							{
								if (def2 == def || (def2.SourceFileName == def.SourceFileName && def2.SourceStartPos == def.SourceStartPos))
								{
									continue;
								}
								funcList.Add(def2);
							}
						}
					}
				}

				PromptDefinitions(funcList);
			}
			else if (!string.IsNullOrWhiteSpace(def.SourceFileName))
			{
				Shell.OpenDocument(def.SourceFileName, def.SourceStartPos);
			}
		}

		private static void PromptDefinitions(IEnumerable<Definition> defs)
		{
			var defList = defs.ToArray();
			if (defList.Length == 0) return;

			if (defList.Length == 1)
			{
				Shell.OpenDocument(defList[0].SourceFileName, defList[0].SourceStartPos);
				return;
			}

			var dlg = new DefinitionPickerWindow();
			dlg.Owner = System.Windows.Application.Current.MainWindow;
			dlg.Definitions = defList;
			if (dlg.ShowDialog() == true)
			{
				var selDef = dlg.SelectedItem;
				if (selDef != null)
				{
					Shell.OpenDocument(selDef.SourceFileName, selDef.SourceStartPos);
				}
			}
		}
	}
}
