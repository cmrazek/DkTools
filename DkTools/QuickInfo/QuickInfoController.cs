//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel.Composition;
//using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Text.Operations;
//using Microsoft.VisualStudio.Text.Tagging;
//using Microsoft.VisualStudio.Utilities;

//namespace DkTools.QuickInfo
//{
//	internal class QuickInfoController : IIntellisenseController
//	{
//		private ITextView _textView;
//		private IList<ITextBuffer> _subjectBuffers;
//		private QuickInfoControllerProvider _provider;
//		private IQuickInfoSession _session;

//		public QuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, QuickInfoControllerProvider provider)
//		{
//			_textView = textView;
//			_subjectBuffers = subjectBuffers;
//			_provider = provider;

//			_textView.MouseHover += TextView_MouseHover;
//		}

//		private void TextView_MouseHover(object sender, MouseHoverEventArgs e)
//		{
//			var point = _textView.BufferGraph.MapDownToFirstMatch(new SnapshotPoint(_textView.TextSnapshot, e.Position),
//				PointTrackingMode.Positive, snapshot => _subjectBuffers.Contains(snapshot.TextBuffer), PositionAffinity.Predecessor);
//			if (point.HasValue)
//			{
//				var triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);

//				if (!_provider.QuickInfoBroker.IsQuickInfoActive(_textView))
//				{
//					_session = _provider.QuickInfoBroker.TriggerQuickInfo(_textView, triggerPoint, true);
//				}
//			}
//		}

//		void IIntellisenseController.Detach(ITextView textView)
//		{
//			if (_textView == textView)
//			{
//				_textView.MouseHover -= TextView_MouseHover;
//				_textView = null;
//			}
//		}

//		void IIntellisenseController.ConnectSubjectBuffer(ITextBuffer subjectBuffer)
//		{
//		}

//		void IIntellisenseController.DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
//		{
//		}
//	}
//}
