using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;
using DkTools.CodeModel.Tokens.Operators;

namespace DkTools.StatementCompletion
{
	internal struct ItemData
	{
		public Definition Definition { get; set; }
	}

	public class ProbeAsyncCompletionSource : IAsyncCompletionSource
	{
		private ITextView _textView;
		private Dictionary<CompletionItem, ItemDataNode> _items = new Dictionary<CompletionItem, ItemDataNode>();

		// Completion trigger parameters
		private CompletionMode _mode = CompletionMode.None;
		private string _op;
		private SnapshotPoint _pt;
		private string _fileName;

		internal class ItemDataNode
		{
			public Func<ItemData, object> DescriptionCallback;
			public ItemData ItemData;
		}

		public ProbeAsyncCompletionSource(ITextView textView)
		{
			_textView = textView;
			InitImages();
		}

		enum CompletionMode
		{
			None,
			AfterAssignOrCompare,
			AfterIfDef
		}

		private static readonly Regex _rxTypingTable = new Regex(@"(\w+)\.(\w*)$");
		private static readonly Regex _rxTypingWord = new Regex(@"\w+$");
		public static readonly Regex RxAfterAssignOrCompare = new Regex(@"(==|=|!=|<|<=|>|>=)\s$");
		private static readonly Regex _rxClassFunctionStartBracket = new Regex(@"(\w+)\s*\.\s*(\w+)\s*\($");
		private static readonly Regex _rxFunctionStartBracket = new Regex(@"(\w+)\s*\($");
		private static readonly Regex _rxAfterIfDef = new Regex(@"\#ifn?def\s$");
		private static readonly Regex _rxAfterInclude = new Regex(@"\#include\s+(\<|\"")$");
		private static readonly Regex _rxOrderBy = new Regex(@"\border\s+by\s$");

		public bool TryGetApplicableToSpan(char typedChar, SnapshotPoint triggerPt, out SnapshotSpan applicableToSpan, CancellationToken token)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_mode = CompletionMode.None;

			if (typedChar == ' ')
			{
				Match match;
				var line = triggerPt.Snapshot.GetLineFromPosition(triggerPt.Position);
				var prefix = line.GetTextUpToPosition(triggerPt);

				#region Assignment or Comparison
				if ((match = RxAfterAssignOrCompare.Match(prefix)).Success)
				{
					_mode = CompletionMode.AfterAssignOrCompare;
					applicableToSpan = triggerPt.ToSnapshotSpan();
					_op = match.Groups[1].Value;
					_pt = new SnapshotPoint(line.Snapshot, match.Groups[1].Index + line.Start.Position);
					_fileName = VsTextUtil.TryGetDocumentFileName(_textView.TextBuffer);
					return true;
				}
				#endregion
				#region #ifdef
				else if ((match = _rxAfterIfDef.Match(prefix)).Success)
				{
					_mode = CompletionMode.AfterIfDef;
					applicableToSpan = triggerPt.ToSnapshotSpan();
					_fileName = VsTextUtil.TryGetDocumentFileName(_textView.TextBuffer);
					return true;
				}
				#endregion
			}

			applicableToSpan = new SnapshotSpan(triggerPt.Snapshot, new Span(0, 0));
			return false;

			//var lastCh = prefix.Length > 0 ? prefix[prefix.Length - 1] : '\0';
			/*
			if (lastCh == ' ')
			{
				#region #ifdef
				else if ((match = _rxAfterIfDef.Match(prefix)).Success)
				{
					AddCompletions(completionList, HandleAfterIfDef(completionSpan));
				}
				#endregion
				#region Comma
				else if (prefix.EndsWith(", "))
				{
					AddCompletions(completionList, HandleAfterComma(completionSpan));
				}
				#endregion
				#region After Word
				else if ((match = ProbeCompletionCommandHandler.RxAfterWord.Match(prefix)).Success)
				{
					switch (match.Groups[1].Value)
					{
						case "case":
							AddCompletions(completionList, HandleAfterCase(completionSpan));
							break;
						case "extract":
							AddCompletions(completionList, HandleAfterExtract(completionSpan, match.Groups[1].Value));
							break;
						case "return":
							AddCompletions(completionList, HandleAfterReturn(completionSpan));
							break;
						case "tag":
							AddCompletions(completionList, HandleAfterTag());
							break;
						default:
							AddCompletions(completionList, HandleAfterWord(match.Groups[1].Value, curPos, snapshot));
							break;
					}
				}
				#endregion
				#region After Symbol
				else if ((match = ProbeCompletionCommandHandler.RxAfterSymbol.Match(prefix)).Success)
				{
					AddCompletions(completionList, HandleAfterSymbol(match.Groups[1].Value, curPos, snapshot));
				}
				#endregion
				#region After Number
				else if ((match = ProbeCompletionCommandHandler.RxAfterNumber.Match(prefix)).Success)
				{
					AddCompletions(completionList, HandleAfterSymbol(match.Groups[1].Value, curPos, snapshot));
				}
				#endregion
				#region After String Literal
				else if ((match = ProbeCompletionCommandHandler.RxAfterStringLiteral.Match(prefix)).Success)
				{
					AddCompletions(completionList, HandleAfterStringLiteral(curPos, snapshot));
				}
				#endregion
				#region order by
				else if ((match = _rxOrderBy.Match(prefix)).Success)
				{
					AddCompletions(completionList, HandleAfterOrderBy());
				}
				#endregion
			}
			#region Table.Field
			else if ((match = _rxTypingTable.Match(prefix)).Success)
			{
				completionSpan = new SnapshotSpan(snapshot, linePos + match.Groups[2].Index, match.Groups[2].Length);
				AddCompletions(completionList, HandleDotSeparatedWords(completionSpan, match.Groups[1].Value, match.Groups[2].Value));
			}
			#endregion
			#region Word
			else if ((match = _rxTypingWord.Match(prefix)).Success)
			{
				// Typing a regular word.
				var wordStartPos = linePos + match.Index;
				completionSpan = new SnapshotSpan(snapshot, wordStartPos, match.Length);
				AddCompletions(completionList, GetWordCompletions(curPos, wordStartPos, snapshot));
			}
			#endregion
			#region Class function bracket
			else if ((match = _rxClassFunctionStartBracket.Match(prefix)).Success)
			{
				//completionSpan = new SnapshotSpan(snapshot, linePos + match.Groups[2].Index, match.Groups[2].Length);
				AddCompletions(completionList, HandleAfterMethodArgsStart(completionSpan, match.Groups[1].Value, match.Groups[2].Value));
			}
			#endregion
			#region Function bracket
			else if ((match = _rxFunctionStartBracket.Match(prefix)).Success)
			{
				AddCompletions(completionList, HandleAfterFunctionArgsStart(completionSpan, match.Groups[1].Value));
			}
			#endregion
			#region #include
			else if ((match = _rxAfterInclude.Match(prefix)).Success)
			{
				completionSpan = new SnapshotSpan(snapshot, linePos + match.Groups[1].Index + match.Groups[1].Length, 0);
				AddCompletions(completionList, HandleAfterInclude(completionSpan, match.Groups[1].Value));
			}
			#endregion

			if (completionList.Count > 0)
			{
				var trackingSpan = snapshot.CreateTrackingSpan(completionSpan, SpanTrackingMode.EdgeInclusive);
				completionSets.Add(new CompletionSet("Tokens", "Tokens", trackingSpan, completionList.Values, null));
			}
			*/
		}

		public Task<CompletionContext> GetCompletionContextAsync(InitialTrigger trigger, SnapshotPoint triggerLocation,
			SnapshotSpan applicableToSpan, CancellationToken token)
		{
			_items.Clear();

			switch (_mode)
			{
				case CompletionMode.AfterAssignOrCompare:
					HandleAfterAssignOrCompare(applicableToSpan, _op, _pt);
					break;
				case CompletionMode.AfterIfDef:
					HandleAfterIfDef();
					break;
			}

			return Task<CompletionContext>.FromResult(new CompletionContext(_items.Keys.ToImmutableArray()));
		}

		public Task<object> GetDescriptionAsync(CompletionItem item, CancellationToken token)
		{
			if (_items.TryGetValue(item, out ItemDataNode itemData))
			{
				return Task<object>.FromResult(itemData.DescriptionCallback(itemData.ItemData));
			}

			return Task<object>.FromResult((object)null);
		}

		private void HandleAfterAssignOrCompare(SnapshotSpan completionSpan, string operatorText, SnapshotPoint operatorPt)
		{
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (store != null)
			{
				var model = store.GetCurrentModel(_fileName, completionSpan.Snapshot, "Auto-completion after assign or compare");
				var modelPos = operatorPt.TranslateTo(model.Snapshot, PointTrackingMode.Negative);
				var opToken = model.File.FindDownward<CompletionOperator>(modelPos.Position).LastOrDefault();
				if (opToken != null)
				{
					var dataType = opToken.CompletionDataType;
					if (dataType != null && dataType.HasCompletionOptions)
					{
						foreach (var opt in dataType.CompletionOptions)
						{
							CreateCompletion(opt);
						}
					}
				}
			}
		}

		private void HandleAfterIfDef()
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetCurrentModel(_fileName, _textView.TextBuffer.CurrentSnapshot, "Auto-completion after #ifdef");

				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<ConstantDefinition>())
				{
					CreateCompletion(def);
				}
			}
		}

		#region Completion Creation
		internal void CreateCompletion(string text, ProbeCompletionType type, ItemData itemData, Func<ItemData, object> descriptionCallback)
		{
			var ci = new CompletionItem(text, this, GetImageForCompletionType(type));
			_items.Add(ci, new ItemDataNode
			{
				DescriptionCallback = descriptionCallback,
				ItemData = itemData
			});
		}

		internal void CreateCompletion(Definition def)
		{
			if (!def.CompletionVisible) return;

			CreateCompletion(def.Name, def.CompletionType, new ItemData
			{
				Definition = def
			}, data =>
			{
				return data.Definition.QuickInfoElements;
			});
		}
		#endregion

		#region Icons
		private static ImageElement _functionImg = null;
		private static ImageElement _variableImg = null;
		private static ImageElement _fieldImg = null;
		private static ImageElement _tableImg = null;
		private static ImageElement _constantImg = null;
		private static ImageElement _dataTypeImg = null;
		private static ImageElement _keywordImg = null;
		private static ImageElement _classImg = null;
		private static ImageElement _interfaceImg = null;

		private void InitImages()
		{
			if (_functionImg == null) _functionImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Method));
			if (_variableImg == null) _variableImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.LocalVariable));
			if (_fieldImg == null) _fieldImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Column));
			if (_tableImg == null) _tableImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Table));
			if (_constantImg == null) _constantImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Constant));
			if (_dataTypeImg == null) _dataTypeImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.UserDataType));
			if (_keywordImg == null) _keywordImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Code));
			if (_classImg == null) _classImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Class));
			if (_interfaceImg == null) _interfaceImg = new ImageElement(new ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.Interface));
		}

		private static ImageElement GetImageForCompletionType(ProbeCompletionType type)
		{
			switch (type)
			{
				case ProbeCompletionType.Function:
					return _functionImg;
				case ProbeCompletionType.Constant:
					return _constantImg;
				case ProbeCompletionType.Table:
					return _tableImg;
				case ProbeCompletionType.TableField:
					return _fieldImg;
				case ProbeCompletionType.DataType:
					return _dataTypeImg;
				case ProbeCompletionType.Keyword:
					return _keywordImg;
				case ProbeCompletionType.Class:
					return _classImg;
				case ProbeCompletionType.Interface:
					return _interfaceImg;
				default:
					return null;
			}
		}
		#endregion
	}
}


/* TODO: remove

namespace DkTools.StatementCompletion
{
	internal class ProbeCompletionSource : ICompletionSource
	{

		private IEnumerable<Completion> HandleDotSeparatedWords(SnapshotSpan completionSpan, string word1, string word2)
		{
			// Typing a table.field.

			ThreadHelper.ThrowIfNotOnUIThread();

			// Table and field
			var table = DkDict.Dict.GetTable(word1);
			if (table != null)
			{
				foreach (var def in table.ColumnDefinitions)
				{
					yield return CreateCompletion(def);
				}
			}

			// Relationship and field
			var relInd = DkDict.Dict.GetRelInd(word1);
			if (relInd != null)
			{
				foreach (var def in relInd.ColumnDefinitions)
				{
					yield return CreateCompletion(def);
				}
			}

			// Extract and field
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (store != null)
			{
				var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
				var model = store.GetMostRecentModel(fileName, completionSpan.Snapshot, "Extract table.field completion.");
				var exDef = model.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(word1).FirstOrDefault();
				if (exDef != null)
				{
					foreach (var def in exDef.Fields)
					{
						yield return CreateCompletion(def);
					}
				}
			}

			// Class and method
			var ffScanner = ProbeToolsPackage.Instance.FunctionFileScanner;
			foreach (var cls in ffScanner.CurrentApp.GetClasses(word1))
			{
				foreach (var def in cls.FunctionDefinitions)
				{
					yield return CreateCompletion(def);
				}
			}

			foreach (var permex in ffScanner.CurrentApp.GetPermExs(word1))
			{
				foreach (var field in permex.Fields)
				{
					yield return CreateCompletion(field);
				}
			}

			// Interface and method/property
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
				var model = fileStore.GetMostRecentModel(fileName, completionSpan.Snapshot, "Interface auto-completion");
				var modelSpan = completionSpan.TranslateTo(model.Snapshot, SpanTrackingMode.EdgeInclusive);

				foreach (var def in model.DefinitionProvider.GetAny(modelSpan.Start.Position, word1))
				{
					if (def is VariableDefinition)
					{
						var varDef = def as VariableDefinition;
						if (varDef.AllowsChild)
						{
							foreach (var childDef in varDef.ChildDefinitions) yield return CreateCompletion(childDef);
						}
					}
				}
			}
		}

		

		

		private IEnumerable<Completion> HandleAfterMethodArgsStart(SnapshotSpan completionSpan, string word1, string word2)
		{
            // Starting a new function that belongs to a class or interface.

            ThreadHelper.ThrowIfNotOnUIThread();

			foreach (var opt in GetOptionsForFunctionArg(word1, word2, 0, completionSpan.Start))
			{
				yield return opt;
			}
		}

		private IEnumerable<Completion> HandleAfterFunctionArgsStart(SnapshotSpan completionSpan, string funcName)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (var opt in GetOptionsForFunctionArg(null, funcName, 0, completionSpan.Start))
			{
				yield return opt;
			}
		}

		private IEnumerable<Completion> HandleAfterComma(SnapshotSpan completionSpan)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var snapPt = completionSpan.Start;
			string className;
			string funcName;
			int argIndex;
			if (GetInsideFunction(completionSpan.Snapshot, completionSpan.Start.Position, out className, out funcName, out argIndex))
			{
				foreach (var opt in GetOptionsForFunctionArg(className, funcName, argIndex, snapPt))
				{
					yield return opt;
				}
			}
		}

		private IEnumerable<Completion> HandleAfterReturn(SnapshotSpan completionSpan)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
				var model = fileStore.GetMostRecentModel(fileName, _textBuffer.CurrentSnapshot, "Auto-completion after return");
				var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive);

				var funcDef = model.PreprocessorModel.LocalFunctions.FirstOrDefault(f => f.Definition.EntireSpan.Contains(modelPos));
				var dataType = funcDef.Definition.DataType;
				if (dataType != null && dataType.HasCompletionOptions)
				{
					foreach (var opt in dataType.CompletionOptions)
					{
						yield return CreateCompletion(opt);
					}
				}
			}
		}

		private IEnumerable<Completion> HandleAfterCase(SnapshotSpan completionSpan)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			var model = store.GetMostRecentModel(fileName, _textBuffer.CurrentSnapshot, "Auto-completion after case");
			var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive);

			var switchToken = (from t in model.FindTokens(modelPos) where t is SwitchStatement select t as SwitchStatement).LastOrDefault();
			if (switchToken != null)
			{
				var dt = switchToken.ExpressionDataType;
				if (dt != null && dt.HasCompletionOptions)
				{
					foreach (var opt in dt.CompletionOptions)
					{
						yield return CreateCompletion(opt);
					}
				}
			}
		}

		private IEnumerable<Completion> HandleAfterExtract(SnapshotSpan completionSpan, string permWord)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (string.IsNullOrEmpty(permWord))
			{
				yield return CreateCompletion("permanent", "permanent extract", ProbeCompletionType.Keyword);
			}

			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (store != null)
			{
				var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
				var model = store.GetMostRecentModel(fileName, _textBuffer.CurrentSnapshot, "Auto-completion after 'extract'");

				foreach (var exDef in model.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>())
				{
					yield return CreateCompletion(exDef);
				}
			}
		}

		private IEnumerable<Completion> HandleAfterInclude(SnapshotSpan completionSpan, string startCh)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			string endCh;
			if (startCh == "<") endCh = ">";
			else if (startCh == "\"") endCh = "\"";
			else endCh = string.Empty;

			var curFileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
			var curDir = System.IO.Path.GetDirectoryName(curFileName);

			IEnumerable<string> fileList;
			if (startCh == "<" || string.IsNullOrEmpty(curFileName)) fileList = ProbeEnvironment.GetAllIncludeFiles();
			else fileList = ProbeEnvironment.GetAllIncludeFilesForDir(curDir);

			var retDict = new Dictionary<string, Completion>();

			foreach (var fileName in fileList)
			{
				if (string.IsNullOrWhiteSpace(fileName)) continue;

				// Don't include the current file.
				if (string.Equals(fileName, curFileName, StringComparison.OrdinalIgnoreCase)) continue;

				// Only include files with the right extension.
				var ext = System.IO.Path.GetExtension(fileName).ToLower();
				if (ext.StartsWith(".")) ext = ext.Substring(1);
				if (!Constants.IncludeExtensions.Contains(ext)) continue;

				var titleExt = System.IO.Path.GetFileName(fileName);
				if (!retDict.ContainsKey(titleExt))
				{
					retDict[titleExt] = CreateCompletion(titleExt, titleExt + endCh, fileName, ProbeCompletionType.Constant);
				}
			}

			return retDict.Values;
		}

		private IEnumerable<Completion> HandleAfterOrderBy()
		{
			foreach (var relind in DkDict.Dict.RelInds)
			{
				var def = relind.Definition;
				yield return CreateCompletion(def);
			}
		}

		private IEnumerable<Completion> HandleAfterTag()
		{
			foreach (var name in Constants.TagNames)
			{
				yield return CreateCompletion(name, name, ProbeCompletionType.Keyword);
			}
		}

		private IEnumerable<Completion> HandleAfterWord(string word, int curPos, ITextSnapshot snapshot)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		private IEnumerable<Completion> HandleAfterSymbol(string word, int curPos, ITextSnapshot snapshot)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		private IEnumerable<Completion> HandleAfterNumber(string word, int curPos, ITextSnapshot snapshot)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		private IEnumerable<Completion> HandleAfterStringLiteral(int curPos, ITextSnapshot snapshot)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				GC.SuppressFinalize(this);
				_disposed = true;
			}
		}

		private IEnumerable<Completion> GetOptionsForFunctionArg(string className, string funcName, int argIndex, SnapshotPoint snapPt)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore == null) yield break;

			var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
			var model = fileStore.GetMostRecentModel(fileName, _textBuffer.CurrentSnapshot, "Signature help get options for arg");
			var modelPos = model.AdjustPosition(snapPt);

			var sigInfos = SignatureHelp.ProbeSignatureHelpSource.GetAllSignaturesForFunction(model, modelPos, className, funcName).ToArray();
			if (sigInfos.Length == 0) yield break;
			var sig = sigInfos[0];

			var arg = sig.TryGetArgument(argIndex);
			if (arg == null) yield break;

			var dataType = arg.DataType;
			if (dataType == null) yield break;

			foreach (var option in dataType.CompletionOptions)
			{
				if (!option.CompletionVisible) continue;
				yield return CreateCompletion(option);
			}
		}

		private bool GetInsideFunction(ITextSnapshot snapshot, int pos, out string className, out string funcName, out int argIndex)
		{
			var lineNum = snapshot.GetLineNumberFromPosition(pos);
			var sb = new StringBuilder(snapshot.GetLineTextUpToPosition(pos));

			var rxFuncCall = new Regex(@"(?:;|{|}|(?:(\w+)\s*\.\s*)?(\w+)\s*(\())");	// groups: 1 = class name, 2 = function name, 3 = start bracket

			while (true)
			{
				var parser = new CodeParser(sb.ToString());

				foreach (var match in rxFuncCall.Matches(parser.Source).Cast<Match>().Reverse())
				{
					if (match.Groups[0].Length == 1)
					{
						// Found a character that proves we're not inside a function.
						className = null;
						funcName = null;
						argIndex = 0;
						return false;
					}
					else
					{
						parser.Position = match.Groups[3].Index;	// position of start bracket
						var startPos = parser.Position;
						if (parser.ReadNestable() && parser.Type != CodeType.Nested)
						{
							className = match.Groups[1].Value;
							funcName = match.Groups[2].Value;

							// Count the number of commas between that position and the end.
							parser.Position = startPos;
							var commaCount = 0;
							while (parser.Read())
							{
								if (parser.Text == ",") commaCount++;
							}

							argIndex = commaCount;
							return true;
						}
					}
				}

				lineNum--;
				if (lineNum < 0) break;
				var line = snapshot.GetLineFromLineNumber(lineNum);
				sb.Insert(0, line.GetText() + "\r\n");
			}

			className = null;
			funcName = null;
			argIndex = 0;
			return false;
		}

		private IEnumerable<Completion> GetWordCompletions(int curPos, int wordStartPos, ITextSnapshot snapshot)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore == null) yield break;

			var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
			var model = fileStore.GetMostRecentModel(fileName, snapshot, "Auto-completion get word completions");
			var modelPos = model.AdjustPosition(curPos, snapshot);

			var tokens = model.FindTokens(modelPos).ToArray();

			foreach (var def in model.DefinitionProvider.GlobalsFromAnywhere)
			{
				if (!def.CompletionVisible) continue;
				yield return CreateCompletion(def);
			}

			foreach (var def in model.DefinitionProvider.GetLocal(modelPos))
			{
				if (!def.CompletionVisible) continue;
				yield return CreateCompletion(def);
			}

			// Dictionary definitions are already provided by the DefinitionProvider.

			foreach (var d in Constants.DataTypeKeywords)
			{
				yield return CreateCompletion(d, ProbeCompletionType.DataType);
			}

			// Don't show functions when on the root.
			if (tokens.Any(t => !t.IsOnRoot))
			{
				var ffApp = ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp;
				foreach (var f in ffApp.GlobalDefinitions)
				{
					if (!f.CompletionVisible) continue;
					yield return CreateCompletion(f);
				}
			}

			var bottomToken = tokens.LastOrDefault();
			if (bottomToken != null)
			{
				if (bottomToken.Scope.BreakOwner != null) yield return CreateCompletion("break", ProbeCompletionType.Keyword);
				if (bottomToken.Scope.ContinueOwner != null) yield return CreateCompletion("continue", ProbeCompletionType.Keyword);
			}

			// Global keywords
			foreach (var k in Constants.GlobalKeywords)
			{
				yield return CreateCompletion(k, ProbeCompletionType.Keyword);
			}

			var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var state = Classifier.State.ToStatement(tracker.GetStateForPosition(wordStartPos, snapshot));
			foreach (var keyword in StatementLayout.GetNextPossibleKeywords(state))
			{
				yield return CreateCompletion(keyword, ProbeCompletionType.Keyword);
			}
		}
	}
}
*/
