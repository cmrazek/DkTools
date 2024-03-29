﻿using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling.Tokens;
using DkTools.Classifier;
using DkTools.CodeModeling;
using DkTools.QuickInfo;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DK.Modeling;

namespace DkTools.StatementCompletion
{
	internal struct ItemData
	{
		public Definition Definition { get; set; }
		public string Description { get; set; }
	}

	public class ProbeAsyncCompletionSource : IAsyncCompletionSource
	{
		public const string CompletionTypeProperty_Include = "DkTools.CompletionType.Include";

		private ITextView _textView;
		private Dictionary<CompletionItem, ItemDataNode> _items = new Dictionary<CompletionItem, ItemDataNode>();
		private HashSet<string> _itemNames = new HashSet<string>();
		private string _fileName;
		private DkAppSettings _appSettings;

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

		internal DkAppSettings AppSettings => _appSettings;

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
				if (TryGetApplicableToSpan(trigger.Character, triggerLocation, out SnapshotSpan applicableToSpan))
				{
					return new CompletionStartData(CompletionParticipation.ProvidesItems, applicableToSpan);
				}
			}

			return new CompletionStartData();
		}

		public bool TryGetApplicableToSpan(char typedChar, SnapshotPoint triggerPt, out SnapshotSpan applicableToSpan)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!NoCompletionChars.Contains(typedChar))
			{
				_mode = CompletionMode.None;
				_fileName = VsTextUtil.TryGetDocumentFileName(_textView.TextBuffer);
				_appSettings = ProbeToolsPackage.Instance.App.Settings;

				var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textView.TextBuffer);
				var state = liveCodeTracker.GetStateForPosition(triggerPt);
				if (LiveCodeTracker.IsStateInLiveCode(state))
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
				}
				#region #include
				else if (typedChar == '"' && (state & LiveCodeTracker.State_IncludeStringLiteral) != 0)
				{
					_mode = CompletionMode.Include;
					applicableToSpan = triggerPt.ToSnapshotSpan();
					_params.str = "\"";
					return true;
				}
				else if (typedChar == '<' && (state & LiveCodeTracker.State_IncludeAngleLiteral) != 0)
                {
					_mode = CompletionMode.Include;
					applicableToSpan = triggerPt.ToSnapshotSpan();
					_params.str = "<";
					return true;
                }
                #endregion
            }

            applicableToSpan = new SnapshotSpan(triggerPt.Snapshot, new Span(0, 0));
			return false;
		}

		public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerPt,
			SnapshotSpan applicableToSpan, CancellationToken cancel)
		{
			_items.Clear();
			_itemNames.Clear();

			switch (_mode)
			{
				case CompletionMode.AfterAssignOrCompare:
					HandleAfterAssignOrCompare(_params.pt);
					break;
				case CompletionMode.AfterIfDef:
					HandleAfterIfDef();
					break;
				case CompletionMode.AfterComma:
					HandleAfterComma(applicableToSpan, ProbeToolsPackage.Instance.App.Settings);
					break;
				case CompletionMode.AfterCase:
					HandleAfterCase(applicableToSpan);
					break;
				case CompletionMode.AfterExtract:
					HandleAfterExtract(_params.str);
					break;
				case CompletionMode.AfterReturn:
					HandleAfterReturn(applicableToSpan);
					break;
				case CompletionMode.AfterTag:
					HandleAfterTag();
					break;
				case CompletionMode.AfterWord:
					HandleAfterWord(triggerPt, _params.snapshot, _fileName, cancel);
					break;
				case CompletionMode.AfterSymbol:
					HandleAfterSymbol(triggerPt, _fileName, cancel);
					break;
				case CompletionMode.AfterNumber:
					HandleAfterSymbol(triggerPt, _fileName, cancel);
					break;
				case CompletionMode.AfterStringLiteral:
					HandleAfterStringLiteral(triggerPt, _fileName, cancel);
					break;
				case CompletionMode.AfterOrderBy:
					HandleAfterOrderBy();
					break;
				case CompletionMode.DotSeparatedWords:
					HandleDotSeparatedWords(applicableToSpan, _params.str);
					break;
				case CompletionMode.Word:
					GetWordCompletions(triggerPt, _params.pt, _fileName, cancel);
					break;
				case CompletionMode.ClassFunction:
					HandleAfterMethodArgsStart(_params.str, _params.str2, ProbeToolsPackage.Instance.App.Settings);
					break;
				case CompletionMode.Function:
					HandleAfterFunctionArgsStart(_params.str, ProbeToolsPackage.Instance.App.Settings);
					break;
				case CompletionMode.Include:
					if (!session.Properties.ContainsProperty(CompletionTypeProperty_Include)) session.Properties.AddProperty(CompletionTypeProperty_Include, string.Empty);
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
		private void HandleAfterAssignOrCompare(SnapshotPoint operatorPt)
		{
			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model?.Snapshot is ITextSnapshot modelSnapshot)
			{
				var modelPos = operatorPt.TranslateTo(modelSnapshot, PointTrackingMode.Negative);
				var opToken = model.File.FindDownward<CompletionOperator>(modelPos.Position).LastOrDefault();
				if (opToken != null)
				{
					var dataType = opToken.CompletionDataType;
					if (dataType != null && dataType.HasCompletionOptions)
					{
						foreach (var opt in dataType.GetCompletionOptions(_appSettings))
						{
							CreateCompletion(opt);
						}
					}
				}
			}
		}

		private void HandleAfterIfDef()
		{
			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model != null)
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<ConstantDefinition>())
				{
					CreateCompletion(def);
				}
			}
		}

		#region After Comma
		private void HandleAfterComma(SnapshotSpan completionSpan, DkAppSettings appSettings)
		{
			var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textView.TextBuffer);
			var funcResult = liveCodeTracker.FindContainingFunctionCall(completionSpan.Start, appSettings);

            if (funcResult.Success)
            {
				var argDataType = funcResult.Definition.ArgumentsSignature.TryGetArgument(funcResult.ArgumentIndex)?.DataType;
				if (argDataType != null && argDataType.HasCompletionOptions)
                {
                    foreach (var option in argDataType.GetCompletionOptions(appSettings))
                    {
						CreateCompletion(option);
					}
                }
			}
		}
		#endregion

		private void HandleAfterCase(SnapshotSpan completionSpan)
		{
			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model != null)
			{
				if (model.Snapshot is ITextSnapshot modelSnapshot)
				{
					var modelPos = completionSpan.Start.TranslateTo(modelSnapshot, PointTrackingMode.Positive);

					var switchToken = (from t in model.FindTokens(modelPos) where t is SwitchStatement select t as SwitchStatement).LastOrDefault();
					if (switchToken != null)
					{
						var dt = switchToken.ExpressionDataType;
						if (dt != null && dt.HasCompletionOptions)
						{
							foreach (var opt in dt.GetCompletionOptions(_appSettings))
							{
								CreateCompletion(opt);
							}
						}
					}
				}
			}
		}

		private void HandleAfterExtract(string permWord)
		{
			if (string.IsNullOrEmpty(permWord))
			{
				CreateCompletion("permanent", ProbeCompletionType.Keyword, "permanent extract");
			}

			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model != null)
            {
				foreach (var exDef in model.DefinitionProvider.GetGlobalFromFile<ExtractTableDefinition>())
				{
					CreateCompletion(exDef);
				}
            }
		}

		private void HandleAfterReturn(SnapshotSpan completionSpan)
		{
			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model?.Snapshot is ITextSnapshot modelSnapshot)
			{
				var modelPos = completionSpan.Start.TranslateTo(modelSnapshot, PointTrackingMode.Positive);

				var funcDef = model.PreprocessorModel.LocalFunctions.FirstOrDefault(f => f.Definition.EntireSpan.Contains(modelPos));
				if (funcDef != null && funcDef.Definition != null)
				{
					var dataType = funcDef.Definition.DataType;
					if (dataType != null && dataType.HasCompletionOptions)
					{
						foreach (var opt in dataType.GetCompletionOptions(_appSettings))
						{
							CreateCompletion(opt);
						}
					}
				}
			}
		}

		private void HandleAfterTag()
		{
			foreach (var name in DK.Constants.TagNames)
			{
				CreateCompletion(name, ProbeCompletionType.Keyword, name);
			}
		}

		private void HandleAfterWord(SnapshotPoint pt, ITextSnapshot snapshot, string fileName, CancellationToken cancel)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(pt, snapshot, fileName, _appSettings, cancel));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterSymbol(SnapshotPoint triggerPt, string fileName, CancellationToken cancel)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(triggerPt, fileName, _appSettings, cancel));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterStringLiteral(SnapshotPoint triggerPt, string fileName, CancellationToken cancel)
		{
			var tracker = TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var stmt = State.ToStatement(tracker.GetStateForPosition(triggerPt, fileName, _appSettings, cancel));
			StatementLayout.GetCompletionsAfterToken(stmt, this);
		}

		private void HandleAfterOrderBy()
		{
			foreach (var relind in _appSettings.Dict.RelInds)
			{
				CreateCompletion(relind.Definition);
			}
		}

		private void GetOptionsForFunctionArg(string className, string funcName, int argIndex, DkAppSettings appSettings)
		{
			DataType argDataType;
			if (className != null)
            {
				argDataType = FileStoreHelper.GetDefinitionProviderOrNull(_textView.TextBuffer)?.GetGlobalFromAnywhere(className)
					.Where(x => x.AllowsChild)
					.SelectMany(x => x.GetChildDefinitions(funcName, appSettings))
					.Where(x => x.ArgumentsRequired)
					.FirstOrDefault()
					?.ArgumentsSignature
					?.TryGetArgument(argIndex)
					?.DataType;
            }
			else
            {
				argDataType = FileStoreHelper.GetDefinitionProviderOrNull(_textView.TextBuffer)?.GetGlobalFromAnywhere(funcName)
					.Where(x => x.ArgumentsRequired)
					.FirstOrDefault()
					?.ArgumentsSignature
					?.TryGetArgument(argIndex)
					?.DataType;
            }

			if (argDataType == null) return;
			if (!argDataType.HasCompletionOptions) return;
					
			foreach (var completionOption in argDataType.GetCompletionOptions(appSettings))
            {
				CreateCompletion(completionOption);
            }
		}

        private void HandleDotSeparatedWords(SnapshotSpan completionSpan, string word1)
        {
            // Typing a table.field.

            // Table and field
            var table = _appSettings.Dict.GetTable(word1);
            if (table != null)
            {
                foreach (var def in table.ColumnDefinitions)
                {
                    CreateCompletion(def);
                }
            }

            // Relationship and field
            var relInd = _appSettings.Dict.GetRelInd(word1);
            if (relInd != null)
            {
                foreach (var def in relInd.ColumnDefinitions)
                {
                    CreateCompletion(def);
                }
            }

            // Extract and field
			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model != null)
            {
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
            var appSettings = ProbeToolsPackage.Instance.App.Settings;
            if (appSettings != null)
            {
                foreach (var cls in appSettings.Repo.GetClassDefinitions(word1))
                {
                    foreach (var def in cls.Functions)
                    {
                        CreateCompletion(def);
                    }
                }

                foreach (var permex in appSettings.Repo.GetPermanentExtractDefinitions(word1))
                {
                    foreach (var field in permex.Fields)
                    {
                        CreateCompletion(field);
                    }
                }
            }

			// Interface and method/property
            if (model != null)
            {
				if (model.Snapshot is ITextSnapshot modelSnapshot)
                {
                    var modelSpan = completionSpan.TranslateTo(modelSnapshot, SpanTrackingMode.EdgeInclusive);

                    foreach (var def in model.DefinitionProvider.GetAny(modelSpan.Start.Position, word1))
                    {
                        if (def is VariableDefinition)
                        {
                            var varDef = def as VariableDefinition;
                            if (varDef.AllowsChild)
                            {
                                foreach (var childDef in varDef.GetChildDefinitions(appSettings))
                                {
                                    CreateCompletion(childDef);
                                }
                            }
                        }
                    }
                }
            }
        }

		private void GetWordCompletions(SnapshotPoint triggerPt, SnapshotPoint wordStartPt, string fileName, CancellationToken cancel)
		{
			var model = FileStoreHelper.GetOrCreateForTextBuffer(_textView.TextBuffer)?.Model;
			if (model == null) return;

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

			foreach (var d in DK.Constants.DataTypeKeywords)
			{
				CreateCompletion(d, ProbeCompletionType.DataType, null);
			}

			var bottomToken = tokens.LastOrDefault();
			if (bottomToken != null)
			{
				if (bottomToken.HasBreakParent) CreateCompletion("break", ProbeCompletionType.Keyword, null);
				if (bottomToken.HasContinueParent) CreateCompletion("continue", ProbeCompletionType.Keyword, null);
			}

			// Global keywords
			foreach (var k in DK.Constants.GlobalKeywords)
			{
				CreateCompletion(k, ProbeCompletionType.Keyword, null);
			}

			var tracker = Classifier.TextBufferStateTracker.GetTrackerForTextBuffer(_textView.TextBuffer);
			var state = Classifier.State.ToStatement(tracker.GetStateForPosition(wordStartPt, fileName, _appSettings, cancel));
			foreach (var keyword in StatementLayout.GetNextPossibleKeywords(state))
			{
				CreateCompletion(keyword, ProbeCompletionType.Keyword, null);
			}
		}

		private void HandleAfterMethodArgsStart(string word1, string word2, DkAppSettings appSettings)
		{
			// Starting a new function that belongs to a class or interface.

			GetOptionsForFunctionArg(word1, word2, 0, appSettings);
		}

		private void HandleAfterFunctionArgsStart(string funcName, DkAppSettings appSettings)
		{
			GetOptionsForFunctionArg(null, funcName, 0, appSettings);
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
				if (!DK.Constants.IncludeExtensions.Contains(ext)) continue;

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
			foreach (var directive in DK.Constants.PreprocessorDirectives)
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
				data => { return data.Description != null ? DefinitionHelper.QuickInfoMainLine_VS(data.Description) : null; });
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
