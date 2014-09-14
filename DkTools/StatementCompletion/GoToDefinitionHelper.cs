using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using DkTools.CodeModel.Definitions;

namespace DkTools.StatementCompletion
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

			var identToken = selTokens.LastOrDefault(t => t is CodeModel.Tokens.IdentifierToken) as CodeModel.Tokens.IdentifierToken;
			if (identToken != null)
			{
				var defs = (from d in model.File.GetDefinitions(identToken.Text) where !string.IsNullOrEmpty(d.SourceFileName) select d).ToArray();
				if (defs.Length == 1)
				{
					Log.WriteDebug("Found definition for basic string.");
					BrowseToDefinition(defs[0]);
					return;
				}
				if (defs.Length > 1)
				{
					Log.WriteDebug("Found multiple definitions for basic string.");
					var dlg = new DefinitionPickerWindow();
					dlg.Definitions = defs;
					if (dlg.ShowDialog() == true)
					{
						var selItem = dlg.SelectedItem;
						if (selItem != null)
						{
							BrowseToDefinition(selItem);
						}
					}
					return;
				}
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
			//else if (def is CodeModel.RelIndDefinition)
			//{
			//	var relIndDef = def as CodeModel.RelIndDefinition;
			//	var table = ProbeEnvironment.GetTable(relIndDef.BaseTableName);
			//	if (table != null) Commands.OpenPst(relIndDef.BaseTableName, null, null, relIndDef.Name);
			//	else Shell.SetStatusText("Table not found.");
			//}
			//else
			if (!string.IsNullOrWhiteSpace(def.SourceFileName))
			{
				Shell.OpenDocument(def.SourceFileName, def.SourceSpan);
			}
		}
	}
}
