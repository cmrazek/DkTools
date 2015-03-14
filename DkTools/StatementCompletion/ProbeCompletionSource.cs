using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;
using DkTools.Classifier;

namespace DkTools.StatementCompletion
{
	internal enum CompletionType
	{
		Function,
		Variable,
		Constant,
		DataType,
		Table,
		TableField,
		Keyword,
		Class,
		Interface
	}

	internal class ProbeCompletionSource : ICompletionSource
	{
		private ProbeCompletionSourceProvider _sourceProvider;
		private ITextBuffer _textBuffer;
		private bool _disposed = false;

		private static ImageSource _functionImg = null;
		private static ImageSource _variableImg = null;
		private static ImageSource _fieldImg = null;
		private static ImageSource _tableImg = null;
		private static ImageSource _constantImg = null;
		private static ImageSource _dataTypeImg = null;
		private static ImageSource _keywordImg = null;
		private static ImageSource _classImg = null;
		private static ImageSource _interfaceImg = null;

		public ProbeCompletionSource(ProbeCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
		{
			_sourceProvider = sourceProvider;
			_textBuffer = textBuffer;

			if (_functionImg == null) _functionImg = BitmapToBitmapSource(Res.FunctionImg);
			if (_variableImg == null) _variableImg = BitmapToBitmapSource(Res.VariableImg);
			if (_fieldImg == null) _fieldImg = BitmapToBitmapSource(Res.FieldImg);
			if (_tableImg == null) _tableImg = BitmapToBitmapSource(Res.TableImg);
			if (_constantImg == null) _constantImg = BitmapToBitmapSource(Res.ConstantImg);
			if (_dataTypeImg == null) _dataTypeImg = BitmapToBitmapSource(Res.DataTypeImg);
			if (_keywordImg == null) _keywordImg = BitmapToBitmapSource(Res.KeywordImg);
			if (_classImg == null) _classImg = BitmapToBitmapSource(Res.ClassImg);
			if (_interfaceImg == null) _interfaceImg = BitmapToBitmapSource(Res.InterfaceImg);
		}

		private static ImageSource GetImageForCompletionType(CompletionType type)
		{
			switch (type)
			{
				case CompletionType.Function:
					return _functionImg;
				case CompletionType.Constant:
					return _constantImg;
				case CompletionType.Table:
					return _tableImg;
				case CompletionType.TableField:
					return _fieldImg;
				case CompletionType.DataType:
					return _dataTypeImg;
				case CompletionType.Keyword:
					return _keywordImg;
				case CompletionType.Class:
					return _classImg;
				case CompletionType.Interface:
					return _interfaceImg;
				default:
					return _variableImg;
			}
		}

		public static Completion CreateCompletion(string text, CompletionType type)
		{
			var img = GetImageForCompletionType(type);
			return new Completion(text, text, string.Empty, img, string.Empty);
		}

		public static Completion CreateCompletion(string text, string description, CompletionType type)
		{
			var img = GetImageForCompletionType(type);
			return new Completion(text, text, description, img, string.Empty);
		}

		public static Completion CreateCompletion(string text, string insertionText, string description, CompletionType type)
		{
			var img = GetImageForCompletionType(type);
			return new Completion(text, insertionText, description, img, string.Empty);
		}

		public static Completion CreateCompletion(Definition def)
		{
			if (!def.CompletionVisible) return null;
			return CreateCompletion(def.Name, def.QuickInfoTextStr, def.CompletionType);
		}

		public static IEnumerable<Completion> CreateCompletions(IEnumerable<string> strings, CompletionType complType)
		{
			foreach (var str in strings)
			{
				yield return CreateCompletion(str, str, complType);
			}
		}

		private ImageSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
		{
			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero,
				System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
		}

		private static readonly Regex _rxTypingTable = new Regex(@"(\w+)\.(\w*)$");
		private static readonly Regex _rxTypingWord = new Regex(@"\w+$");
		private static readonly Regex _rxAfterAssignOrCompare = new Regex(@"(==|=|!=)\s$");
		private static readonly Regex _rxClassFunctionStartBracket = new Regex(@"(\w+)\s*\.\s*(\w+)\s*\($");
		private static readonly Regex _rxFunctionStartBracket = new Regex(@"(\w+)\s*\($");
		private static readonly Regex _rxAfterIfDef = new Regex(@"\#ifn?def\s$");
		private static readonly Regex _rxAfterInclude = new Regex(@"\#include\s+(\<|\"")$");
		private static readonly Regex _rxOrderBy = new Regex(@"\border\s+by\s$");

		void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			//var completionList = new Dictionary<string, Completion>();
			var completionList = new SortedDictionary<string, Completion>();

			var snapPt = session.TextView.Caret.Position.BufferPosition;
			var curPos = snapPt.Position;
			var snapshot = session.TextView.TextBuffer.CurrentSnapshot;
			var prefix = snapshot.GetLineTextUpToPosition(curPos);
			var linePos = snapshot.GetLineFromPosition(curPos).Start.Position;

			var completionSpan = new SnapshotSpan(snapshot, curPos, 0);
			Match match;

			var lastCh = prefix.Length > 0 ? prefix[prefix.Length - 1] : '\0';

			if (lastCh == ' ')
			{
				#region Assignment or Comparison
				if ((match = _rxAfterAssignOrCompare.Match(prefix)).Success)
				{
					var operatorText = match.Groups[1].Value;
					AddCompletions(completionList, HandleAfterAssignOrCompare(completionSpan, operatorText));
				}
				#endregion
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
				completionSpan = new SnapshotSpan(snapshot, linePos + match.Groups[1].Index, match.Groups[1].Length);
				AddCompletions(completionList, HandleAfterInclude(completionSpan, match.Groups[1].Value));
			}
			#endregion

			if (completionList.Count > 0)
			{
				var trackingSpan = snapshot.CreateTrackingSpan(completionSpan, SpanTrackingMode.EdgeInclusive);
				completionSets.Add(new CompletionSet("Tokens", "Tokens", trackingSpan, completionList.Values, null));
			}
		}

		void AddCompletions(SortedDictionary<string, Completion> dict, IEnumerable<Completion> completions)
		{
			foreach (var completion in completions)
			{
				if (completion == null) continue;
				if (!dict.ContainsKey(completion.DisplayText)) dict[completion.DisplayText] = completion;
			}
		}

		private IEnumerable<Completion> HandleDotSeparatedWords(SnapshotSpan completionSpan, string word1, string word2)
		{
			// Typing a table.field.

			// Table and field
			var table = ProbeEnvironment.GetTable(word1);
			if (table != null)
			{
				foreach (var def in table.FieldDefinitions)
				{
					yield return CreateCompletion(def);
				}
			}

			// Relationship and field
			var relInd = ProbeEnvironment.GetRelInd(word1);
			if (relInd != null)
			{
				foreach (var def in relInd.FieldDefinitions)
				{
					yield return CreateCompletion(def);
				}
			}

			// Extract and field
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (store != null)
			{
				var model = store.GetMostRecentModel(completionSpan.Snapshot, "Extract table.field completion.");
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

			// Interface and method/property
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(completionSpan.Snapshot, "Interface auto-completion");
				var modelSpan = completionSpan.TranslateTo(model.Snapshot, SpanTrackingMode.EdgeInclusive);

				foreach (var def in model.DefinitionProvider.GetAny(modelSpan.Start.Position, word1))
				{
					if (def is VariableDefinition)
					{
						var varDef = def as VariableDefinition;
						foreach (var def2 in varDef.DataType.MethodsAndProperties)
						{
							yield return CreateCompletion(def2);
						}
					}
				}
			}
		}

		private IEnumerable<Completion> HandleAfterAssignOrCompare(SnapshotSpan completionSpan, string operatorText)
		{
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (store != null)
			{
				var model = store.GetCurrentModel(completionSpan.Snapshot, "Auto-completion after assign or compare");
				var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive).Position;
				var parentToken = (from t in model.FindTokens(modelPos)
								   where t is GroupToken && t.Span.Start < modelPos
								   select t as GroupToken).LastOrDefault();
				if (parentToken != null)
				{
					var opToken = parentToken.FindLastChildBeforeOffset(modelPos);
					if (opToken != null && opToken is OperatorToken && opToken.Text == operatorText)
					{
						var prevToken = parentToken.FindPreviousSibling(opToken);

						if (prevToken != null)
						{
							var dt = prevToken.ValueDataType;
							if (dt != null && dt.HasCompletionOptions)
							{
								foreach (var opt in dt.CompletionOptions)
								{
									yield return CreateCompletion(opt);
								}
							}
						}
					}
				}
			}
		}

		private IEnumerable<Completion> HandleAfterIfDef(SnapshotSpan completionSpan)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetCurrentModel(_textBuffer.CurrentSnapshot, "Auto-completion after #ifdef");

				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<ConstantDefinition>())
				{
					if (!def.CompletionVisible) continue;
					yield return CreateCompletion(def);
				}
			}
		}

		private IEnumerable<Completion> HandleAfterMethodArgsStart(SnapshotSpan completionSpan, string word1, string word2)
		{
			// Starting a new function that belongs to a class or interface.

			foreach (var opt in GetOptionsForFunctionArg(word1, word2, 0, completionSpan.Start))
			{
				yield return opt;
			}
		}

		private IEnumerable<Completion> HandleAfterFunctionArgsStart(SnapshotSpan completionSpan, string funcName)
		{
			foreach (var opt in GetOptionsForFunctionArg(null, funcName, 0, completionSpan.Start))
			{
				yield return opt;
			}
		}

		private IEnumerable<Completion> HandleAfterComma(SnapshotSpan completionSpan)
		{
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
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(_textBuffer.CurrentSnapshot, "Auto-completion after return");
				var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive);

				var funcDef = model.PreprocessorModel.LocalFunctions.FirstOrDefault(f => f.EntireSpan.Contains(modelPos));
				var dataType = funcDef.DataType;
				if (dataType != null)
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
			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			var model = store.GetMostRecentModel(_textBuffer.CurrentSnapshot, "Auto-completion after case");
			var modelPos = completionSpan.Start.TranslateTo(model.Snapshot, PointTrackingMode.Positive);

			var switchToken = (from t in model.FindTokens(modelPos) where t is SwitchToken select t as SwitchToken).LastOrDefault();
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
			if (string.IsNullOrEmpty(permWord))
			{
				yield return CreateCompletion("permanent", "permanent extract", CompletionType.Keyword);
			}

			var store = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (store != null)
			{
				var model = store.GetMostRecentModel(_textBuffer.CurrentSnapshot, "Auto-completion after 'extract'");

				foreach (var exDef in model.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>())
				{
					yield return CreateCompletion(exDef);
				}
			}
		}

		private IEnumerable<Completion> HandleAfterInclude(SnapshotSpan completionSpan, string startCh)
		{
			string endCh;
			if (startCh == "<") endCh = ">";
			else if (startCh == "\"") endCh = "\"";
			else endCh = string.Empty;

			var curFileName = _textBuffer.TryGetFileName();
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
					retDict[titleExt] = CreateCompletion(titleExt, titleExt + endCh, fileName, CompletionType.Constant);
				}
			}

			return retDict.Values;
		}

		private IEnumerable<Completion> HandleAfterOrderBy()
		{
			foreach (var relind in ProbeEnvironment.RelInds)
			{
				var def = relind.Definition;
				yield return CreateCompletion(def);
			}
		}

		private IEnumerable<Completion> HandleAfterTag()
		{
			foreach (var name in Constants.TagNames)
			{
				yield return CreateCompletion(name, name, CompletionType.Keyword);
			}
		}

		private IEnumerable<Completion> HandleAfterWord(string word, int curPos, ITextSnapshot snapshot)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		private IEnumerable<Completion> HandleAfterSymbol(string word, int curPos, ITextSnapshot snapshot)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		private IEnumerable<Completion> HandleAfterNumber(string word, int curPos, ITextSnapshot snapshot)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(curPos, snapshot));
			return StatementLayout.GetCompletionsAfterToken(stmt);
		}

		private IEnumerable<Completion> HandleAfterStringLiteral(int curPos, ITextSnapshot snapshot)
		{
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
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore == null) yield break;
			var model = fileStore.GetMostRecentModel(_textBuffer.CurrentSnapshot, "Signature help get options for arg");
			var modelPos = model.AdjustPosition(snapPt);

			var sigInfos = SignatureHelp.ProbeSignatureHelpSource.GetAllSignaturesForFunction(model, modelPos, className, funcName).ToArray();
			if (sigInfos.Length == 0) yield break;
			var sig = sigInfos[0].Signature;

			var argText = GetArgumentText(sig, argIndex);
			if (string.IsNullOrWhiteSpace(argText)) yield break;

			var editPos = model.AdjustPosition(snapPt);
			var parentToken = model.FindTokens(editPos, t => t is GroupToken).LastOrDefault() as GroupToken;

			var argParser = new TokenParser.Parser(argText);
			var dataType = CodeModel.DataType.Parse(new DataType.ParseArgs
			{
				Code = argParser,
				DataTypeCallback = dataTypeName =>
				{
					var def = model.DefinitionProvider.GetLocal<DataTypeDefinition>(editPos, dataTypeName).FirstOrDefault();
					if (def != null) return def;

					return model.DefinitionProvider.GetGlobalFromAnywhere<DataTypeDefinition>(dataTypeName).FirstOrDefault();
				},
				VariableCallback = varName =>
				{
					var def = model.DefinitionProvider.GetLocal<VariableDefinition>(editPos, varName).FirstOrDefault();
					if (def != null) return def;

					return model.DefinitionProvider.GetGlobalFromAnywhere<VariableDefinition>(varName).FirstOrDefault();
				}
			});
			if (dataType != null)
			{
				foreach (var opt in dataType.CompletionOptions)
				{
					if (!opt.CompletionVisible) continue;
					yield return CreateCompletion(opt);
				}
			}
		}

		private bool GetInsideFunction(ITextSnapshot snapshot, int pos, out string className, out string funcName, out int argIndex)
		{
			var lineNum = snapshot.GetLineNumberFromPosition(pos);
			var sb = new StringBuilder(snapshot.GetLineTextUpToPosition(pos));

			var rxFuncCall = new Regex(@"(?:;|{|}|(?:(\w+)\s*\.\s*)?(\w+)\s*(\())");	// groups: 1 = class name, 2 = function name, 3 = start bracket

			while (true)
			{
				var parser = new TokenParser.Parser(sb.ToString());

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
						if (parser.ReadNestable() && parser.TokenType != TokenParser.TokenType.Nested)
						{
							className = match.Groups[1].Value;
							funcName = match.Groups[2].Value;

							// Count the number of commas between that position and the end.
							parser.Position = startPos;
							var commaCount = 0;
							while (parser.Read())
							{
								if (parser.TokenText == ",") commaCount++;
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
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore == null) yield break;
			var model = fileStore.GetMostRecentModel(snapshot, "Auto-completion get word completions");
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
			//foreach (var t in ProbeEnvironment.DictDefinitions)
			//{
			//	if (!t.CompletionVisible) continue;
			//	yield return CreateCompletion(t);
			//}

			foreach (var d in Constants.DataTypeKeywords)
			{
				yield return CreateCompletion(d, CompletionType.DataType);
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

			// Global keywords
			foreach (var k in Constants.GlobalKeywords)
			{
				yield return CreateCompletion(k, CompletionType.Keyword);
			}

			var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(_textBuffer);
			var state = Classifier.State.ToStatement(tracker.GetStateForPosition(wordStartPos, snapshot));
			foreach (var keyword in StatementLayout.GetNextPossibleKeywords(state))
			{
				yield return CreateCompletion(keyword, CompletionType.Keyword);
			}
		}

		public string GetArgumentText(string sig, int argIndex)
		{
			var args = SignatureHelp.ProbeSignatureHelpSource.GetSignatureArguments(sig).ToArray();
			if (argIndex < 0 || argIndex >= args.Length) return null;

			return args[argIndex].Text;
		}
	}
}
