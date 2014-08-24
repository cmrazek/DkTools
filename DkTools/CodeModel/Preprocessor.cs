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
		private bool _active;

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
			CodeAttributes att;
			string text;
			var sb = new StringBuilder();
			var rdr = p.reader;

			while (!rdr.EOF)
			{
				ch = rdr.PeekChar(out att);

				if (Char.IsWhiteSpace(ch))
				{
					text = rdr.ReadSegmentUntil(c => Char.IsWhiteSpace(c), out att);
					Append(p, text, att);
					continue;
				}

				if (IgnoreComments(p)) continue;

				if (ch == '#' && p.allowDirectives)
				{
					rdr.MoveNext();
					ProcessDirective(p, att);
					continue;
				}

				if (Char.IsLetter(ch) || ch == '_')
				{
					// This could be an identifer

					var first = true;
					text = rdr.ReadSegmentUntil(c =>
						{
							if (first)
							{
								first = false;
								return c.IsWordChar(true);
							}
							else return c.IsWordChar(false);
						}, out att);

					ch = rdr.PeekChar();
					if (ch.IsWordChar(false))
					{
						// This identifier name is split out over multiple segments.
						// Read the parts of the word until the end is found.

						var parts = new List<TextPart>();
						parts.Add(new TextPart { text = text, att = att });

						sb.Clear();
						sb.Append(text);

						while ((text = rdr.ReadSegmentUntil(c => c.IsWordChar(false), out att)).Length > 0)
						{
							parts.Add(new TextPart { text = text, att = att });
							sb.Append(text);
						}

						text = sb.ToString();
						if (_defines.ContainsKey(text)) ProcessDefineUse(p, text);
						else if (text == "STRINGIZE") ProcessStringize(p, parts[0].att);
						else
						{
							foreach (var part in parts) Append(p, part.text, part.att);
						}
					}
					else
					{
						if (_defines.ContainsKey(text)) ProcessDefineUse(p, text);
						else if (text == "STRINGIZE") ProcessStringize(p, att);
						else Append(p, text, att);
					}
					continue;
				}

				// All other char types will be added to the destination as-is.
				Append(p, ch.ToString(), att);
				rdr.MoveNext();
			}
		}

		private void ProcessDirective(PreprocessorParams p, CodeAttributes hashAtt)
		{
			// This function is called after the '#' has been read from the file.

			var attribs = new CodeAttributes(hashAtt.FileName, hashAtt.FilePosition, false);
			var inst = p.reader.ReadSegmentUntil(c => c.IsWordChar(false));

			switch (inst)
			{
				case "define":
					ProcessDefine(p, attribs);
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
				default:
					Append(p, string.Concat("#", inst), hashAtt);
					break;
			}

			// TODO: warnadd, warndel
		}

		private void ProcessDefine(PreprocessorParams p, CodeAttributes attribs)
		{
			var rdr = p.reader;

			// Get the define name
			IgnoreWhiteSpaceAndComments(p, true);
			var name = rdr.ReadIdentifier();
			if (string.IsNullOrEmpty(name)) return;

			// Read the define value
			IgnoreWhiteSpaceAndComments(p, true);
			var insideBlock = false;
			var braceLevel = 0;
			var ch = rdr.PeekChar();
			if (ch == '{')
			{
				insideBlock = true;
				braceLevel = 1;
				rdr.MoveNext();
			}

			var sb = new StringBuilder();

			while (!rdr.EOF)
			{
				ch = rdr.PeekChar();

				if (IgnoreComments(p)) continue;

				if (ch == '\r')
				{
					rdr.MoveNext();
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
							rdr.MoveNext();
							continue;
						}
						else if (!insideBlock)
						{
							rdr.MoveNext();
							break;
						}
					}
					else
					{
						rdr.MoveNext();
						break;
					}
				}

				if (ch == '{' && insideBlock)
				{
					braceLevel++;
					rdr.MoveNext();
					sb.Append('{');
					continue;
				}

				if (ch == '}' && insideBlock)
				{
					rdr.MoveNext();
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
				rdr.MoveNext();
			}

			_defines[name] = new Define { name = name, content = sb.ToString(), attribs = attribs };
		}

		private void ProcessUndef(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, true);
			var name = p.reader.ReadIdentifier();
			if (string.IsNullOrEmpty(name)) return;

			_defines.Remove(name);
		}

		private void ProcessDefineUse(PreprocessorParams p, string name)
		{
			Define define;
			if (_defines.TryGetValue(name, out define))
			{
				var textToAdd = ResolveMacros(define.content);
				Append(p, textToAdd, define.attribs);
			}
		}

		private void ProcessStringize(PreprocessorParams p, CodeAttributes cmdAtt)
		{
			IgnoreWhiteSpaceAndComments(p, false);
			if (p.reader.EOF) return;

			if (p.reader.PeekChar() != '(') return;
			p.reader.MoveNext();
			IgnoreWhiteSpaceAndComments(p, true);

			var content = ReadNestableContent(p, ')');
			Log.WriteDebug("STRINGIZE arguments: {0}", content);
			content = ResolveMacros(content);
			Log.WriteDebug("STRINGIZE resolved arguments: {0}", content);

			Append(p, content, new CodeAttributes(cmdAtt.FileName, cmdAtt.FilePosition, false));
		}

		private string ReadNestableContent(PreprocessorParams p, char endChar)
		{
			var sb = new StringBuilder();
			IgnoreWhiteSpaceAndComments(p, false);

			var rdr = p.reader;

			while (!rdr.EOF)
			{
				var ch = rdr.PeekChar();

				if (IgnoreComments(p)) continue;

				if (ch == '(')
				{
					sb.Append(ch);
					rdr.MoveNext();
					sb.Append(ReadNestableContent(p, ')'));
					sb.Append(')');
					continue;
				}

				if (ch == '{')
				{
					sb.Append(ch);
					rdr.MoveNext();
					sb.Append(ReadNestableContent(p, '}'));
					sb.Append('}');
					continue;
				}

				if (ch == '[')
				{
					sb.Append(ch);
					rdr.MoveNext();
					sb.Append(ReadNestableContent(p, ']'));
					sb.Append(']');
					continue;
				}

				if (ch == endChar)
				{
					rdr.MoveNext();
					break;
				}

				sb.Append(ch);
				rdr.MoveNext();
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
			while (true)
			{
				var ch = p.reader.PeekChar();

				if (stayOnSameLine && (ch == '\r' || ch == '\n')) break;

				if (Char.IsWhiteSpace(ch))
				{
					p.reader.MoveNext();
					continue;
				}

				if (!IgnoreComments(p)) break;
			}
		}

		private bool IgnoreComments(PreprocessorParams p, bool multiLineOnly = false)
		{
			var rdr = p.reader;

			if (rdr.PeekChar() == '/')
			{
				var str = rdr.Peek(2);
				if (str == "/*")
				{
					rdr.MoveNext(2);
					while (!rdr.EOF)
					{
						if (rdr.PeekChar() == '*' && rdr.Peek(2) == "*/")
						{
							rdr.MoveNext(2);
							return true;
						}
						else if (!IgnoreComments(p, true))
						{
							rdr.MoveNext();
						}
					}
					return true;
				}
				else if (str == "//" && multiLineOnly == false)
				{
					char ch;
					rdr.MoveNext(2);
					while (!rdr.EOF)
					{
						ch = rdr.PeekChar();
						if (ch == '\r' || ch == '\n')
						{
							return true;
						}
						else if (!IgnoreComments(p, true))
						{
							rdr.MoveNext();
						}
					}
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
			var ch = rdr.PeekChar();
			if (ch == '\"')
			{
				rdr.MoveNext();
				includeName = rdr.ReadAllUntil(c => c != '\"');
				searchSameDir = true;
				rdr.MoveNext();
			}
			else if (ch == '<')
			{
				rdr.MoveNext();
				includeName = rdr.ReadAllUntil(c => c != '>');
				searchSameDir = false;
				rdr.MoveNext();
			}
			else return;
			if (string.IsNullOrEmpty(includeName)) return;

			if (_active) AppendIncludeFile(p, includeName, searchSameDir);
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

			Append(p, includeSource);
		}

		private void ProcessIfDef(PreprocessorParams p, bool activeIfDefined)
		{
			IgnoreWhiteSpaceAndComments(p, true);

			var name = p.reader.ReadIdentifier();
			if (string.IsNullOrEmpty(name)) return;

			bool result = _defines.ContainsKey(name);
			if (!activeIfDefined) result = !result;
			p.conditions.Push(new ConditionScope { result = result });
			_active = result;

			IgnoreWhiteSpaceAndComments(p, true);
		}

		private void ProcessEndIf(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, true);

			if (p.conditions.Count > 0) p.conditions.Pop();
			_active = p.conditions.Count > 0 ? p.conditions.Peek().result : true;

			IgnoreWhiteSpaceAndComments(p, true);
		}

		private void ProcessElse(PreprocessorParams p)
		{
			IgnoreWhiteSpaceAndComments(p, true);

			if (p.conditions.Count == 0) return;

			var scope = p.conditions.Peek();

			if (scope.gotElse) return;
			scope.gotElse = true;

			_active = !scope.result;

			IgnoreWhiteSpaceAndComments(p, true);
		}

		private void ProcessIf(PreprocessorParams p, bool elif)
		{
			var sb = new StringBuilder();
			char ch;
			var rdr = p.reader;

			IgnoreWhiteSpaceAndComments(p, true);

			while (!rdr.EOF)
			{
				ch = rdr.PeekChar();
				if (ch == '\r' || ch == '\n') break;
				if (IgnoreComments(p)) continue;
				sb.Append(ch);
				rdr.MoveNext();
			}

			var result = EvaluateCondition(sb.ToString());

			if (!elif)
			{
				p.conditions.Push(new ConditionScope { result = result });
			}
			else
			{
				if (p.conditions.Count == 0) return;
				var cond = p.conditions.Peek();
				if (cond.gotElse) return;
				cond.result = result;
			}
			_active = result;
		}

		private void Append(PreprocessorParams p, string text, CodeAttributes attribs)
		{
			if (_active) p.writer.Append(text, attribs);
		}

		private void Append(PreprocessorParams p, CodeSource source)
		{
			if (_active) p.writer.Append(source);
		}

		private struct TextPart
		{
			public string text;
			public CodeAttributes att;
		}

		private struct Define
		{
			public string name;
			public string content;
			public CodeAttributes attribs;
		}

		private class ConditionScope
		{
			public bool result;
			public bool gotElse;
		}

		private class PreprocessorParams
		{
			public IPreprocessorReader reader;
			public IPreprocessorWriter writer;
			public string fileName;
			public string[] parentFiles;
			public Stack<ConditionScope> conditions = new Stack<ConditionScope>();
			public bool allowDirectives = true;

			public PreprocessorParams(IPreprocessorReader reader, IPreprocessorWriter writer, string fileName, IEnumerable<string> parentFiles)
			{
				this.reader = reader;
				this.writer = writer;
				this.fileName = fileName;
				if (parentFiles != null) this.parentFiles = parentFiles.ToArray();
			}
		}

		private bool EvaluateCondition(string conditionStr)
		{
			// Run preprocessor on the condition string
			var reader = new StringPreprocessorReader(conditionStr);
			var writer = new StringPreprocessorWriter();
			var parms = new PreprocessorParams(reader, writer, string.Empty, null);
			parms.allowDirectives = false;
			Preprocess(parms, false);

			//var parser = new DkTools.TokenParser.Parser(writer.Text);
			//var tokens = parser.ToList();
			//var topNode = new TokenNode();
			//topNode.Parse(tokens);

			// TODO: break the string down into tokens and evaluate

			return true;
		}
	}
}
