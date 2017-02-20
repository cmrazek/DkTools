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
//	[Export(typeof(IIntellisenseControllerProvider))]
//	[Name("ToolTip QuickInfo Controller")]
//	[ContentType("DK")]
//	internal class QuickInfoControllerProvider : IIntellisenseControllerProvider
//	{
//		[Import]
//		public IQuickInfoBroker QuickInfoBroker { get; set; }

//		IIntellisenseController IIntellisenseControllerProvider.TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
//		{
//			return new QuickInfoController(textView, subjectBuffers, this);
//		}
//	}
//}
