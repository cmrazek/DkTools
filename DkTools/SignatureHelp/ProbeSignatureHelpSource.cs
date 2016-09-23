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
				foreach (var sig in HandleOpenBracket(snapshot, origPos))
				{
					signatures.Add(sig);
				}
			}
			else if (ProbeSignatureHelpCommandHandler.s_typedChar == ',')
			{
				foreach (var sig in HandleComma(snapshot, origPos))
				{
					signatures.Add(sig);
				}
			}
		}

		private IEnumerable<ISignature> HandleOpenBracket(VsText.ITextSnapshot snapshot, int triggerPos)
		{
			var lineText = snapshot.GetLineTextUpToPosition(triggerPos);

			var match = _rxFuncBeforeBracket.Match(lineText);
			if (match.Success)
			{
				var tableName = match.Groups[2].Value;
				var funcName = match.Groups[3].Value;

				if (!string.IsNullOrEmpty(funcName))
				{
					var applicableToSpan = snapshot.CreateTrackingSpan(new VsText.Span(triggerPos, 0), VsText.SpanTrackingMode.EdgeInclusive);
					var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
					if (fileStore != null)
					{
						var model = fileStore.GetMostRecentModel(snapshot, "Signature help after '('");
						var modelPos = model.AdjustPosition(triggerPos, snapshot);

						foreach (var sig in GetSignatures(model, modelPos, tableName, funcName, applicableToSpan))
						{
							yield return sig;
						}

						if (!string.IsNullOrEmpty(tableName))
						{
							foreach (var cls in ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp.GetClasses(tableName))
							{
								foreach (var def in cls.GetFunctionDefinitions(funcName))
								{
									yield return CreateSignature(_textBuffer, def.Signature, applicableToSpan);
								}
							}
						}
					}
				}
			}
		}

		private IEnumerable<ISignature> HandleComma(VsText.ITextSnapshot snapshot, int triggerPos)
		{
			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textBuffer);
			if (fileStore != null)
			{
				var model = fileStore.GetMostRecentModel(snapshot, "Signature help after ','");
				var modelPos = model.AdjustPosition(triggerPos, snapshot);

				var argsToken = model.File.FindDownward<ArgsToken>().LastOrDefault();
				if (argsToken != null)
				{
					var sig = argsToken.Signature;
					if (sig != null)
					{
						var modelSpan = new VsText.SnapshotSpan(model.Snapshot, argsToken.Span.Start, modelPos - argsToken.Span.Start + 1);
						var snapshotSpan = modelSpan.TranslateTo(snapshot, VsText.SpanTrackingMode.EdgeInclusive);
						var applicableToSpan = snapshot.CreateTrackingSpan(snapshotSpan.Span, VsText.SpanTrackingMode.EdgeInclusive, 0);

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

						foreach (var method in dataType.GetMethods(funcName).Cast<CodeModel.Definitions.InterfaceMethodDefinition>())
						{
							yield return method.Signature;
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
