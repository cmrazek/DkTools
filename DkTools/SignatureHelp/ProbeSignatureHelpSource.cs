using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
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
		private ITextBuffer _textBuffer;

		public ProbeSignatureHelpSource(ITextBuffer textBuffer)
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

						ITrackingSpan applicableToSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
							new Microsoft.VisualStudio.Text.Span(origPos, 0), SpanTrackingMode.EdgeInclusive, 0);

						var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetOrCreateModelForSnapshot(snapshot);
						foreach (var sig in GetSignatures(model, word, applicableToSpan))
						{
							signatures.Add(sig);
						}
					}
				}
			}
			else if (ProbeSignatureHelpCommandHandler.s_typedChar == ',')
			{
				var model = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer).GetOrCreateModelForSnapshot(snapshot);

				var tokens = model.FindTokens(model.GetPosition(origPos));
				var funcCallToken = tokens.LastOrDefault(t => t.GetType() == typeof(FunctionCallToken));
				if (funcCallToken != null)
				{
					var nameToken = (funcCallToken as FunctionCallToken).NameToken;
					var word = nameToken.Text;

					var bracketPos = nameToken.Span.End.Offset;
					if (origPos >= bracketPos)
					{
						ITrackingSpan applicableToSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
							new Microsoft.VisualStudio.Text.Span(bracketPos, origPos - bracketPos), SpanTrackingMode.EdgeInclusive, 0);

						foreach (var sig in GetSignatures(model, word, applicableToSpan))
						{
							signatures.Add(sig);
						}
					}
				}
			}
		}

		private ProbeSignature CreateSignature(ITextBuffer textBuffer, string methodSig, ITrackingSpan span)
		{
			var sig = new ProbeSignature(textBuffer, methodSig, string.Empty, null);
			textBuffer.Changed += sig.SubjectBufferChanged;

			var paramList = new List<IParameter>();

			var parser = new SimpleTokenParser(methodSig);
			var insideBrackets = false;
			int startPos = -1;
			while (!parser.EndOfFile)
			{
				var token = parser.ParseToken();
				if (token == "(")
				{
					if (!insideBrackets)
					{
						insideBrackets = true;
					}
					else
					{
						while (!parser.EndOfFile && parser.ParseToken() != ")") ;
					}
				}
				else if (token == ")" || token == ",")
				{
					if (startPos != -1)
					{
						var length = parser.TokenStart - startPos;
						var locus = new Microsoft.VisualStudio.Text.Span(startPos, length);
						var text = parser.GetText(startPos, length);
						paramList.Add(new ProbeParameter("", locus, text, sig));
					}

					startPos = -1;
					if (token == ")") break;
				}
				else if (insideBrackets)
				{
					if (startPos == -1) startPos = parser.TokenStart;
				}
			}

			if (startPos != -1)
			{
				var length = parser.Length - startPos;
				var locus = new Microsoft.VisualStudio.Text.Span(startPos, length);
				var text = parser.GetText(startPos, length);
				paramList.Add(new ProbeParameter("", locus, text, sig));
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

		private IEnumerable<ProbeSignature> GetSignatures(CodeModel.CodeModel model, string name, ITrackingSpan applicableToSpan)
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
				if (def is FunctionDefinition) yield return (def as FunctionDefinition).Signature;
				else if (def is MacroDefinition) yield return (def as MacroDefinition).Signature;
			}
		}
	}
}
