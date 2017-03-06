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
		[Name(ErrorTagger.CodeErrorLight)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class CodeErrorLightDefinition : EditorFormatDefinition
		{
			public CodeErrorLightDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.Red;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeErrorLight;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeErrorLight)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeErrorLight)]
			internal static ErrorTypeDefinition CodeErrorTypeDefinition = null;
		}

		[Export(typeof(EditorFormatDefinition))]
		[Name(ErrorTagger.CodeErrorDark)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class CodeErrorDarkDefinition : EditorFormatDefinition
		{
			public CodeErrorDarkDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.OrangeRed;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeErrorDark;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeErrorDark)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeErrorDark)]
			internal static ErrorTypeDefinition CodeErrorTypeDefinition = null;
		}

		[Export(typeof(EditorFormatDefinition))]
		[Name(ErrorTagger.CodeWarningLight)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class CodeWarningLightDefinition : EditorFormatDefinition
		{
			public CodeWarningLightDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.LimeGreen;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeWarningLight;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeWarningLight)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeWarningLight)]
			internal static ErrorTypeDefinition CodeWarningTypeDefinition = null;
		}

		[Export(typeof(EditorFormatDefinition))]
		[Name(ErrorTagger.CodeWarningDark)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class CodeWarningDarkDefinition : EditorFormatDefinition
		{
			public CodeWarningDarkDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.Chartreuse;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeWarningDark;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeWarningDark)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeWarningDark)]
			internal static ErrorTypeDefinition CodeWarningTypeDefinition = null;
		}

		[Export(typeof(EditorFormatDefinition))]
		[Name(ErrorTagger.CodeAnalysisErrorLight)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class CodeAnalysisErrorLightDefinition : EditorFormatDefinition
		{
			public CodeAnalysisErrorLightDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.Blue;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeAnalysisErrorLight;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeAnalysisErrorLight)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeAnalysisErrorLight)]
			internal static ErrorTypeDefinition CodeAnalysisErrorTypeDefinition = null;
		}

		[Export(typeof(EditorFormatDefinition))]
		[Name(ErrorTagger.CodeAnalysisErrorDark)]
		[Order(After = Priority.High)]
		[UserVisible(true)]
		internal class CodeAnalysisErrorDarkDefinition : EditorFormatDefinition
		{
			public CodeAnalysisErrorDarkDefinition()
			{
				this.ForegroundColor = System.Windows.Media.Colors.SkyBlue;
				this.BackgroundCustomizable = false;
				this.DisplayName = ErrorTagger.CodeAnalysisErrorDark;
			}

			[Export(typeof(ErrorTypeDefinition))]
			[Name(ErrorTagger.CodeAnalysisErrorDark)]
			[Microsoft.VisualStudio.Utilities.DisplayName(ErrorTagger.CodeAnalysisErrorDark)]
			internal static ErrorTypeDefinition CodeAnalysisErrorTypeDefinition = null;
		}
	}
}
