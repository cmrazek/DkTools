using DK.Code;
using DK.Modeling;
using DK.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.ObjectModel;

namespace DkTools.SignatureHelp
{
	/// <summary>
	/// Signature object used for Intellisense Signature Help
	/// </summary>
    internal class ProbeSignature : ISignature
    {
        private ITextBuffer _subjectBuffer;
        private IParameter _currentParam;
		private FunctionSignature _sig;
        private ITrackingSpan _applicableToSpan;
        private ReadOnlyCollection<IParameter> _params;

        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

        public ProbeSignature(ITextBuffer subjectBuffer, FunctionSignature sig, ReadOnlyCollection<IParameter> parameters)
        {
            _subjectBuffer = subjectBuffer;
			_sig = sig;
            _params = parameters;
            _subjectBuffer.Changed += SubjectBufferChanged;
        }

        ~ProbeSignature()
        {
            _subjectBuffer.Changed -= SubjectBufferChanged;
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

		private int CalcCurrentParameter(SnapshotPoint triggerPt)
		{
			if (_params.Count == 0)
			{
				_currentParam = null;
				return -1;
			}

			var source = _applicableToSpan.GetText(triggerPt.Snapshot);
			var localTriggerPos = triggerPt.Position - _applicableToSpan.GetStartPoint(triggerPt.Snapshot);

			var parser = new CodeParser(source);
			parser.ReadExact('(');  // Skip past opening bracket
			var commaCount = 0;
			while (parser.Position < localTriggerPos && parser.ReadNestable())
			{
				if (parser.Text == ",") commaCount++;
			}

			return commaCount < _params.Count ? commaCount : _params.Count - 1;
		}

        internal void ComputeCurrentParameter(SnapshotPoint triggerPt)
        {
			var parm = CalcCurrentParameter(triggerPt);
			if (parm < 0)
			{
				CurrentParameter = null;
			}
			else
			{
				CurrentParameter = _params[parm];
			}
        }

        internal void SubjectBufferChanged(object sender, TextContentChangedEventArgs e)
        {
			int pos = -1;
			foreach (var change in e.Changes)
			{
				pos = change.NewEnd;
			}
			if (pos == -1) pos = _applicableToSpan.GetStartPoint(_subjectBuffer.CurrentSnapshot).Position;

			ComputeCurrentParameter(new SnapshotPoint(_subjectBuffer.CurrentSnapshot, pos));
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
