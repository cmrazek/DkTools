using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.ErrorTagging
{
	internal class ErrorClassificationDefinitions
	{
		[Export(typeof(EditorFormatDefinition))]
		[Name(ErrorTagger.CodeError)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class ErrorClassificationFormatDefinition : EditorFormatDefinition
		{
			public ErrorClassificationFormatDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.Red;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeError;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeError)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeError)]
			internal static ErrorTypeDefinition CodeErrorTypeDefinition = null;
		}
	}
}
