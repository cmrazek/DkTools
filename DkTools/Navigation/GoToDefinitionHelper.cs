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
		private static readonly Guid _findReferencesPaneGuid = new Guid("18f484e0-b0c1-4c1e-b661-1c5f83ce8f5c");

		public static void TriggerGoToDefinition(ITextView textView)
		{
			try
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
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
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

		public static void TriggerFindReferences(ITextView textView)
		{
			try
			{
				var snapPt = textView.Caret.Position.BufferPosition;

				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(snapPt.Snapshot.TextBuffer);
				if (fileStore == null) return;

				var fullModel = fileStore.CreatePreprocessedModel(snapPt.Snapshot, false, "Find Local References");
				var fullModelPos = fullModel.PreprocessorModel.Source.PrimaryFilePositionToSource(fullModel.AdjustPosition(snapPt));

				var fullToken = fullModel.File.FindDownward(fullModelPos, t => t.SourceDefinition != null).FirstOrDefault();
				if (fullToken == null)
				{
					Shell.SetStatusText("No reference found at cursor.");
					return;
				}

				var def = fullToken.SourceDefinition;

				var pane = StartFindReferences(def.Name);

				var refList = new List<Reference>();

				foreach (var token in fullModel.File.FindDownward(t => t.SourceDefinition == fullToken.SourceDefinition))
				{
					var localFilePos = fullModel.PreprocessorModel.Source.GetFilePosition(token.Span.Start);

					refList.Add(new Reference(localFilePos.FileName, localFilePos.Position, false));
				}

				if (!string.IsNullOrEmpty(def.ExternalRefId))
				{
					refList.AddRange(FindGlobalReferences(def.ExternalRefId));
				}

				EndFindReferences(pane, refList);
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		public static void TriggerFindReferences(string extRefId, string refName)
		{
			var pane = StartFindReferences(refName);
			EndFindReferences(pane, FindGlobalReferences(extRefId));
		}

		private static OutputPane StartFindReferences(string refName)
		{
			var pane = Shell.CreateOutputPane(_findReferencesPaneGuid, Constants.FindReferencesOutputPaneTitle);
			pane.Clear();
			pane.Show();
			pane.WriteLine(string.Format("Finding references of '{0}':", refName));
			return pane;
		}

		private static void EndFindReferences(OutputPane pane, IEnumerable<Reference> refs)
		{
			var refList = refs.ToList();
			refList.Sort();
			Reference.ResolveContext(refList);

			string lastFileName = null;
			int lastLineNumber = -1;
			int lastLineOffset = -1;
			var refCount = 0;

			foreach (var r in refList)
			{
				if (!string.Equals(r.FileName, lastFileName, StringComparison.OrdinalIgnoreCase) ||
					r.LineNumber != lastLineNumber ||
					r.LineOffset != lastLineOffset)
				{
					pane.WriteLine(string.Format("  {0}({1},{2}): {3}", r.FileName, r.LineNumber + 1, r.LineOffset + 1, r.Context, r.Global ? "global" : "local"));
					refCount++;

					lastFileName = r.FileName;
					lastLineNumber = r.LineNumber;
					lastLineOffset = r.LineOffset;
				}
			}

			pane.WriteLine(string.Format("{0} reference(s) found.", refCount));
		}

		private static IEnumerable<Reference> FindGlobalReferences(string extRefId)
		{
			if (string.IsNullOrEmpty(extRefId)) throw new ArgumentException("Definition has no external ref ID.");

			var db = new FunctionFileScanning.FFDatabase();
			using (var cmd = db.CreateCommand(
				"select file_.file_name, ref.pos, alt_file.file_name as true_file_name from ref"
				+ " inner join file_ on file_.id = ref.file_id"
				+ " left outer join alt_file on alt_file.id = ref.true_file_id"
				+ " where ref.ext_ref_id = @ext_ref_id"
				+ " and ref.app_id = @app_id"
			)) {
				cmd.Parameters.AddWithValue("@ext_ref_id", extRefId);
				cmd.Parameters.AddWithValue("@app_id", ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp.Id);

				using (var rdr = cmd.ExecuteReader())
				{
					var ordFileId = rdr.GetOrdinal("file_name");
					var ordPos = rdr.GetOrdinal("pos");
					var ordTrueFileName = rdr.GetOrdinal("true_file_name");

					while (rdr.Read())
					{
						var fileName = rdr.GetString(ordFileId);
						var pos = rdr.GetInt32(ordPos);
						var trueFileName = FunctionFileScanning.FFUtil.GetStringOrNull(rdr, ordTrueFileName);

						if (!string.IsNullOrEmpty(trueFileName)) fileName = trueFileName;

						yield return new Reference(fileName, pos, true);
					}
				}
			}
		}

		private class Reference : IComparable<Reference>
		{
			private string _fileName;
			private int _pos;
			private int _lineNumber;
			private int _lineOffset;
			private string _context;
			private bool _global;

			public Reference(string fileName, int pos, bool global)
			{
				_fileName = fileName;
				_pos = pos;
				_global = global;
			}

			public static void ResolveContext(IEnumerable<Reference> refs)
			{
				var list = refs.OrderBy(x => x._fileName.ToLower()).ToArray();

				var curFileName = string.Empty;
				var content = string.Empty;

				foreach (var item in list)
				{
					if (!string.Equals(item._fileName, curFileName))
					{
						curFileName = item._fileName;
						if (System.IO.File.Exists(curFileName)) content = System.IO.File.ReadAllText(curFileName);
						else content = string.Empty;
					}

					item._lineNumber = GetLineNumberForPos(content, item._pos, out item._context, out item._lineOffset);
				}
			}

			private static int GetLineNumberForPos(string content, int filePos, out string context, out int lineOffset)
			{
				if (filePos < 0 || filePos > content.Length)
				{
					context = string.Empty;
					lineOffset = 0;
					return 0;
				}

				var lineNumber = 0;
				var pos = 0;
				var len = content.Length;
				char ch;
				var lineStartPos = 0;

				while (pos < filePos && pos < len)
				{
					ch = content[pos];
					if (ch == '\n')
					{
						lineNumber++;
						lineStartPos = pos + 1;
					}
					pos++;
				}

				lineOffset = pos - lineStartPos;

				var endLine = content.IndexOfAny(new char[] { '\r', '\n' }, lineStartPos);
				if (endLine < 0) context = content.Substring(lineStartPos);
				else context = content.Substring(lineStartPos, endLine - lineStartPos);

				return lineNumber;
			}

			public string FileName
			{
				get { return _fileName; }
			}

			public int LineNumber
			{
				get { return _lineNumber; }
			}

			public string Context
			{
				get { return _context; }
			}

			public int Position
			{
				get { return _pos; }
			}

			public int LineOffset
			{
				get { return _lineOffset; }
			}

			public bool Global
			{
				get { return _global; }
			}

			public int CompareTo(Reference other)
			{
				var ret = string.Compare(_fileName, other._fileName);
				if (ret != 0) return ret;

				return _pos.CompareTo(other._pos);
			}
		}
	}
}
