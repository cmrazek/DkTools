using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.Intellisense;
using VsText = Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using DkTools.CodeModel;
using DkTools.CodeModel.Tokens;

namespace DkTools.SignatureHelp
{
	internal class ProbeSignatureHelpSource : ISignatureHelpSource
	{
		private VsText.ITextBuffer _textBuffer;

		private Regex _rxFuncBeforeBracket = new Regex(@"((\w+)\s*\.\s*)?(\w+)\s*$");

		public ProbeSignatureHelpSource(VsText.ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
		}

		public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
		{
			var snapshot = _textBuffer.CurrentSnapshot;
			var origPos = session.GetTriggerPoint(_textBuffer).GetPosition(snapshot);
			//var source = snapshot.GetText();
			var pos = origPos;

			if (ProbeSignatureHelpCommandHandler.s_typedChar == '(')
			{
				var lineText = snapshot.GetLineFromPosition(session.GetTriggerPoint(_textBuffer).GetPosition(snapshot)).GetText();

				var match = _rxFuncBeforeBracket.Match(lineText);
				if (match.Success)
				{
					var tableName = match.Groups[2].Value;
					var funcName = match.Groups[3].Value;
					if (!string.IsNullOrEmpty(funcName))
					{
						if (string.IsNullOrEmpty(tableName))
						{
							var applicableToSpan = snapshot.CreateTrackingSpan(new VsText.Span(origPos, 0), VsText.SpanTrackingMode.EdgeInclusive);
							var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(snapshot, "Signature help after '('");
							foreach (var sig in GetSignatures(model, funcName, applicableToSpan)) signatures.Add(sig);
						}
						else
						{
							var cls = ProbeToolsPackage.Instance.FunctionFileScanner.GetClass(tableName);
							if (cls != null)
							{
								var applicableToSpan = snapshot.CreateTrackingSpan(new VsText.Span(origPos, 0), VsText.SpanTrackingMode.EdgeInclusive);
								var def = cls.GetFunctionDefinition(funcName);
								if (def != null)
								{
									signatures.Add(CreateSignature(_textBuffer, def.Signature, applicableToSpan));
								}
							}
						}
					}
				}
			}
			else if (ProbeSignatureHelpCommandHandler.s_typedChar == ',')
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(snapshot, "Signature help after ','");

				var tokens = model.FindTokens(model.GetPosition(origPos));
				var funcCallToken = tokens.LastOrDefault(t => t.GetType() == typeof(FunctionCallToken));
				if (funcCallToken != null)
				{
					var nameToken = (funcCallToken as FunctionCallToken).NameToken;
					var word = nameToken.Text;

					var bracketPos = nameToken.Span.End.Offset;
					if (origPos >= bracketPos)
					{
						VsText.ITrackingSpan applicableToSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
							new Microsoft.VisualStudio.Text.Span(bracketPos, origPos - bracketPos), VsText.SpanTrackingMode.EdgeInclusive, 0);

						foreach (var sig in GetSignatures(model, word, applicableToSpan))
						{
							signatures.Add(sig);
						}
					}
				}
			}
		}

		private ProbeSignature CreateSignature(VsText.ITextBuffer textBuffer, string methodSig, VsText.ITrackingSpan span)
		{
			var sig = new ProbeSignature(textBuffer, methodSig, string.Empty, null);
			textBuffer.Changed += sig.SubjectBufferChanged;

			var paramList = new List<IParameter>();
			var parser = new TokenParser.Parser(methodSig);
			var insideArgs = false;
			var argStartPos = -1;
			var argName = string.Empty;
			var argEmpty = true;
			var done = false;

			while (!parser.EndOfFile && !done)
			{
				if (!insideArgs)
				{
					if (!parser.Read()) break;
					if (parser.TokenText == "(")
					{
						insideArgs = true;
						argStartPos = parser.Position.Offset;
						argEmpty = true;
						argName = string.Empty;
					}
				}
				else
				{
					if (!parser.ReadNestable()) break;
					switch (parser.TokenType)
					{
						case TokenParser.TokenType.Operator:
							if (parser.TokenText == ",")
							{
								paramList.Add(new ProbeParameter(string.Empty, new VsText.Span(argStartPos, parser.TokenStartPostion.Offset - argStartPos), argName, sig));
								argStartPos = parser.Position.Offset;
								argEmpty = true;
								argName = string.Empty;
							}
							else if (parser.TokenText == ")")
							{
								if (!argEmpty || paramList.Count > 0)
								{
									paramList.Add(new ProbeParameter(string.Empty, new VsText.Span(argStartPos, parser.TokenStartPostion.Offset - argStartPos), argName, sig));
								}
								done = true;
							}
							break;

						case TokenParser.TokenType.Word:
							argName = parser.TokenText;
							argEmpty = false;
							break;

						default:
							argEmpty = false;
							break;
					}
				}
			}

			if (!argEmpty)
			{
				paramList.Add(new ProbeParameter(string.Empty, new VsText.Span(argStartPos, parser.TokenStartPostion.Offset - argStartPos), argName, sig));
			}

			sig.Parameters = new ReadOnlyCollection<IParameter>(paramList);
			sig.ApplicableToSpan = span;
			sig.ComputeCurrentParameter();
			return sig;
		}

		public ISignature GetBestMatch(ISignatureHelpSession session)
		{
			if (session.Signatures.Count > 0)
			{
				// Probe has no function overloading.
				return session.Signatures[0];
			}

			return null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
		}

		private IEnumerable<ProbeSignature> GetSignatures(CodeModel.CodeModel model, string name, VsText.ITrackingSpan applicableToSpan)
		{
			foreach (var sig in GetAllSignaturesForFunction(model, name))
			{
				yield return CreateSignature(_textBuffer, sig, applicableToSpan);
			}
		}

		public static IEnumerable<string> GetAllSignaturesForFunction(CodeModel.CodeModel model, string name)
		{
			foreach (var def in model.GetDefinitions(name))
			{
				if (def is CodeModel.Definitions.FunctionDefinition) yield return (def as CodeModel.Definitions.FunctionDefinition).Signature;
				else if (def is CodeModel.Definitions.MacroDefinition) yield return (def as CodeModel.Definitions.MacroDefinition).Signature;
			}
		}

		public static IEnumerable<string> GetSignatureArguments(string sig)
		{
			var parser = new TokenParser.Parser(sig);
			parser.ReturnWhiteSpace = true;

			var insideArgs = false;
			var sb = new StringBuilder();
			string str;
			var gotComma = false;

			while (!parser.EndOfFile)
			{
				if (!insideArgs)
				{
					if (!parser.Read()) yield break;
					if (parser.TokenType == TokenParser.TokenType.Operator && parser.TokenText == "(") insideArgs = true;
				}
				else
				{
					if (!parser.ReadNestable()) yield break;
					str = parser.TokenText;
					switch (str)
					{
						case ",":
							yield return sb.ToString().Trim();
							sb.Clear();
							gotComma = true;
							break;
						case ")":
							str = sb.ToString().Trim();
							if (gotComma || !string.IsNullOrEmpty(str)) yield return str;
							yield break;
						default:
							sb.Append(str);
							break;
					}
				}
			}

			if (gotComma) yield return sb.ToString().Trim();
		}
	}
}
