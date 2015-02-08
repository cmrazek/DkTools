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
									yield return CreateSignature(_textBuffer, def.Signature, def.DevDescription, applicableToSpan);
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
				var tokens = model.FindTokens(modelPos).ToArray();

				var lastToken = tokens.LastOrDefault(t => t is FunctionCallToken || t is InterfaceMethodCallToken);
				if (lastToken != null)
				{
					if (lastToken is FunctionCallToken)
					{
						var funcCallToken = lastToken as FunctionCallToken;
						var classToken = funcCallToken.ClassToken;
						var className = classToken != null ? classToken.Text : null;
						var nameToken = funcCallToken.NameToken;
						var funcName = nameToken.Text;

						var bracketPos = nameToken.Span.End;
						if (modelPos >= bracketPos)
						{
							var modelSpan = new VsText.SnapshotSpan(model.Snapshot, bracketPos, modelPos - bracketPos);
							var snapshotSpan = modelSpan.TranslateTo(snapshot, VsText.SpanTrackingMode.EdgeInclusive);
							var applicableToSpan = snapshot.CreateTrackingSpan(snapshotSpan.Span, VsText.SpanTrackingMode.EdgeInclusive, 0);

							foreach (var sig in GetSignatures(model, modelPos, className, funcName, applicableToSpan))
							{
								yield return sig;
							}
						}

						yield break;
					}

					if (lastToken is InterfaceMethodCallToken)
					{
						var methodToken = lastToken as InterfaceMethodCallToken;
						var bracketPos = methodToken.NameToken.Span.End;

						var modelSpan = new VsText.SnapshotSpan(model.Snapshot, bracketPos, modelPos - bracketPos);
						var snapshotSpan = modelSpan.TranslateTo(snapshot, VsText.SpanTrackingMode.EdgeInclusive);
						var applicableToSpan = snapshot.CreateTrackingSpan(snapshotSpan.Span, VsText.SpanTrackingMode.EdgeInclusive, 0);
						
						var methodDef = methodToken.MethodDefinition;
						yield return CreateSignature(_textBuffer, methodDef.Signature, methodDef.DevDescription, applicableToSpan);
						yield break;
					}
				}
			}
		}

		private ProbeSignature CreateSignature(VsText.ITextBuffer textBuffer, string methodSig, string devDesc, VsText.ITrackingSpan span)
		{
			var sig = new ProbeSignature(textBuffer, methodSig, devDesc != null ? devDesc : string.Empty, null);
			textBuffer.Changed += sig.SubjectBufferChanged;

			sig.Parameters = new ReadOnlyCollection<IParameter>((from a in GetSignatureArguments(methodSig) select new ProbeParameter(string.Empty, a.Span.ToVsTextSpan(), a.Name, sig)).Cast<IParameter>().ToList());


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
				yield return CreateSignature(_textBuffer, sig.Signature, sig.DevDescription, applicableToSpan);
			}
		}

		public static IEnumerable<SignatureInfo> GetAllSignaturesForFunction(CodeModel.CodeModel model, int modelPos, string className, string funcName)
		{
			if (string.IsNullOrEmpty(className))
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<CodeModel.Definitions.FunctionDefinition>(funcName))
				{
					var funcDef = (def as CodeModel.Definitions.FunctionDefinition);
					if (string.IsNullOrEmpty(funcDef.ClassName) || funcDef.ClassName == model.ClassName)
					{
						yield return new SignatureInfo(def.Signature, def.DevDescription);
					}
				}
			}
			else
			{
				foreach (var def in model.DefinitionProvider.GetGlobalFromAnywhere<CodeModel.Definitions.FunctionDefinition>(funcName))
				{
					if ((def as CodeModel.Definitions.FunctionDefinition).ClassName == className)
					{
						yield return new SignatureInfo(def.Signature, def.DevDescription);
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
							yield return new SignatureInfo(method.Signature, method.DevDescription);
						}
					}
				}
			}
		}

		public static IEnumerable<ArgumentInfo> GetSignatureArguments(string sig)
		{
			// Find the arguments token
			string argsText = null;
			int argsOffset = 0;
			var argsParser = new TokenParser.Parser(sig);
			while (argsParser.ReadNestable())
			{
				if (argsParser.TokenType == TokenParser.TokenType.Nested)
				{
					argsText = argsParser.TokenText;
					argsOffset = argsParser.TokenStartPostion + 1;
				}
			}

			if (string.IsNullOrEmpty(argsText)) yield break;
			if (argsText.StartsWith("(")) argsText = argsText.Substring(1);
			if (argsText.EndsWith(")")) argsText = argsText.Substring(0, argsText.Length - 1);

			// Parse the arguments
			var parser = new TokenParser.Parser(argsText);
			parser.ReturnWhiteSpace = true;

			var sb = new StringBuilder();
			string str;
			var gotComma = false;
			var argStartPos = 0;
			string argName = null;
			while (!parser.EndOfFile)
			{
				if (!parser.ReadNestable()) yield break;
				str = parser.TokenText;
				switch (str)
				{
					case ",":
						yield return new ArgumentInfo(sb.ToString().Trim(), argName, new Span(argStartPos + argsOffset, parser.TokenStartPostion + argsOffset));
						sb.Clear();
						gotComma = true;
						argStartPos = parser.Position;
						argName = null;
						break;
					//case ")":
					//	str = sb.ToString().Trim();
					//	if (gotComma || !string.IsNullOrEmpty(str)) yield return new ArgumentInfo(str, argName, new Span(argStartPos + argsOffset, parser.TokenStartPostion + argsOffset));
					//	yield break;
					default:
						sb.Append(str);
						if (parser.TokenType == TokenParser.TokenType.Word) argName = parser.TokenText;
						break;
				}
			}

			var lastArg = sb.ToString();
			if (gotComma || !string.IsNullOrWhiteSpace(lastArg)) yield return new ArgumentInfo(lastArg.Trim(), argName, new Span(argStartPos + argsOffset, parser.Position + argsOffset));
		}

		public struct SignatureInfo
		{
			private string _sig;
			private string _devDesc;

			public SignatureInfo(string sig, string devDesc)
			{
				_sig = sig;
				_devDesc = devDesc;
			}

			public string Signature
			{
				get { return _sig; }
			}

			public string DevDescription
			{
				get { return _devDesc; }
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
