using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling;
using DkTools.CodeModeling;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace DkTools.SignatureHelp
{
	internal class ProbeSignatureHelpSource : ISignatureHelpSource
	{
		private ITextBuffer _textBuffer;
		private int _triggerPos;

		public ProbeSignatureHelpSource(ITextBuffer textBuffer)
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
				foreach (var sig in HandleOpenBracket(snapshot, DkEnvironment.CurrentAppSettings))
				{
					signatures.Add(sig);
				}
			}
			else if (ProbeSignatureHelpCommandHandler.s_typedChar == ',')
			{
				foreach (var sig in HandleComma(snapshot, DkEnvironment.CurrentAppSettings))
				{
					signatures.Add(sig);
				}
			}
		}

		private Regex _rxFuncBeforeBracket = new Regex(@"((\w+)\s*\.\s*)?(\w+)\s*$");

		private IEnumerable<ISignature> HandleOpenBracket(ITextSnapshot snapshot, DkAppSettings appSettings)
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

				var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_textBuffer);
				if (fileStore != null)
				{
					if (!string.IsNullOrEmpty(word1))
					{
						ITrackingSpan applicableToSpan = null;

						var fileName = VsTextUtil.TryGetDocumentFileName(snapshot.TextBuffer);
						var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "Signature help after '(' - dot separated words");
						var modelPos = model.AdjustPosition(word1Start, snapshot);

						foreach (var word1Def in model.DefinitionProvider.GetAny(modelPos, word1))
						{
							if (!word1Def.AllowsChild) continue;
							foreach (var word2Def in word1Def.GetChildDefinitions(funcName, model.AppSettings))
							{
								if (!word2Def.ArgumentsRequired) continue;
								if (applicableToSpan == null) applicableToSpan = snapshot.CreateTrackingSpan(new Span(_triggerPos, 0), SpanTrackingMode.EdgeInclusive);
								yield return CreateSignature(_textBuffer, word2Def.ArgumentsSignature, applicableToSpan);
							}
						}
					}
					else
					{
						ITrackingSpan applicableToSpan = null;

						var fileName = VsTextUtil.TryGetDocumentFileName(snapshot.TextBuffer);
						var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "Signature help after '('");
						var modelPos = model.AdjustPosition(funcNameStart, snapshot);
						foreach (var def in model.DefinitionProvider.GetAny(modelPos, funcName))
						{
							if (!def.ArgumentsRequired) continue;

							if (applicableToSpan == null) applicableToSpan = snapshot.CreateTrackingSpan(new Span(_triggerPos, 0), SpanTrackingMode.EdgeInclusive);
							yield return CreateSignature(_textBuffer, def.ArgumentsSignature, applicableToSpan);
						}
					}
				}
				
			}
		}

		private IEnumerable<ISignature> HandleComma(ITextSnapshot snapshot, DkAppSettings appSettings)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var fileName = VsTextUtil.TryGetDocumentFileName(_textBuffer);
				var model = fileStore.GetMostRecentModel(appSettings, fileName, snapshot, "Signature help after ','");
				var modelSnapshot = model.Snapshot as ITextSnapshot;
				if (modelSnapshot != null)
				{
					var modelPos = (new SnapshotPoint(snapshot, _triggerPos)).TranslateTo(modelSnapshot, PointTrackingMode.Negative).Position;

					var finder = new FunctionCallFinder();
					var findResult = finder.FindContainingFunctionCall(_textBuffer.CurrentSnapshot.GetText(), _triggerPos);
					if (findResult != null)
					{
						var applicableToSpan = snapshot.CreateTrackingSpan(findResult.ArgumentsSpan.ToVsTextSpan(), SpanTrackingMode.EdgeInclusive);
						foreach (var sig in GetAllSignaturesForFunction(model, modelPos, findResult.ClassName, findResult.FunctionName))
						{
							yield return CreateSignature(_textBuffer, sig, applicableToSpan);
						}
					}
				}
			}
		}

		private ProbeSignature CreateSignature(ITextBuffer textBuffer, FunctionSignature signature, ITrackingSpan span)
		{
			var sig = new ProbeSignature(textBuffer, signature, null);
			textBuffer.Changed += sig.SubjectBufferChanged;

			sig.Parameters = new ReadOnlyCollection<IParameter>((from a in signature.Arguments
																 select new ProbeParameter(string.Empty, a.SignatureSpan.ToVsTextSpan(),
																	 string.IsNullOrEmpty(a.Name) ? string.Empty : a.Name, sig)).Cast<IParameter>().ToArray());

			sig.ApplicableToSpan = span;
			sig.ComputeCurrentParameter(new SnapshotPoint(textBuffer.CurrentSnapshot, _triggerPos));
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

		private IEnumerable<ProbeSignature> GetSignatures(CodeModel model, int modelPos, string className, string funcName, ITrackingSpan applicableToSpan)
		{
			foreach (var sig in GetAllSignaturesForFunction(model, modelPos, className, funcName))
			{
				yield return CreateSignature(_textBuffer, sig, applicableToSpan);
			}
		}

		public static IEnumerable<FunctionSignature> GetAllSignaturesForFunction(CodeModel model, int modelPos, string className, string funcName)
		{
			if (string.IsNullOrEmpty(className))
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<FunctionDefinition>(funcName))
				{
					var funcDef = (def as FunctionDefinition);
					if (string.IsNullOrEmpty(funcDef.ClassName) || funcDef.ClassName.EqualsI(model.ClassName))
					{
						yield return def.Signature;
					}
				}
			}
			else
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<FunctionDefinition>(funcName))
				{
					if ((def as FunctionDefinition).ClassName.EqualsI(className))
					{
						yield return def.Signature;
						yield break;
					}
				}

				foreach (var def1 in model.DefinitionProvider.GetAny(modelPos, className))
				{
					if (def1 is VariableDefinition)
					{
						var varDef = def1 as VariableDefinition;
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
			private CodeSpan _span;

			public ArgumentInfo(string text, string name, CodeSpan span)
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

			public CodeSpan Span
			{
				get { return _span; }
			}
		}
	}
}
