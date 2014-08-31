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
		Keyword
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
				default:
					img = _variableImg;
					break;
			}

			return new Completion(text, text, description, img, string.Empty);
		}

		private Completion CreateCompletion(Definition def)
		{
			return CreateCompletion(def.Name, def.CompletionDescription, def.CompletionType);
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
		private Regex _rxFunctionStartBracket = new Regex(@"(\w+)\s*\($");
		private Regex _rxReturn = new Regex(@"\breturn\s$");
		private Regex _rxCase = new Regex(@"\bcase\s$");

		void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			var completionList = new Dictionary<string, Completion>();

			var curPos = session.TextView.Caret.Position.BufferPosition.Position;
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
				var modelPos = model.GetPosition(curPos, snapshot);
				var parentToken = (from t in model.FindTokens(modelPos)
								   where t is GroupToken && t.Span.Start.Offset < curPos
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
								foreach (var opt in dt.CompletionOptions) completionList[opt] = CreateCompletion(opt, opt, CompletionType.Constant);
							}
						}
					}
				}
			}
			else if ((match = _rxFunctionStartBracket.Match(prefix)).Success)
			{
				// Starting a new function.

				var funcName = match.Groups[1].Value;
				foreach (var opt in GetOptionsForFunctionArg(funcName, 0)) completionList[opt.DisplayText] = opt;
			}
			else if (prefix.EndsWith(", "))
			{
				// Moving to next argument in function.

				string funcName;
				int argIndex;
				if (GetInsideFunction(snapshot, curPos, out funcName, out argIndex))
				{
					foreach (var opt in GetOptionsForFunctionArg(funcName, argIndex)) completionList[opt.DisplayText] = opt;
				}
			}
			else if ((match = _rxReturn.Match(prefix)).Success)
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(_textBuffer.CurrentSnapshot, "Auto-completion after return");
				var modelPos = model.GetPosition(curPos, snapshot);

				var funcToken = model.FindTokens(modelPos).LastOrDefault(x => x is CodeModel.FunctionToken) as CodeModel.FunctionToken;
				if (funcToken != null)
				{
					var dataType = funcToken.DataTypeToken;
					if (dataType != null && dataType is CodeModel.IDataTypeToken)
					{
						foreach (var opt in (dataType as CodeModel.IDataTypeToken).DataType.CompletionOptions) completionList[opt] = CreateCompletion(opt, opt, CompletionType.Constant);
					}
				}
			}
			else if ((match = _rxCase.Match(prefix)).Success)
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(_textBuffer.CurrentSnapshot, "Auto-completion after case");
				var modelPos = model.GetPosition(curPos, snapshot);

				var switchToken = (from t in model.FindTokens(modelPos) where t is SwitchToken select t as SwitchToken).LastOrDefault();
				if (switchToken != null)
				{
					var dt = switchToken.ExpressionDataType;
					if (dt != null && dt.HasCompletionOptions)
					{
						foreach (var opt in dt.CompletionOptions) completionList[opt] = CreateCompletion(opt, opt, CompletionType.Constant);
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

		private IEnumerable<Completion> GetOptionsForFunctionArg(string funcName, int argIndex)
		{
			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(_textBuffer.CurrentSnapshot, "Signature help get options for arg");

			var sig = SignatureHelp.ProbeSignatureHelpSource.GetAllSignaturesForFunction(model, funcName).FirstOrDefault();
			if (string.IsNullOrEmpty(sig)) yield break;

			var argText = GetArgumentText(sig, argIndex);
			if (string.IsNullOrWhiteSpace(argText)) yield break;

			foreach (var opt in CodeModel.DataType.ParseCompletionOptionsFromArgText(argText, model))
			{
				yield return CreateCompletion(opt, opt, CompletionType.Constant);
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
					parser.SetOffset(match.Groups[2].Index);
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
			var modelPos = model.GetPosition(curPos, snapshot);

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
				foreach (var f in ProbeToolsPackage.Instance.FunctionFileScanner.AllDefinitions) yield return CreateCompletion(f);
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
			var argStartIndex = sig.IndexOf('(');
			if (argStartIndex <= 0) return null;
			argStartIndex++;

			var argEndIndex = sig.LastIndexOf(')');
			if (argEndIndex <= 0) argEndIndex = sig.Length;

			var args = sig.Substring(argStartIndex, argEndIndex - argStartIndex).Split(',');
			if (argIndex >= args.Length) return null;
			return args[argIndex].Trim();
		}
	}
}
