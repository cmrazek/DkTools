using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.BraceCompletion
{
    [Export(typeof(IBraceCompletionSessionProvider))]
    [ContentType(Constants.DkContentType)]
    [BracePair('(', ')')]
    [BracePair('{', '}')]
    [BracePair('[', ']')]
    [BracePair('"', '"')]
    [BracePair('\'', '\'')]
    public class DkBraceCompletionSessionProvider : IBraceCompletionSessionProvider
    {
        [Import]
        public ITextBufferUndoManagerProvider UndoProvider = null;

        [Import]
        public IEditorOperationsFactoryService EditorOperations = null;

        //
        // Summary:
        //     If appropriate, creates an Microsoft.VisualStudio.Text.BraceCompletion.IBraceCompletionSession
        //     based on the language context at the openingPoint.
        //
        // Parameters:
        //   textView:
        //     The view containing the openingPoint.
        //
        //   openingPoint:
        //     The insertion point of the openingBrace within the subject buffer. The content
        //     type of the subject buffer must match one of the [ContentType] attributes for
        //     this extension.
        //
        //   openingBrace:
        //     The opening brace that has been typed by the user.
        //
        //   closingBrace:
        //     The closing brace character.
        //
        //   session:
        //     The brace completion session, if it has been created.
        //
        // Returns:
        //     true if the openingPoint was a valid point in the buffer to start a Microsoft.VisualStudio.Text.BraceCompletion.IBraceCompletionSession.
        public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionSession session)
        {
            var undoHistory = UndoProvider.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory;
            var editorOperations = EditorOperations.GetEditorOperations(textView);

            session = new DkBraceCompletionSession(textView, openingPoint, openingBrace, closingBrace, undoHistory, editorOperations);
            return true;
        }
    }
}
