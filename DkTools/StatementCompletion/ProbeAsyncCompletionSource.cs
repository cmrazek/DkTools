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
using DkTools.Classifier;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;
using DkTools.CodeModel.Tokens.Operators;

namespace DkTools.StatementCompletion
{
	internal struct ItemData
	{
		public Definition Definition { get; set; }
		public string Description { get; set; }
	}

	public class ProbeAsyncCompletionSource : IAsyncCompletionSource
	{
		private ITextView _textView;
		private Dictionary<CompletionItem, ItemDataNode> _items = new Dictionary<CompletionItem, ItemDataNode>();
		private HashSet<string> _itemNames = new HashSet<string>();
		private string _fileName;
		private ProbeAppSettings _appSettings;

		// Completion trigger parameters
		private CompletionMode _mode = CompletionMode.None;
		private TriggerParams _params = new TriggerParams();
		private class TriggerParams
		{
			public string str;
			public string str2;
			public SnapshotPoint pt;
			public ITextSnapshot snapshot;
		}
		
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
			AfterIfDef,
			AfterComma,
			AfterCase,
			AfterExtract,
			AfterReturn,
			AfterTag,
			AfterWord,
			AfterSymbol,
			AfterNumber,
			AfterStringLiteral,
			AfterOrderBy,
			DotSeparatedWords,
			Word,
			ClassFunction,
			Function,
			Include,
			Preprocessor
		}

		private static readonly Regex _rxTypingTable = new Regex(@"(\w+)\.(\w*)$");
		private static readonly Regex _rxTypingWord = new Regex(@"\w+$");
		private static readonly Regex _rxAfterAssignOrCompare = new Regex(@"(==|=|!=|<|<=|>|>=)\s$");
		private static readonly Regex _rxAfterWord = new Regex(@"\b(\w+)\s$");
		private static readonly Regex _rxClassFunctionStartBracket = new Regex(@"(\w+)\s*\.\s*(\w+)\s*\($");
		private static readonly Regex _rxFunctionStartBracket = new Regex(@"(\w+)\s*\($");
		private static readonly Regex _rxAfterIfDef = new Regex(@"\#ifn?def\s$");
		private static readonly Regex _rxAfterInclude = new Regex(@"\#include\s+(\<|\"")$");
		private static readonly Regex _rxOrderBy = new Regex(@"\border\s+by\s$");
		private static readonly Regex _rxAfterSymbol = new Regex(@"(\*|,|\(|\))\s$");
		private static readonly Regex _rxAfterNumber = new Regex(@"(\d+)\s$");
		private static readonly Regex _rxAfterStringLiteral = new Regex(@"""\s$");

		public static readonly char[] NoCompletionChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-' };

		public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (trigger.Reason == CompletionTriggerReason.Insertion)
			{
				if (TryGetApplicableToSpan(trigger.Character, triggerLocation, out SnapshotSpan applicableToSpan, token))
				{
					return new CompletionStartData(CompletionParticipation.ProvidesItems, applicableToSpan);
				}
			}

			return new CompletionStartData();
		}

		public bool TryGetApplicableToSpan(char typedChar, SnapshotPoint triggerPt, out SnapshotSpan applicableToSpan, CancellationToken token)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!NoCompletionChars.Contains(typedChar))
			{
				_mode = CompletionMode.None;
				_fileName = VsTextUtil.TryGetDocumentFileName(_textView.TextBuffer);
				_appSettings = ProbeEnvironment.CurrentAppSettings;

				var state = triggerPt.GetQuickState();
				if (QuickState.IsInLiveCode(state))
				{
					Match match;
					var line = triggerPt.Snapshot.GetLineFromPosition(triggerPt.Position);
					var prefix = line.GetTextUpToPosition(triggerPt);

					if (typedChar == ' ')
					{
						#region Assignment or Comparison
						if ((match = _rxAfterAssignOrCompare.Match(prefix)).Success)
						{
							_mode = CompletionMode.AfterAssignOrCompare;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							_params.str = match.Groups[1].Value;
							_params.pt = new SnapshotPoint(line.Snapshot, match.Groups[1].Index + line.Start.Position);
							return true;
						}
						#endregion
						#region #ifdef
						else if ((match = _rxAfterIfDef.Match(prefix)).Success)
						{
							_mode = CompletionMode.AfterIfDef;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							return true;
						}
						#endregion
						#region Comma
						else if (prefix.EndsWith(", "))
						{
							_mode = CompletionMode.AfterComma;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							return true;
						}
						#endregion
						#region order by
						else if ((match = _rxOrderBy.Match(prefix)).Success)
						{
							_mode = CompletionMode.AfterOrderBy;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							return true;
						}
						#endregion
						#region After Word
						else if ((match = _rxAfterWord.Match(prefix)).Success)
						{
							switch (match.Groups[1].Value)
							{
								case "case":
									_mode = CompletionMode.AfterCase;
									applicableToSpan = triggerPt.ToSnapshotSpan();
									return true;
								case "extract":
									_mode = CompletionMode.AfterExtract;
									applicableToSpan = triggerPt.ToSnapshotSpan();
									_params.str = match.Groups[1].Value;
									return true;
								case "return":
									_mode = CompletionMode.AfterReturn;
									applicableToSpan = triggerPt.ToSnapshotSpan();
									return true;
								case "tag":
									_mode = CompletionMode.AfterTag;
									applicableToSpan = triggerPt.ToSnapshotSpan();
									return true;
								default:
									_mode = CompletionMode.AfterWord;
									applicableToSpan = triggerPt.ToSnapshotSpan();
									_params.str = match.Groups[1].Value;
									_params.snapshot = triggerPt.Snapshot;
									return true;
							}
						}
						#endregion
						#region After Symbol
						else if ((match = _rxAfterSymbol.Match(prefix)).Success)
						{
							_mode = CompletionMode.AfterSymbol;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							return true;
						}
						#endregion
						#region After Number
						else if ((match = _rxAfterNumber.Match(prefix)).Success)
						{
							_mode = CompletionMode.AfterNumber;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							return true;
						}
						#endregion
						#region After String Literal
						else if ((match = _rxAfterStringLiteral.Match(prefix)).Success)
						{
							_mode = CompletionMode.AfterStringLiteral;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							return true;
						}
						#endregion
					}
					#region Preprocessor
					else if (typedChar == '#')
					{
						_mode = CompletionMode.Preprocessor;
						applicableToSpan = new SnapshotSpan(triggerPt.Snapshot, triggerPt.Position - 1, 1);
						return true;
					}
					#endregion
					#region Table.Field
					else if ((match = _rxTypingTable.Match(prefix)).Success)
					{
						_mode = CompletionMode.DotSeparatedWords;
						applicableToSpan = new SnapshotSpan(triggerPt.Snapshot, match.Groups[2].Index + line.Start.Position, match.Groups[2].Length);
						_params.str = match.Groups[1].Value;
						_params.str2 = match.Groups[2].Value;
						return true;
					}
					#endregion
					#region Word
					else if ((match = _rxTypingWord.Match(prefix)).Success)
					{
						// Typing a regular word.
						_mode = CompletionMode.Word;
						_params.pt = new SnapshotPoint(triggerPt.Snapshot, line.Start.Position + match.Index);
						applicableToSpan = new SnapshotSpan(_params.pt, match.Length);
						return true;
					}
					#endregion
					#region Class function bracket
					else if ((match = _rxClassFunctionStartBracket.Match(prefix)).Success)
					{
						_mode = CompletionMode.ClassFunction;
						applicableToSpan = triggerPt.ToSnapshotSpan();
						_params.str = match.Groups[1].Value;
						_params.str2 = match.Groups[2].Value;
						return true;
					}
					#endregion
					#region Function bracket
					else if ((match = _rxFunctionStartBracket.Match(prefix)).Success)
					{
						_mode = CompletionMode.Function;
						applicableToSpan = triggerPt.ToSnapshotSpan();
						_params.str = match.Groups[1].Value;
						return true;
					}
					#endregion
					#region #include
					else if ((match = _rxAfterInclude.Match(prefix)).Success)
					{
						_mode = CompletionMode.Include;
						applicableToSpan = triggerPt.ToSnapshotSpan();
						_params.str = match.Groups[1].Value;
						return true;
					}
					#endregion
				}
				else
				{
					if ((state & QuickState.StringLiteral) != 0)
					{
						Match match;
						var line = triggerPt.Snapshot.GetLineFromPosition(triggerPt.Position);
						var prefix = line.GetTextUpToPosition(triggerPt);

						#region #include (for string literal)
						if ((match = _rxAfterInclude.Match(prefix)).Success)
						{
							_mode = CompletionMode.Include;
							applicableToSpan = triggerPt.ToSnapshotSpan();
							_params.str = match.Groups[1].Value;
							return true;
						}
						#endregion
					}
				}
			}

			applicableToSpan = new SnapshotSpan(triggerPt.Snapshot, new Span(0, 0));
			return false;
		}

		public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerPt,
			SnapshotSpan applicableToSpan, CancellationToken token)
		{
			_items.Clear();
			_itemNames.Clear();

			switch (_mode)
			{
				case CompletionMode.AfterAssignOrCompare:
					HandleAfterAssignOrCompare(applicableToSpan, _params.str, _params.pt, _fileName);
					break;
				case CompletionMode.AfterIfDef:
					HandleAfterIfDef(_fileName);
					break;
				case CompletionMode.AfterComma:
					HandleAfterComma(applicableToSpan, _fileName);
					break;
				case CompletionMode.AfterCase:
					HandleAfterCase(applicableToSpan, _fileName);
					break;
				case CompletionMode.AfterExtract:
					HandleAfterExtract(applicableToSpan, _params.str, _fileName);
					break;
				case CompletionMode.AfterReturn:
					HandleAfterReturn(applicableToSpan, _fileName);
					break;
				case CompletionMode.AfterTag:
					HandleAfterTag();
					break;
				case CompletionMode.AfterWord:
					HandleAfterWord(_params.str, triggerPt, _params.snapshot, _fileName);
					break;
				case CompletionMode.AfterSymbol:
					HandleAfterSymbol(triggerPt, _fileName);
					break;
				case CompletionMode.AfterNumber:
					HandleAfterSymbol(triggerPt, _fileName);
					break;
				case CompletionMode.AfterStringLiteral:
					HandleAfterStringLiteral(triggerPt, _fileName);
					break;
				case CompletionMode.AfterOrderBy:
					HandleAfterOrderBy();
					break;
				case CompletionMode.DotSeparatedWords:
					HandleDotSeparatedWords(applicableToSpan, _params.str, _params.str2, _fileName);
					break;
				case CompletionMode.Word:
					GetWordCompletions(triggerPt, _params.pt, _fileName);
					break;
				case CompletionMode.ClassFunction:
					HandleAfterMethodArgsStart(triggerPt, _params.str, _params.str2, _fileName);
					break;
				case CompletionMode.Function:
					HandleAfterFunctionArgsStart(triggerPt, _params.str, _fileName);
					break;
				case CompletionMode.Include:
					HandleAfterInclude(_params.str, _fileName);
					break;
				case CompletionMode.Preprocessor:
					HandlePreprocessor();
					break;
			}

			return Task<CompletionContext>.FromResult(new CompletionContext(_items.Keys.OrderBy(i => i.SortText.ToLower()).ToImmutableArray()));
		}

		public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
		{
			if (_items.TryGetValue(item, out ItemDataNode itemData))
			{
				return Task<object>.FromResult(itemData.DescriptionCallback(itemData.ItemData));
			}

			return Task<object>.FromResult((object)null);
		}

		#region Completion Fulfillment Logic
		private void HandleAfterAssignOrCompare(SnapshotSpan completionSpan, string operatorText, SnapshotPoint operatorPt, string fileName)
		{
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (store != null)
			{
				var model = store.GetCurrentModel(_appSettings, fileName, completionSpan.Snapshot, "Auto-completion after assign or compare");
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

		private void HandleAfterIfDef(string fileName)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetCurrentModel(_appSettings, fileName, _textView.TextBuffer.CurrentSnapshot, "Auto-completion after #ifdef");

				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<ConstantDefinition>())
				{
					CreateCompletion(def);
				}
			}
		}

		#region After Comma
		private void HandleAfterComma(SnapshotSpan completionSpan, string fileName)
		{
			var snapPt = completionSpan.Start;
			string className;
			string funcName;
			int argIndex;
			if (GetInsideFunction(completionSpan.Snapshot, completionSpan.Start.Position, out className, out funcName, out argIndex))
			{
				GetOptionsForFunctionArg(fileName, className, funcName, argIndex, snapPt);
			}
		}

		private bool GetInsideFunction(ITextSnapshot snapshot, int pos, out string className, out string funcName, out int argIndex)
		{
			var lineNum = snapshot.GetLineNumberFromPosition(pos);
			var sb = new StringBuilder(snapshot.GetLineTextUpToPosition(pos));

			var rxFuncCall = new Regex(@"(?:;|{|}|(?:(\w+)\s*\.\s*)?(\w+)\s*(\())");    // groups: 1 = class name, 2 = function name, 3 = start bracket

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
						parser.Position = match.Groups[3].Index;    // position of start bracket
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
		#endregion

		private void HandleAfterCase(SnapshotSpan completionSpan, string fileName)
		{
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			var model = store.GetMostRecentModel(_appSettings, fileName, _textView.TextBuffer.CurrentSnapshot, "Auto-completion after case");
			var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive);

			var switchToken = (from t in model.FindTokens(modelPos) where t is SwitchStatement select t as SwitchStatement).LastOrDefault();
			if (switchToken != null)
			{
				var dt = switchToken.ExpressionDataType;
				if (dt != null && dt.HasCompletionOptions)
				{
					foreach (var opt in dt.CompletionOptions)
					{
						CreateCompletion(opt);
					}
				}
			}
		}

		private void HandleAfterExtract(SnapshotSpan completionSpan, string permWord, string fileName)
		{
			if (string.IsNullOrEmpty(permWord))
			{
				CreateCompletion("permanent", ProbeCompletionType.Keyword, "permanent extract");
			}

			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (store != null)
			{
				var model = store.GetMostRecentModel(_appSettings, fileName, _textView.TextBuffer.CurrentSnapshot, "Auto-completion after 'extract'");

				foreach (var exDef in model.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>())
				{
					CreateCompletion(exDef);
				}
			}
		}

		private void HandleAfterReturn(SnapshotSpan completionSpan, string fileName)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(_appSettings, fileName, _textView.TextBuffer.CurrentSnapshot, "Auto-completion after return");
				var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive);

				var funcDef = model.PreprocessorModel.LocalFunctions.FirstOrDefault(f => f.Definition.EntireSpan.Contains(modelPos));
				if (funcDef != null && funcDef.Definition != null)
				{
					var dataType = funcDef.Definition.DataType;
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

		private void HandleAfterTag()
		{
			foreach (var name in Constants.TagNames)
			{
				CreateCompletion(name, ProbeCompletionType.Keyword, name);
			}
		}

		private void HandleAfterWord(string word, int curPos, ITextSnapshot snapshot, string fileName)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot, fileName, _appSettings));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterSymbol(SnapshotPoint triggerPt, string fileName)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(triggerPt, fileName, _appSettings));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterNumber(SnapshotPoint triggerPt, string fileName)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(triggerPt, fileName, _appSettings));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterStringLiteral(SnapshotPoint triggerPt, string fileName)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(triggerPt, fileName, _appSettings));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterOrderBy()
		{
			foreach (var relind in DkDict.Dict.RelInds)
			{
				CreateCompletion(relind.Definition);
			}
		}

		private void GetOptionsForFunctionArg(string fileName, string className, string funcName, int argIndex, SnapshotPoint snapPt)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (fileStore == null) return;

			var model = fileStore.GetMostRecentModel(_appSettings, fileName, _textView.TextBuffer.CurrentSnapshot, "Signature help get options for arg");
			var modelPos = model.AdjustPosition(snapPt);

			var sigInfos = SignatureHelp.ProbeSignatureHelpSource.GetAllSignaturesForFunction(model, modelPos, className, funcName).ToArray();
			if (sigInfos.Length == 0) return;
			var sig = sigInfos[0];

			var arg = sig.TryGetArgument(argIndex);
			if (arg == null) return;

			var dataType = arg.DataType;
			if (dataType == null) return;

			foreach (var option in dataType.CompletionOptions)
			{
				CreateCompletion(option);
			}
		}

		private void HandleDotSeparatedWords(SnapshotSpan completionSpan, string word1, string word2, string fileName)
		{
			// Typing a table.field.

			// Table and field
			var table = DkDict.Dict.GetTable(word1);
			if (table != null)
			{
				foreach (var def in table.ColumnDefinitions)
				{
					CreateCompletion(def);
				}
			}

			// Relationship and field
			var relInd = DkDict.Dict.GetRelInd(word1);
			if (relInd != null)
			{
				foreach (var def in relInd.ColumnDefinitions)
				{
					CreateCompletion(def);
				}
			}

			// Extract and field
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (store != null)
			{
				var model = store.GetMostRecentModel(_appSettings, fileName, completionSpan.Snapshot, "Extract table.field completion.");
				var exDef = model.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>(word1).FirstOrDefault();
				if (exDef != null)
				{
					foreach (var def in exDef.Fields)
					{
						CreateCompletion(def);
					}
				}
			}

			// Class and method
			var ds = DefinitionStore.Current;
			if (ds != null)
			{
				foreach (var cls in ds.GetClasses(word1))
				{
					foreach (var def in cls.Functions)
					{
						CreateCompletion(def);
					}
				}

				foreach (var permex in ds.GetPermanentExtracts(word1))
				{
					foreach (var field in permex.Fields)
					{
						CreateCompletion(field);
					}
				}
			}

			// Interface and method/property
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(_appSettings, fileName, completionSpan.Snapshot, "Interface auto-completion");
				var modelSpan = completionSpan.TranslateTo(model.Snapshot, SpanTrackingMode.EdgeInclusive);

				foreach (var def in model.DefinitionProvider.GetAny(modelSpan.Start.Position, word1))
				{
					if (def is VariableDefinition)
					{
						var varDef = def as VariableDefinition;
						if (varDef.AllowsChild)
						{
							foreach (var childDef in varDef.ChildDefinitions)
							{
								CreateCompletion(childDef);
							}
						}
					}
				}
			}
		}

		private void GetWordCompletions(SnapshotPoint triggerPt, SnapshotPoint wordStartPt, string fileName)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
			if (fileStore == null) return;

			var model = fileStore.GetMostRecentModel(_appSettings, fileName, triggerPt.Snapshot, "Auto-completion get word completions");
			var modelPos = model.AdjustPosition(triggerPt);

			var tokens = model.FindTokens(modelPos).ToArray();

			foreach (var def in model.DefinitionProvider.GlobalsFromAnywhere)
			{
				CreateCompletion(def);
			}

			foreach (var def in model.DefinitionProvider.GetLocal(modelPos))
			{
				CreateCompletion(def);
			}

			// Dictionary definitions are already provided by the DefinitionProvider.

			foreach (var d in Constants.DataTypeKeywords)
			{
				CreateCompletion(d, ProbeCompletionType.DataType, null);
			}

			// Don't show functions when on the root.
			if (tokens.Any(t => !t.IsOnRoot))
			{
				var ds = DefinitionStore.Current;
				if (ds != null)
				{
					foreach (var f in ds.GlobalDefinitions)
					{
						CreateCompletion(f);
					}
				}
			}

			var bottomToken = tokens.LastOrDefault();
			if (bottomToken != null)
			{
				if (bottomToken.Scope.BreakOwner != null) CreateCompletion("break", ProbeCompletionType.Keyword, null);
				if (bottomToken.Scope.ContinueOwner != null) CreateCompletion("continue", ProbeCompletionType.Keyword, null);
			}

			// Global keywords
			foreach (var k in Constants.GlobalKeywords)
			{
				CreateCompletion(k, ProbeCompletionType.Keyword, null);
			}

			var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var state = Classifier.State.ToStatement(tracker.GetStateForPosition(wordStartPt, fileName, _appSettings));
			foreach (var keyword in StatementLayout.GetNextPossibleKeywords(state))
			{
				CreateCompletion(keyword, ProbeCompletionType.Keyword, null);
			}
		}

		private void HandleAfterMethodArgsStart(SnapshotPoint triggerPt, string word1, string word2, string fileName)
		{
			// Starting a new function that belongs to a class or interface.

			GetOptionsForFunctionArg(fileName, word1, word2, 0, triggerPt);
		}

		private void HandleAfterFunctionArgsStart(SnapshotPoint triggerPt, string funcName, string fileName)
		{
			GetOptionsForFunctionArg(fileName, null, funcName, 0, triggerPt);
		}

		private void HandleAfterInclude(string startCh, string curFileName)
		{
			string endCh;
			if (startCh == "<") endCh = ">";
			else if (startCh == "\"") endCh = "\"";
			else endCh = string.Empty;

			var curDir = System.IO.Path.GetDirectoryName(curFileName);

			IEnumerable<string> fileList;
			if (startCh == "<" || string.IsNullOrEmpty(curFileName)) fileList = _appSettings.IncludeFiles;
			else fileList = _appSettings.GetAllIncludeFilesForDir(curDir);

			var retDict = new Dictionary<string, string>();

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
					retDict[titleExt] = fileName;
				}
			}

			foreach (var fileName in retDict.Values)
			{
				CreateCompletion(System.IO.Path.GetFileName(fileName), ProbeCompletionType.Constant, fileName);
			}
		}

		private void HandlePreprocessor()
		{
			foreach (var directive in Constants.PreprocessorDirectives)
			{
				CreateCompletion(directive, ProbeCompletionType.Keyword, null);
			}
		}
		#endregion

		#region Completion Creation
		internal void CreateCompletion(string text, ProbeCompletionType type, ItemData itemData, Func<ItemData, object> descriptionCallback)
		{
			if (_itemNames.Contains(text)) return;

			var ci = new CompletionItem(text, this, GetImageForCompletionType(type));
			_items.Add(ci, new ItemDataNode
			{
				DescriptionCallback = descriptionCallback,
				ItemData = itemData
			});
			_itemNames.Add(text);
		}

		internal void CreateCompletion(Definition def)
		{
			if (!def.CompletionVisible) return;

			CreateCompletion(def.Name, def.CompletionType, new ItemData
			{
				Definition = def
			}, data =>
			{
				return data.Definition.QuickInfo.GenerateElements_VS();
			});
		}

		internal void CreateCompletion(string text, ProbeCompletionType type, string description)
		{
			CreateCompletion(text, type, new ItemData { Description = description },
				data => { return data.Description != null ? Definition.QuickInfoMainLine_VS(data.Description) : null; });
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
