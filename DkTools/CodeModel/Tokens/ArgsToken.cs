using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	class ArgsToken : GroupToken
	{
		private FunctionSignature _sig;
		private FunctionSignature[] _sigAlternatives;
		private bool _terminated;

		private ArgsToken(Scope scope, OperatorToken openBracketToken)
			: base(scope)
		{
			AddToken(openBracketToken);
		}

		private static string[] _endTokens = new string[] { ",", ")" };

		/// <summary>
		/// Parses a set of brackets containing arguments.
		/// </summary>
		/// <param name="scope">The scope for this token.</param>
		/// <param name="openBracketToken">The open bracket token.</param>
		/// <param name="sig">(optional) A signature for the function being called.</param>
		/// <returns>A new argument token.</returns>
		/// <remarks>This function assumes the opening bracket has already been read from the stream.</remarks>
		public static ArgsToken Parse(Scope scope, OperatorToken openBracketToken, FunctionSignature sig)
		{
			var code = scope.Code;
			var ret = new ArgsToken(scope, openBracketToken);
			var argIndex = 0;

			scope = scope.Clone();
			scope.Hint |= ScopeHint.SuppressStatementStarts;

			ret._sig = sig;
			var args = sig != null ? sig.Arguments.ToArray() : ArgumentDescriptor.EmptyArray;

			while (code.SkipWhiteSpace())
			{
				code.Peek();
				if (code.Text == ")")
				{
					ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), ")"));
					ret._terminated = true;
					return ret;
				}

				if (code.Text == ",")
				{
					ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), ","));
					argIndex++;
					continue;
				}


				var dataType = argIndex < args.Length ? args[argIndex].DataType : null;
				var exp = ExpressionToken.TryParse(scope, _endTokens, expectedDataType: dataType);
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			return ret;
		}

		public static ArgsToken ParseAndChooseArguments(Scope scope, OperatorToken openBracketToken, Definition[] sigDefs, out Definition selectedDef)
		{
#if DEBUG
			if (sigDefs == null) throw new ArgumentNullException("sigDefs");
			if (sigDefs.Length == 0) throw new ArgumentException("sigDefs must contain at least one signature definition.");
#endif

			if (sigDefs.Length == 1)
			{
				selectedDef = sigDefs[0];
				return Parse(scope, openBracketToken, sigDefs[0].ArgumentsSignature);
			}

			var code = scope.Code;
			var ret = new ArgsToken(scope, openBracketToken);

			scope = scope.Clone();
			scope.Hint |= ScopeHint.SuppressStatementStarts;

			var tokens = new List<Token>();
			var dataTypes = new List<DataType>();
			List<Token> bestTokens = null;
			var bestConfidence = 0.0f;
			Definition bestSigDef = null;
			var codeResetPos = code.Position;

			foreach (var sigDef in sigDefs)
			{
				var args = sigDef.Arguments.ToArray();
				var argIndex = 0;
				var expectingDataType = true;

				tokens.Clear();
				dataTypes.Clear();
				code.Position = codeResetPos;

				while (code.SkipWhiteSpace())
				{
					code.Peek();
					if (code.Text == ")")
					{
						tokens.Add(new OperatorToken(scope, code.MovePeekedSpan(), ")"));
						ret._terminated = true;
						break;
					}
					else if (code.Text == ",")
					{
						tokens.Add(new OperatorToken(scope, code.MovePeekedSpan(), ","));
						argIndex++;
						expectingDataType = true;
						continue;
					}

					var exp = ExpressionToken.TryParse(scope, _endTokens,
						expectedDataType: argIndex < args.Length ? args[argIndex].DataType : null);
					if (exp != null)
					{
						tokens.Add(exp);
						if (expectingDataType) dataTypes.Add(exp.ValueDataType);
						expectingDataType = false;
					}
					else break;
				}

				if (dataTypes.Count == args.Length)
				{
					var confidence = 1.0f;
					for (int i = 0, ii = args.Length; i < ii; i++)
					{
						confidence *= DataType.CalcArgumentCompatibility(args[i].DataType, dataTypes[i]);
					}

					if (confidence > bestConfidence)
					{
						bestConfidence = confidence;
						bestTokens = tokens;
						bestSigDef = sigDef;
					}
				}
				else if (bestTokens == null)
				{
					bestTokens = tokens;
					bestConfidence = 0.0f;
					bestSigDef = sigDef;
				}
			}

			ret.AddTokens(bestTokens);
			ret._sig = bestSigDef.ArgumentsSignature;
			ret._sigAlternatives = (from s in sigDefs where s.ArgumentsSignature != ret._sig select s.ArgumentsSignature).ToArray();
			selectedDef = bestSigDef;
			return ret;
		}

		public FunctionSignature Signature
		{
			get { return _sig; }
		}

		public IEnumerable<FunctionSignature> SignatureAlternatives
		{
			get
			{
				if (_sigAlternatives != null) return _sigAlternatives;
				return FunctionSignature.EmptyArray;
			}
		}

		public bool IsTerminated
		{
			get { return _terminated; }
		}
	}
}
