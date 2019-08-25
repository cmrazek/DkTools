using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.SignatureHelp
{
	[Export(typeof(IClassifierProvider))]
	[ContentType("DK Signature Help")]
	public class ProbeSignatureHelpClassifierProvider : IClassifierProvider
	{
		[Export]
		[ContentType("DK")]
		[Name("DK Signature Help")]
		[BaseDefinition("sighelp")]
		public static readonly ContentTypeDefinition SignatureHelpContentTypeDefinition;

		IClassifier IClassifierProvider.GetClassifier(ITextBuffer textBuffer)
		{
			return new ProbeSignatureHelpClassifier(textBuffer);
		}
	}
}
