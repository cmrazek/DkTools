﻿using DK;
using DK.Code;
using DK.Definitions;
using DK.Modeling.Tokens;
using DkTools.CodeModeling;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DkTools.Navigation
{
	public static class GoToDefinitionHelper
	{
		private static readonly Guid _findReferencesPaneGuid = new Guid("18f484e0-b0c1-4c1e-b661-1c5f83ce8f5c");

		public static void TriggerGoToDefinition(ITextView textView, CancellationToken cancel)
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				var caretPtTest = textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
				if (!caretPtTest.HasValue) return;
				var caretPt = caretPtTest.Value;

				var def = GetDefinitionAtPoint(caretPt, cancel);
				if (def != null && !def.FilePosition.IsEmpty)
				{
					Shell.OpenDocument(def.FilePosition);
				}
			}
			catch (Exception ex)
			{
				Shell.ShowError(ex);
			}
		}

		internal static Definition GetDefinitionAtPoint(SnapshotPoint point, CancellationToken cancel, bool realCodeOnly = false)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(point.Snapshot.TextBuffer);
			if (fileStore == null) return null;

			var appSettings = ProbeToolsPackage.Instance.App.Settings;
			if (appSettings == null) return null;

			var model = fileStore.Model;
			if (model == null) return null;

			var modelPos = model.AdjustPosition(point.Position, point.Snapshot);
			var selTokens = model.File.FindDownwardTouching(modelPos).ToArray();
			if (selTokens.Length == 0) return null;

			var defToken = selTokens.LastOrDefault(t => t.SourceDefinition != null);
			if (defToken != null && defToken.SourceDefinition != null)
			{
				var def = defToken.SourceDefinition;

				if (def is FunctionDefinition)
				{
					var funcDef = def as FunctionDefinition;

					var funcList = new List<FunctionDefinition>();
					funcList.Add(funcDef);

					if (funcDef.Extern)
					{
						foreach (var def2 in appSettings.Repo.SearchForFunctionDefinitions(funcDef.Name))
						{
							if (def2 == def || (def2.SourceFileName == def.SourceFileName && def2.SourceStartPos == def.SourceStartPos))
							{
								continue;
							}
							funcList.Add(def2);
						}

						// If there is one true function definition and the list, and the others are externs, then just go straight to the true definition.
						if (funcList.Count > 1 &&
							funcList.Count(x => !x.Extern) == 1 &&
							funcList.Count(x => x.Extern) == funcList.Count - 1)
						{
							funcList = funcList.Where(x => !x.Extern).ToList();
						}
					}

					return PromptDefinitions(funcList);
				}

				return def;
			}

			if (!realCodeOnly)
			{
				var includeToken = selTokens.LastOrDefault(t => t is IncludeToken) as IncludeToken;
				if (includeToken != null)
				{
					var pathName = includeToken.FullPathName;
					if (!string.IsNullOrEmpty(pathName))
					{
						return new FilePositionDefinition(new FilePosition(pathName, 0));
					}
				}
			}

			return null;
		}

		internal static bool BrowseToDefinition(Definition def)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (def != null && !def.FilePosition.IsEmpty)
			{
				Shell.OpenDocument(def.FilePosition);
			}

			return false;
		}

		private static Definition PromptDefinitions(IEnumerable<Definition> defs)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var defList = defs.ToArray();
			if (defList.Length == 0) return null;
			if (defList.Length == 1) return defList[0];

			var dlg = new DefinitionPickerWindow();
			dlg.Owner = System.Windows.Application.Current.MainWindow;
			dlg.Definitions = defList;
			if (dlg.ShowDialog() == true)
			{
				var selDef = dlg.SelectedItem;
				if (selDef != null) return selDef;
			}

			return null;
		}

		public static void TriggerFindReferences(ITextView textView, CancellationToken cancel)
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				var snapPt = textView.Caret.Position.BufferPosition;

				var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(snapPt.Snapshot.TextBuffer);
				if (fileStore == null) return;

				var appSettings = ProbeToolsPackage.Instance.App.Settings;
				var fileName = VsTextUtil.TryGetDocumentFileName(textView.TextBuffer);
				var fullModel = fileStore.CreatePreprocessedModelSync(appSettings, fileName, snapPt.Snapshot, visible: false, "Find Local References", cancel);
				var fullModelPos = fullModel.PreprocessorModel.Source.PrimaryFilePositionToSource(fullModel.AdjustPosition(snapPt));

				var fullToken = fullModel.File.FindDownward(fullModelPos, t => t.SourceDefinition != null).FirstOrDefault();
				if (fullToken == null)
				{
					var model = fileStore.Model;
					if (model == null)
                    {
						ProbeToolsPackage.Instance.SetStatusText("No code model available.");
						return;
                    }

					var modelPos = model.AdjustPosition(snapPt);
					var token = model.File.FindDownward(modelPos, t => t.SourceDefinition != null).FirstOrDefault();
					if (token == null)
					{
						ProbeToolsPackage.Instance.SetStatusText("No reference found at cursor.");
						return;
					}

					fullToken = token;
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
					foreach (var globalRef in FindGlobalReferences(def.ExternalRefId))
					{
						if (refList.Any(x => x.FileName.EqualsI(globalRef.FileName) && x.Position == globalRef.Position)) continue;
						refList.Add(globalRef);
					}
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
			ThreadHelper.ThrowIfNotOnUIThread();

			var pane = StartFindReferences(refName);
			EndFindReferences(pane, FindGlobalReferences(extRefId));
		}

		private static OutputPane StartFindReferences(string refName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

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

			var app = ProbeToolsPackage.Instance.App.Settings;
			if (app == null) yield break;

			foreach (var rf in app.Repo.FindAllReferences(extRefId))
			{
				yield return new Reference(rf.FileName, rf.Position, global: true);
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
