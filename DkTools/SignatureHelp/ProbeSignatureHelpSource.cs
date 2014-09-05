using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace DkTools.SignatureHelp
{
	internal class ProbeSignatureHelpSource : ISignatureHelpSource
	{
		private VsText.ITextBuffer _textBuffer;

		public ProbeSignatureHelpSource(VsText.ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
		}

		public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
		{
			var snapshot = _textBuffer.CurrentSnapshot;
			var origPos = session.GetTriggerPoint(_textBuffer).GetPosition(snapshot);
			var source = snapshot.GetText();
			var pos = origPos;

			if (ProbeSignatureHelpCommandHandler.s_typedChar == '(')
			{
				// Back up to before any whitespace before the '('
				while (pos > 0 && (pos >= source.Length || char.IsWhiteSpace(source[pos]) || source[pos] == '(')) pos--;

				if (pos >= 0 && pos < source.Length && source[pos].IsWordChar(false))
				{
					int startPos, length;
					if (source.GetWordExtent(pos, out startPos, out length))
					{
						var word = source.Substring(startPos, length);

						VsText.ITrackingSpan applicableToSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
							new Microsoft.VisualStudio.Text.Span(origPos, 0), VsText.SpanTrackingMode.EdgeInclusive, 0);

						var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetCurrentModel(snapshot, "Signature help after '('");
						foreach (var sig in GetSignatures(model, word, applicableToSpan))
						{
							signatures.Add(sig);
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
	}
}
