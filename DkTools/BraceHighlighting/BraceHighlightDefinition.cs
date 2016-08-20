using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.BraceHighlighting
{
    [Export(typeof(EditorFormatDefinition))]
    [Name("DkCodeBraceHighlightDefinition.Light")]
    [UserVisible(true)]
    internal class BraceHighlightDefinitionLight : MarkerFormatDefinition
    {
        public BraceHighlightDefinitionLight()
        {
            this.BackgroundColor = Colors.LightGray;
            this.Border = new Pen(Brushes.DarkGray, 0.5);
            this.DisplayName = "BraceHighlight";
            this.ZOrder = 5;
        }
    }

	[Export(typeof(EditorFormatDefinition))]
	[Name("DkCodeBraceHighlightDefinition.Dark")]
	[UserVisible(true)]
	internal class BraceHighlightDefinitionDark : MarkerFormatDefinition
	{
		public BraceHighlightDefinitionDark()
		{
			this.BackgroundColor = Colors.DarkSlateGray;
			this.Border = new Pen(Brushes.DarkGray, 0.5);
			this.DisplayName = "BraceHighlight";
			this.ZOrder = 5;
		}
	}
}
