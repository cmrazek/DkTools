using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
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
		private int _triggerPos;

		public ProbeSignatureHelpSource(VsText.ITextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
		}

		public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var snapshot = _textBuffer.CurrentSnapshot;
			_triggerPos = session.GetTriggerPoint(_textBuffer).GetPosition(snapshot);

			if (ProbeSignatureHelpCommandHandler.s_typedChar == '(')
			{
				foreach (var sig in HandleOpenBracket(snapshot, ProbeEnvironment.CurrentAppSettings))
				{
					signatures.Add(sig);
				}
			}
			else if (ProbeSignatureHelpCommandHandler.s_typedChar == ',')
			{
				foreach (var sig in HandleComma(snapshot, ProbeEnvironment.CurrentAppSettings))
				{
					signatures.Add(sig);
				}
			}
		}

		private Regex _rxFuncBeforeBracket = new Regex(@"((\w+)\s*\.\s*)?(\w+)\s*$");

		private IEnumerable<ISignature> HandleOpenBracket(VsText.ITextSnapshot snapshot, ProbeAppSettings appSettings)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var lineText = snapshot.GetLineTextUpToPosition(_triggerPos);

			var match = _rxFuncBeforeBracket.Match(lineText);
			if (match.Success)
			{
				var line = snapshot.GetLineFromPosition(_triggerPos);
				var word1 = match.Groups[2].Value;
				var word1Start = line.Start.Position + match.Groups[2].Index;
				var funcName = match.Groups[3].Value;
				var funcNameStart = line.Start.Position + match.Groups[3].Index;

				var fileStore = FileStore.GetOrCreateForTextBuffer(_textBuffer);
				if (fileStore != null)
				{
					if (!string.IsNullOrEmpty(word1))
					{
						VsText.ITrackingSpan applicableToSpan = null;

						var fileName = VsTextUtil.TryGetDocumentFileName(snapshot.TextBuffer);
						var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "Signature help after '(' - dot separated words");
						var modelPos = model.AdjustPosition(word1Start, snapshot);

						foreach (var word1Def in model.DefinitionProvider.GetAny(modelPos, word1))
						{
							if (!word1Def.AllowsChild) continue;
							foreach (var word2Def in word1Def.GetChildDefinitions(funcName, model.AppSettings))
							{
								if (!word2Def.ArgumentsRequired) continue;
								if (applicableToSpan == null) applicableToSpan = snapshot.CreateTrackingSpan(new VsText.Span(_triggerPos, 0), VsText.SpanTrackingMode.EdgeInclusive);
								yield return CreateSignature(_textBuffer, word2Def.ArgumentsSignature, applicableToSpan);
							}
						}
					}
					else
					{
						VsText.ITrackingSpan applicableToSpan = null;

						var fileName = VsTextUtil.TryGetDocumentFileName(snapshot.TextBuffer);
						var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "Signature help after '('");
						var modelPos = model.AdjustPosition(funcNameStart, snapshot);
						foreach (var def in model.DefinitionProvider.GetAny(modelPos, funcName))
						{
							if (!def.ArgumentsRequired) continue;

							if (applicableToSpan == null) applicableToSpan = snapshot.CreateTrackingSpan(new VsText.Span(_triggerPos, 0), VsText.SpanTrackingMode.EdgeInclusive);
							yield return CreateSignature(_textBuffer, def.ArgumentsSignature, applicableToSpan);
						}
					}
				}
				
			}
		}

		private IEnumerable<ISignature> HandleComma(VsText.ITextSnapshot snapshot, ProbeAppSettings appSettings)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
				var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "Signature help after ','");
				var modelPos = (new VsText.SnapshotPoint(snapshot, _triggerPos)).TranslateTo(model.Snapshot, VsText.PointTrackingMode.Negative).Position;

				var finder = new FunctionCallFinder();
				var findResult = finder.FindContainingFunctionCall(_textBuffer.CurrentSnapshot.GetText(), _triggerPos);
				if (findResult != null)
				{
					var applicableToSpan = snapshot.CreateTrackingSpan(findResult.ArgumentsSpan.ToVsTextSpan(), VsText.SpanTrackingMode.EdgeInclusive);
					foreach (var sig in GetAllSignaturesForFunction(model, modelPos, findResult.ClassName, findResult.FunctionName))
					{
						yield return CreateSignature(_textBuffer, sig, applicableToSpan);
					}
				}
			}
		}

		private ProbeSignature CreateSignature(VsText.ITextBuffer textBuffer, FunctionSignature signature, VsText.ITrackingSpan span)
		{
			var sig = new ProbeSignature(textBuffer, signature, null);
			textBuffer.Changed += sig.SubjectBufferChanged;

			sig.Parameters = new ReadOnlyCollection<IParameter>((from a in signature.Arguments
																 select new ProbeParameter(string.Empty, a.SignatureSpan.ToVsTextSpan(),
																	 string.IsNullOrEmpty(a.Name) ? string.Empty : a.Name, sig)).Cast<IParameter>().ToArray());

			sig.ApplicableToSpan = span;
			sig.ComputeCurrentParameter(new VsText.SnapshotPoint(textBuffer.CurrentSnapshot, _triggerPos));
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

		private IEnumerable<ProbeSignature> GetSignatures(CodeModel.CodeModel model, int modelPos, string className, string funcName, VsText.ITrackingSpan applicableToSpan)
		{
			foreach (var sig in GetAllSignaturesForFunction(model, modelPos, className, funcName))
			{
				yield return CreateSignature(_textBuffer, sig, applicableToSpan);
			}
		}

		public static IEnumerable<FunctionSignature> GetAllSignaturesForFunction(CodeModel.CodeModel model, int modelPos, string className, string funcName)
		{
			if (string.IsNullOrEmpty(className))
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<CodeModel.Definitions.FunctionDefinition>(funcName))
				{
					var funcDef = (def as CodeModel.Definitions.FunctionDefinition);
					if (string.IsNullOrEmpty(funcDef.ClassName) || funcDef.ClassName == model.ClassName)
					{
						yield return def.Signature;
					}
				}
			}
			else
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<CodeModel.Definitions.FunctionDefinition>(funcName))
				{
					if ((def as CodeModel.Definitions.FunctionDefinition).ClassName == className)
					{
						yield return def.Signature;
						yield break;
					}
				}

				foreach (var def1 in model.DefinitionProvider.GetAny(modelPos, className))
				{
					if (def1 is CodeModel.Definitions.VariableDefinition)
					{
						var varDef = def1 as CodeModel.Definitions.VariableDefinition;
						var dataType = varDef.DataType;

						foreach (var opt in dataType.GetCompletionOptions(model.AppSettings))
						{
							if (opt.ArgumentsRequired) yield return opt.ArgumentsSignature;
						}
					}
				}
			}
		}

		public static IEnumerable<ArgumentInfo> GetSignatureArguments(FunctionSignature sig)
		{
			foreach (var arg in sig.Arguments)
			{
				yield return new ArgumentInfo(arg.SignatureText, string.IsNullOrEmpty(arg.Name) ? string.Empty : arg.Name, arg.SignatureSpan);
			}
		}

		public struct ArgumentInfo
		{
			private string _text;
			private string _name;
			private Span _span;

			public ArgumentInfo(string text, string name, Span span)
			{
				_text = text;
				_name = name;
				_span = span;
			}

			public string Text
			{
				get { return _text; }
			}

			public string Name
			{
				get { return _name; }
			}

			public Span Span
			{
				get { return _span; }
			}
		}
	}
}
