using DK.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;

namespace DkTools.BraceCompletion
{
	public class DkBraceCompletionSession : IBraceCompletionSession
	{
		private ITextView _textView;
		private ITextBuffer _textBuffer;
		private char _openingBrace;
		private char _closingBrace;
		private ITextUndoHistory _undoHistory;
		private IEditorOperations _editorOperations;
		private ITrackingPoint _openingPoint;
		private ITrackingPoint _closingPoint;

		private const string UndoDescription = "DK-Brace-Completion";

		public DkBraceCompletionSession(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace,
			ITextUndoHistory undoHistory, IEditorOperations editorOperations)
		{
			_textView = textView ?? throw new ArgumentNullException(nameof(textView));
			_textBuffer = openingPoint.Snapshot.TextBuffer;
			_openingBrace = openingBrace;
			_closingBrace = closingBrace;
			_undoHistory = undoHistory ?? throw new ArgumentNullException(nameof(undoHistory));
			_editorOperations = editorOperations ?? throw new ArgumentNullException(nameof(editorOperations));

			_closingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingPoint.Position, PointTrackingMode.Positive);
		}

		public char ClosingBrace => _closingBrace;
		public char OpeningBrace => _openingBrace;
		public ITextBuffer SubjectBuffer => _textBuffer;
		public ITextView TextView => _textView;
		public ITrackingPoint OpeningPoint => _openingPoint;
		public ITrackingPoint ClosingPoint => _closingPoint;

		private SnapshotPoint? TryGetCaretPosition() => _textView.Caret.Position.Point.GetPoint(_textBuffer, PositionAffinity.Predecessor);

		public void EndSession()
		{
			_openingPoint = null;
			_closingPoint = null;
		}

		/// <summary>
		/// Called before the session is added to the stack.
		/// </summary>
		public void Start()
		{
			if (_closingPoint == null)
			{
				EndSession();
				return;
			}

			var pos = _textView.Caret.Position.BufferPosition;
			var beforeTrackingPoint = pos.Snapshot.CreateTrackingPoint(pos.Position, PointTrackingMode.Negative);

			var snapshot = _textBuffer.CurrentSnapshot;

			var closingSnapshotPoint = _closingPoint.GetPoint(snapshot);
			if (closingSnapshotPoint.Position < 1)
			{
				Log.Warning("Closing point not found.");
				EndSession();
				return;
			}

			var openingSnapshotPoint = closingSnapshotPoint.Subtract(1);
			_openingPoint = snapshot.CreateTrackingPoint(openingSnapshotPoint.Position, PointTrackingMode.Positive);

			var undo = _undoHistory.CreateTransaction(UndoDescription);

			var edit = _textBuffer.CreateEdit();
			edit.Insert(closingSnapshotPoint.Position, _closingBrace.ToString());
			if (edit.HasFailedChanges)
			{
				Log.Warning("Failed to insert closing brace.");
				edit.Cancel();
				undo.Cancel();
				return;
			}
			var nextSnapshot = edit.Apply();

			var beforePoint = beforeTrackingPoint.GetPoint(_textView.TextSnapshot);

			// switch from positive to negative tracking so it stays against the closing brace
			_closingPoint = _textBuffer.CurrentSnapshot.CreateTrackingPoint(_closingPoint.GetPoint(nextSnapshot).Position, PointTrackingMode.Negative);

			// Move the caret back to between the braces
			_textView.Caret.MoveTo(beforePoint);

			undo.Complete();
		}

		/// <summary>
		/// Called by the editor before the character has been removed.
		/// </summary>
		/// <param name="handledCommand">true to prevent the backspace action from completing, otherwise false.</param>
		public void PreBackspace(out bool handledCommand)
		{
			handledCommand = false;

			var caretPos = TryGetCaretPosition();

			if (caretPos.HasValue &&
				caretPos.Value.Position > 0 &&
				(caretPos.Value.Position - 1) == _openingPoint.GetPoint(_textBuffer.CurrentSnapshot).Position &&
				!HasForwardTyping())
			{
				var undo = _undoHistory.CreateTransaction(UndoDescription);
				var edit = _textBuffer.CreateEdit();

				var span = new SnapshotSpan(_openingPoint.GetPoint(_textBuffer.CurrentSnapshot), _closingPoint.GetPoint(_textBuffer.CurrentSnapshot));
				edit.Delete(span);
				if (edit.HasFailedChanges)
				{
					Log.Warning("Failed to clear braces.");
					edit.Cancel();
					undo.Cancel();
					return;
				}

				handledCommand = true;
				edit.Apply();
				undo.Complete();
				EndSession();
			}
		}

		/// <summary>
		/// Called by the editor after the character has been removed.
		/// </summary>
		public void PostBackspace() { }

		/// <summary>
		/// Called by the editor when the closing brace character has been typed and before it is inserted into the buffer.
		/// </summary>
		/// <param name="handledCommand">true to prevent the closing brace character from being inserted into the buffer, otherwise false.</param>
		public void PreOverType(out bool handledCommand)
		{
			handledCommand = false;

			if (_closingPoint == null) return;

			var snapshot = _textBuffer.CurrentSnapshot;
			var closingSnapshotPoint = _closingPoint.GetPoint(snapshot);
			if (!HasForwardTyping())
			{
				var caretPos = TryGetCaretPosition();
				if (caretPos.HasValue && caretPos.Value.Position > 0 && caretPos.Value.Position < closingSnapshotPoint.Position)
				{
					var undo = _undoHistory.CreateTransaction(UndoDescription);
					_editorOperations.AddBeforeTextBufferChangePrimitive();

					var span = new SnapshotSpan(caretPos.Value, closingSnapshotPoint.Subtract(1));
					var edit = _textBuffer.CreateEdit();
					edit.Delete(span.Span);
					if (edit.HasFailedChanges)
					{
						Log.Warning("Failed to overtype brace.");
						edit.Cancel();
						undo.Cancel();
						return;
					}

					handledCommand = true;
					edit.Apply();
					MoveCaretToClosingPoint();
					_editorOperations.AddAfterTextBufferChangePrimitive();
					undo.Complete();
				}
			}
		}

		/// <summary>
		/// Called by the editor after the closing brace character has been typed.
		/// </summary>
		public void PostOverType() { }

		/// <summary>
		/// Called by the editor when tab has been pressed and before it is inserted into the buffer.
		/// </summary>
		/// <param name="handledCommand">true to prevent the tab from being inserted into the buffer, otherwise false.</param>
		public void PreTab(out bool handledCommand)
		{
			handledCommand = false;

			if (!HasForwardTyping())
			{
				handledCommand = true;

				var undo = _undoHistory.CreateTransaction(UndoDescription);
				_editorOperations.AddBeforeTextBufferChangePrimitive();
				MoveCaretToClosingPoint();
				_editorOperations.AddAfterTextBufferChangePrimitive();
				undo.Complete();
			}
		}

		/// <summary>
		/// Called by the editor after the tab has been inserted.
		/// </summary>
		public void PostTab() { }

		/// <summary>
		/// Called by the editor when return is pressed within the session.
		/// </summary>
		/// <param name="handledCommand">true to prevent the insertion of the newline, otherwise false.</param>
		public void PreReturn(out bool handledCommand)
		{
			handledCommand = false;
		}

		/// <summary>
		/// Called by the editor after the newline has been inserted.
		/// </summary>
		public void PostReturn()
		{
			if (_openingBrace == '{')
			{
				var caretPos = TryGetCaretPosition();
				if (caretPos.HasValue)
				{
					var snapshot = _textBuffer.CurrentSnapshot;
					var closingSnapshotPoint = _closingPoint.GetPoint(snapshot);
					if (closingSnapshotPoint.Position > 0 && this.HasNoForwardTyping(caretPos.Value, closingSnapshotPoint.Subtract(1)))
					{
						var closingLine = closingSnapshotPoint.GetContainingLine();
						var tabSize = _textView.GetTabSize();
						var indent = closingLine.GetText().GetIndentCount(tabSize);
						var midLineStartPos = closingLine.Start.Position;
						var newLineText = _textView.Options.GetOptionValue<string>(DefaultOptions.NewLineCharacterOptionId);

						var undo = _undoHistory.CreateTransaction(UndoDescription);
						var edit = _textBuffer.CreateEdit();
						edit.Insert(closingLine.Start, newLineText);
						if (edit.HasFailedChanges)
						{
							Log.Warning("Failed to auto-indent between new brace lines.");
							edit.Cancel();
							undo.Cancel();
							return;
						}
						var newSnapshot = edit.Apply();
						undo.Complete();

						var midLine = newSnapshot.GetLineFromPosition(midLineStartPos);
						var virtualPt = new VirtualSnapshotPoint(midLine.Start, indent + tabSize);
						_textView.Caret.MoveTo(virtualPt);
					}
				}
			}
		}

		/// <summary>
		/// Called by the editor when delete is pressed within the session.
		/// </summary>
		/// <param name="handledCommand">true to prevent the deletion, otherwise false.</param>
		public void PreDelete(out bool handledCommand)
		{
			handledCommand = false;
		}

		/// <summary>
		/// Called by the editor after the delete action.
		/// </summary>
		public void PostDelete() { }

		/// <summary>
		/// Called after the session has been removed from the stack.
		/// </summary>
		public void Finish() { }

		private bool HasForwardTyping()
		{
			var closingSnapshotPoint = _closingPoint.GetPoint(_textBuffer.CurrentSnapshot);

			if (closingSnapshotPoint.Position > 0)
			{
				var caretPos = TryGetCaretPosition();
				if (caretPos.HasValue && !HasNoForwardTyping(caretPos.Value, closingSnapshotPoint.Subtract(1)))
				{
					return true;
				}
			}

			return false;
		}

		private bool HasNoForwardTyping(SnapshotPoint caretPoint, SnapshotPoint endPoint)
		{
			if (caretPoint.Snapshot != endPoint.Snapshot) throw new InvalidOperationException("Snapshots don't match.");

			if (caretPoint == endPoint) return true;

			if (caretPoint.Position < endPoint.Position)
			{
				var span = new SnapshotSpan(caretPoint, endPoint);
				return string.IsNullOrWhiteSpace(span.GetText());
			}
			else
			{
				return false;
			}
		}

		private void MoveCaretToClosingPoint()
		{
			var closingSnapshotPoint = _closingPoint.GetPoint(_textBuffer.CurrentSnapshot);

			var afterBrace = _textView.BufferGraph.MapUpToBuffer(closingSnapshotPoint, PointTrackingMode.Negative, PositionAffinity.Predecessor, _textView.TextBuffer);
			if (!afterBrace.HasValue)
			{
				Log.Warning("Failed to move caret to closing brace position.");
				return;
			}

			_textView.Caret.MoveTo(afterBrace.Value);
		}


	}
}
