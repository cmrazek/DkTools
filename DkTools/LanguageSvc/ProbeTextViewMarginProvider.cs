using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.LanguageSvc
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [ContentType(Constants.DkContentType)]
    [Name(ProbeTextViewMargin.Name)]
    [TextViewRole(PredefinedTextViewRoles.Structured)]
    [Order(After = PredefinedMarginNames.Top)]
    [MarginContainer(PredefinedMarginNames.Top)]
    internal class ProbeTextViewMarginProvider : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return new ProbeTextViewMargin(wpfTextViewHost.TextView);
        }
    }
}
