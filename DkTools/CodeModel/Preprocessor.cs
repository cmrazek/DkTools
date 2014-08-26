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

			char ch;
			string text;
			var sb = new StringBuilder();
			var rdr = p.reader;
			p.reader.SetWriter(p.writer);

			while (!rdr.EOF)
			{
				ch = rdr.Peek();

				if (Char.IsWhiteSpace(ch))
				{
					rdr.UseUntil(c => Char.IsWhiteSpace(c));
					continue;
				}

				if (ch == '/')
				{
					if (!IgnoreComments(p)) rdr.Use(1);
					continue;
				}

				if (ch == '#' && p.allowDirectives)
				{
					rdr.Ignore(1);
					ProcessDirective(p);
					continue;
				}

				if (Char.IsLetter(ch) || ch == '_')
				{
					// This could be an identifer

					text = rdr.PeekIdentifier();

					if (_defines.ContainsKey(text))
					{
						rdr.Ignore(text.Length);
						ProcessDefineUse(p, text);
					}
					else if (text == "STRINGIZE")
					{
						rdr.Ignore(text.Length);
						ProcessStringize(p);
					}
					else
					{
						rdr.Use(text.Length);
					}
					continue;
				}

				if (Char.IsNumber(ch))
				{
					rdr.UseUntil(c => Char.IsNumber(c));
					continue;
				}

				// All other char types will be added to the destination as-is.
				rdr.UseUntil(c =>
					{
						// Ignore everything that doesn't matter to the other processing, above.
						if (Char.IsWhiteSpace(c)) return true;
						if (c == '#' || c == '/') return false;
						if (c.IsWordChar(false)) return false;
						return true;
					});
			}
		}

		private void ProcessDirective(PreprocessorParams p)
		{
			// This function is called after the '#' has been read from the file.

			var directiveName = p.reader.PeekIdentifier();
			p.reader.Ignore(directiveName.Length);

			switch (directiveName)
			{
				case "define":
					ProcessDefine(p);
					break;
				case "undef":
					ProcessUndef(p);
					break;
				case "include":
					ProcessInclude(p);
					break;
				case "if":
					ProcessIf(p, false);
					break;
				case "elif":
					ProcessIf(p, true);
					break;
				case "ifdef":
					ProcessIfDef(p, true);
					break;
				case "ifndef":
					ProcessIfDef(p, false);
					break;
				case "else":
					ProcessElse(p);
					break;
				case "endif":
					ProcessEndIf(p);
					break;
			}

			// TODO: warnadd, warndel
		}

		private void ProcessDefine(PreprocessorParams p)
		{
			var rdr = p.reader;

			// Get the define name
			IgnoreWhiteSpaceAndComments(p, true);
			var name = rdr.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			rdr.Ignore(name.Length);

			// Read the define value
			IgnoreWhiteSpaceAndComments(p, true);
			var insideBlock = false;
			var braceLevel = 0;
			var ch = rdr.Peek();
			if (ch == '{')
			{
				insideBlock = true;
				braceLevel = 1;
				rdr.Ignore(1);
			}

			var sb = new StringBuilder();

			while (!rdr.EOF)
			{
				ch = rdr.Peek();

				if (IgnoreComments(p)) continue;

				if (ch == '\r')
				{
					rdr.Ignore(1);
					continue;
				}

				if (ch == '\n')
				{
					char endCh;
					int index;
					if (sb.GetLastNonWhiteChar(out endCh, out index))
					{
						if (endCh == '\\')
						{
							// define continues down to the next line, but don't include the slash in the resulting text.
							sb.Remove(index, 1);
							sb.Append("\r\n");
							rdr.Ignore(1);
							continue;
						}
						else if (!insideBlock)
						{
							rdr.Ignore(1);
							break;
						}
					}
					else
					{
						rdr.Ignore(1);
						break;
					}
				}

				if (ch == '{' && insideBlock)
				{
					braceLevel++;
					rdr.Ignore(1);
					sb.Append('{');
					continue;
				}

				if (ch == '}' && insideBlock)
				{
					rdr.Ignore(1);
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

				sb.Append(ch);
				rdr.Ignore(1);
			}

			_defines[name] = new Define { name = name, content = sb.ToString() };
		}

		private void ProcessUndef(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, true);
			var name = p.reader.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			p.reader.Ignore(name.Length);

			_defines.Remove(name);
		}

		private void ProcessDefineUse(PreprocessorParams p, string name)
		{
			if (p.suppress) return;

			Define define;
			if (_defines.TryGetValue(name, out define))
			{
				var textToAdd = ResolveMacros(define.content);
				p.reader.Insert(textToAdd);
			}
		}

		private void ProcessStringize(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, false);
			if (p.reader.EOF) return;

			if (p.reader.Peek() != '(') return;
			p.reader.Ignore(1);
			IgnoreWhiteSpaceAndComments(p, true);

			var content = ReadAndIgnoreNestableContent(p, ')');
			Log.WriteDebug("STRINGIZE arguments: {0}", content);
			content = ResolveMacros(content);
			Log.WriteDebug("STRINGIZE resolved arguments: {0}", content);

			p.reader.Insert(EscapeString(content));
		}

		private string ReadAndIgnoreNestableContent(PreprocessorParams p, char endChar)
		{
			var sb = new StringBuilder();
			IgnoreWhiteSpaceAndComments(p, false);

			var rdr = p.reader;
			string text;

			while (!rdr.EOF)
			{
				var ch = rdr.Peek();

				if (IgnoreComments(p)) continue;

				if (ch == '(')
				{
					sb.Append(ch);
					rdr.Ignore(1);
					sb.Append(ReadAndIgnoreNestableContent(p, ')'));
					sb.Append(')');
					continue;
				}

				if (ch == '{')
				{
					sb.Append(ch);
					rdr.Ignore(1);
					sb.Append(ReadAndIgnoreNestableContent(p, '}'));
					sb.Append('}');
					continue;
				}

				if (ch == '[')
				{
					sb.Append(ch);
					rdr.Ignore(1);
					sb.Append(ReadAndIgnoreNestableContent(p, ']'));
					sb.Append(']');
					continue;
				}

				if (ch == endChar)
				{
					rdr.Ignore(1);
					break;
				}

				if (ch == ')' || ch == '}' || ch == ']')
				{
					sb.Append(ch);
					rdr.Ignore(1);
					continue;
				}

				text = rdr.PeekUntil(c =>
					{
						switch (c)
						{
							case '(': case ')':
							case '{': case '}':
							case '[': case ']':
							case '/':
								return false;
							default:
								return true;
						}
					});
				sb.Append(text);
				rdr.Ignore(text.Length);
			}

			return sb.ToString();
		}

		private string ResolveMacros(string source)
		{
			var reader = new StringPreprocessorReader(source);
			var writer = new StringPreprocessorWriter();
			var parms = new PreprocessorParams(reader, writer, string.Empty, null);

			Preprocess(parms, false);
			return writer.Text;
		}

		private void IgnoreWhiteSpaceAndComments(PreprocessorParams p, bool stayOnSameLine)
		{
			char ch;

			while (true)
			{
				ch = p.reader.Peek();

				if (stayOnSameLine && (ch == '\r' || ch == '\n')) break;

				if (Char.IsWhiteSpace(ch))
				{
					p.reader.IgnoreUntil(c => Char.IsWhiteSpace(c));
					continue;
				}

				if (!IgnoreComments(p)) break;
			}
		}

		private bool IgnoreComments(PreprocessorParams p, bool multiLineOnly = false)
		{
			var rdr = p.reader;
			var writer = p.writer;

			if (rdr.Peek() == '/')
			{
				var str = rdr.Peek(2);
				if (str == "/*")
				{
					rdr.Ignore(2);
					rdr.IgnoreUntil(c => c != '/' && c != '*');
					while (!rdr.EOF)
					{
						if (rdr.Peek() == '*' && rdr.Peek(2) == "*/")
						{
							rdr.Ignore(2);
							return true;
						}
						else if (!IgnoreComments(p, true))
						{
							rdr.Ignore(1);
						}
						rdr.IgnoreUntil(c => c != '/' && c != '*');
					}
					return true;
				}
				else if (str == "//" && multiLineOnly == false)
				{
					rdr.IgnoreUntil(c => c != '\r' && c != '\n');
					return true;
				}
			}
			return false;
		}

		private void ProcessInclude(PreprocessorParams p)
		{
			string includeName = null;
			var searchSameDir = false;

			var rdr = p.reader;

			IgnoreWhiteSpaceAndComments(p, true);
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

			//Shell.OpenTempContent(rawSource.Dump(), System.IO.Path.GetFileName(fileName), ".txt");	// TODO: remove

			// Run the preprocessor on the include file.
			var includeSource = new CodeSource();
			var parms = new PreprocessorParams(reader, includeSource, fileName, parentFiles);
			Preprocess(parms, false);

			p.writer.Append(includeSource);
		}

		private void ProcessIfDef(PreprocessorParams p, bool activeIfDefined)
		{
			IgnoreWhiteSpaceAndComments(p, true);

			var name = p.reader.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			p.reader.Ignore(name.Length);

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
			IgnoreWhiteSpaceAndComments(p, true);
		}

		private void ProcessEndIf(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, true);

			if (p.ifStack.Count > 0) p.ifStack.Pop();
			UpdateSuppress(p);

			IgnoreWhiteSpaceAndComments(p, true);
		}

		private void ProcessElse(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, true);

			if (p.ifStack.Count == 0) return;

			var scope = p.ifStack.Peek();

			if (scope.gotElse) return;
			scope.gotElse = true;
			UpdateSuppress(p);

			IgnoreWhiteSpaceAndComments(p, true);
		}

		private void ProcessIf(PreprocessorParams p, bool elif)
		{
			var sb = new StringBuilder();
			char ch;
			var rdr = p.reader;
			string text;

			IgnoreWhiteSpaceAndComments(p, true);

			while (!rdr.EOF)
			{
				ch = rdr.Peek();
				if (ch == '\r' || ch == '\n') break;

				if (ch == '/')
				{
					if (IgnoreComments(p)) continue;
					sb.Append(ch);
					rdr.Ignore(1);
					continue;
				}

				text = rdr.PeekUntil(c => c != '\r' && c != '\n' && c != '/');
				rdr.Ignore(text.Length);
				sb.Append(text);
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

		private struct Define
		{
			public string name;
			public string content;
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
