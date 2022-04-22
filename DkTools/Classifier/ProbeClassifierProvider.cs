using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.Classifier
{
	[Export(typeof(IClassifierProvider))]
	[ContentType(Constants.DkContentType)]
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
