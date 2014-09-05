using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
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
    internal class ProbeSignature : ISignature
    {
        private ITextBuffer _subjectBuffer;
        private IParameter _currentParam;
        private string _content;
        private string _documentation;
        private ITrackingSpan _applicableToSpan;
        private ReadOnlyCollection<IParameter> _params;
        private string _printContent;

        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

        public ProbeSignature(ITextBuffer subjectBuffer, string content, string doc, ReadOnlyCollection<IParameter> parameters)
        {
            _subjectBuffer = subjectBuffer;
            _content = content;
            _documentation = doc;
            _params = parameters;
            _subjectBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(SubjectBufferChanged);
        }

        public IParameter CurrentParameter
        {
            get { return _currentParam; }
            internal set
            {
                if (_currentParam != value)
                {
                    var prevParam = _currentParam;
                    _currentParam = value;

                    var ev = CurrentParameterChanged;
                    if (ev != null) ev(this, new CurrentParameterChangedEventArgs(prevParam, _currentParam));
                }
            }
        }

        internal void ComputeCurrentParameter()
        {
            if (_params.Count == 0)
            {
                _currentParam = null;
                return;
            }

            var source = _applicableToSpan.GetText(_subjectBuffer.CurrentSnapshot);

			var parser = new TokenParser.Parser(source);
			var commaCount = 0;
			while (parser.ReadNestable())
			{
				if (parser.TokenText == ",") commaCount++;
			}

            CurrentParameter = _params[commaCount < _params.Count ? commaCount : _params.Count - 1];
        }

        internal void SubjectBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ComputeCurrentParameter();
        }

        public ITrackingSpan ApplicableToSpan
        {
            get { return _applicableToSpan; }
            internal set { _applicableToSpan = value; }
        }

        public string Content
        {
            get { return _content; }
            internal set { _content = value; }
        }

        public string Documentation
        {
            get { return _documentation; }
            internal set { _documentation = value; }
        }

        public ReadOnlyCollection<IParameter> Parameters
        {
            get { return _params; }
            internal set { _params = value; }
        }

        public string PrettyPrintedContent
        {
            get { return _printContent; }
            internal set { _printContent = value; }
        }
    }
}
