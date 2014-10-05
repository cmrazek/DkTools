using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace DkTools.Reformatting
{
	internal sealed class FormatSource : Source
	{
		public FormatSource(LanguageService service, IVsTextLines textLines, Colorizer colorizer)
			: base(service, textLines, colorizer)
		{
			Log.WriteDebug("FormatSource.ctor()");	// TODO: remove
		}

		public override void ReformatSpan(EditArray mgr, TextSpan span)
		{
			Log.WriteDebug("Reformatting lines {0}-{1}", span.iStartLine, span.iEndLine);	// TODO
		}
	}
}
