using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

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
			var model = fileStore.GetModelForSnapshotOrNewer(caretPt.Snapshot);
			var modelPos = model.GetPosition(caretPt.Position, caretPt.Snapshot);
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

			var identToken = selTokens.LastOrDefault(t => t is CodeModel.IdentifierToken) as CodeModel.IdentifierToken;
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

			var includeToken = selTokens.LastOrDefault(t => t is CodeModel.IncludeToken) as CodeModel.IncludeToken;
			if (includeToken != null)
			{
				Log.WriteDebug("Found include token.");
				// TODO: this code needs to be rewritten to not rely on a CodeFile being returned
				//if (includeToken.IncludeFile != null && !string.IsNullOrEmpty(includeToken.IncludeFile.FileName))
				//{
				//	Shell.OpenDocument(includeToken.IncludeFile.FileName);
				//	return;
				//}
				//else
				//{
				//	Log.WriteDebug("Include token has no filename.");
				//}
			}

			Log.WriteDebug("Found no definitions.");
			Shell.SetStatusText("Definition not found.");
		}

		private static void BrowseToDefinition(CodeModel.Definition def)
		{
			//if (def is CodeModel.TableDefinition)
			//{
			//	var table = ProbeEnvironment.GetTable((def as CodeModel.TableDefinition).Name);
			//	if (table != null) Commands.OpenPst(table.BaseTable, table.Name, null, null);
			//	else Shell.SetStatusText("Table not found.");
			//}
			//else if (def is CodeModel.TableFieldDefinition)
			//{
			//	var tfdef = def as CodeModel.TableFieldDefinition;
			//	var table = ProbeEnvironment.GetTable(tfdef.TableName);
			//	if (table != null) Commands.OpenPst(table.BaseTable, table.Name, tfdef.FieldName, null);
			//	else Shell.SetStatusText("Table not found.");
			//}
			//else if (def is CodeModel.RelIndDefinition)
			//{
			//	var relIndDef = def as CodeModel.RelIndDefinition;
			//	var table = ProbeEnvironment.GetTable(relIndDef.BaseTableName);
			//	if (table != null) Commands.OpenPst(relIndDef.BaseTableName, null, null, relIndDef.Name);
			//	else Shell.SetStatusText("Table not found.");
			//}
			//else
			//if (!string.IsNullOrWhiteSpace(def.SourceFileName))
			//{
			//	Shell.OpenDocument(def.SourceFileName, def.SourceSpan);
			//}

			string fileName;
			CodeModel.Span span;
			def.GetLocalFileSpan(out fileName, out span);
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				Shell.OpenDocument(fileName, span);
			}
		}
	}
}
