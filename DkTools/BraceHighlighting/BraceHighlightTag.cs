using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.BraceHighlighting
{
    internal class BraceHighlightTag : TextMarkerTag
    {
        public BraceHighlightTag()
			: base(VSTheme.CurrentTheme == VSThemeMode.Light ? "DkCodeBraceHighlightDefinition.Light" : "DkCodeBraceHighlightDefinition.Dark")
        {
        }
    }
}
