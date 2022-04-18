using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.Classifier
{
	[Export(typeof(IClassifierProvider))]
	[ContentType("DK")]
	internal class ProbeClassifierProvider : IClassifierProvider
	{
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null;

		IClassifier IClassifierProvider.GetClassifier(ITextBuffer textBuffer)
		{
			return new ProbeClassifier(ClassificationRegistry, textBuffer);
		}
	}
}
