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
		Class
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
		}

		private Completion CreateCompletion(string text, string description, CompletionType type)
		{
			ImageSource img;
			switch (type)
			{
				case CompletionType.Function:
					img = _functionImg;
					break;
				case CompletionType.Constant:
					img = _constantImg;
					break;
				case CompletionType.Table:
					img = _tableImg;
					break;
				case CompletionType.TableField:
					img = _fieldImg;
					break;
				case CompletionType.DataType:
					img = _dataTypeImg;
					break;
				case CompletionType.Keyword:
					img = _keywordImg;
					break;
				case CompletionType.Class:
					img = _classImg;
					break;
				default:
					img = _variableImg;
					break;
			}

			return new Completion(text, text, description, img, string.Empty);
		}

		private Completion CreateCompletion(Definition def)
		{
			return CreateCompletion(def.Name, def.QuickInfoText, def.CompletionType);
		}

		private IEnumerable<Completion> CreateCompletions(IEnumerable<string> strings, CompletionType complType)
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

		private Regex _rxTypingTable = new Regex(@"(\w{1,8})\.(\w*)$");
		private Regex _rxTypingWord = new Regex(@"\w+$");
		private Regex _rxAfterAssignOrCompare = new Regex(@"(==|=|!=)\s$");
		private Regex _rxClassFunctionStartBracket = new Regex(@"(\w+)\s*\.\s*(\w+)\s*\($");
		private Regex _rxFunctionStartBracket = new Regex(@"(\w+)\s*\($");
		private Regex _rxReturn = new Regex(@"\breturn\s$");
		private Regex _rxCase = new Regex(@"\bcase\s$");
		private Regex _rxAfterIfDef = new Regex(@"\#ifn?def\s$");

		void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			var completionList = new Dictionary<string, Completion>();

			var snapPt = session.TextView.Caret.Position.BufferPosition;
			var curPos = snapPt.Position;
			var snapshot = session.TextView.TextBuffer.CurrentSnapshot;
			var prefix = snapshot.GetLineTextUpToPosition(curPos);
			var linePos = snapshot.GetLineFromPosition(curPos).Start.Position;

			var completionSpan = new Microsoft.VisualStudio.Text.Span(curPos, 0);
			Match match;

			if ((match = _rxTypingTable.Match(prefix)).Success)
			{
				// Typing a table.field.

				var tableName = match.Groups[1].Value;
				var table = ProbeEnvironment.GetTable(match.Groups[1].Value);
				if (table != null)
				{
					completionSpan = new Microsoft.VisualStudio.Text.Span(linePos + match.Groups[2].Index, match.Groups[2].Length);
					foreach (var def in table.FieldDefinitions) completionList[def.Name] = CreateCompletion(def);
				}

				var ffScanner = ProbeToolsPackage.Instance.FunctionFileScanner;
				var cls = ffScanner.GetClass(tableName);
				if (cls != null)
				{
					completionSpan = new Microsoft.VisualStudio.Text.Span(linePos + match.Groups[2].Index, match.Groups[2].Length);
					foreach (var def in cls.FunctionDefinitions)
					{
						if (!completionList.ContainsKey(def.Name)) completionList[def.Name] = CreateCompletion(def);
					}
				}
			}
			else if ((match = _rxTypingWord.Match(prefix)).Success)
			{
				// Typing a regular word.
				completionSpan = new Microsoft.VisualStudio.Text.Span(linePos + match.Index, match.Length);
				foreach (var comp in GetWordCompletions(curPos, snapshot)) completionList[comp.DisplayText] = comp;
			}
			else if ((match = _rxAfterAssignOrCompare.Match(prefix)).Success)
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(snapshot, "Auto-completion after assign or compare");
				var modelPos = model.AdjustPosition(curPos, snapshot);
				var parentToken = (from t in model.FindTokens(modelPos)
								   where t is GroupToken && t.Span.Start < curPos
								   select t as GroupToken).LastOrDefault();
				if (parentToken != null)
				{
					var opToken = parentToken.FindLastChildBeforeOffset(modelPos);
					if (opToken != null && opToken is OperatorToken && opToken.Text == match.Groups[1].Value)
					{
						var prevToken = parentToken.FindPreviousSibling(opToken);

						if (prevToken != null)
						{
							var dt = prevToken.ValueDataType;
							if (dt != null && dt.HasCompletionOptions)
							{
								foreach (var opt in dt.CompletionOptions) completionList[opt.Name] = CreateCompletion(opt);
							}
						}
					}
				}
			}
			else if ((match = _rxAfterIfDef.Match(prefix)).Success)
			{
				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
				var model = fileStore.GetCurrentModel(_textBuffer.CurrentSnapshot, "Auto-completion after #ifdef");

				foreach (var def in model.GetDefinitions<ConstantDefinition>())
				{
					completionList[def.Name] = CreateCompletion(def);
				}
			}
			else if ((match = _rxClassFunctionStartBracket.Match(prefix)).Success)
			{
				// Starting a new function that belongs to a class

				var cls = ProbeToolsPackage.Instance.FunctionFileScanner.GetClass(match.Groups[1].Value);
				if (cls != null)
				{
					var className = match.Groups[1].Value;
					var funcName = match.Groups[2].Value;
					foreach (var opt in GetOptionsForFunctionArg(className, funcName, 0, snapPt)) completionList[opt.DisplayText] = opt;
				}
			}
			else if ((match = _rxFunctionStartBracket.Match(prefix)).Success)
			{
				// Starting a new function.

				var funcName = match.Groups[1].Value;
				foreach (var opt in GetOptionsForFunctionArg(null, funcName, 0, snapPt)) completionList[opt.DisplayText] = opt;
			}
			else if (prefix.EndsWith(", "))
			{
				// Moving to next argument in function.

				string funcName;
				int argIndex;
				if (GetInsideFunction(snapshot, curPos, out funcName, out argIndex))
				{
					foreach (var opt in GetOptionsForFunctionArg(null, funcName, argIndex, snapPt)) completionList[opt.DisplayText] = opt;
				}
			}
			else if ((match = _rxReturn.Match(prefix)).Success)
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(_textBuffer.CurrentSnapshot, "Auto-completion after return");
				var modelPos = model.AdjustPosition(curPos, snapshot);

				var funcToken = model.FindTokens(modelPos).LastOrDefault(x => x is CodeModel.Tokens.FunctionToken) as CodeModel.Tokens.FunctionToken;
				if (funcToken != null)
				{
					var dataType = funcToken.DataTypeToken;
					if (dataType != null && dataType is CodeModel.Tokens.IDataTypeToken)
					{
						foreach (var opt in (dataType as CodeModel.Tokens.IDataTypeToken).DataType.CompletionOptions) completionList[opt.Name] = CreateCompletion(opt);
					}
				}
			}
			else if ((match = _rxCase.Match(prefix)).Success)
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(_textBuffer.CurrentSnapshot, "Auto-completion after case");
				var modelPos = model.AdjustPosition(curPos, snapshot);

				var switchToken = (from t in model.FindTokens(modelPos) where t is SwitchToken select t as SwitchToken).LastOrDefault();
				if (switchToken != null)
				{
					var dt = switchToken.ExpressionDataType;
					if (dt != null && dt.HasCompletionOptions)
					{
						foreach (var opt in dt.CompletionOptions) completionList[opt.Name] = CreateCompletion(opt);
					}
				}
			}

			if (completionList.Count > 0)
			{
				var trackingSpan = snapshot.CreateTrackingSpan(completionSpan, SpanTrackingMode.EdgeInclusive);
				completionSets.Add(new CompletionSet("Tokens", "Tokens", trackingSpan,
					(from c in completionList.Values orderby c.DisplayText select c),
					null));
			}
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
			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(_textBuffer.CurrentSnapshot, "Signature help get options for arg");

			string sig;
			if (!string.IsNullOrEmpty(className))
			{
				var func = ProbeToolsPackage.Instance.FunctionFileScanner.GetFunction(className, funcName);
				if (func == null) yield break;
				sig = func.Signature;
				if (string.IsNullOrWhiteSpace(sig)) yield break;
			}
			else
			{
				sig = SignatureHelp.ProbeSignatureHelpSource.GetAllSignaturesForFunction(model, funcName).FirstOrDefault();
				if (string.IsNullOrEmpty(sig)) yield break;
			}

			var argText = GetArgumentText(sig, argIndex);
			if (string.IsNullOrWhiteSpace(argText)) yield break;

			var editPos = model.GetPosition(snapPt);
			var parentToken = model.FindTokens(editPos, t => t is GroupToken).LastOrDefault() as GroupToken;

			var argParser = new TokenParser.Parser(argText);
			var dataType = CodeModel.DataType.Parse(argParser, null, model, parentToken);
			if (dataType != null)
			{
				foreach (var opt in dataType.CompletionOptions)
				{
					yield return CreateCompletion(opt);
				}
			}
		}

		private bool GetInsideFunction(ITextSnapshot snapshot, int pos, out string funcName, out int argIndex)
		{
			var lineNum = snapshot.GetLineNumberFromPosition(pos);
			var sb = new StringBuilder(snapshot.GetLineTextUpToPosition(pos));

			var rxFuncCall = new Regex(@"(\w+)\s*(\()");

			while (true)
			{
				var parser = new TokenParser.Parser(sb.ToString());

				foreach (var match in rxFuncCall.Matches(parser.Source).Cast<Match>().Reverse())
				{
					parser.Position = match.Groups[2].Index;
					var startPos = parser.Position;
					if (parser.ReadNestable() && parser.TokenType != TokenParser.TokenType.Nested)
					{
						funcName = match.Groups[1].Value;

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

				lineNum--;
				if (lineNum < 0) break;
				var line = snapshot.GetLineFromLineNumber(lineNum);
				sb.Insert(0, line.GetText() + "\r\n");
			}

			funcName = null;
			argIndex = 0;
			return false;
		}

		private IEnumerable<Completion> GetWordCompletions(int curPos, ITextSnapshot snapshot)
		{
			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetMostRecentModel(snapshot, "Auto-completion get word completions");
			var modelPos = model.AdjustPosition(curPos, snapshot);

			var tokens = model.FindTokens(modelPos).ToArray();

			if (tokens.Length > 0)
			{
				var lastToken = tokens.Last();

				foreach (var def in lastToken.GetDefinitions())
				{
					if (def.CompletionVisible) yield return CreateCompletion(def);
				}

				var hint = lastToken.Scope.Hint;

				if (hint.HasFlag(ScopeHint.SelectFrom))
				{
					foreach (var k in Constants.SelectFromKeywords) yield return CreateCompletion(k, k, CompletionType.Keyword);
				}

				if (hint.HasFlag(ScopeHint.SelectOrderBy))
				{
					foreach (var k in Constants.SelectOrderByKeywords) yield return CreateCompletion(k, k, CompletionType.Keyword);
				}

				if (hint.HasFlag(ScopeHint.SelectBody))
				{
					foreach (var k in Constants.SelectBodyKeywords) yield return CreateCompletion(k, k, CompletionType.Keyword);
				}
			}
			else
			{
				foreach (var def in model.File.GetDefinitions())
				{
					if (def.CompletionVisible) yield return CreateCompletion(def);
				}
			}

			foreach (var t in ProbeEnvironment.DictDefinitions) yield return CreateCompletion(t);
			foreach (var d in Constants.DataTypeKeywords) yield return CreateCompletion(d, d, CompletionType.DataType);

			// Don't show functions when on the root.
			if (tokens.Any(t => !t.IsOnRoot))
			{
				foreach (var f in ProbeToolsPackage.Instance.FunctionFileScanner.GlobalDefinitions) yield return CreateCompletion(f);
			}

			// Global keywords
			foreach (var k in Constants.GlobalKeywords) yield return CreateCompletion(k, k, CompletionType.Keyword);

			foreach (var token in tokens)
			{
				if (token is SwitchToken)
				{
					foreach (var k in Constants.SwitchKeywords) yield return CreateCompletion(k, k, CompletionType.Keyword);
				}

				if (token is FunctionToken)
				{
					foreach (var k in Constants.FunctionKeywords) yield return CreateCompletion(k, k, CompletionType.Keyword);
				}
			}
		}

		public string GetArgumentText(string sig, int argIndex)
		{
			var args = SignatureHelp.ProbeSignatureHelpSource.GetSignatureArguments(sig).ToArray();
			if (argIndex < 0 || argIndex >= args.Length) return null;

			return args[argIndex];
		}
	}
}
