using DK.AppEnvironment;
using DK.Code;
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

        public ProbeSignatureHelpSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ProbeSignatureHelpCommandHandler.s_typedChar == '(')
            {
                foreach (var sig in HandleOpenBracket(session))
                {
                    signatures.Add(sig);
                }
            }
            else if (ProbeSignatureHelpCommandHandler.s_typedChar == ',')
            {
                foreach (var sig in HandleComma(session, ProbeToolsPackage.Instance.App.Settings))
                {
                    signatures.Add(sig);
                }
            }
        }

        private Regex _rxFuncBeforeBracket = new Regex(@"((\w+)\s*\.\s*)?(\w+)\s*$");

        private IEnumerable<ISignature> HandleOpenBracket(ISignatureHelpSession session)
        {
            var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textBuffer);
            var triggerPt = session.GetTriggerPoint(_textBuffer).GetPoint(liveCodeTracker.Snapshot);
            var revCode = liveCodeTracker.CreateReverseCodeParser(triggerPt);

            var item1 = revCode.GetPreviousItem();
            if (item1?.Type == CodeType.Word)
            {
                var itemDot = revCode.GetPreviousItem();
                if (itemDot?.Type == CodeType.Operator && itemDot?.Text == ".")
                {
                    var item2 = revCode.GetPreviousItem();
                    if (item2?.Type == CodeType.Word)
                    {
                        var def = FileStoreHelper.GetDefinitionProviderOrNull(_textBuffer)?.GetGlobalFromAnywhere(item2.Value.Text)
                            .Where(x => x.AllowsChild)
                            .SelectMany(x => x.GetChildDefinitions(item1.Value.Text, ProbeToolsPackage.Instance.App.Settings))
                            .Where(x => x.ArgumentsRequired)
                            .FirstOrDefault();
                        if (def != null)
                        {
                            var applicableToSpan = liveCodeTracker.Snapshot.CreateTrackingSpan(triggerPt.Position, 0, SpanTrackingMode.EdgeInclusive);
                            yield return CreateSignature(_textBuffer, def.ArgumentsSignature, applicableToSpan, triggerPt);
                        }
                    }
                }
                else
                {
                    var def = FileStoreHelper.GetDefinitionProviderOrNull(_textBuffer)?.GetGlobalFromAnywhere(item1.Value.Text)
                        .Where(x => x.ArgumentsRequired)
                        .FirstOrDefault();
                    if (def != null)
                    {
                        var applicableToSpan = liveCodeTracker.Snapshot.CreateTrackingSpan(triggerPt.Position, 0, SpanTrackingMode.EdgeInclusive);
                        yield return CreateSignature(_textBuffer, def.ArgumentsSignature, applicableToSpan, triggerPt);
                    }
                }
            }
        }

        private IEnumerable<ISignature> HandleComma(ISignatureHelpSession session, DkAppSettings appSettings)
        {
            var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textBuffer);
            var snapshot = liveCodeTracker.Snapshot;
            var triggerPt = session.GetTriggerPoint(_textBuffer).GetPoint(snapshot);
            var funcResult = liveCodeTracker.FindContainingFunctionCall(triggerPt, appSettings);

            if (funcResult.Success)
            {
                var applicableToSpan = GetTrackingSpanForArguments(snapshot, funcResult.OpenBracketSpan);
                if (applicableToSpan != null) yield return CreateSignature(_textBuffer, funcResult.Definition.ArgumentsSignature, applicableToSpan, triggerPt);
            }
        }

        private ITrackingSpan GetTrackingSpanForArguments(ITextSnapshot snapshot, CodeSpan openBracketSpan)
        {
            var code = new CodeParser(snapshot.GetText());
            code.Position = openBracketSpan.End;
            while (code.ReadNestable())
            {
                if (code.Type == CodeType.Operator)
                {
                    switch (code.Text)
                    {
                        case ")":
                        case "{":
                        case "}":
                        case "]":
                        case ";":
                            return snapshot.CreateTrackingSpan(openBracketSpan.Start, code.Span.End - openBracketSpan.Start, SpanTrackingMode.EdgeInclusive);
                    }
                }
            }

            return null;
        }


        private ProbeSignature CreateSignature(ITextBuffer textBuffer, FunctionSignature signature, ITrackingSpan span, SnapshotPoint triggerPt)
        {
            var sig = new ProbeSignature(textBuffer, signature, null);

            sig.Parameters = new ReadOnlyCollection<IParameter>((from a in signature.Arguments
                                                                 select new ProbeParameter(string.Empty, a.SignatureSpan.ToVsTextSpan(),
                                                                     string.IsNullOrEmpty(a.Name) ? string.Empty : a.Name, sig)).Cast<IParameter>().ToArray());

            sig.ApplicableToSpan = span;
            sig.ComputeCurrentParameter(triggerPt);
            return sig;
        }

        public ISignature GetBestMatch(ISignatureHelpSession session) => session.Signatures.FirstOrDefault();   // DK has no function overloading

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
        }
    }
}
