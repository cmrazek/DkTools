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
using DkTools.Classifier;

namespace DkTools.SignatureHelp
{
	/// <summary>
	/// Signature object used for Intellisense Signature Help
	/// </summary>
    internal class ProbeSignature : ISignature
    {
        private ITextBuffer _subjectBuffer;
        private IParameter _currentParam;
		private CodeModel.FunctionSignature _sig;
        private ITrackingSpan _applicableToSpan;
        private ReadOnlyCollection<IParameter> _params;

        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

        public ProbeSignature(ITextBuffer subjectBuffer, CodeModel.FunctionSignature sig, ReadOnlyCollection<IParameter> parameters)
        {
            _subjectBuffer = subjectBuffer;
			_sig = sig;
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

			var parser = new CodeParser(source);
			var commaCount = 0;
			while (parser.ReadNestable())
			{
				if (parser.Text == ",") commaCount++;
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
			get { return _sig.PrettySignature; }
		}

        public string Documentation
        {
            get { return _sig.Description != null ? _sig.Description : string.Empty; }
        }

        public ReadOnlyCollection<IParameter> Parameters
        {
            get { return _params; }
            internal set { _params = value; }
        }

        public string PrettyPrintedContent
        {
			get { return _sig.PrettySignature; }
        }

		public ProbeClassifiedString ClassifiedContent
		{
			get { return _sig.ClassifiedString; }
		}
    }
}
