using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.TokenParser;

namespace DkTools.CodeModel
{
	internal class Preprocessor
	{
		private FileStore _store;
		private Dictionary<string, Define> _defines = new Dictionary<string, Define>();

		public Preprocessor(FileStore store)
		{
			if (store == null) throw new ArgumentNullException("store");
			_store = store;
		}

		public void Preprocess(IPreprocessorReader reader, IPreprocessorWriter writer, string fileName, IEnumerable<string> parentFiles, bool includeStdLib)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			Preprocess(new PreprocessorParams(reader, writer, fileName, parentFiles), includeStdLib);
		}

		private void Preprocess(PreprocessorParams p, bool includeStdLib)
		{
			// This function assumes the source has already been merged.

			if (includeStdLib)
			{
				AppendIncludeFile(p, "stdlib.i", false);
			}

			string str;
			var sb = new StringBuilder();
			var rdr = p.reader;
			p.reader.SetWriter(p.writer);

			while (!rdr.EOF)
			{
				str = rdr.PeekToken(false);
				if (string.IsNullOrEmpty(str)) continue;

				if (str[0] == '#')
				{
					rdr.Ignore(str.Length);
					ProcessDirective(p, str);
					continue;
				}

				if (str[0].IsWordChar(true))
				{
					if (p.args != null && p.args.Any(x => x.name == str))
					{
						rdr.Ignore(str.Length);
						ProcessDefineUse(p, str);
					}
					else if (_defines.ContainsKey(str))
					{
						rdr.Ignore(str.Length);
						ProcessDefineUse(p, str);
					}
					else if (str == "STRINGIZE")
					{
						rdr.Ignore(str.Length);
						ProcessStringize(p);
					}
					else
					{
						rdr.Use(str.Length);
					}
					continue;
				}

				rdr.Use(str.Length);
			}
		}

		private void ProcessDirective(PreprocessorParams p, string directiveName)
		{
			// This function is called after the '#' has been read from the file.

			switch (directiveName)
			{
				case "#define":
					ProcessDefine(p);
					break;
				case "#undef":
					ProcessUndef(p);
					break;
				case "#include":
					ProcessInclude(p);
					break;
				case "#if":
					ProcessIf(p, false);
					break;
				case "#elif":
					ProcessIf(p, true);
					break;
				case "#ifdef":
					ProcessIfDef(p, true);
					break;
				case "#ifndef":
					ProcessIfDef(p, false);
					break;
				case "#else":
					ProcessElse(p);
					break;
				case "#endif":
					ProcessEndIf(p);
					break;
				case "#warndel":
				case "#warnadd":
					ProcessWarnAddDel(p);
					break;
			}
		}

		private void ProcessDefine(PreprocessorParams p)
		{
			var rdr = p.reader;
			char ch;
			string str;

			// Get the define name
			rdr.IgnoreWhiteSpaceAndComments(true);
			var name = rdr.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			rdr.Ignore(name.Length);

			// Check if this is parameterized
			List<string> paramNames = null;
			if (rdr.Peek() == '(')
			{
				rdr.Ignore(1);

				while (!rdr.EOF)
				{
					rdr.IgnoreWhiteSpaceAndComments(true);

					str = rdr.PeekToken(false);
					if (string.IsNullOrEmpty(str)) return;
					if (str == ",")
					{
						rdr.Ignore(str.Length);
					}
					else if (str[0].IsWordChar(true))
					{
						rdr.Ignore(str.Length);
						if (!p.suppress)
						{
							if (paramNames == null) paramNames = new List<string>();
							paramNames.Add(str);
						}
					}
					else if (str == ")")
					{
						rdr.Ignore(str.Length);
						break;
					}
					else return;
				}
			}

			// Read the define value
			rdr.IgnoreWhiteSpaceAndComments(true);
			var insideBlock = false;
			var braceLevel = 0;
			ch = rdr.Peek();
			if (ch == '{')
			{
				insideBlock = true;
				braceLevel = 1;
				rdr.Ignore(1);
			}

			var sb = new StringBuilder();

			while (!rdr.EOF)
			{
				if (rdr.IgnoreComments()) continue;

				str = rdr.PeekToken(true);
				if (str == null)
				{
					// End of line found

					char endCh;
					int index;
					if (sb.GetLastNonWhiteChar(out endCh, out index))
					{
						if (endCh == '\\')
						{
							// define continues down to the next line, but don't include the slash in the resulting text.
							sb.Remove(index, 1);
							sb.Append("\r\n");
							rdr.IgnoreUntil(c => c == '\r' || c == '\n');
							continue;
						}
						else if (insideBlock)
						{
							rdr.IgnoreUntil(c => c == '\r' || c == '\n');
							sb.Append("\r\n");
							continue;
						}
						else break;
					}
					else
					{
						if (insideBlock)
						{
							rdr.IgnoreUntil(c => c == '\r' || c == '\n');
							sb.Append("\r\n");
							continue;
						}
						else break;
					}
				}

				if (str == "{" && insideBlock)
				{
					braceLevel++;
					rdr.Ignore(str.Length);
					sb.Append('{');
					continue;
				}

				if (str == "}" && insideBlock)
				{
					rdr.Ignore(str.Length);
					if (--braceLevel <= 0)
					{
						break;
					}
					else
					{
						sb.Append('}');
						continue;
					}
				}

				rdr.Ignore(str.Length);
				sb.Append(str);
			}

			if (!p.suppress)
			{
				_defines[name] = new Define
				{
					name = name,
					content = sb.ToString(),
					paramNames = paramNames
				};
			}
		}

		private void ProcessUndef(PreprocessorParams p)
		{
			p.reader.IgnoreWhiteSpaceAndComments(true);
			var name = p.reader.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			p.reader.Ignore(name.Length);

			_defines.Remove(name);
		}

		private void ProcessDefineUse(PreprocessorParams p, string name)
		{
			if (p.suppress) return;
			if (p.restrictedDefines != null && p.restrictedDefines.Contains(name)) return;

			var rdr = p.reader;

			Define define = null;
			if (p.args != null) define = p.args.FirstOrDefault(x => x.name == name);
			if (define == null) _defines.TryGetValue(name, out define);
			if (define == null) return;

			List<string> paramList = null;
			if (define.paramNames != null)
			{
				// This is a parameterized macro
				rdr.IgnoreWhiteSpaceAndComments(false);
				if (rdr.Peek() != '(') return;
				rdr.Ignore(1);

				char ch;
				var sb = new StringBuilder();
				paramList = new List<string>();

				rdr.IgnoreWhiteSpaceAndComments(false);
				while (!rdr.EOF)
				{
					if (rdr.IgnoreComments()) continue;

					ch = rdr.Peek();
					if (ch == ',')
					{
						rdr.Ignore(1);
						paramList.Add(sb.ToString().Trim());
						sb.Clear();
					}
					else if (ch == ')')
					{
						rdr.Ignore(1);
						break;
					}
					else if (ch == '(')
					{
						rdr.Ignore(1);
						sb.Append('(');
						sb.Append(rdr.ReadAndIgnoreNestableContent(")"));
						sb.Append(')');
					}
					else if (ch == '{')
					{
						rdr.Ignore(1);
						sb.Append('{');
						sb.Append(rdr.ReadAndIgnoreNestableContent("}"));
						sb.Append('}');
					}
					else if (ch == '[')
					{
						rdr.Ignore(1);
						sb.Append('[');
						sb.Append(rdr.ReadAndIgnoreNestableContent("]"));
						sb.Append(']');
					}
					else
					{
						rdr.Ignore(1);
						sb.Append(ch);
					}
				}
				if (sb.Length > 0) paramList.Add(sb.ToString().Trim());

				if (define.paramNames.Count != paramList.Count) return;
			}
			
			List<Define> args = null;
			if (p.args != null)
			{
				args = new List<Define>();
				args.AddRange(p.args);
			}
			if (paramList != null)
			{
				if (define.paramNames == null || define.paramNames.Count != paramList.Count) return;
				if (args == null) args = new List<Define>();
				for (int i = 0, ii = paramList.Count; i < ii; i++)
				{
					args.Add(new Define { name = define.paramNames[i], content = paramList[i] });
				}
			}

			string[] restrictedDefines = null;
			if (p.restrictedDefines != null) restrictedDefines = p.restrictedDefines.Concat(new string[] { name }).ToArray();
			else restrictedDefines = new string[] { name };

			var textToAdd = ResolveMacros(define.content, restrictedDefines, args);
			rdr.Insert(textToAdd);
		}

		private void ProcessStringize(PreprocessorParams p)
		{
			var rdr = p.reader;
			rdr.IgnoreWhiteSpaceAndComments(false);
			if (rdr.EOF) return;

			if (rdr.Peek() != '(') return;
			rdr.Ignore(1);
			rdr.IgnoreWhiteSpaceAndComments(true);

			var content = rdr.ReadAndIgnoreNestableContent(")");
			content = ResolveMacros(content, p.restrictedDefines, p.args);

			p.reader.Insert(EscapeString(content));
		}

		private string ResolveMacros(string source, IEnumerable<string> restrictedDefines, IEnumerable<Define> args)
		{
			var reader = new StringPreprocessorReader(source);
			var writer = new StringPreprocessorWriter();

			var parms = new PreprocessorParams(reader, writer, string.Empty, null);
			parms.restrictedDefines = restrictedDefines;
			parms.args = args;

			Preprocess(parms, false);
			return writer.Text;
		}

		private void ProcessInclude(PreprocessorParams p)
		{
			string includeName = null;
			var searchSameDir = false;

			var rdr = p.reader;

			rdr.IgnoreWhiteSpaceAndComments(true);
			var ch = rdr.Peek();
			if (ch == '\"')
			{
				rdr.Ignore(1);
				includeName = rdr.PeekUntil(c => c != '\"' && c != '\r' && c != '\n');
				rdr.Ignore(includeName.Length);
				if (rdr.Peek() == '\"') rdr.Ignore(1);
				searchSameDir = true;
			}
			else if (ch == '<')
			{
				rdr.Ignore(1);
				includeName = rdr.PeekUntil(c => c != '>' && c != '\r' && c != '\n');
				rdr.Ignore(includeName.Length);
				if (rdr.Peek() == '>') rdr.Ignore(1);
				searchSameDir = false;
			}
			else return;
			if (string.IsNullOrEmpty(includeName)) return;

			if (!p.suppress) AppendIncludeFile(p, includeName, searchSameDir);
		}

		private void AppendIncludeFile(PreprocessorParams p, string fileName, bool searchSameDir)
		{
			// Load the include file
			string[] parentFiles;
			if (string.IsNullOrEmpty(p.fileName)) parentFiles = p.parentFiles;
			else if (p.parentFiles == null) parentFiles = new string[0];
			else parentFiles = p.parentFiles.Concat(new string[] { p.fileName }).ToArray();

			var includeNode = _store.GetIncludeFile(p.fileName, fileName, searchSameDir, parentFiles);
			if (includeNode == null) return;

			var rawSource = includeNode.Source;
			if (rawSource == null) return;
			var reader = new CodeSource.CodeSourcePreprocessorReader(rawSource);

			// Run the preprocessor on the include file.
			var includeSource = new CodeSource();
			var parms = new PreprocessorParams(reader, includeSource, fileName, parentFiles);
			Preprocess(parms, false);

			p.writer.Append(includeSource);
		}

		private void ProcessIfDef(PreprocessorParams p, bool activeIfDefined)
		{
			var rdr = p.reader;
			rdr.IgnoreWhiteSpaceAndComments(true);

			var name = rdr.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			rdr.Ignore(name.Length);

			if (p.suppress)
			{
				p.ifStack.Push(new ConditionScope { result = ConditionResult.Indeterminate });
			}
			else
			{
				bool defined = _defines.ContainsKey(name);
				if (!activeIfDefined) defined = !defined;
				p.ifStack.Push(new ConditionScope { result = defined ? ConditionResult.Positive : ConditionResult.Negative });
				UpdateSuppress(p);
			}
			rdr.IgnoreWhiteSpaceAndComments(true);
		}

		private void ProcessEndIf(PreprocessorParams p)
		{
			p.reader.IgnoreWhiteSpaceAndComments(true);

			if (p.ifStack.Count > 0) p.ifStack.Pop();
			UpdateSuppress(p);

			p.reader.IgnoreWhiteSpaceAndComments(true);
		}

		private void ProcessElse(PreprocessorParams p)
		{
			p.reader.IgnoreWhiteSpaceAndComments(true);

			if (p.ifStack.Count == 0) return;

			var scope = p.ifStack.Peek();

			if (scope.gotElse) return;
			scope.gotElse = true;
			UpdateSuppress(p);

			p.reader.IgnoreWhiteSpaceAndComments(true);
		}

		private void ProcessIf(PreprocessorParams p, bool elif)
		{
			var sb = new StringBuilder();
			var rdr = p.reader;
			string str;

			rdr.IgnoreWhiteSpaceAndComments(true);

			// Read the rest of the line
			while (!rdr.EOF)
			{
				str = rdr.PeekToken(true);
				if (str == null) break;

				rdr.Ignore(str.Length);
				sb.Append(str);
			}

			ConditionResult result;
			if (p.suppress) result = ConditionResult.Indeterminate;
			else result = EvaluateCondition(sb.ToString());

			if (!elif)
			{
				p.ifStack.Push(new ConditionScope { result = result });
			}
			else
			{
				if (p.ifStack.Count == 0) return;
				var ifLevel = p.ifStack.Peek();
				if (ifLevel.gotElse) return;
				ifLevel.result = result;
			}
			UpdateSuppress(p);
		}

		private void UpdateSuppress(PreprocessorParams p)
		{
			var suppress = false;

			foreach (var scope in p.ifStack)
			{
				if (!scope.gotElse)
				{
					if (scope.result == ConditionResult.Negative)
					{
						suppress = true;
						break;
					}
				}
				else
				{
					if (scope.result == ConditionResult.Positive)
					{
						suppress = true;
						break;
					}
				}
			}

			p.reader.Suppress = p.suppress = suppress;
		}

		private string EscapeString(string str)
		{
			var sb = new StringBuilder();
			sb.Append("\"");

			foreach (var ch in str)
			{
				switch (ch)
				{
					case '\\':
						sb.Append("\\\\");
						break;
					case '\n':
						sb.Append("\\n");
						break;
					case '\r':
						sb.Append("\\r");
						break;
					case '\t':
						sb.Append("\\t");
						break;
					case '"':
						sb.Append("\\\"");
						break;
					default:
						if (ch < 0x20 || ch > 0x7f) sb.AppendFormat("\\x{0:X4}", (int)ch);
						else sb.Append(ch);
						break;
				}
			}

			sb.Append("\"");
			return sb.ToString();
		}

		private void ProcessWarnAddDel(PreprocessorParams p)
		{
			var rdr = p.reader;
			rdr.IgnoreWhiteSpaceAndComments(true);

			var number = rdr.PeekToken(true);
			if (!string.IsNullOrEmpty(number) && char.IsNumber(number[0])) rdr.Ignore(number.Length);
		}

		private class Define
		{
			public string name;
			public string content;
			public List<string> paramNames;
		}

		public enum ConditionResult
		{
			Negative,
			Positive,
			Indeterminate
		}

		private class ConditionScope
		{
			public ConditionResult result;
			public bool gotElse;
		}

		private class PreprocessorParams
		{
			public IPreprocessorReader reader;
			public IPreprocessorWriter writer;
			public string fileName;
			public string[] parentFiles;
			public bool allowDirectives = true;
			public Stack<ConditionScope> ifStack = new Stack<ConditionScope>();
			public bool suppress;
			public IEnumerable<Define> args;
			public IEnumerable<string> restrictedDefines;

			public PreprocessorParams(IPreprocessorReader reader, IPreprocessorWriter writer, string fileName, IEnumerable<string> parentFiles)
			{
				this.reader = reader;
				this.writer = writer;
				this.fileName = fileName;
				if (parentFiles != null) this.parentFiles = parentFiles.ToArray();
			}
		}

		private ConditionResult EvaluateCondition(string conditionStr)
		{
			// Run preprocessor on the condition string
			var reader = new StringPreprocessorReader(conditionStr);
			var writer = new StringPreprocessorWriter();
			var parms = new PreprocessorParams(reader, writer, string.Empty, null);
			parms.allowDirectives = false;
			Preprocess(parms, false);

			// Evaluate the condition string
			long? finalValue;
			try
			{
				var parser = new TokenParser.Parser(writer.Text);
				var tokenGroup = PreprocessorTokens.GroupToken.Parse(null, parser, null);
				finalValue = tokenGroup.Value;
			}
			catch (PreprocessorTokens.PreprocessorConditionException ex)
			{
				Log.WriteDebug("Exception when processing #if condition: {0}", ex);
				finalValue = null;
			}

			if (finalValue.HasValue)
			{
				if (finalValue.Value != 0) return ConditionResult.Positive;
				else return ConditionResult.Negative;
			}
			else return ConditionResult.Indeterminate;
		}
	}
}
