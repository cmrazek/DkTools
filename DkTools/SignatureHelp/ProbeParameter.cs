using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace DkTools.SignatureHelp
{
    internal class ProbeParameter : IParameter
    {
        private string _documentation;
        private Span _locus;
        private string _name;
        private ISignature _signature;
        private Span _prettyPrintedLocus;

        public ProbeParameter(string documentation, Span locus, string name, ISignature signature)
        {
            _documentation = documentation;
            _locus = locus;
            _name = name;
            _signature = signature;
        }

        public Span Locus
        {
            get { return _locus; }
        }

        public string Documentation
        {
            get { return _documentation; }
            internal set { _documentation = value; }
        }

        public string Name
        {
            get { return _name; }
        }

        public Span PrettyPrintedLocus
        {
            get { return _prettyPrintedLocus; }
            internal set { _prettyPrintedLocus = value; }
        }

        public ISignature Signature
        {
            get { return _signature; }
        }
    }
}
