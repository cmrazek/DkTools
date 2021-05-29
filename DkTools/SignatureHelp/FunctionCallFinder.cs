using DK;
using DK.Code;
using System.Collections.Generic;

namespace DkTools.SignatureHelp
{
	internal class FunctionCallFinder
	{
		public class FindResult
		{
			public string ClassName { get; private set; }
			public string FunctionName { get; private set; }
			public CodeSpan FunctionNameSpan { get; private set; }
			public int ArgumentIndex { get; private set; }
			public CodeSpan ArgumentsSpan { get; private set; }

			public FindResult(BracketScope foundScope, int argumentsEnd)
			{
				ClassName = foundScope.ClassName;
				FunctionName = foundScope.Name;
				FunctionNameSpan = foundScope.NameSpan;
				ArgumentIndex = foundScope.ArgumentIndex;
				ArgumentsSpan = new CodeSpan(foundScope.ArgumentsStart, argumentsEnd);
			}
		}

		public struct BracketScope
		{
			private string _className;
			private string _name;
			private CodeSpan _nameSpan;
			private int _argIndex;
			private int _argStart;

			public static readonly BracketScope Empty = new BracketScope();

			public BracketScope(string className, string name, CodeSpan nameSpan, int argStart)
			{
				_className = className;
				_name = name;
				_nameSpan = nameSpan;
				_argIndex = 0;
				_argStart = argStart;
			}

			private BracketScope(string className, string name, CodeSpan nameSpan, int argStart, int argIndex)
			{
				_className = className;
				_name = name;
				_nameSpan = nameSpan;
				_argIndex = argIndex;
				_argStart = argStart;
			}

			public BracketScope NextArgument()
			{
				return new BracketScope(_className, _name, _nameSpan, _argStart, _argIndex + 1);
			}

			public string ClassName => _className;
			public string Name => _name;
			public CodeSpan NameSpan => _nameSpan;
			public int ArgumentIndex => _argIndex;
			public int ArgumentsStart => _argStart;
			public bool IsFunctionCall => _name.IsWord();
			public bool IsFunctionCallOrBracket => _name == "(" || _name.IsWord();
			public bool IsEmpty => _name == null;

			public static bool operator ==(BracketScope a, BracketScope b) => a._className == b._className && a._name == b._name && a._argStart == b._argStart;
			public static bool operator !=(BracketScope a, BracketScope b) => a._className != b._className || a._name != b._name || a._argStart != b._argStart;

			public override bool Equals(object obj)
			{
				if (obj == null || obj.GetType() != typeof(BracketScope)) return false;
				var x = (BracketScope)obj;
				return _className == x._className && _name == x._name && _argStart == x._argStart;
			}

			public override int GetHashCode()
			{
				return _className.GetHashCode() * 59 + _name.GetHashCode() * 13 + _argStart;
			}
		}

		public FindResult FindContainingFunctionCall(string sourceCode, int position)
		{
			var code = new CodeParser(sourceCode);
			var scopes = new Stack<BracketScope>();
			var lastFuncName = (string)null;
			var lastClassName = (string)null;
			var saveLastClassName = false;
			BracketScope foundScope = BracketScope.Empty;

			while (code.Read())
			{
				if (code.TokenStartPostion >= position &&
					foundScope.IsEmpty &&
					scopes.Count > 0 &&
					scopes.Peek().IsFunctionCall)
				{
					foundScope = scopes.Peek();
				}

				switch (code.Type)
				{
					case CodeType.Word:
						if (!DK.Constants.GlobalKeywords.Contains(code.Text))
						{
							lastFuncName = code.Text;
						}
						break;

					case CodeType.Operator:
						saveLastClassName = false;
						switch (code.Text)
						{
							// Tokens that open a bracket scope
							case "(":
								if (lastFuncName != null)
								{
									scopes.Push(new BracketScope(lastClassName, lastFuncName, code.Span, code.Span.End));
								}
								else
								{
									scopes.Push(new BracketScope(null, "(", CodeSpan.Empty, 0));
								}
								break;
							case "[":
								scopes.Push(new BracketScope(null, "[", CodeSpan.Empty, 0));
								break;

							// Tokens that close off a bracket scope
							case ")":
								if (scopes.Count > 0)
								{
									var scope = scopes.Peek();
									if (scope == foundScope) return new FindResult(scope, code.TokenStartPostion);
									if (scope.IsFunctionCallOrBracket) scopes.Pop();
								}
								break;
							case "]":
								if (scopes.Count > 0 && scopes.Peek().Name == "]") scopes.Pop();
								break;

							// Tokens that can't be used inside brackets and reset the entire stack when used
							case "{":
							case "}":
							case ";":
								if (!foundScope.IsEmpty) return new FindResult(foundScope, code.TokenStartPostion);
								if (code.Position >= position) return null;
								scopes.Clear();
								break;

							case ",":
								// Comma increments the argument index
								if (scopes.Count > 0)
								{
									scopes.Push(scopes.Pop().NextArgument());
								}
								break;

							case ".":
								// Dot can link class and function name
								if (lastFuncName != null)
								{
									lastClassName = lastFuncName;
									saveLastClassName = true;
								}
								break;
						}
						lastFuncName = null;
						if (!saveLastClassName) lastClassName = null;
						break;

					default:
						lastFuncName = null;
						lastClassName = null;
						break;
				}
			}

			if (!foundScope.IsEmpty) return new FindResult(foundScope, code.Length);
			return null;
		}
	}
}
