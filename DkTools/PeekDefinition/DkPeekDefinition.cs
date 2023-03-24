using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.PeekDefinition
{
    internal static class DkPeekDefinition
    {
        public static void TriggerPeekDefinition(ITextView textView)
        {
            var caretPt = textView.TextSnapshot.CreateTrackingPoint(textView.Caret.Position.BufferPosition.Position, PointTrackingMode.Negative);
            if (textView.Properties.TryGetProperty<IPeekBroker>(typeof(IPeekBroker), out var peekBroker))
            {
                peekBroker.TriggerPeekSession(new PeekSessionCreationOptions(
                    textView: textView,
                    relationshipName: DkPeekRelationship.RelationshipName,
                    triggerPoint: caretPt));
            }
        }
    }
}
